using System;
using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{

    [SerializeField] private GameObject crossArrowGameObject;
    [SerializeField] private GameObject circleArrowGameObject;
    [SerializeField] private GameObject crossYouTextGameObject;
    [SerializeField] private GameObject circleYouTextGameObject;
    [SerializeField] private TextMeshProUGUI playerCrossScoreTextMesh;
    [SerializeField] private TextMeshProUGUI playerCircleScoreTextMesh;



    private void Awake()
    {
        var gm = GameManager.Instance;
        gm.OnGameStarted += GameManager_OnGameStarted;
        gm.OnCurrentPlayablePlayerTypeChanged += GameManager_OnCurrentPlayablePlayerTypeChanged;
        gm.OnScoreChanged += GameManager_OnScoreChanged;
        gm.OnRematch += GameManager_OnRematch;
    }



    private void Start()
    {
        // If the game has already started by the time this UI appears, force it on:
        if (GameManager.Instance.GetCurrentPlayablePlayerType() != GameManager.PlayerType.None)
        {
            GameManager_OnGameStarted(this, EventArgs.Empty);
        }
    }


    private void GameManager_OnGameStarted(object sender, EventArgs e)
    {
        // first, hide both “You are…” labels
        crossYouTextGameObject.SetActive(false);
        circleYouTextGameObject.SetActive(false);

        // now show only the one for this client
        if (GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross)
            crossYouTextGameObject.SetActive(true);
        else
            circleYouTextGameObject.SetActive(true);

        // initialize the running score display immediately
        GameManager.Instance.GetScores(out var cScore, out var oScore);
        playerCrossScoreTextMesh.text = cScore.ToString();
        playerCircleScoreTextMesh.text = oScore.ToString();

        // also reset arrows then point at whoever starts
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);
        UpdateCurrentArrow();

    }

    private void GameManager_OnCurrentPlayablePlayerTypeChanged(object sender, EventArgs e)
    {
        // hide both arrows
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);

        UpdateCurrentArrow();
    }


    private void GameManager_OnScoreChanged(object sender, EventArgs e)
    {
        GameManager.Instance.GetScores(out var cross, out var circle);
        playerCrossScoreTextMesh.text = cross.ToString();
        playerCircleScoreTextMesh.text = circle.ToString();
    }

    private void GameManager_OnRematch(object sender, EventArgs e)
    {
        // When rematch fires, hide everything and wait for OnGameStarted again
        HideAll();
    }

    private void UpdateCurrentArrow()
    {
        if (GameManager.Instance.GetCurrentPlayablePlayerType() == GameManager.PlayerType.Cross)
            crossArrowGameObject.SetActive(true);
        else
            circleArrowGameObject.SetActive(true);
    }

    private void HideAll()
    {
        crossArrowGameObject.SetActive(false);
        circleArrowGameObject.SetActive(false);
        crossYouTextGameObject.SetActive(false);
        circleYouTextGameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // always unsubscribe
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStarted -= GameManager_OnGameStarted;
            gm.OnCurrentPlayablePlayerTypeChanged -= GameManager_OnCurrentPlayablePlayerTypeChanged;
            gm.OnScoreChanged -= GameManager_OnScoreChanged;
            gm.OnRematch -= GameManager_OnRematch;
        }
    }

}
