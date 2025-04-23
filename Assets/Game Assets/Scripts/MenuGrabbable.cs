using System.Linq;
using System.Collections;
using Mirror;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class MenuGrabbable : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float      spawnCooldown = 1.5f;
    [SerializeField] private GameObject menuModel;

    [Header("Local References")]
    [SerializeField] private SteeringInputManager vehicleInputManager;
    [SerializeField] private GameObject           menuItemGO;
    [SerializeField] private HandGrabInteractable handGrabInteractable;
    [SerializeField] private Grabbable            grabbable;

    private HandGrabInteractor   localInteractor;      // hand that grabbed the tile
    private HandGrabInteractable spawnedCarGrab;       // interactable on spawned car

    private bool   hasSpawned;
    private GameObject spawnedCar;                     // server‑side handle

    /* ───────────────────────────────────────────── */

    private void Awake()
    {
        grabbable.WhenPointerEventRaised += OnMenuSelect;
    }

    private void OnDestroy()
    {
        grabbable.WhenPointerEventRaised -= OnMenuSelect;
    }

    /* ───────── menu grab → spawn request ───────── */

    private void OnMenuSelect(PointerEvent evt)
    {
        if (evt.Type != PointerEventType.Select) return;
        if (!isOwned || hasSpawned || prefabToSpawn == null) return;

        if (grabbable.SelectingPointsCount == 0 ||
            !handGrabInteractable.SelectingInteractors.Any()) return;

        localInteractor = handGrabInteractable.SelectingInteractors.First();

        menuItemGO.SetActive(false);
        localInteractor.ForceRelease();

        hasSpawned = true;

        CmdSpawnOrReplace(
            prefabToSpawn.name,
            menuModel.transform.position + new Vector3(0, 0.05f, 0),
            menuModel.transform.rotation.normalized
        );
    }

    /* ───────── server spawn / replace ───────── */

    [Command]
    private void CmdSpawnOrReplace(string prefabName, Vector3 pos, Quaternion rot)
    {
        if (spawnedCar) NetworkServer.Destroy(spawnedCar);

        var prefab = NetworkManager.singleton.spawnPrefabs.FirstOrDefault(p => p.name == prefabName);
        if (prefab == null)
        {
            Debug.LogError($"MenuGrabbable: prefab '{prefabName}' not registered.");
            return;
        }

        spawnedCar = Instantiate(prefab, pos, rot);
        NetworkServer.Spawn(spawnedCar);
        
        spawnedCar.GetComponent<ArcadeVehicleController>()?.ServerSetDriver(connectionToClient.identity);

        uint netId = spawnedCar.GetComponent<NetworkIdentity>().netId;
        TargetAssignCar(connectionToClient, netId);
    }
    
    /* ───────── client auto‑grab spawned car ───────── */

    [TargetRpc]
    private void TargetAssignCar(NetworkConnection _, uint netId)
    {
        vehicleInputManager.AssignCar(netId);
        StartCoroutine(WaitAndForceGrab(netId));
    }

    private IEnumerator WaitAndForceGrab(uint netId)
    {
        GameObject carGO;
        NetworkIdentity identity;

        while (!NetworkClient.spawned.TryGetValue(netId, out identity))
            yield return null;

        carGO = identity.gameObject;
        spawnedCarGrab = carGO.GetComponent<HandGrabInteractable>();

        if (localInteractor == null)
        {
            Debug.LogWarning("[MenuGrabbable] LocalInteractor was null during WaitAndForceGrab. Cannot force grab.");
            yield break;
        }
        
        float timer = 0f;
        while (!localInteractor.CanSelect(spawnedCarGrab))
        {
            if (timer >= 5)
            {
                Debug.LogWarning("[MenuGrabbable] Timed out waiting for CanSelect.");
                yield break; // Exit the coroutine early
            }
    
            timer += Time.deltaTime;
            yield return null;
        }

        // Step 1: Force grab without manual release
        localInteractor.ForceSelect(spawnedCarGrab, allowManualRelease: false);
        Debug.Log("[MenuGrabbable] Force-selected with manual release disabled.");
        
        // Wait 2 seconds
        yield return new WaitForSeconds(4f);
        
        if (spawnedCarGrab == null)
        {
            Debug.LogWarning("[MenuGrabbable] Cannot re-select — interactable was destroyed or missing.");
            yield break;
        }
        
        // Step 2: Re-force grab with manual release enabled
        localInteractor.ForceSelect(spawnedCarGrab, allowManualRelease: true);
        Debug.Log("[MenuGrabbable] Re-selecting with manual release enabled.");

        yield return new WaitForSeconds(2f);
        menuItemGO.SetActive(true);
        localInteractor   = null;
        spawnedCarGrab    = null;
        hasSpawned = false;
    }
}
