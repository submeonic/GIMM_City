using Oculus.Interaction;
using UnityEngine;

public class SteeringWheelUtilities : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [Tooltip("The Grabbable component this script listens to. If not assigned, it will attempt to find one on the same GameObject.")]
    [SerializeField] private Grabbable grabbable;

    [Tooltip("The GameObject that represents the tracking space. If not assigned, it will attempt to find one in the scene.")]
    [SerializeField] private GameObject trackingSpace;

    [Tooltip("The Poke Menu GameObject to toggle based on grab state.")]
    [SerializeField] private GameObject pokeMenu;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the component by subscribing to the Grabbable's grab state change event.
    /// </summary>
    void Start()
    {
        if (trackingSpace == null)
        {
            trackingSpace = GameObject.Find("TrackingSpace");
        }

        // Subscribe to the grab state event if available
        if (grabbable != null)
        {
           // grabbable.OnGrabStateChanged += HandleGrabStateChanged;
        }
        else
        {
            Debug.LogError("Grabbable component not found on the GameObject!");
        }
    }

    /// <summary>
    /// Ensures the event is unsubscribed when the object is destroyed to avoid memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        if (grabbable != null)
        {
         //   grabbable.OnGrabStateChanged -= HandleGrabStateChanged;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles changes in the Grabbable's grab state and updates the visibility of the Poke Menu.
    /// </summary>
    /// <param name="grabPoints">The number of hands currently grabbing the Grabbable.</param>
    private void HandleGrabStateChanged(int grabPoints)
    {
        if (grabPoints == 0)
        {
            if (trackingSpace != null && transform.parent != trackingSpace)
            {
                // No hand grabbing: set parent to null
                transform.SetParent(null);
            }
            // No hands grabbing: Hide the Poke Menu
            pokeMenu.SetActive(false);
        }
        else if (grabPoints == 1)
        {
            if (trackingSpace != null && transform.parent != trackingSpace)
            {
                // One hand grabbing: Move the object to the tracking space
                transform.SetParent(trackingSpace.transform);
            }
            // One hand grabbing: Show the Poke Menu
            pokeMenu.SetActive(true);
        }
        else if (grabPoints == 2)
        {
            if (trackingSpace != null && transform.parent != trackingSpace)
            {
                // Two hands grabbing: Move the object to the tracking space
                transform.SetParent(trackingSpace.transform);
            }
            // Two hands grabbing: Hide the Poke Menu
            pokeMenu.SetActive(false);
        }
    }

    #endregion

    private void OnDisable()
    {
        transform.SetParent(null);
    }
}
