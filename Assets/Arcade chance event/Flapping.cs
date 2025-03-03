using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class Flapping : MonoBehaviour
{
    [SerializeField] private float velocity = 3.5f;
    [SerializeField] private float rotationSpeed = 5f;

    private Rigidbody rb;
    public float gravityScale = 0.1f;
    private static float globalGravity = -9.81f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    // Update is called once per frame
    private void Update()
    {
        Vector3 gravity = globalGravity * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            rb.linearVelocity = Vector3.up * velocity;
        }
        else
        {

        }
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.z, rb.linearVelocity.y * rotationSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameManager.instance.GameOver();
    }
}
