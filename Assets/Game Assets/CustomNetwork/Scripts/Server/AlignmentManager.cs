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
        // Wait for tracking system to settle
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        Vector3 startPos = _cameraRigTransform.position;
        _cameraRigTransform.position = new Vector3(0f, startPos.y, 0f);
        _cameraRigTransform.eulerAngles = Vector3.zero;
        
        GameObject skeletonGO = LocalReferenceManager.Instance.OvrSkeleton.gameObject;
        skeletonGO.transform.localPosition = Vector3.zero;
        
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
}