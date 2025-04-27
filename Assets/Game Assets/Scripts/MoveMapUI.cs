using UnityEngine;

public class MoveMapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform moveMapTextTransform;
    [SerializeField] private Transform downArrowTransform;
    private Transform targetCamera;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 5f;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main.transform;
    }

    private void Update()
    {
        if (targetCamera == null) return;

        // Handle Move Map text (X and Y free, Z locked upright)
        Vector3 moveMapDirection = targetCamera.position - moveMapTextTransform.position;
        if (moveMapDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveMapDirection);
            // Lock Z-axis rotation (prevent rolling)
            Vector3 euler = targetRotation.eulerAngles;
            euler.z = 0f;
            targetRotation = Quaternion.Euler(euler);

            moveMapTextTransform.rotation = Quaternion.Slerp(moveMapTextTransform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Handle Down Arrow (Y-axis only rotation)
        Vector3 arrowDirection = targetCamera.position - downArrowTransform.position;
        arrowDirection.y = 0; // Flatten to horizontal plane
        if (arrowDirection != Vector3.zero)
        {
            Quaternion targetArrowRotation = Quaternion.LookRotation(arrowDirection);
            downArrowTransform.rotation = Quaternion.Slerp(downArrowTransform.rotation, targetArrowRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
