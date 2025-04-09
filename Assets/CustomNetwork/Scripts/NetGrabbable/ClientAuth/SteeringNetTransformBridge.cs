using Mirror;
using UnityEngine;

public class SteeringNetTransformBridge : NetworkBehaviour
{
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

        netController.ClientUpdateTransform(pos, rot);
    }
}