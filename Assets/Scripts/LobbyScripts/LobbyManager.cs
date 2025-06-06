using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;

public class LobbyManager : MonoBehaviour {


    public static LobbyManager Instance { get; private set; }


    public static bool IsHost { get; private set; }
    public static string RelayJoinCode { get; private set; }



    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_START_GAME = "StartGame";
    public const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";


   



    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public event EventHandler<LobbyEventArgs> OnLobbyStartGame;
    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }


    public enum GameMode {
        CaptureTheFlag,
        Conquest
    }

    public enum PlayerCharacter {
        Marine,
        Ninja,
        Zombie
    }



    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName;
    private bool alreadyStartedGame;


    private void Awake() {
        Instance = this;
    }

    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) {
        playerName = playerName.Replace(" ", "_");
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {
            // do nothing
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void HandleRefreshLobbyList() {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn) {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f) {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();
            }
        }
    }

    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling() {
        if (joinedLobby != null) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });



                if (!IsLobbyHost() && !alreadyStartedGame)
                {
                    string code = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
                    if (!string.IsNullOrEmpty(code))
                    {
                        alreadyStartedGame = true;
                        Debug.Log("Client detected Relay Join Code: " + code);
                        JoinGame(code);
                    }
                }


                if (!alreadyStartedGame)
                {
                    if (IsLobbyHost())
                    {
                        if(joinedLobby.Players.Count == 2)
                        {
                            //Two player joined, start game
                            StartGame();
                        }
                    }
                }


                if (!IsPlayerInLobby())
                {
                    //Player was kicked out of this lobby

                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }
            }
        }
    }


    public Lobby GetJoinedLobby() {
        return joinedLobby;
    }


    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }


    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }


    private Player GetPlayer() {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) }
        });
    }

    //public void ChangeGameMode() {
    //    if (IsLobbyHost()) {
    //        GameMode gameMode =
    //            Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

    //        switch (gameMode) {
    //            default:
    //            case GameMode.CaptureTheFlag:
    //                gameMode = GameMode.Conquest;
    //                break;
    //            case GameMode.Conquest:
    //                gameMode = GameMode.CaptureTheFlag;
    //                break;
    //        }

    //        UpdateLobbyGameMode(gameMode);
    //    }
    //}


    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {
        Unity.Services.Lobbies.Models.Player player = GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") },
                { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0") }
}
        
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;


        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });


        Debug.Log("Created Lobby " + lobby.Name);
    }



    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode) {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions {
            Player = player
        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }


    public async void JoinLobby(Lobby lobby) {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });


        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void UpdatePlayerName(string playerName) {
        this.playerName = playerName;

        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    //public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter) {
    //    if (joinedLobby != null) {
    //        try {
    //            UpdatePlayerOptions options = new UpdatePlayerOptions();

    //            options.Data = new Dictionary<string, PlayerDataObject>() {
    //                {
    //                    KEY_PLAYER_CHARACTER, new PlayerDataObject(
    //                        visibility: PlayerDataObject.VisibilityOptions.Public,
    //                        value: playerCharacter.ToString())
    //                }
    //            };

    //            string playerId = AuthenticationService.Instance.PlayerId;

    //            Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
    //            joinedLobby = lobby;

    //            OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
    //        } catch (LobbyServiceException e) {
    //            Debug.Log(e);
    //        }
    //    }
    //}

    public async void QuickJoinLobby() {
        try {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    //public async void UpdateLobbyGameMode(GameMode gameMode) {
    //    try {
    //        Debug.Log("UpdateLobbyGameMode " + gameMode);
            
    //        Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
    //            Data = new Dictionary<string, DataObject> {
    //                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
    //            }
    //        });

    //        joinedLobby = lobby;

    //        OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
    //    } catch (LobbyServiceException e) {
    //        Debug.Log(e);
    //    }
    //}

    public async void StartGame()
    {
       try
       {
           Debug.Log("StartGame");

            string relayCode = await StartGameManager.Instance.CreateRelay();

           Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });

            joinedLobby = lobby;

            IsHost = true;
            alreadyStartedGame = true;

            Debug.Log("[LobbyManager] Starting Game via Netcode.SceneManager");

            NetworkManager.Singleton.SceneManager.LoadScene(
                "GameScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
            );

            OnLobbyStartGame?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

       }
        catch(LobbyServiceException e)
        {
        Debug.Log(e);
        }

    }
        

    private async void JoinGame(string relayJoinCode)
    {
        Debug.Log($"JoinGame() called with code: '{relayJoinCode}' (length={relayJoinCode?.Length})");
        if (string.IsNullOrEmpty(relayJoinCode))
        {
            Debug.LogWarning("Skipping Relay join because code is empty or null.");
            return;
        }

        try
        {

            var joinAllocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // Create RelayServerData from the join allocation. "dtls" is the protocol (use "dtls" unless you have a reason to change it).
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            // Get the UnityTransport component from the NetworkManager and set the relay data.
            var unityTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            unityTransport.SetRelayServerData(relayServerData);

            // Start the client network session.
            NetworkManager.Singleton.StartClient();



            //IsHost = false;
            RelayJoinCode = relayJoinCode;
            //SceneManager.LoadScene(1);
            alreadyStartedGame = true;
            OnLobbyStartGame?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        }
        catch (Exception ex)
        {
            Debug.LogError("Error joining relay: " + ex.Message);
        }


    }


    public void SetRelayJoinCode(string joinCode)
    {
        RelayJoinCode = joinCode;
        Debug.Log("Relay Join Code set: " + RelayJoinCode);
    }


   
}