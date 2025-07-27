using System;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] NetworkManager _networkManager;
    [SerializeField] GameObject _mainMenuUI;
    [SerializeField] GameObject _gameUI;
    [SerializeField] LevelGenerator _levelGenerator;
    [SerializeField] LevelSpawnData _levelSpawnData;
    [SerializeField] TurnManager _turnManager;

    [SerializeField] private GameObject _winGameScreen;
    [SerializeField] private GameObject _looseGameScreen;

    void Start()
    {
        _networkManager.OnClientConnectedCallback += GameManager_OnClientConnectedCallback;
    }
    
    public void EndGame(int teamIdWinner)
    {
        var allPlayerControllers = FindObjectsOfType<PlayerController>();
        foreach (var controller in allPlayerControllers)
        {
            if (controller.TeamId == teamIdWinner)
                ShowWinGameScreenClientRpc(controller.gameObject);
            else
                ShowLooseGameScreenClientRpc(controller.gameObject);
        }
    }


    [ClientRpc]
    public void ShowWinGameScreenClientRpc(NetworkObjectReference playerNetObjReference)
    {
        if (!playerNetObjReference.TryGet(out NetworkObject netPlayerObj)) return;
        var playerController = netPlayerObj.gameObject.GetComponent<PlayerController>();
        if (playerController == null || 
            !playerController.IsOwner) return;
        
        _winGameScreen.SetActive(true);
    }
    
    [ClientRpc]
    public void ShowLooseGameScreenClientRpc(NetworkObjectReference playerNetObjReference)
    {
        if (!playerNetObjReference.TryGet(out NetworkObject netPlayerObj)) return;
        var playerController = netPlayerObj.gameObject.GetComponent<PlayerController>();
        if (playerController == null ||
            !playerController.IsOwner) return;

        _looseGameScreen.SetActive(true);
    }

    private void GameManager_OnClientConnectedCallback(ulong obj)
    {
        if (!IsServer) return;
        _networkManager.ConnectedClients[obj].PlayerObject.GetComponent<PlayerController>()
            .Init((uint)(_networkManager.ConnectedClients.Count - 1));
        if (_networkManager.ConnectedClients.Count == 2)
            StartGame();
    }

    private void StartGame()
    {
        _mainMenuUI.SetActive(false);
        _gameUI.SetActive(true);

        // seed for random generation based on current time
        int levelSeed = (int)(DateTime.Now.Ticks & 0xFFFFFFFF);
        
        _levelGenerator.GenerateLevel(_levelSpawnData, levelSeed);
        StartGameClientRpc(levelSeed);
        _turnManager.StartGame();
    }

    [ClientRpc]
    private void StartGameClientRpc(int seed)
    {
        if (IsHost) return; // Already did it because it is server
        _mainMenuUI.SetActive(false);
        _gameUI.SetActive(true);
        _levelGenerator.GenerateLevel(_levelSpawnData, seed);
    }
}
