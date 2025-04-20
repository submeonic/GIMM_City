using Mirror;
using Oculus.Interaction;
using UnityEngine;

public class PlaceMap : NetworkBehaviour
{
    [SerializeField] private GameObject mapRoot;
    [SerializeField] private GameObject highlight;
    
    private Grabbable _grabbable;
    private bool selected = false;

    [SyncVar(hook = nameof(OnPlaceableChanged))]
    private bool placeable = false;

    [SyncVar]
    private bool placed = false;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
        mapRoot.SetActive(false);
    }

    #region Map Placement
    
    [Command(requiresAuthority = false)]
    private void CmdShowAndMoveMap(Vector3 position, Quaternion rotation)
    {
        mapRoot.transform.SetPositionAndRotation(position, rotation);
        mapRoot.SetActive(true);
        
        RpcShowAndMoveMap(position, rotation);
    }

    [ClientRpc]
    private void RpcShowAndMoveMap(Vector3 position, Quaternion rotation)
    {
        mapRoot.transform.SetPositionAndRotation(position, rotation);
        mapRoot.SetActive(true);
    }
        
    #endregion

    #region Placement Control

    [Command(requiresAuthority = false)]
    private void CmdSetPlaceable(bool canPlace)
    {
        if (!placed)
        {
            placeable = canPlace;
        }
    }

    private void OnPlaceableChanged(bool oldValue, bool newValue)
    {
        if (highlight != null)
            highlight.SetActive(newValue);
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
                CmdShowAndMoveMap(transform.position, transform.rotation);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!placed && other.gameObject.layer == LayerMask.NameToLayer("drivable") && selected)
        {
            CmdSetPlaceable(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!placed && other.gameObject.layer == LayerMask.NameToLayer("drivable"))
        {
            CmdSetPlaceable(false);
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }
}
