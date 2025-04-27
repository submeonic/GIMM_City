using UnityEngine;

public class ServerControlSelection : MonoBehaviour
{
    private enum State { START, JOIN };
    [SerializeField] private State state;

    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private MusicController _musicController;
    [SerializeField] private MusicController.MusicSnapShotLevel _musicSnapShotLevel;
    [SerializeField] private GameObject _staticMap;
    
    private void ActivateServer()
    {
        if (state == State.START)
        {
            ColocationNetworkManager.singleton.StartHost();
        }

        if (state == State.JOIN)
        {
            ColocationNetworkManager.singleton.StartClient();
        }
        Destroy(transform.parent.gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Selector"))
        {
            _musicController.SetSnapshotLevel(_musicSnapShotLevel);
            audioSource.PlayOneShot(selectClip);
            ActivateServer();
            _staticMap.SetActive(true);
            Destroy(other.gameObject);
        }
    }
}
