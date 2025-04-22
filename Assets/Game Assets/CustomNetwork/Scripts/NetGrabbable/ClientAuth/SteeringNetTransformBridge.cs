using Mirror;
using UnityEngine;

/// <summary>
/// Called every tick with a new local transform. Sends updates to the server
/// at a fixed sync interval (e.g., 0.2s) for forwarding to other clients.
/// </summary>
public class SteeringNetTransformBridge : NetworkBehaviour
{
    [Tooltip("Seconds between network updates")]
    [SerializeField] private float syncInt = 0.2f;

    private SteeringNetController netController;
    private float syncTimer = 0f;

    private Vector3 queuedPosition;
    private Quaternion queuedRotation;
    private bool hasQueuedUpdate = false;

    private void Awake()
    {
        netController = GetComponentInParent<SteeringNetController>();
    }

    private void Update()
    {
        if (!isOwned || netController == null || !hasQueuedUpdate)
            return;

        syncTimer += Time.deltaTime;

        if (syncTimer >= syncInt)
        {
            syncTimer = 0f;
            netController.ClientUpdateTransform(queuedPosition, queuedRotation);
            hasQueuedUpdate = false;
        }
    }

    /// <summary>
    /// Called every tick with latest transform.
    /// Only forwarded to network on sync interval.
    /// </summary>
    public void UpdateTransformFromTransformer(Vector3 pos, Quaternion rot)
    {
        if (!isOwned || netController == null)
            return;

        queuedPosition = pos;
        queuedRotation = rot;
        hasQueuedUpdate = true;
    }
}