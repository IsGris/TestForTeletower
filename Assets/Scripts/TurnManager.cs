using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class TurnManager : NetworkBehaviour
{
    // int - team id who makes turn now
    public event Action<int> OnTurnStart;

    [SerializeField] private TMP_Text _turnText;
    [SerializeField] private TMP_Text _turnTimerText;
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _moveText;
    [SerializeField] private TMP_Text _turnOwnerText;
    [SerializeField] private GameManager _gameManager;

    private int _currentTurnAttacksLeft = _attacksPerTurn;
    private int _currentTurnMovesLeft = _movesPerTurn;
    private int _currentTurn = 1;
    private int _currentTurnTeamId => (_currentTurn - 1) % _teamsAmount;
    private bool _skipCurrentTurn = false;

    private Coroutine _gameLoopCoroutine;
    private Coroutine _turnTimerCoroutine;
    private const int _teamsAmount = 2;
    private const float _turnsBeforeTieBreaker = 15;
    private const float _timePerTurn = 60;
    private const int _attacksPerTurn = 1;
    private const int _movesPerTurn = 1;

    public void StartGame()
    {
        if (_gameLoopCoroutine != null) return; // Already started
        _gameLoopCoroutine = StartCoroutine(GameLoopCoroutine());
    }

    private IEnumerator GameLoopCoroutine()
    {
        while (_currentTurn != _turnsBeforeTieBreaker)
        {
            ChangeTurn(_currentTurn);
            float turnStartTime = Time.time;
            while (!_skipCurrentTurn && 
                    turnStartTime + _timePerTurn > Time.time) 
            { 
                yield return null;
            }
            _skipCurrentTurn = false;
            _currentTurn += 1;
        }
        // Get team with most amount of units
        Dictionary<int, int> teamCounts = GetTeamUnitCounts();
        int maxTeamId = -1;
        int maxUnitCount = -1;
        bool isTie = false;

        foreach (var team in teamCounts)
        {
            if (team.Value > maxUnitCount)
            {
                maxUnitCount = team.Value;
                maxTeamId = team.Key;
                isTie = false;
            }
            else if (team.Value == maxUnitCount)
                isTie = true;
        }

        if (!isTie)
        {
            _gameManager.EndGame(maxTeamId);
            yield break;
        }

        // Continue game because it is tie
        MakeEveryoneInfiniteSpeed();
        MakeEveryoneInfiniteSpeedClientRpc();

        while (true)
        {
            ChangeTurn(_currentTurn);
            yield return new WaitForSeconds(_timePerTurn);
            _currentTurn += 1;
        }
    }

    [ClientRpc]
    private void MakeEveryoneInfiniteSpeedClientRpc()
    {
        if (IsHost) return; // Already did it on server
        MakeEveryoneInfiniteSpeed();
    }

    private void MakeEveryoneInfiniteSpeed()
    {
        var allTeamsUnits = GetAllTeamsUnits();
        foreach (var teamUnits in allTeamsUnits)
        {
            foreach (var unit in teamUnits.Value)
            {
                unit.UnitSettings.Speed = 1000000;
            }
        }
    }

    private Dictionary<int, int> GetTeamUnitCounts()
    {
        var teamsUnits = GetAllTeamsUnits();

        Dictionary<int, int> teamUnitCounts = new();

        foreach (var team in teamsUnits)
            teamUnitCounts[team.Key] = team.Value.Count;

        return teamUnitCounts;
    }


    private Dictionary<int, List<UnitController>> GetAllTeamsUnits()
    {
        Dictionary<int, List<UnitController>> result = new();
        var allControllers = FindObjectsOfType<UnitController>();
        foreach (var controller in allControllers)
        {
            if (!result.ContainsKey(((int)controller.UnitSettings.TeamId)))
                result[((int)controller.UnitSettings.TeamId)] = new();
            result[((int)controller.UnitSettings.TeamId)].Add(controller);
        }
        return result;
    }

    public bool TryUseAttack(int teamId)
    {
        Assert.IsTrue(IsServer);
        if (!CanAttack(teamId)) return false;
        _currentTurnAttacksLeft--;
        ChangeAttacksLeftTextClientRpc(_currentTurnAttacksLeft);
        if (_currentTurnAttacksLeft == 0 &&
            _currentTurnMovesLeft == 0) _skipCurrentTurn = true;
        return true;
    }

    [ClientRpc]
    private void ChangeAttacksLeftTextClientRpc(int newAmount)
    {
        _attackText.text = newAmount.ToString();
    }

    public bool CanMove(int teamId)
    {
        if (_currentTurnTeamId != teamId) return false;
        if (_currentTurnMovesLeft <= 0) return false;
        return true;
    }
    public bool CanAttack(int teamId)
    {
        if (_currentTurnTeamId != teamId) return false;
        if (_currentTurnAttacksLeft <= 0) return false;
        return true;
    }

    public bool TryUseMove(int teamId)
    {
        Assert.IsTrue(IsServer);
        if (!CanMove(teamId)) return false;
        _currentTurnMovesLeft--;
        ChangeMovesLeftTextClientRpc(_currentTurnMovesLeft);
        if (_currentTurnAttacksLeft == 0 &&
            _currentTurnMovesLeft == 0) _skipCurrentTurn = true;
        return true;
    }

    [ClientRpc]
    private void ChangeMovesLeftTextClientRpc(int newAmount)
    {
        _moveText.text = newAmount.ToString();
    }

    private void ChangeTurn(int turn)
    {
        // Check for game end
        Dictionary<int, int> teamCounts = GetTeamUnitCounts();
        var teamsWithUnits = teamCounts.Where(team => team.Value > 0).ToList();
        if (teamsWithUnits.Count == 1)
        {
            if (_gameLoopCoroutine != null)
                StopCoroutine(_gameLoopCoroutine);
            if (_turnTimerCoroutine != null)
                StopCoroutine(_turnTimerCoroutine);
            _gameManager.EndGame(teamCounts.Keys.ElementAt(0));
        }
        // Make new turn
        _turnText.text = turn.ToString();
        _currentTurnAttacksLeft = _attacksPerTurn;
        _currentTurnMovesLeft = _movesPerTurn;
        _attackText.text = _currentTurnAttacksLeft.ToString();
        _moveText.text = _currentTurnMovesLeft.ToString();
        if (NetworkUtils.GetMyTeamId() == _currentTurnTeamId)
            _turnOwnerText.text = "YOUR TURN";
        else
            _turnOwnerText.text = "ENEMY TURN";
        OnTurnStart?.Invoke(GetTeamIdFromTurn(turn));
        ChangeTurnClientRpc(turn);
        RestartTurnTimerClientRpc(_timePerTurn);
    }   

    [ClientRpc]
    private void ChangeTurnClientRpc(int turn)
    {
        if (IsHost) return; // already did this on server

        _turnText.text = turn.ToString();
        _currentTurnAttacksLeft = _attacksPerTurn;
        _currentTurnMovesLeft = _movesPerTurn;
        _attackText.text = _currentTurnAttacksLeft.ToString();
        _moveText.text = _currentTurnMovesLeft.ToString();
        if (NetworkUtils.GetMyTeamId() == GetTeamIdFromTurn(turn))
            _turnOwnerText.text = "YOUR TURN";
        else
            _turnOwnerText.text = "ENEMY TURN";
        OnTurnStart?.Invoke(GetTeamIdFromTurn(turn));
    }

    [ClientRpc]
    private void RestartTurnTimerClientRpc(float time)
    {
        IEnumerator TurnTimerCoroutine()
        {
            string FormatTime(float timeToFormat)
            {
                int minutes = Mathf.FloorToInt(timeToFormat / 60);
                int seconds = Mathf.FloorToInt(timeToFormat % 60);
                return string.Format("{0:D2}:{1:D2}", minutes, seconds);
            }
            while (time >= 0)
            {
                _turnTimerText.text = FormatTime(time);
                yield return null;
                time -= Time.deltaTime;
            }
            _turnTimerText.text = FormatTime(0);
        }

        if (_turnTimerCoroutine != null)
        {
            StopCoroutine(_turnTimerCoroutine);
            _turnTimerCoroutine = null;
        }
        _turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    private int GetTeamIdFromTurn(int turn) => (turn - 1) % _teamsAmount;
}
