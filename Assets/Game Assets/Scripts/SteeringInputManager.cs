using Mirror;
using UnityEngine;
using TMPro;
using System;

public class SteeringInputManager : NetworkBehaviour
{
    [Header("Optional Debug UI")]
    [SerializeField] private TextMeshProUGUI debugText;

    // NetId of the server‑spawned car this client drives
    private uint controlledCarNetId;
    
    public event Action<uint> OnCarAssigned;

    /// <summary>
    /// Called by MenuGrabbable.TargetAssignCar on this client.
    /// </summary>
    public void AssignCar(uint carNetId)
    {
        controlledCarNetId = carNetId;
        OnCarAssigned?.Invoke(carNetId);
        debugText?.SetText($"Driving Car: {carNetId}");
        Debug.Log($"[Client] Assigned car NetId {carNetId}");
    }

    /// <summary>
    /// Hook this up to your VR steering wheel input events.
    /// </summary>
    public void SetInput(float steering, float throttle)
    {
        if (!isOwned) return;             // only the local wheel owner can send
        if (controlledCarNetId == 0) return;   // ensure a car is assigned
        CmdSendInput(steering, throttle, controlledCarNetId);
    }

    [Command]
    private void CmdSendInput(float steering, float throttle, uint carNetId)
    {
        // 1) Server applies inputs to its authoritative car instance
        if (NetworkServer.spawned.TryGetValue(carNetId, out var identity))
        {
            var vc = identity.GetComponent<ArcadeVehicleController>();
            if (vc != null)
            {
                float brake = (steering == 0f && throttle == 0f) ? 1f : 0f;
                vc.ProvideInputs(steering, throttle, brake);
            }
        }

        // 2) Broadcast same inputs to all clients for local prediction
        RpcReceiveInput(steering, throttle, carNetId);
    }

    [ClientRpc]
    private void RpcReceiveInput(float steering, float throttle, uint carNetId)
    {
        // Every client—including the one who sent it—applies inputs locally
        if (NetworkClient.spawned.TryGetValue(carNetId, out var identity))
        {
            var vc = identity.GetComponent<ArcadeVehicleController>();
            if (vc != null)
            {
                float brake = (steering == 0f && throttle == 0f) ? 1f : 0f;
                vc.ProvideInputs(steering, throttle, brake);
            }
        }
    }
}
