using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour {


    public static LobbyUI Instance { get; private set; }


    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    //[SerializeField] private TextMeshProUGUI gameModeText;
    //[SerializeField] private Button changeMarineButton;
    //[SerializeField] private Button changeNinjaButton;
    //[SerializeField] private Button changeZombieButton;
    [SerializeField] private Button leaveLobbyButton;
    //[SerializeField] private Button changeGameModeButton;


    private void Awake() {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        //changeMarineButton.onClick.AddListener(() => {
        //    LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Marine);
        //});
        //changeNinjaButton.onClick.AddListener(() => {
        //    LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Ninja);
        //});
        //changeZombieButton.onClick.AddListener(() => {
        //    LobbyManager.Instance.UpdatePlayerCharacter(LobbyManager.PlayerCharacter.Zombie);
        //});

        leaveLobbyButton.onClick.AddListener(() => {
            LobbyManager.Instance.LeaveLobby();
        });

        //changeGameModeButton.onClick.AddListener(() => {
        //    LobbyManager.Instance.ChangeGameMode();
        //});
    }

    private void Start() {
        LobbyManager.Instance.OnJoinedLobby += UpdateLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;
        

        Hide();
    }

    private void OnDestroy()
    {
        // unsubscribe so we stop getting events once this UI is torn down
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnJoinedLobby -= UpdateLobby_Event;
            LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateLobby_Event;
            LobbyManager.Instance.OnLobbyGameModeChanged -= UpdateLobby_Event;
            LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
            LobbyManager.Instance.OnKickedFromLobby -= LobbyManager_OnLeftLobby;
        }
    }



    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e) {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e) {
        UpdateLobby();
    }

    private void UpdateLobby() {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }

    private void UpdateLobby(Lobby lobby) {
        ClearLobby();

        foreach (Player player in lobby.Players) {
            Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
            playerSingleTransform.gameObject.SetActive(true);
            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

            lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
            );

            lobbyPlayerSingleUI.UpdatePlayer(player);
        }

        //changeGameModeButton.gameObject.SetActive(LobbyManager.Instance.IsLobbyHost());

        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        //gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;

        Show();
    }

    private void ClearLobby() {
        // iterate backwards so removing children doesn't shift the others you still need to process
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            // don’t destroy the template itself
            if (child.gameObject == playerSingleTemplate.gameObject)
                continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void Show() {
        gameObject.SetActive(true);
    }

}