using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;

public class ColocationManager : NetworkBehaviour
{
    [SerializeField] private AlignmentManager alignmentManager;
    [SerializeField] private LANDiscovery lanDiscovery;
    [SerializeField] private ColocationNetworkManager networkManager;

    // The session's group ID remains constant for the session.
    private Guid _sharedAnchorGroupId;

    // This SyncVar holds the unique identifier (UUID) of the current spatial anchor as a string.
    // When updated, the hook triggers clients to re-align.
    [SyncVar(hook = nameof(OnCurrentAnchorUuidChanged))]
    public string currentAnchorUuid = "";

    // Flag indicating that colocation (anchor discovery/advertisement) succeeded.
    public static bool ColocationSuccessful { get; private set; } = false;

    #region Server Methods

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("ColocationManager: Server started, preparing colocation session.");
        StartColocationSession();

        // Subscribe to host recenter events.
        OVRManager.display.RecenteredPose += OnHostRecenteredPose;
    }

    /// <summary>
    /// Begins the colocation advertisement process.
    /// </summary>
    private void StartColocationSession()
    {
        if (isServer)
        {
            Debug.Log("ColocationManager: Starting colocation advertisement...");
            AdvertiseColocationSession();
        }
    }

    private async Task AdvertiseColocationSession()
    {
        try
        {
            string uri = lanDiscovery.GetLanServerUri();
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogError("ColocationManager: No valid LAN server URI found.");
                return;
            }

            Uri parsedUri = new Uri(uri);
            string ipAddress = parsedUri.Host;
            string advertisementMessage = $"SharedSpatialAnchorSession|{ipAddress}";
            byte[] advertisementData = Encoding.UTF8.GetBytes(advertisementMessage);

            var startResult = await OVRColocationSession.StartAdvertisementAsync(advertisementData);
            if (startResult.Success)
            {
                _sharedAnchorGroupId = startResult.Value;
                Debug.Log($"ColocationManager: Advertisement started. GroupID: {_sharedAnchorGroupId}, LAN: {ipAddress}");
                // Optionally store the group ID as a string elsewhere if needed.
                // Now create and share the initial anchor.
                await CreateAndShareInitialAnchor();
            }
            else
            {
                Debug.LogError($"ColocationManager: Advertisement failed with status: {startResult.Status}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during advertisement: {e.Message}");
        }
    }

    private async Task CreateAndShareInitialAnchor()
    {
        try
        {
            Debug.Log("ColocationManager: Creating initial alignment anchor...");
            var anchor = await CreateAnchor(Vector3.zero, Quaternion.identity);
            if (anchor == null)
            {
                Debug.LogError("ColocationManager: Failed to create initial alignment anchor.");
                return;
            }
            if (!anchor.Localized)
            {
                Debug.LogError("ColocationManager: Initial anchor is not localized. Cannot proceed with sharing.");
                return;
            }
            var saveResult = await anchor.SaveAnchorAsync();
            if (!saveResult.Success)
            {
                Debug.LogError($"ColocationManager: Failed to save initial anchor. Error: {saveResult}");
                return;
            }
            Debug.Log($"ColocationManager: Initial alignment anchor saved. UUID: {anchor.Uuid}");

            var shareResult = await OVRSpatialAnchor.ShareAsync(new List<OVRSpatialAnchor> { anchor }, _sharedAnchorGroupId);
            if (!shareResult.Success)
            {
                Debug.LogError($"ColocationManager: Failed to share initial anchor. Error: {shareResult}");
                return;
            }
            // Update the SyncVar so that clients load this anchor.
            currentAnchorUuid = anchor.Uuid.ToString();
            Debug.Log("ColocationManager: Initial alignment anchor shared and synchronized.");
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during initial anchor creation and sharing: {e.Message}");
        }
    }

    /// <summary>
    /// When the host recenters, update the shared spatial anchor without starting a new session.
    /// </summary>
    private async void OnHostRecenteredPose()
    {
        if (!isServer) return;
        Debug.Log("ColocationManager (Server): Host recentered. Updating shared anchor...");
        await UpdateSharedAnchor();
    }

    private async Task UpdateSharedAnchor()
    {
        try
        {
            // Create a new alignment anchor at the host’s current origin.
            var anchor = await CreateAnchor(Vector3.zero, Quaternion.identity);
            if (anchor == null)
            {
                Debug.LogError("ColocationManager: Failed to update alignment anchor.");
                return;
            }
            if (!anchor.Localized)
            {
                Debug.LogError("ColocationManager: Updated anchor is not localized.");
                return;
            }
            var saveResult = await anchor.SaveAnchorAsync();
            if (!saveResult.Success)
            {
                Debug.LogError($"ColocationManager: Failed to save updated anchor. Error: {saveResult}");
                return;
            }
            Debug.Log($"ColocationManager: Updated alignment anchor saved. UUID: {anchor.Uuid}");

            var shareResult = await OVRSpatialAnchor.ShareAsync(new List<OVRSpatialAnchor> { anchor }, _sharedAnchorGroupId);
            if (!shareResult.Success)
            {
                Debug.LogError($"ColocationManager: Failed to share updated anchor. Error: {shareResult}");
                return;
            }
            // Update the SyncVar so that clients are notified of the new anchor.
            currentAnchorUuid = anchor.Uuid.ToString();
            Debug.Log("ColocationManager: Shared anchor updated and synchronized.");
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during anchor update: {e.Message}");
        }
    }

    // Creates an OVRSpatialAnchor at a given position and rotation.
    private async Task<OVRSpatialAnchor> CreateAnchor(Vector3 position, Quaternion rotation)
    {
        try
        {
            var anchorGO = new GameObject("Alignment Anchor") { transform = { position = position, rotation = rotation } };
            var spatialAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
            while (!spatialAnchor.Created) await Task.Yield();
            Debug.Log($"ColocationManager: Anchor created. UUID: {spatialAnchor.Uuid}");
            return spatialAnchor;
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error creating anchor: {e.Message}");
            return null;
        }
    }

    public async void StopColocationAdvertisement()
    {
        try
        {
            var stopResult = await OVRColocationSession.StopAdvertisementAsync();
            if (stopResult.Success)
            {
                Debug.Log("ColocationManager: Stopped colocation advertisement.");
            }
            else
            {
                Debug.LogWarning($"ColocationManager: Failed to stop advertisement. Status: {stopResult.Status}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error stopping advertisement: {e.Message}");
        }
    }

    #endregion

    #region Client Methods

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("ColocationManager: Client started, searching for colocation session...");
        DiscoverNearbySession();

        // Subscribe to client recenter events.
        OVRManager.display.RecenteredPose += OnClientRecenteredPose;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // Unsubscribe from client recenter events.
        OVRManager.display.RecenteredPose -= OnClientRecenteredPose;
    }

    private async void DiscoverNearbySession()
    {
        try
        {
            OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;
            var discoveryResult = await OVRColocationSession.StartDiscoveryAsync();
            if (!discoveryResult.Success)
            {
                Debug.LogError($"ColocationManager: Discovery failed with status: {discoveryResult.Status}");
                return;
            }
            Debug.Log("ColocationManager: Discovery started. Will fall back to LAN if no session found.");
            await Task.Delay(100000); // 100-second timeout
            if (string.IsNullOrEmpty(currentAnchorUuid))
            {
                Debug.LogWarning("ColocationManager: No session discovered, switching to LAN discovery.");
                lanDiscovery.StartClientDiscoveryWithFallback();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during discovery: {e.Message}");
        }
    }

    private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
    {
        Debug.Log("ColocationManager: Colocation session discovered.");
        OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;

        string advertisementMessage = Encoding.UTF8.GetString(session.Metadata);
        string[] splitData = advertisementMessage.Split('|');
        if (splitData.Length < 2)
        {
            Debug.LogError("ColocationManager: Advertisement data is invalid.");
            return;
        }

        _sharedAnchorGroupId = session.AdvertisementUuid;
        string lanServerAddress = splitData[1];
        // On discovery, update the current anchor UUID if not already set.
        if (string.IsNullOrEmpty(currentAnchorUuid))
            currentAnchorUuid = _sharedAnchorGroupId.ToString();

        Debug.Log($"ColocationManager: Discovered session. GroupID: {_sharedAnchorGroupId}, LAN: {lanServerAddress}");
        ColocationSuccessful = true;
        RequestAnchorSync(_sharedAnchorGroupId);
        networkManager.RequestLanConnection(lanServerAddress);
    }

    /// <summary>
    /// Initiates anchor loading and alignment on the client using the current shared anchor.
    /// </summary>
    public void RequestAnchorSync(Guid groupUuid)
    {
        LoadAndAlignToAnchor(groupUuid, currentAnchorUuid);
    }

    private async void LoadAndAlignToAnchor(Guid groupUuid, string expectedAnchorUuid)
    {
        try
        {
            Debug.Log($"ColocationManager: Loading anchors for group {groupUuid}...");
            var unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();
            var loadResult = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(groupUuid, unboundAnchors);
            if (!loadResult.Success || unboundAnchors.Count == 0)
            {
                Debug.LogError($"ColocationManager: Failed to load anchors. Success: {loadResult.Success}, Count: {unboundAnchors.Count}");
                return;
            }
            foreach (var unboundAnchor in unboundAnchors)
            {
                // Compare using ToString() so that both sides are strings.
                if (unboundAnchor.Uuid.ToString() == expectedAnchorUuid)
                {
                    if (await unboundAnchor.LocalizeAsync())
                    {
                        Debug.Log($"ColocationManager: Anchor {expectedAnchorUuid} localized successfully.");
                        var anchorGO = new GameObject($"Anchor_{expectedAnchorUuid}");
                        var spatialAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
                        unboundAnchor.BindTo(spatialAnchor);
                        alignmentManager.AlignUserToAnchor(spatialAnchor);
                        return;
                    }
                }
            }
            Debug.LogWarning($"ColocationManager: Could not localize anchor with expected UUID: {expectedAnchorUuid}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during anchor loading and alignment: {e.Message}");
        }
    }

    /// <summary>
    /// Client recenter event handler. When the client recenters, re-align with the current shared anchor.
    /// </summary>
    private void OnClientRecenteredPose()
    {
        Debug.Log("ColocationManager (Client): Recentered. Realigning to current shared anchor.");
        if (!string.IsNullOrEmpty(currentAnchorUuid))
        {
            RequestAnchorSync(_sharedAnchorGroupId);
        }
    }

    #endregion

    #region SyncVar Hook

    private void OnCurrentAnchorUuidChanged(string oldValue, string newValue)
    {
        Debug.Log($"ColocationManager: Shared anchor updated from {oldValue} to {newValue}");
        // If we're on a client, trigger re-alignment.
        if (!isServer && !string.IsNullOrEmpty(newValue))
        {
            RequestAnchorSync(_sharedAnchorGroupId);
        }
    }

    #endregion
}
