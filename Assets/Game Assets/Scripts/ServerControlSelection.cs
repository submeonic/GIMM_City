using Oculus.Interaction;
using UnityEngine;

public class ServerControlSelection : MonoBehaviour
{
    private enum State { START, JOIN };
    [SerializeField] private State state;
    
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
            ActivateServer();
            Destroy(other.gameObject);
        }
    }
}
