using Mirror;
using UnityEngine;
using TMPro;

public class SteeringInputManager : NetworkBehaviour
{
    [SyncVar] private float steeringInput;
    [SyncVar] private float accelerationInput;
    [SyncVar] private float brakeInput;
    //temp
    [SerializeField] private TextMeshProUGUI debugText;

    private ArcadeVP.ArcadeVehicleController carController = null;

    public void SetInput(float _steeringInput, float _throttleInput)
    {
        //Temporary for testing
        if (carController == null)
        {
            SetActiveCarController(FindObjectOfType<ArcadeVP.ArcadeVehicleController>());
            Debug.Log(carController.name);
        }
        
        if (!isOwned || carController == null)
            return;
        
        steeringInput = _steeringInput;
        accelerationInput = _throttleInput;
        brakeInput = 0;

        if (_steeringInput == 0 && _throttleInput == 0)
        {
            brakeInput = 1;
        }

        // Local input affects the linked car
        carController.ProvideInputs(steeringInput, accelerationInput, brakeInput);
        debugText.text = $"Steering Input: {steeringInput} /n" + $"Throttle Input: {accelerationInput}";
    }

    public void SetActiveCarController(ArcadeVP.ArcadeVehicleController _carController)
    {
        carController = _carController;
    }

    [TargetRpc]
    public void TargetAssignCar(NetworkConnection target, NetworkIdentity carNetIdentity)
    {
        ArcadeVP.ArcadeVehicleController car = carNetIdentity.GetComponent<ArcadeVP.ArcadeVehicleController>();
        SetActiveCarController(car);
    }
}