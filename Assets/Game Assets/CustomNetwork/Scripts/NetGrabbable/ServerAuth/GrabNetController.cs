using Mirror; 
using UnityEngine;

public class GrabNetController : NetworkBehaviour
{
    // Cached component references.
    private Rigidbody _rb;

    // Tracks whether the object is currently grabbed.
    [SyncVar] public bool isGrabbed = false;
    // Stores the network identity of the client that grabbed the object.
    [SyncVar] private NetworkIdentity grabber = null;

    #region Initialization

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Start with physics simulation enabled.
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    #endregion

    #region Public Methods (Called by Grab Net Transform Bridge)

    public void ClientRequestGrab()
    {
        CmdTryGrab();
    }

    public void ClientRequestRelease()
    {
        CmdTryRelease();
    }

    // This method is called by the local proxy repeatedly to update the object's transform.
    public void ClientUpdateTransform(Vector3 pos, Quaternion rot)
    {
        CmdUpdateTransform(pos, rot);
    }

    #endregion

    #region Network Commands & RPCs

    [Command(requiresAuthority = false)]
    private void CmdTryGrab(NetworkConnectionToClient sender = null)
    {
        if (!isGrabbed)
        {
            isGrabbed = true;
            grabber = sender.identity;
            Debug.Log($"[SERVER-AUTH] {gameObject.name} grabbed by {sender}");

            // Set object to kinematic so physics stops affecting it.
            SetKinematicState(true);

            RpcOnGrabbed(sender.identity);
        }
        else
        {
            Debug.Log($"[SERVER-AUTH] {gameObject.name} already grabbed by {grabber}");
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdTryRelease(NetworkConnectionToClient sender = null)
    {
        if (isGrabbed && grabber == sender.identity)
        {
            isGrabbed = false;
            grabber = null;
            Debug.Log($"[SERVER-AUTH] {gameObject.name} released by {sender}");

            SetKinematicState(false);
            RpcOnReleased();
        }
        else
        {
            Debug.LogWarning($"[SERVER-AUTH] {gameObject.name} release attempt by {sender} rejected.");
        }
    }

    // Command that receives transform updates from the client.
    [Command(requiresAuthority = false)]
    private void CmdUpdateTransform(Vector3 pos, Quaternion rot, NetworkConnectionToClient sender = null)
    {
        // Only update if the sender is the client that grabbed the object.
        if (isGrabbed && grabber == sender.identity)
        {
            // Update the object's transform on the server.
            transform.position = Vector3.Lerp(transform.position, pos, 0.85f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 0.85f);
        }
    }

    [Command(requiresAuthority = false)]
    private void SetKinematicState(bool isKinematic)
    {
        _rb.isKinematic = isKinematic;
        _rb.useGravity = !isKinematic;
        
        if (isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        
        RpcSetKinematic(isKinematic);
    }

    [ClientRpc]
    private void RpcSetKinematic(bool isKinematic)
    {
        _rb.isKinematic = isKinematic;
        _rb.useGravity = !isKinematic;
        
        if (isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    private void RpcOnGrabbed(NetworkIdentity newGrabber)
    {
        Debug.Log($"[SERVER-AUTH] {gameObject.name} now grabbed by {newGrabber}");
    }

    [ClientRpc]
    private void RpcOnReleased()
    {
        Debug.Log($"[SERVER-AUTH] {gameObject.name} released");
    }

    #endregion
}