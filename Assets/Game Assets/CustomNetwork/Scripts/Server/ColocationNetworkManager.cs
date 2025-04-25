using System.Collections;
using Mirror;
using UnityEngine;

public class ColocationNetworkManager : NetworkManager
{
    [Header("Colocation Settings")]
    [SerializeField] private LANDiscovery lanDiscovery;
    [SerializeField] private ColocationManager colocationManager;

    #region Server Overrides

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("ColocationNetworkManager: Server started.");
        lanDiscovery.StartHostAdvertisement();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        Debug.Log($"ColocationNetworkManager: Player added. Total players: {numPlayers}/{maxConnections}");
        if (numPlayers >= maxConnections)
        {
            Debug.Log("ColocationNetworkManager: Max connections reached, stopping colocation advertisement.");
            colocationManager.StopColocationAdvertisement();
        }
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("ColocationNetworkManager: Client connected to server.");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log("ColocationNetworkManager: Client disconnected from server.");
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("ColocationNetworkManager: Server stopped.");
    }

    #endregion

    #region Client Overrides

    // Note: In the base NetworkManager these methods have no parameters.
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("ColocationNetworkManager: Client started.");
        colocationManager.OnStartClient();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("ColocationNetworkManager: Client connected.");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("ColocationNetworkManager: Client disconnected.");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("ColocationNetworkManager: Client stopped.");
    }

    #endregion

    #region LAN Fallback and Connection

    /// <summary>
    /// Initiates a LAN connection request.
    /// </summary>
    public void RequestLanConnection(string serverAddress)
    {
        if (NetworkClient.active)
        {
            Debug.Log("ColocationNetworkManager: Client already connected.");
            return;
        }
        Debug.Log("ColocationNetworkManager: Requesting LAN connection...");
        StartCoroutine(EnsureAlignmentBeforeConnecting(serverAddress));
    }

    private IEnumerator EnsureAlignmentBeforeConnecting(string serverAddress)
    {
        Debug.Log("ColocationNetworkManager: Waiting for spatial anchor alignment...");
        // Wait until the colocation manager flags success.
        while (!ColocationManager.ColocationSuccessful)
        {
            yield return null;
        }
        Debug.Log("ColocationNetworkManager: Alignment complete, connecting to LAN server...");
        ProcessRequestLanConnection(serverAddress);
    }

    private void ProcessRequestLanConnection(string serverAddress)
    {
        Debug.Log($"ColocationNetworkManager: Setting network address to {serverAddress}");
        networkAddress = serverAddress;
        StartClient();
    }

    #endregion
}
