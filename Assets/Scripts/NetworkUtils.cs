using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Collections;

public enum NetworkState
{
    NotConnected,
    Client,
    Host,
    Server
}

public static class NetworkUtils
{
    // Checks does NetworkManager started Host || Server || Client
    public static bool IsRunningNetwork() =>
        (NetworkManager.Singleton?.IsClient == true || NetworkManager.Singleton?.IsServer == true || NetworkManager.Singleton?.IsHost == true) &&
        NetworkManager.Singleton?.ShutdownInProgress == false;

    // Get current network state(Client, Host...)
    public static NetworkState GetCurrentNetworkState()
    {
        if (NetworkManager.Singleton?.ShutdownInProgress == true) return NetworkState.NotConnected;
        if (NetworkManager.Singleton?.IsHost == true) return NetworkState.Host;
        if (NetworkManager.Singleton?.IsServer == true) return NetworkState.Server;
        if (NetworkManager.Singleton?.IsClient == true) return NetworkState.Client;
        return NetworkState.NotConnected;
    }

    // Ensures that UnityServices is initialized and player signed in anonymously
    public static async Task EnsureUnityServicesInitialized()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            return;

        if (UnityServices.State == ServicesInitializationState.Initializing)
        {
            do { await Task.Yield(); } // waiting initialization
            while (UnityServices.State != ServicesInitializationState.Initialized);
            return;
        }

        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("UnityServices initialized successfully");
    }

    // Gets GameObject by its name
    public static GameObject GetGameObject(this FixedString128Bytes unitObjectName) =>
        GameObject.Find(unitObjectName.ToString());

    public static PlayerController GetMyPlayerController()
    {
        PlayerController[] allPlayerControllers = GameObject.FindObjectsOfType<PlayerController>();

        foreach (PlayerController controller in allPlayerControllers)
        {
            if (controller.IsOwner)
            {
                return controller;
            }
        }
        return null;
    }

    public static uint GetMyTeamId() =>
        GetMyPlayerController().TeamId;
}
