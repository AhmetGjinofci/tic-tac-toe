using UnityEngine;

public class SoundManager : MonoBehaviour
{

    [SerializeField] private Transform placeSfxPrefab;


    private void Start()
    {
        GameManager.Instance.OnPlacedObject += GameManager_OnPlacedObject;
    }

    private void GameManager_OnPlacedObject(object sender, System.EventArgs e)
    {
        Transform sfxTransform = Instantiate(placeSfxPrefab);
        Destroy(sfxTransform.gameObject, 5f);
    }
}
