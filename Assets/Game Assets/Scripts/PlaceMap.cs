using Mirror;
using Oculus.Interaction;
using UnityEngine;

public class PlaceMap : NetworkBehaviour
{
    [SerializeField] private GameObject map;
    [SerializeField] private GameObject highlight;
    private Grabbable _grabbable;
    
    private bool placeable = false;
    [SyncVar] private bool placed = false;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }
    private void Update()
    {
        //highlights
        highlight.SetActive(placeable);
    }

    private void LateUpdate()
    {
        placeable = false;
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
        NetworkServer.Spawn(spawnedMap);

        placed = true;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Unselect && placeable && !placed)
        {
            ClientSpawnMapRequest();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("drivable"))
        {
            placeable = true;
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
