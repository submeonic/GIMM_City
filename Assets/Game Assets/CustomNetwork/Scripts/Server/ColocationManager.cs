using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;

public class ColocationManager : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AlignmentManager alignmentManager;
    [SerializeField] private LANDiscovery lanDiscovery;
    [SerializeField] private ColocationNetworkManager networkManager;
    [SerializeField] private FullSceneResetManager fullSceneResetManager;
    
    [Header("Retry Settings")]
    [SerializeField] private int   maxAnchorAttempts    = 10;
    [SerializeField] private float delayBetweenAttempts = 1f;  // seconds

    // The session's group ID remains constant for the session.
    private Guid _sharedAnchorGroupId;

    // Flag indicating that colocation (anchor discovery/advertisement) succeeded.
    public static bool ColocationSuccessful { get; private set; } = false;

    #region Server Methods

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("ColocationManager: Server started, preparing colocation session.");
        StartColocationSession();
    }

    /// <summary>
    /// Begins the colocation advertisement process.
    /// </summary>
    private void StartColocationSession()
    {
        if (!isServer) return;
        Debug.Log("ColocationManager: Starting colocation advertisement...");
        _ = AdvertiseColocationSessionAsync();
    }

    private async Task AdvertiseColocationSessionAsync()
    {
        try
        {
            var uri = lanDiscovery.GetLanServerUri();
            if (string.IsNullOrEmpty(uri))
            {
                Debug.LogError("ColocationManager: No valid LAN server URI found.");
                return;
            }

            var parsedUri = new Uri(uri);
            var ipAddress = parsedUri.Host;
            var message   = $"SharedSpatialAnchorSession|{ipAddress}";
            var data      = Encoding.UTF8.GetBytes(message);

            var advResult = await OVRColocationSession.StartAdvertisementAsync(data);
            if (!advResult.Success)
            {
                Debug.LogError($"ColocationManager: Advertisement failed: {advResult.Status}");
                return;
            }

            _sharedAnchorGroupId = advResult.Value;
            Debug.Log($"ColocationManager: Advertisement started (UUID: {_sharedAnchorGroupId}), LAN: {ipAddress}");

            bool created = await TryCreateAndShareAnchorAsync();
            if (created)
            {
                ColocationSuccessful = true;
                Debug.Log("ColocationManager: Anchor creation & share succeeded.");
            }
            else
            {
                Debug.LogError("ColocationManager: Anchor creation & share ultimately failed after retries.");
                fullSceneResetManager.TriggerFullReset();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during advertisement: {e}");
        }
    }

    /// <summary>
    /// Tries up to maxAnchorAttempts to create, save, and share a spatial anchor.
    /// </summary>
    private async Task<bool> TryCreateAndShareAnchorAsync()
    {
        for (int attempt = 1; attempt <= maxAnchorAttempts; attempt++)
        {
            Debug.Log($"ColocationManager: Anchor attempt {attempt}/{maxAnchorAttempts}…");

            // 1) Create the anchor
            var anchor = await CreateAnchor(Vector3.zero, Quaternion.identity);
            if (anchor == null)
            {
                Debug.LogWarning("  • CreateAnchor returned null.");
            }
            else if (!anchor.Localized)
            {
                Debug.LogWarning("  • Anchor not localized yet.");
            }
            else
            {
                // 2) Save the anchor
                var saveResult = await anchor.SaveAnchorAsync();
                if (saveResult.Success)
                {
                    Debug.Log($"  • Saved anchor {anchor.Uuid}");

                    // 3) Share the anchor
                    var shareResult = await OVRSpatialAnchor.ShareAsync(
                        new List<OVRSpatialAnchor> { anchor },
                        _sharedAnchorGroupId);

                    if (shareResult.Success)
                    {
                        Debug.Log("  • Share succeeded!");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"  • Share failed: {shareResult.Status}");
                        
                    }
                }
                else
                {
                    Debug.LogWarning($"  • Save failed: {saveResult.Status}");
                }
            }

            // if we haven't returned success, wait then retry
            if (attempt < maxAnchorAttempts)
            {
                Debug.Log($"  → Retrying in {delayBetweenAttempts} seconds…");
                await Task.Delay(TimeSpan.FromSeconds(delayBetweenAttempts));
            }
        }

        return false;
    }

    /// <summary>
    /// Creates an OVRSpatialAnchor at the given pose.
    /// </summary>
    private async Task<OVRSpatialAnchor> CreateAnchor(Vector3 position, Quaternion rotation)
    {
        try
        {
            var go = new GameObject("Alignment Anchor");
            go.transform.SetPositionAndRotation(position, rotation);
            var spatialAnchor = go.AddComponent<OVRSpatialAnchor>();
            while (!spatialAnchor.Created)
                await Task.Yield();

            Debug.Log($"ColocationManager: Anchor created (UUID: {spatialAnchor.Uuid})");
            return spatialAnchor;
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error creating anchor: {e}");
            return null;
        }
    }

    public async void StopColocationAdvertisement()
    {
        try
        {
            var stopResult = await OVRColocationSession.StopAdvertisementAsync();
            if (stopResult.Success)
                Debug.Log("ColocationManager: Stopped colocation advertisement.");
            else
                Debug.LogWarning($"ColocationManager: Failed to stop advertisement: {stopResult.Status}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error stopping advertisement: {e}");
        }
    }

    #endregion
    
    #region Client Methods

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("ColocationManager: Client started, searching for colocation session...");
        DiscoverNearbySession();
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
            Debug.Log("ColocationManager: Discovery started successfully.");
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
        Debug.Log($"ColocationManager: Discovered session. UUID: {_sharedAnchorGroupId}, LAN: {lanServerAddress}");
        LoadAndAlignToAnchor(_sharedAnchorGroupId);
        networkManager.RequestLanConnection(lanServerAddress);
    }

    private async void LoadAndAlignToAnchor(Guid groupUuid)
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
                if (await unboundAnchor.LocalizeAsync())
                {
                    Debug.Log($"ColocationManager: Anchor localized successfully. UUID: {unboundAnchor.Uuid}");
                    var anchorGO = new GameObject($"Anchor_{unboundAnchor.Uuid}");
                    var spatialAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
                    unboundAnchor.BindTo(spatialAnchor);
                    alignmentManager.AlignUserToAnchor(spatialAnchor);
                    return;
                }
                
                Debug.LogWarning($"ColocationManager: Failed to localize anchor: {unboundAnchor.Uuid}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during anchor loading and alignment: {e.Message}");
        }
    }
    
    public async void StopColocationDiscovery()
    {
        try
        {
            var stopResult = await OVRColocationSession.StopDiscoveryAsync();
            if (stopResult.Success)
            {
                Debug.Log("ColocationManager: Stopped discovery.");
            }
            else
            {
                Debug.LogWarning($"ColocationManager: Stop discovery failed with status: {stopResult.Status}");
            }
            OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error stopping discovery: {e.Message}");
        }
    }
    
    #endregion
}
