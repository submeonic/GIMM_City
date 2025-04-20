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
    [SerializeField] private float spawnCooldown = 1.5f;
    [SerializeField] private GameObject menuModel;

    [Header("Local References")]
    [SerializeField] private SteeringInputManager vehicleInputManager;

    // ──────────────────────────────────────────────────────────
    private HandGrabInteractable menuInteractable;
    private Grabbable            menuGrabbable;
    private HandGrabInteractor   localInteractor;   // hand that grabbed the menu
    private HandGrabInteractable spawnedCarInteractable;
    private bool                 hasSpawned;

    // server‑only handle so we can replace the old car
    private GameObject           spawnedCarServer;

    private void Awake()
    {
        menuInteractable = GetComponent<HandGrabInteractable>();
        menuGrabbable    = GetComponent<Grabbable>();
        menuGrabbable.WhenPointerEventRaised += OnMenuPointerEvent;
    }

    private void OnDestroy()
    {
        menuGrabbable.WhenPointerEventRaised -= OnMenuPointerEvent;
    }

    // ──────────────────────────────────────────────────────────
    /// <summary>Called every time the menu receives a pointer event.</summary>
    private void OnMenuPointerEvent(PointerEvent e)
    {
        // 1) Only care about an actual grab
        if (e.Type != PointerEventType.Select)          return;
        if (!isOwned || hasSpawned || prefabToSpawn==null) return;

        // 2) Which hand grabbed?
        localInteractor = e.Data as HandGrabInteractor;
        if (localInteractor == null) return;            // safety – shouldn't happen

        // 3) Drop the menu, then hide it
        localInteractor.ForceRelease();                 // clean release
        menuInteractable.enabled = false;

        // 4) Block re‑entry until cooldown
        hasSpawned = true;
        StartCoroutine(ResetSpawnCooldown());

        // 5) Ask server to (re)spawn the networked car
        CmdSpawnOrReplace(prefabToSpawn.name,
                          menuModel.transform.position,
                          menuModel.transform.rotation);
    }

    // ──────────────────────────────────────────────────────────
    [Command]
    private void CmdSpawnOrReplace(string prefabName, Vector3 pos, Quaternion rot)
    {
        if (spawnedCarServer != null)
        {
            NetworkServer.Destroy(spawnedCarServer);
            spawnedCarServer = null;
        }

        var prefab = NetworkManager.singleton.spawnPrefabs
                        .FirstOrDefault(p => p.name == prefabName);
        if (prefab == null)
        {
            Debug.LogError($"MenuGrabbable: '{prefabName}' not in spawn list.");
            return;
        }

        spawnedCarServer = Instantiate(prefab, pos, rot);
        NetworkServer.Spawn(spawnedCarServer, connectionToClient);

        uint netId = spawnedCarServer.GetComponent<NetworkIdentity>().netId;
        TargetAssignCar(connectionToClient, netId);
    }

    // ──────────────────────────────────────────────────────────
    [TargetRpc]
    private void TargetAssignCar(NetworkConnection _, uint carNetId)
    {
        // 1) Hand the SteeringInputManager its new target
        vehicleInputManager.AssignCar(carNetId);

        // 2) Grab the local clone
        if (!NetworkClient.spawned.TryGetValue(carNetId, out var ni))
        {
            Debug.LogError("Client has not yet spawned the car!");
            return;
        }

        spawnedCarInteractable = ni.gameObject.GetComponent<HandGrabInteractable>();
        if (localInteractor == null || spawnedCarInteractable == null)
        {
            menuInteractable.enabled = true;            // fail‑safe: re‑enable menu
            return;
        }

        // 3) Listen for the drop so we can re‑enable the menu later
        spawnedCarInteractable.WhenPointerEventRaised += OnCarPointerEvent;

        // 4) Force‑grab the car
        localInteractor.ForceSelect(spawnedCarInteractable, true); // allow pinch‑open to drop
    }

    // ──────────────────────────────────────────────────────────
    private void OnCarPointerEvent(PointerEvent e)
    {
        if (e.Type != PointerEventType.Unselect) return;

        // stop listening
        spawnedCarInteractable.WhenPointerEventRaised -= OnCarPointerEvent;

        // make the menu grabbable again
        menuInteractable.enabled = true;
        localInteractor          = null;
    }

    private IEnumerator ResetSpawnCooldown()
    {
        yield return new WaitForSeconds(spawnCooldown);
        hasSpawned = false;
    }
}
