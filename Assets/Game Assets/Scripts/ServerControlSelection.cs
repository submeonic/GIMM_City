using UnityEngine;

public class ServerControlSelection : MonoBehaviour
{
    private enum State { START, JOIN };
    [SerializeField] private State state;

    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioSource audioSource;
    
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
            audioSource.PlayOneShot(selectClip);
            ActivateServer();
            Destroy(other.gameObject);
        }
    }
}
