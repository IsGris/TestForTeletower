using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] LobbyManager _lobbyManager;

    [SerializeField] Button _hostGameBTN;
    [SerializeField] TMP_Text _hostGameBTNText;

    [SerializeField] GameObject _hostGameCodeBTN;
    [SerializeField] TMP_Text _hostGameCodeText;

    [SerializeField] Button _joinGameBTN;
    [SerializeField] TMP_Text _joinGameBTNText;

    [SerializeField] TMP_InputField _joinGameCodeInput;

    public void HostGame() => HostGameAsync();

    public async Task HostGameAsync()
    {
        DisableDefaultButtons();
        try
        {
            await _lobbyManager.CreateRoomAsync(
                name: $"{UnityEngine.Random.Range(10000000, 99999999)}",
                password: null,
                isPrivate: true,
                maxPlayers: 2
                );
            _hostGameCodeText.text = _lobbyManager.CurrentRoomJoinCode;
            _hostGameCodeBTN.SetActive(true);
        } catch
        {
            _hostGameBTNText.text = "Failed";
            EnableDefaultButtons();
            throw;
        }
    }

    public void JoinGame() => JoinGameAsync();

    public async Task JoinGameAsync()
    {
        DisableDefaultButtons();
        try
        {
            await _lobbyManager.JoinRoomByInviteAsync(_joinGameCodeInput.text.Trim());
            _hostGameCodeText.text = _lobbyManager.CurrentRoomJoinCode;
            _hostGameCodeBTN.SetActive(true);
        }
        catch
        {
            _joinGameBTNText.text = "Failed";
            EnableDefaultButtons();
            throw;
        }
    }

    public void CopyRoomCode()
    {
        GUIUtility.systemCopyBuffer = _lobbyManager.CurrentRoomJoinCode;
        _hostGameCodeText.text = "Copied";
    }

    private void DisableDefaultButtons()
    {
        _hostGameBTN.interactable = false;
        _joinGameBTN.interactable = false;
    }

    private void EnableDefaultButtons()
    {
        _hostGameBTN.interactable = true;
        _joinGameBTN.interactable = true;
    }
}
