using Mirror;
using Oculus.Interaction;
using UnityEngine;

public class PlaceMap : NetworkBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private GameObject highlight;
    [SerializeField] private Grabbable _grabbable;
    private bool selected = false;

    [SyncVar(hook = nameof(OnPlaceableChanged))]
    private bool placeable = false;

    private void Awake()
    {
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    #region Map Placement

    [ClientRpc]
    private void RpcMovePlayersWithOffset()
    {
        AlignmentManager alignmentManager = FindObjectOfType<AlignmentManager>();
        if (alignmentManager != null)
        {
            alignmentManager.OffsetPlayerToMap(transform);
        }
        else
        {
            Debug.LogError("[PlaceMap] AlignmentManager not found.");
        }

        CmdResetMapPosition(); // Server-side authoritative reset
    }

    [Command(requiresAuthority = false)]
    private void CmdResetMapPosition()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Debug.Log("[PlaceMap] Map reset to origin on server.");
    }

    #endregion

    #region Placement Control

    [Command(requiresAuthority = false)]
    private void CmdSetPlaceable(bool canPlace)
    {
        placeable = canPlace;
    }

    private void OnPlaceableChanged(bool oldValue, bool newValue)
    {
        if (highlight != null)
        {
            highlight.SetActive(newValue);
        }
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

            if (placeable)
            {
                RpcMovePlayersWithOffset(); // Only happens on valid surface after release
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Floor") && selected)
        {
            CmdSetPlaceable(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Floor"))
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
