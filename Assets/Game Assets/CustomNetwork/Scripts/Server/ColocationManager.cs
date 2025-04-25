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

            var startAdvertisementResult = await OVRColocationSession.StartAdvertisementAsync(advertisementData);
            if (startAdvertisementResult.Success)
            {
                _sharedAnchorGroupId = startAdvertisementResult.Value;
                Debug.Log($"ColocationManager: Advertisement started. UUID: {_sharedAnchorGroupId}, LAN: {ipAddress}");
                await CreateAndShareAlignmentAnchor();
            }
            else
            {
                Debug.LogError($"ColocationManager: Advertisement failed with status: {startAdvertisementResult.Status}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during advertisement: {e.Message}");
        }
    }

    private async Task CreateAndShareAlignmentAnchor()
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
            
            Debug.Log($"ColocationManager: Alignment anchor shared successfull. GroupUUID: {_sharedAnchorGroupId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ColocationManager: Error during initial anchor creation and sharing: {e.Message}");
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
