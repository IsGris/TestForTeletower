using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using static NetworkUtils;

public class LobbyManager : MonoBehaviour
{
    public string CurrentRelayJoinCode { get; protected set; } = null;
    public string CurrentRoomJoinCode { get; protected set; } = null;

    const int MaxPlayersPerRoom = 2;
    const string ConnectionType = "udp";

    public async Task<string> CreateRoomAsync(string name, string password = null, bool isPrivate = false, int maxPlayers = MaxPlayersPerRoom)
    {
        if (maxPlayers > MaxPlayersPerRoom)
            throw new ArgumentOutOfRangeException(
                $"Given {nameof(maxPlayers)}({maxPlayers}) argument is bigger than {nameof(MaxPlayersPerRoom)}({MaxPlayersPerRoom}) variable");

        await EnsureUnityServicesInitialized();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayersPerRoom - 1);
        var relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        CurrentRelayJoinCode = relayJoinCode;

        Debug.Log("Created relay allocation with join code: " + relayJoinCode);

        CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions();
        createLobbyOptions.IsPrivate = isPrivate;
        if (password != null)
            createLobbyOptions.Password = password;
        createLobbyOptions.Data =
            new Dictionary<string, DataObject>
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            };
        var currentLobby = await LobbyService.Instance.CreateLobbyAsync(name, MaxPlayersPerRoom, createLobbyOptions);

        Debug.Log($"Created lobby with join code: {currentLobby.LobbyCode}");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, ConnectionType));
        if (!NetworkManager.Singleton.StartHost())
            throw new InvalidOperationException("Failed to start host, check logs for more info");
        
        CurrentRoomJoinCode = currentLobby.LobbyCode;

        Debug.Log($"Started host");

        return currentLobby.LobbyCode;
    }

    public async Task JoinRoomByInviteAsync(string lobbyJoinCode, string password = null)
    {
        await EnsureUnityServicesInitialized();

        Lobby lobby;
        JoinLobbyByCodeOptions lobbyOptions = new();
        if (password != null)
            lobbyOptions.Password = password;
        lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyJoinCode.Trim().ToUpper(), lobbyOptions);
        Debug.Log($"Joined lobby with code: {lobbyJoinCode}");

        CurrentRelayJoinCode = lobby.Data.GetValueOrDefault("RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "abs")).Value;

        var relayServerData = await RelayService.Instance.JoinAllocationAsync(CurrentRelayJoinCode);
        Debug.Log("Joined relay server");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(relayServerData, ConnectionType));
        if (!NetworkManager.Singleton.StartClient())
            throw new InvalidOperationException("Failed to start client, check logs for more info");

        CurrentRoomJoinCode = lobbyJoinCode;

        Debug.Log("Started client");
    }

    public async Task JoinRandomLobby()
    {
        await EnsureUnityServicesInitialized();

        Lobby lobby;
        lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
        Debug.Log($"Joined random public lobby");

        CurrentRelayJoinCode = lobby.Data.GetValueOrDefault("RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "abs")).Value;

        var relayServerData = await RelayService.Instance.JoinAllocationAsync(CurrentRelayJoinCode);
        Debug.Log("Joined relay server");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(relayServerData, ConnectionType));
        if (!NetworkManager.Singleton.StartClient())
            throw new InvalidOperationException("Failed to start client, check logs for more info");

        CurrentRoomJoinCode = lobby.LobbyCode;

        Debug.Log("Started client");
    }

    public async Task LeaveRoomAsync()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Stopping network...");
            NetworkManager.Singleton.Shutdown();
        }

        if (!string.IsNullOrEmpty(CurrentRoomJoinCode))
        {
            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(CurrentRoomJoinCode);

                if (lobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
                    Debug.Log("Deleted lobby as host");
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
                    Debug.Log("Left lobby as client");
                }
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogWarning($"Error while leaving lobby: {ex.Message}");
            }

            CurrentRoomJoinCode = null;
            CurrentRelayJoinCode = null;
        }
        else
        {
            Debug.Log("No active lobby to leave.");
        }
    }
}
