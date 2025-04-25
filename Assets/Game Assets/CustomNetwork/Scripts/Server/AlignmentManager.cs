using UnityEngine;
using System.Collections;

public class AlignmentManager : MonoBehaviour
{
    private Transform _cameraRigTransform;

    #region Initialization
    private void Awake()
    {
        // Assumes an OVRCameraRig is present in the scene.
        _cameraRigTransform = FindObjectOfType<OVRCameraRig>().transform;
    }

    private void Start()
    {
        StartCoroutine(AlignToWorldOriginOnStart());
    }
    
    private IEnumerator AlignToWorldOriginOnStart()
    {
        yield return new WaitForSeconds(3f);
        
        Vector3 startPos = _cameraRigTransform.position;
        _cameraRigTransform.position = new Vector3(0f, 0f, 0f);
        _cameraRigTransform.eulerAngles = Vector3.zero;
        
        GameObject skeletonGO = LocalReferenceManager.Instance.OvrSkeleton.gameObject;
        skeletonGO.transform.localPosition = Vector3.zero;
        skeletonGO.transform.localRotation = Quaternion.identity;
        
        GameObject trackingSpaceGO = LocalReferenceManager.Instance.TrackingSpace.gameObject;
        trackingSpaceGO.transform.localPosition = Vector3.zero;
        trackingSpaceGO.transform.localRotation = Quaternion.identity;
        
        Debug.Log("AlignmentManager: CameraRig aligned to world origin at startup.");
    }

    #endregion

    #region Anchor Alignment

    /// <summary>
    /// Aligns the user's rig to the given spatial anchor.
    /// </summary>
    /// <param name="anchor">The spatial anchor to align to.</param>
    public void AlignUserToAnchor(OVRSpatialAnchor anchor)
    {
        if (!anchor || !anchor.Localized)
        {
            Debug.LogError("AlignmentManager: Invalid or unlocalized anchor. Cannot align.");
            return;
        }
        Debug.Log($"AlignmentManager: Starting alignment to anchor {anchor.Uuid}.");
        StartCoroutine(AlignmentCoroutine(anchor));
    }

    private IEnumerator AlignmentCoroutine(OVRSpatialAnchor anchor)
    {
        var anchorTransform = anchor.transform;

        for (var alignmentCount = 2; alignmentCount > 0; alignmentCount--)
        {
            _cameraRigTransform.position = Vector3.zero;
            _cameraRigTransform.eulerAngles = Vector3.zero;
            yield return null;
            
            // Align rig relative to the anchor.
            _cameraRigTransform.position = anchorTransform.InverseTransformPoint(Vector3.zero);
            _cameraRigTransform.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);
            Debug.Log($"AlignmentManager: Aligned Camera Rig Position: {_cameraRigTransform.position}, Rotation: {_cameraRigTransform.eulerAngles}");
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("AlignmentManager: Alignment complete.");
    }

    #endregion
    
    #region Map Alignment
    
    /// <summary>
    /// Offsets the user's rig based on the new map position and rotation.
    /// Used after dropping a placement object to relocate the world origin.
    /// </summary>
    /// <param name="newMapTransform">Transform of the placed map object</param>
    public void OffsetPlayerToMap(Transform newMapTransform)
    {
        if (_cameraRigTransform == null)
        {
            Debug.LogError("AlignmentManager: Camera rig transform not found.");
            return;
        }

        // Get yaw rotation only
        float yaw = newMapTransform.eulerAngles.y;
        Quaternion rotationOffset = Quaternion.Euler(0f, yaw, 0f);

        // Offset player's rig relative to the map's position and rotation
        Vector3 pivot = newMapTransform.position;
        Vector3 direction = _cameraRigTransform.position - pivot;

        // Rotate the direction vector around the pivot using yaw
        direction = rotationOffset * direction;
        _cameraRigTransform.position = pivot + direction;

        // Apply the yaw rotation to the rig
        _cameraRigTransform.rotation = rotationOffset * _cameraRigTransform.rotation;

        Debug.Log($"AlignmentManager: Applied map offset. New Pos: {_cameraRigTransform.position}, New Rot: {_cameraRigTransform.rotation.eulerAngles}");
    }

    #endregion
}