using Oculus.Interaction;
using TMPro;
using UnityEngine;

public class DebugSteeringInput : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TwoHandSteeringTransformer steeringTransformer;

    private void Update()
    {
        if (steeringTransformer == null)
        {
            return;
        }

        debugText.text = $"Steering Input: {steeringTransformer.GetSteeringInput()} /n" + $"Throttle Input: {steeringTransformer.GetThrottleInput()}";
    }
}
