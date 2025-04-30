using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;


public class StartGameManager : MonoBehaviour
{
    public static StartGameManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    private void Start() 
    {
        Debug.Log("StartGameManager subscribing to OnLobbyStartGame");
        LobbyManager.Instance.OnLobbyStartGame += LobbyManager_OnLobbyStartGame;
    }

    private async void LobbyManager_OnLobbyStartGame(object sender, LobbyManager.LobbyEventArgs e)
    {
        //StartGame!
        if (LobbyManager.IsHost)
        {
            string joinCode = await CreateRelay();

            LobbyManager.Instance.SetRelayJoinCode(joinCode);
        }
        else
        {
            JoinRelay(LobbyManager.RelayJoinCode);
        }
    }


    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }


    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }



    public async Task<string> CreateRelay()
    {
        try
        {
         
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log("Allocated Relay JoinCode: " + joinCode);

            

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);


            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartHost();
            else
                Debug.LogWarning("StartHost() skipped because a host is already running.");

            return joinCode;

        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }


    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with " + joinCode); 

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

           
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);


            if (!NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.StartClient();
            else
                Debug.LogWarning("StartClient() skipped because a client is already running.");
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

   
}

