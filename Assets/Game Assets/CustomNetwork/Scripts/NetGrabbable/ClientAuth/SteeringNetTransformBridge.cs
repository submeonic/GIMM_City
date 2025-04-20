using Mirror;
using UnityEngine;

public class SteeringNetTransformBridge : NetworkBehaviour
{
    [SerializeField] private float _steeringSyncInterval = 0.2f;
    private float lastSendTime = 0f;
    
    private SteeringNetController netController;

    private void Awake()
    {
        netController = GetComponentInParent<SteeringNetController>();
    }

    /// <summary>
    /// Called directly by transformer scripts to sync movement.
    /// </summary>
    public void UpdateTransformFromTransformer(Vector3 pos, Quaternion rot)
    {
        if (!isOwned || netController == null)
            return;
        if (Time.time - lastSendTime < syncInterval)
            return;

        netController.ClientUpdateTransform(pos, rot);
    }
}