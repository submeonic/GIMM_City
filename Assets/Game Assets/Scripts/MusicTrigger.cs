using UnityEngine;
public class MusicTrigger : MonoBehaviour
{
    [SerializeField] private MusicController musicController;
    [SerializeField] private MusicController.MusicEnergyLevel musicEnergyLevel;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            musicController.currentLevel = musicEnergyLevel;
        }
    }
}
