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

    private void Awake()
    {
        grabbable.WhenPointerEventRaised += OnMenuSelect;
    }

    private void OnDestroy()
    {
        grabbable.WhenPointerEventRaised -= OnMenuSelect;
    }

    private void OnMenuSelect(PointerEvent evt)
    {
        if (evt.Type != PointerEventType.Select) return;
        if (!isOwned || prefabToSpawn == null) return;
        if (grabbable.SelectingPointsCount == 0 || !handGrabInteractable.SelectingInteractors.Any()) return;

        localInteractor = handGrabInteractable.SelectingInteractors.First();
        if (localInteractor == null)
        {
            Debug.LogWarning("[MenuGrabbable] LocalInteractor was null.");
            return;
        }

        localInteractor.ForceRelease();
        menuItemGO.SetActive(false);

        // Send spawn request to server
        CmdSpawnOrReplace(
            prefabToSpawn.name,
            menuModel.transform.position + new Vector3(0, 0.05f, 0),
            menuModel.transform.rotation.normalized
        );
    }

    [Command]
    private void CmdSpawnOrReplace(string prefabName, Vector3 pos, Quaternion rot, NetworkConnectionToClient sender = null)
    {
        var prefab = NetworkManager.singleton.spawnPrefabs.FirstOrDefault(p => p.name == prefabName);
        if (prefab == null)
        {
            Debug.LogError($"MenuGrabbable: prefab '{prefabName}' not registered.");
            return;
        }

        GameObject car = Instantiate(prefab, pos, rot);
        NetworkServer.Spawn(car);

        car.GetComponent<ArcadeVehicleController>()?.ServerSetDriver(sender.identity);

        uint netId = car.GetComponent<NetworkIdentity>().netId;
        TargetAssignCar(sender, netId);
    }

    [TargetRpc]
    private void TargetAssignCar(NetworkConnection target, uint netId)
    {
        vehicleInputManager.AssignCar(netId);
        StartCoroutine(WaitAndForceGrab(netId));
    }

    private IEnumerator WaitAndForceGrab(uint netId)
    {
        NetworkIdentity identity;

        while (!NetworkClient.spawned.TryGetValue(netId, out identity))
            yield return null;

        GameObject carGO = identity.gameObject;
        spawnedCarGrab = carGO.GetComponentInChildren<HandGrabInteractable>();

        if (spawnedCarGrab == null)
            Debug.LogWarning("[MenuGrabbable] spawnedCarGrab was null!");

        if (localInteractor == null)
            Debug.LogWarning("[MenuGrabbable] localInteractor was null!");

        float timer = 0f;
        while (!localInteractor.CanSelect(spawnedCarGrab))
        {
            if (timer >= 5f)
            {
                Debug.LogWarning("[MenuGrabbable] Timed out waiting for CanSelect.");
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Phase 1: Temporary lock
        localInteractor.ForceSelect(spawnedCarGrab, allowManualRelease: false);
        Debug.Log("[MenuGrabbable] Force-selected with manual release disabled.");

        yield return new WaitForSeconds(2f);

        // Phase 2: Hand control
        localInteractor.ForceSelect(spawnedCarGrab, allowManualRelease: true);
        Debug.Log("[MenuGrabbable] Re-selecting with manual release enabled.");

        // Final cleanup
        yield return new WaitForSeconds(spawnCooldown);
        menuItemGO.SetActive(true);
        localInteractor = null;
        spawnedCarGrab = null;
    }
}
