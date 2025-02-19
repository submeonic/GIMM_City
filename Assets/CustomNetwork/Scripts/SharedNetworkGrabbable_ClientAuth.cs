using Mirror;
using UnityEngine;
using Oculus.Interaction;
using System.Threading.Tasks;

public class SharedNetworkGrabbable_ClientAuth : NetworkBehaviour
{
    // Cached component references.
    private Rigidbody _rb;
    private Grabbable _grabbable;

    [Header("Network Rigidbody Components")]
    [Tooltip("Active while grabbed (Client To Server sync).")]
    [SerializeField] private NetworkRigidbodyReliable clientAuthNetworkRigidbody;
    
    [Tooltip("Active when released (Server To Client sync).")]
    [SerializeField] private NetworkRigidbodyReliable serverAuthNetworkRigidbody;

    #region Initialization

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabbable = GetComponentInChildren<Grabbable>();

        // Subscribe to grab/release events.
        _grabbable.WhenPointerEventRaised += OnGrabEvent;

        // At startup, the object is server-controlled.
        clientAuthNetworkRigidbody.enabled = false;
        serverAuthNetworkRigidbody.enabled = true;
    }

    private void OnDestroy()
    {
        _grabbable.WhenPointerEventRaised -= OnGrabEvent;
    }

    #endregion

    #region Authority Callbacks

    /// <summary>
    /// Called when this client is granted authority.
    /// With authority, we switch the object to client-controlled physics.
    /// </summary>
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log($"[DEBUG] Authority granted on {gameObject.name} to {netIdentity.connectionToClient}");
        // With authority, set the object to kinematic so that the client drives physics.
        CmdSetKinematic(true);
    }

    /// <summary>
    /// Called when this client loses authority.
    /// (We no longer call CmdSetKinematic here because it is already handled in CmdReleaseAuthority.)
    /// </summary>
    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        Debug.Log($"[DEBUG] Authority lost on {gameObject.name} for {netIdentity.connectionToClient}");
        // No need to call CmdSetKinematic(false) here because it was already called before authority removal.
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Called when a grab or release event occurs.
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
    /// Handles grab events:
    /// Requests authority and switches to client-controlled physics.
    /// </summary>
    private void HandleGrab()
    {
        Debug.Log($"[DEBUG] {gameObject.name} grabbed by {NetworkClient.localPlayer}");
        // Request authority. When granted, OnStartAuthority() will call CmdSetKinematic(true).
        CmdRequestAuthority();

        // Enable client-to-server syncing.
        clientAuthNetworkRigidbody.enabled = true;
        serverAuthNetworkRigidbody.enabled = false;
    }

    /// <summary>
    /// Handles release events:
    /// Initiates a graceful handover from client-controlled to server-controlled physics.
    /// </summary>
    private void HandleRelease()
    {
        Debug.Log($"[DEBUG] {gameObject.name} released by {NetworkClient.localPlayer}");
        if (authority)
        {
            // Begin the authority release process (which includes a grace period).
            CmdReleaseAuthority();

            // Switch to server-to-client syncing.
            clientAuthNetworkRigidbody.enabled = false;
            serverAuthNetworkRigidbody.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[WARNING] {gameObject.name}: Attempted to release but client lacks authority!");
        }
    }

    #endregion

    #region Network Commands & RPCs

    /// <summary>
    /// Requests authority for this client.
    /// Removes any existing authority before assigning it.
    /// </summary>
    /// <param name="sender">The requesting client's connection.</param>
    [Command(requiresAuthority = false)]
    private void CmdRequestAuthority(NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[DEBUG] CmdRequestAuthority called by {sender} for {gameObject.name}");

        // Remove any existing client authority.
        if (netIdentity.connectionToClient != null)
        {
            Debug.Log($"[DEBUG] {gameObject.name}: Removing authority from {netIdentity.connectionToClient}");
            netIdentity.RemoveClientAuthority();
        }

        netIdentity.AssignClientAuthority(sender);
        Debug.Log($"[DEBUG] {gameObject.name}: Authority assigned to {sender}");
    }

    /// <summary>
    /// Releases client authority after a grace period.
    /// First sets the object to dynamic (non-kinematic), waits, then removes authority.
    /// </summary>
    [Command(requiresAuthority = true)]
    private async void CmdReleaseAuthority()
    {
        if (!authority)
        {
            Debug.LogWarning($"[ERROR] CmdReleaseAuthority() called on {gameObject.name} without authority!");
            return;
        }

        Debug.Log($"[DEBUG] {gameObject.name}: Releasing authority.");

        // Switch the object to dynamic mode so that the server can simulate it.
        CmdSetKinematic(false);

        // Wait for a grace period (e.g., 200ms) to allow for smooth physics transition.
        await Task.Delay(500);

        // Remove client authority so the server resumes control.
        netIdentity.RemoveClientAuthority();
        Debug.Log($"[DEBUG] {gameObject.name}: Authority removed after grace period.");
    }

    /// <summary>
    /// Sets the kinematic state of the Rigidbody.
    /// When kinematic, the client drives physics.
    /// When not, the server simulates physics.
    /// </summary>
    /// <param name="isKinematic">Desired kinematic state.</param>
    [Command(requiresAuthority = true)]
    private void CmdSetKinematic(bool isKinematic)
    {
        Debug.Log($"[DEBUG] {gameObject.name}: CmdSetKinematic({isKinematic})");
        _rb.isKinematic = isKinematic;
        _rb.useGravity = !isKinematic;
        RpcSetKinematic(isKinematic);
    }

    /// <summary>
    /// Propagates the kinematic state to all clients.
    /// (The NetworkRigidbodyReliable component does not automatically sync properties like isKinematic.)
    /// </summary>
    /// <param name="isKinematic">Desired kinematic state.</param>
    [ClientRpc]
    private void RpcSetKinematic(bool isKinematic)
    {
        Debug.Log($"[DEBUG] {gameObject.name}: RpcSetKinematic({isKinematic}) received.");
        // Update the Rigidbody only if this client does not hold authority.
        if (!authority)
        {
            _rb.isKinematic = isKinematic;
            _rb.useGravity = !isKinematic;
        }
    }

    #endregion
}
