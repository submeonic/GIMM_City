using Oculus.Interaction;
using UnityEngine;

public class ServerControlSelection : MonoBehaviour
{
    private enum State { START, JOIN };
    [SerializeField] private State state;
    [SerializeField] private GameObject staticMap;
    
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
        staticMap.SetActive(true);
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
