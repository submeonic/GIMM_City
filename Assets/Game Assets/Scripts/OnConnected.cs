using Mirror;
using UnityEngine;

public class OnConnected : NetworkBehaviour
{
    [SerializeField] private GameObject staticMap;
    public override void OnStartClient()
    {
        base.OnStartClient();
        staticMap.SetActive(true);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        staticMap.SetActive(true);
    }
}
