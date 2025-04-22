using Mirror;
using UnityEngine;
using System.Collections;

public class GrabMenuController : NetworkBehaviour
{
    public GameObject activeCar
    {
        get => activeCar;
        set => activeCar = value;
    }

    [Command]
    public void DestroyActiveCar()
    {
        if (activeCar == null)
            return;
        
        NetworkServer.Destroy(activeCar);
        activeCar = null;
    }
}
