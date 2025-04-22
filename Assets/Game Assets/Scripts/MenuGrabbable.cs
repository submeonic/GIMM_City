using System.Linq;
using System.Collections;
using Mirror;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

[RequireComponent(typeof(HandGrabInteractable), typeof(Grabbable))]
public class MenuGrabbable : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float      spawnCooldown = 1.5f;
    [SerializeField] private GameObject menuModel;

    [Header("Local References")]
    [SerializeField] private SteeringInputManager vehicleInputManager;

    private HandGrabInteractable handGrabInteractable;
    private Grabbable            grabbable;

    private HandGrabInteractor   localInteractor;      // hand that grabbed the tile
    private HandGrabInteractable spawnedCarGrab;       // interactable on spawned car

    private bool   hasSpawned;
    private GameObject spawnedCar;                     // server‑side handle

    /* ───────────────────────────────────────────── */

    private void Awake()
    {
        handGrabInteractable = GetComponent<HandGrabInteractable>();
        grabbable            = GetComponent<Grabbable>();
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

        handGrabInteractable.enabled = false;          // disable tile
        localInteractor.Unselect();                    // drop tile

        hasSpawned = true;
        StartCoroutine(ResetSpawnCooldown());

        CmdSpawnOrReplace(
            prefabToSpawn.name,
            menuModel.transform.position,
            menuModel.transform.rotation
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

        var carGO = NetworkClient.spawned[netId].gameObject;
        spawnedCarGrab = carGO.GetComponent<HandGrabInteractable>();
        if (localInteractor == null || spawnedCarGrab == null)
        {
            handGrabInteractable.enabled = true;
            return;
        }

        // auto‑grab (user can pinch‑open to drop)
        localInteractor.ForceSelect(spawnedCarGrab, allowManualRelease: true);

        // wait until the car is released, then re‑enable menu
        StartCoroutine(WaitUntilCarReleased());
    }

    private IEnumerator WaitUntilCarReleased()
    {
        // Wait while any hand is selecting the car interactable
        yield return new WaitUntil(() => !spawnedCarGrab.SelectingInteractors.Any());

        handGrabInteractable.enabled = true; // menu usable again
        localInteractor   = null;
        spawnedCarGrab    = null;
    }

    /* ───────── cooldown flag ───────── */

    private IEnumerator ResetSpawnCooldown()
    {
        yield return new WaitForSeconds(spawnCooldown);
        hasSpawned = false;
    }
}
