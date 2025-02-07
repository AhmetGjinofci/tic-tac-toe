using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;


public class TestLobby : MonoBehaviour
{


    private Lobby hostLobby;
    private float heartbeatTimer;
    private string playerName = "Meti" + UnityEngine.Random.Range(10, 99);



    private async void Start()
    {
        await UnityServices.InitializeAsync();


        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();


        Debug.Log(playerName);
    }


    private void Update()
    {
        HandleLobbyHeartbeat();
    }


    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;


                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }



    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayers = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, "CaptureFlag") }
                }
            };


            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;

            
            Debug.Log("Created lobby! " + lobby.Name + " " + lobby.MaxPlayers);
            PrintPlayer(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    
                },

                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };


            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions { 
         
            Player = GetPlayer()
        };

            Lobby joinedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            Debug.Log("Joined lobby with code " + lobbyCode);

            PrintPlayer(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }   
    }


    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e); 
        }
    }


    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
    }


    private void PrintPlayer(Lobby lobby)
    {
        Debug.Log("Players in Lobby " + lobby.Name + " " + lobby.Data["GameMode"].Value);
        
        foreach(Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }


    private void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
            }
            });

            PrintPlayer(hostLobby);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

}
