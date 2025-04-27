using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class FullSceneResetManager : MonoBehaviour
{   
    private ColocationManager colocationManager;
    private bool isResetting = false;

    /// <summary>
    /// Call this method to begin a full reset of the scene.
    /// </summary>
    public void TriggerFullReset()
    {
        if (!isResetting)
        {
            StartCoroutine(ResetAndReload());
        }
    }

    private IEnumerator ResetAndReload()
    {
        isResetting = true;
        Debug.Log("[FullSceneResetManager] Starting full scene reset...");
        
        colocationManager = LocalReferenceManager.Instance.ColocationManager;
        
        // Stop Colocation Advertisement/Discovery
        if (colocationManager != null)
        {
            Debug.Log("[FullSceneResetManager] Stopping colocation processes...");
            colocationManager.StopColocationAdvertisement();
            colocationManager.StopColocationDiscovery();
            yield return new WaitForSeconds(0.1f); // give async task a moment
        }

        // 2. Stop Networking (Host, Server, or Client)
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            Debug.Log("[FullSceneResetManager] Stopping host...");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkServer.active)
        {
            Debug.Log("[FullSceneResetManager] Stopping server...");
            NetworkManager.singleton.StopServer();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("[FullSceneResetManager] Stopping client...");
            NetworkManager.singleton.StopClient();
        }

        yield return new WaitForSeconds(0.5f); // allow networking to shut down cleanly

        //Reload Scene
        Debug.Log("[FullSceneResetManager] Reloading scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
