using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;

public class GameVisualManager : NetworkBehaviour
{

    private const float GRID_SIZE = 3.1f;


    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;

    private List<GameObject> visualGameObjectList;



    private void Awake()
    {
        visualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManager_OnClickedOnGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnClickedOnGridPosition -= GameManager_OnClickedOnGridPosition;
            GameManager.Instance.OnGameWin -= GameManager_OnGameWin;
            GameManager.Instance.OnRematch -= GameManager_OnRematch;
        }
    }


    private void GameManager_OnRematch(object sender, EventArgs e)
    {
        // destroy all the old X’s and O’s
        foreach (var go in visualGameObjectList)
            Destroy(go);
        visualGameObjectList.Clear();
    }


    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        float eulerZ = 0f;
        switch (e.line.orientation)
        {
            default:
            case GameManager.Orientation.Horizontal:    eulerZ = 0f;    break;
            case GameManager.Orientation.Vertical:      eulerZ = 90f;   break;
            case GameManager.Orientation.DiagonalA:     eulerZ = 45f;   break;
            case GameManager.Orientation.DiagonalB:     eulerZ = -45f;  break;
        }
        Transform lineCompleteTransform =
            Instantiate(
                lineCompletePrefab,
                GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y),
                Quaternion.Euler(0, 0, eulerZ)
                );
        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true);

        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }

    private void GameManager_OnClickedOnGridPosition(object sender, OnClickedOnGridPositionEventArgs e)
    {
        if (!IsServer) return;               // server only

        var prefab = (e.playerType == GameManager.PlayerType.Cross)
                     ? crossPrefab
                     : circlePrefab;

        var worldPos = GetGridWorldPosition(e.x, e.y);
        var instance = Instantiate(prefab, worldPos, Quaternion.identity);

        // tell Netcode about it
        instance.GetComponent<NetworkObject>().Spawn();

        // **track it** so we can destroy it on rematch
        visualGameObjectList.Add(instance.gameObject);
    }


    //[Rpc(SendTo.Server)]
    //private void SpawnObjectRpc(int x, int y, GameManager.PlayerType playerType)
    //{
    //    Debug.Log("SpawnObject!");
    //    Transform prefab;
    //    switch (playerType)
    //    {
    //        default:
    //        case GameManager.PlayerType.Cross:
    //            prefab = crossPrefab;
    //            break;
    //        case GameManager.PlayerType.Circle:
    //            prefab = circlePrefab;
    //            break;
    //    }
         
    //    Transform spawnedCrossTransform = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);
    //    spawnedCrossTransform.GetComponent<NetworkObject>().Spawn(true);

    //    visualGameObjectList.Add(spawnedCrossTransform.gameObject);
    //}



    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }


}
