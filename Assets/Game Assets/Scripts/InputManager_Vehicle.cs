using Oculus.Interaction;
using UnityEngine;
public class InputManager_Vehicle : MonoBehaviour
{
    [SerializeField] private ArcadeVP.ArcadeVehicleController arcadeVehicleController;
    [SerializeField] private TwoSteeringNetTransformer twoHandSteeringTransformer;

    [HideInInspector] public float steeringInput;
    [HideInInspector] public float accelerationInput;
    [HideInInspector] public float brakeInput;
    private void Update()
    {
        //steeringInput = twoHandSteeringTransformer.GetSteeringInput();
        //float throttle = twoHandSteeringTransformer.GetThrottleInput();

        // Forward acceleration when throttle is positive
        //accelerationInput = throttle;

        // Braking when throttle is negative
        //brakeInput = 0;

        //arcadeVehicleController.ProvideInputs(steeringInput, accelerationInput, brakeInput);
    }
}
