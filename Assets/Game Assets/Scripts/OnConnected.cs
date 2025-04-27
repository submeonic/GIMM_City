using Mirror;
using UnityEngine;

public class OnConnected : NetworkBehaviour
{
    [SerializeField] private GameObject staticMap;
    [SerializeField] private MusicController musicController;
    public override void OnStartClient()
    {
        base.OnStartClient();
        staticMap.SetActive(true);
        musicController.currentLevel = MusicController.MusicEnergyLevel.Low;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        staticMap.SetActive(true);
        musicController.currentLevel = MusicController.MusicEnergyLevel.Low;
    }
}
