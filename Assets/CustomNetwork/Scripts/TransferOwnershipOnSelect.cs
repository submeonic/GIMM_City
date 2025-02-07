using Mirror;
using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Mirror version of TransferOwnershipOnSelect.
/// Listens for pointer (select) events from a Grabbable component and, when exactly one pointer is selecting,
/// transfers authority to the local client by calling a server Command.
/// Optionally, if UseGravity is true, it ensures the Rigidbody is in the proper kinematic state.
/// </summary>
public class MirrorTransferOwnershipOnSelect : NetworkBehaviour
{
    /// <summary>
    /// If true, the object is affected by gravity and we adjust its Rigidbody's isKinematic state.
    /// </summary>
    public bool UseGravity;

    // Reference to the Grabbable component (from Oculus Interaction)
    private Grabbable _grabbable;

    // Cached Rigidbody reference (if UseGravity is enabled)
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _grabbable = GetComponentInChildren<Grabbable>();
        if (_grabbable == null)
        {
            Debug.LogError("MirrorTransferOwnershipOnSelect requires a Grabbable component in its children.");
            return;
        }

        // Subscribe to pointer events (the Fusion version listens to WhenPointerEventRaised)
        _grabbable.WhenPointerEventRaised += OnPointerEventRaised;
    }

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
        }
    }

    /// <summary>
    /// Called when the Grabbable raises a pointer event.
    /// In this example we only care about Select events.
    /// </summary>
    /// <param name="pointerEvent">The pointer event information.</param>
    private void OnPointerEventRaised(PointerEvent pointerEvent)
    {
        // Only process Select events.
        if (pointerEvent.Type != PointerEventType.Select)
        {
            return;
        }

        // When exactly one selecting point exists and we don't already have authority, request ownership.
        if (_grabbable.SelectingPointsCount == 1)
        {
            // On the client, if we don't have authority, ask the server to transfer authority.
            if (!authority)
            {
                CmdRequestOwnership();
            }
        }
    }

    /// <summary>
    /// Server command to assign client authority to the requester.
    /// </summary>
    /// <param name="sender">Automatically provided by Mirror (the client making the call).</param>
    [Command(requiresAuthority = false)]
    private void CmdRequestOwnership(NetworkConnectionToClient sender = null)
    {
        Debug.Log("CmdRequestOwnership called on server from " + sender);

        // If someone already holds client authority, remove it.
        if (netIdentity.connectionToClient != null)
        {
            Debug.Log("Removing authority from current owner: " + netIdentity.connectionToClient);
            netIdentity.RemoveClientAuthority();
        }

        // Assign authority to the requesting client.
        netIdentity.AssignClientAuthority(sender);
        Debug.Log("Authority assigned to " + sender);
    }

    /// <summary>
    /// In LateUpdate we optionally adjust the Rigidbody's kinematic state.
    /// This mimics the Fusion version which uses a RigidbodyKinematicLocker.
    /// </summary>
    private void LateUpdate()
    {
        if (UseGravity && authority)
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody == null)
                {
                    Debug.LogError("UseGravity is enabled but no Rigidbody component was found.");
                    return;
                }
            }

            // In the Fusion version, the Rigidbody's isKinematic is set based on a lock check.
            // Here you might want to simply ensure that if you have authority the object is not kinematic,
            // or implement your own logic.
            _rigidbody.isKinematic = false;
        }
    }
}
