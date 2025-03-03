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

    #endregion

    #region Anchor Alignment

    /// <summary>
    /// Aligns the user's rig to the given spatial anchor.
    /// </summary>
    /// <param name="anchor">The spatial anchor to align to.</param>
    public void AlignUserToAnchor(OVRSpatialAnchor anchor)
    {
        if (anchor == null || !anchor.Localized)
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
        // Perform alignment over two iterations for precision.
        for (int i = 0; i < 2; i++)
        {
            // Reset rig transform.
            _cameraRigTransform.position = Vector3.zero;
            _cameraRigTransform.eulerAngles = Vector3.zero;
            yield return null;
            // Align rig relative to the anchor.
            _cameraRigTransform.position = anchorTransform.InverseTransformPoint(Vector3.zero);
            _cameraRigTransform.eulerAngles = new Vector3(0, -anchorTransform.eulerAngles.y, 0);
            Debug.Log($"AlignmentManager: Aligned rig Position: {_cameraRigTransform.position}, Rotation: {_cameraRigTransform.eulerAngles}");
            yield return new WaitForEndOfFrame();
        }
        Debug.Log("AlignmentManager: Alignment complete.");
    }

    #endregion
}