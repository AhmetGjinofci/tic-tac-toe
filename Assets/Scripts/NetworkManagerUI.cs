using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{

    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;


    private void Awake()
    {
        startHostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
        });

        startClientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
        });
    }


    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
