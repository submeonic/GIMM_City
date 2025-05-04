using Mirror;
using Mirror.Discovery;
using System;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runtime-agnostic LAN discovery component.
/// • Hosts call <see cref="StartHostAdvertisement"/> to broadcast their URI.
/// • Clients call <see cref="StartDiscovery"/>; when a host responds
///   <see cref="onServerFound"/> fires with a full "scheme://ip:port" string.
/// The component is designed to run in parallel with an anchor-discovery
/// stage (OVR or similar).  Connection to the host should be deferred until
/// the anchor workflow has completed.
/// </summary>
[AddComponentMenu("Networking/LAN Discovery")]
public class LANDiscovery
       : NetworkDiscoveryBase<LANDiscovery.LanRequest, LANDiscovery.LanResponse>
{
    /* ──────────────────────────  public API  ───────────────────────────── */

    /// <summary>Invoked on the *client* once the first host reply is received.
    /// Argument format: "<scheme>://<ip>:<port>", e.g. "kcp://192.168.0.42:7777".</summary>
    public UnityEvent<string> onServerFound = new();

    /// <summary>Server-side: start broadcasting the transport’s URI.</summary>
    public void StartHostAdvertisement() => AdvertiseServer();

    /// <summary>Client-side: begin searching the LAN (safe to call repeatedly).</summary>
    public new void StartDiscovery()
    {
        if (isSearching) return;   // already running

        base.StartDiscovery();     // returns void
        isSearching = true;
    }

    /// <summary>Stop searching/broadcasting and clear state.</summary>
    public new void StopDiscovery()
    {
        Debug.Log("LANDiscovery: Stopping LAN discovery…");
        base.StopDiscovery();
        isSearching = false;
    }

    /// <summary>
    /// Convenience helper for the *server* to embed its URI in an anchor
    /// advertisement or similar metadata packet.
    /// </summary>
    public string GetLanServerUri()
    {
        ColocationNetworkManager nm = GetComponent<ColocationNetworkManager>();
        return nm?.transport?.ServerUri()?.ToString() ?? string.Empty;
    }

    /* ───────────────────────  NetworkDiscoveryBase  ────────────────────── */

    [Serializable] public class LanRequest  : NetworkMessage { }
    [Serializable] public class LanResponse : NetworkMessage
    {
        public string serverUri;   // populated by the host
    }

    /// <summary>Server replies to every discovery request with its URI.</summary>
    protected override LanResponse ProcessRequest(LanRequest req, IPEndPoint ep)
    {
        ColocationNetworkManager nm = GetComponent<ColocationNetworkManager>();
        if (nm == null || nm.transport == null)
        {
            Debug.LogError("LANDiscovery: NetworkManager / transport missing.");
            return new LanResponse { serverUri = string.Empty };
        }

        return new LanResponse
        {
            serverUri = nm.transport.ServerUri().ToString()
        };
    }

    /// <summary>Client handles the first response, forwards it, then stops.</summary>
    protected override void ProcessResponse(LanResponse res, IPEndPoint ep)
    {
        if (!isSearching) return;

        // Prefer the URI explicitly sent by the host; if absent, build one
        // from the sender’s IP plus the active transport’s port.
        string uriStr = string.IsNullOrWhiteSpace(res.serverUri)
                      ? $"kcp://{ep.Address}:{GetDefaultPort()}"
                      : res.serverUri;

        Debug.Log($"LANDiscovery: server reply → {uriStr}");
        onServerFound.Invoke(uriStr);
        StopDiscovery();   // first hit wins
    }

    /* ───────────────────────────  helpers  ─────────────────────────────── */

    static ushort GetDefaultPort()
    {
        Transport t = Transport.active;
        return t switch
        {
            kcp2k.KcpTransport   kcp => kcp.Port,
            TelepathyTransport   tel => tel.Port,
            // add other transport types here if needed
            _                       => 7777
        };
    }

    bool isSearching = false;
}
