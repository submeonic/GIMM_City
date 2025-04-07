using Mirror;
using Oculus.Interaction;
using UnityEngine;

public class PlaceMap : NetworkBehaviour
{
    [SerializeField] private GameObject map;
    [SerializeField] private GameObject highlight;
    private Grabbable _grabbable;
    
    private bool placeable = false;
    private bool selected = false;
    [SyncVar] private bool placed = false;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    private void ClientSpawnMapRequest()
    {
        CmdSpawnMap();
    }

    [Command(requiresAuthority = false)]
    private void CmdSpawnMap()
    {
        if (placed) return; // prevent multiple placements

        GameObject spawnedMap = Instantiate(map, transform.position, transform.rotation);

        // First spawn the root map
        NetworkServer.Spawn(spawnedMap);

        // Now spawn any child NetworkIdentity objects (excluding the root)
        var childIdentities = spawnedMap.GetComponentsInChildren<NetworkIdentity>(true);
        foreach (var netId in childIdentities)
        {
            if (netId.gameObject != spawnedMap) // ⬅️ This is the fix
            {
                NetworkServer.Spawn(netId.gameObject);
            }
        }

        placed = true;
        highlight.SetActive(false);
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            selected = true;
        }
        if (evt.Type == PointerEventType.Unselect)
        {
            selected = false;
            if (placeable && !placed)
            {
                ClientSpawnMapRequest();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!placed)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("drivable") && selected)
            {
                placeable = true;
                highlight.SetActive(placeable);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!placed)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("drivable"))
            {
                placeable = false;
                highlight.SetActive(placeable);
            }
        }
    }
    
    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }
}
