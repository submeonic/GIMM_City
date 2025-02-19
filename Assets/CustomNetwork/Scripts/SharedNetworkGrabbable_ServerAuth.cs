using Mirror;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(NetworkRigidbodyReliable))]
public class SharedNetworkGrabbable_ServerAuth : NetworkBehaviour
{
    // Cached component references.
    private Rigidbody _rb;
    private Grabbable _grabbable;

    #region Initialization

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabbable = GetComponentInChildren<Grabbable>();

        // Subscribe to grab/release events.
        _grabbable.WhenPointerEventRaised += OnGrabEvent;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // On the server, physics simulation is active.
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    private void OnDestroy()
    {
        _grabbable.WhenPointerEventRaised -= OnGrabEvent;
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Handles grab and release pointer events.
    /// </summary>
    /// <param name="evt">The pointer event.</param>
    private void OnGrabEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            HandleGrab();
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            HandleRelease();
        }
    }

    /// <summary>
    /// Handles grab events by setting the object to kinematic.
    /// </summary>
    private void HandleGrab()
    {
        Debug.Log($"[SERVER-AUTH] {gameObject.name} grabbed by {NetworkClient.localPlayer}");
        // Set the object to kinematic so that it stops being affected by physics.
        CmdSetKinematic(true);
    }

    /// <summary>
    /// Handles release events by setting the object back to dynamic.
    /// </summary>
    private void HandleRelease()
    {
        Debug.Log($"[SERVER-AUTH] {gameObject.name} released by {NetworkClient.localPlayer}");
        // Set the object to dynamic (non-kinematic) so that physics simulation resumes.
        CmdSetKinematic(false);
    }

    #endregion

    #region Network Commands & RPCs

    /// <summary>
    /// Sets the kinematic state on the server and propagates it to all clients.
    /// This command now requires authority.
    /// </summary>
    /// <param name="isKinematic">True to set the object kinematic, false to set it dynamic.</param>
    [Command(requiresAuthority = false)]
    private void CmdSetKinematic(bool isKinematic)
    {
        Debug.Log($"[SERVER-AUTH] CmdSetKinematic({isKinematic}) called on {gameObject.name}");
        _rb.isKinematic = isKinematic;
        _rb.useGravity = !isKinematic;
        RpcSetKinematic(isKinematic);
    }

    /// <summary>
    /// Propagates the kinematic state change to all clients.
    /// </summary>
    /// <param name="isKinematic">The new kinematic state.</param>
    [ClientRpc]
    private void RpcSetKinematic(bool isKinematic)
    {
        // Update the Rigidbody on all clients.
        _rb.isKinematic = isKinematic;
        _rb.useGravity = !isKinematic;
    }

    #endregion
}
