using Mirror;
using UnityEngine;

public class SteeringNetController : NetworkBehaviour
{
    private Transform steeringTransform;

    private void Awake()
    {
        steeringTransform = transform;
    }

    /// <summary>
    /// Called by the local client (via SteeringNetTransformBridge) to update the world transform.
    /// </summary>
    public void ClientUpdateTransform(Vector3 position, Quaternion rotation)
    {
        if (!isOwned)
            return;

        // Send to server to relay to other clients
        CmdSendTransform(position, rotation);
    }

    [Command]
    private void CmdSendTransform(Vector3 position, Quaternion rotation)
    {
        // Optional: validation, clamping, or input control logic
        RpcApplyTransform(position, rotation);
    }

    [ClientRpc]
    private void RpcApplyTransform(Vector3 position, Quaternion rotation)
    {
        if (isOwned) return; // Local client already applied this

        steeringTransform.position = position;
        steeringTransform.rotation = rotation;
    }
}