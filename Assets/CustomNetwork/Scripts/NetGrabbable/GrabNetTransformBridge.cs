using Mirror;
using UnityEngine;
using Oculus.Interaction;

public class GrabNetTransformBridge : NetworkBehaviour
{

    // Reference to the Grab Net Controller
    private GrabNetController _grabNetController;
    
    // Reference to Meta's Grabbable for input events.
    private Grabbable _grabbable;

    private Vector3 _latestPosition;
    private Quaternion _latestRotation;
    
    // Flag to track if we're currently grabbed.
    private bool isGrabbed = false;

    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        _grabNetController = GetComponentInParent<GrabNetController>();
        _grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    private void OnDestroy()
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            Debug.Log("[Local Proxy] Grabbing object");
            isGrabbed = true;
            _grabNetController.ClientRequestGrab();
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            Debug.Log("[Local Proxy] Releasing object");
            isGrabbed = false;
            _grabNetController.ClientRequestRelease();
        }
    }
    
    private void Update()
    {
        // While grabbed, continuously send transform updates.
        if (isGrabbed)
        {
            // Send updates to the server
            _grabNetController.ClientUpdateTransform(_latestPosition, _latestRotation);
        }
    }
    
    public void UpdateTransformFromTransformer(Vector3 pos, Quaternion rot)
    {
        _latestPosition = pos;
        _latestRotation = rot;
    }
}