using System;
using UnityEngine;
using FishNet.Managing;

public class BootstrapNet : MonoBehaviour
{
    [SerializeField] private NetworkManager nm;
    [SerializeField] private string defaultAddress = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 53353;

    void Awake()
    {
        if (nm == null) nm = FindObjectOfType<NetworkManager>();

        // Set address/port on transport (Tugboat example)
        var tugboat = nm.TransportManager?.Transport as FishNet.Transporting.Tugboat.Tugboat;
        if (tugboat != null)
        {
            tugboat.SetClientAddress(defaultAddress);
            tugboat.SetPort(defaultPort);
        }

        // Read command-line args so ParrelSync clones can decide mode.
        var args = Environment.GetCommandLineArgs();
        bool wantServer = HasArg(args, "-server");
        bool wantClient = HasArg(args, "-client");

        string addr = GetArgValue(args, "-address") ?? defaultAddress;
        if (tugboat != null) tugboat.SetClientAddress(addr);

        if (wantServer)
            nm.ServerManager.StartConnection();

        if (wantClient)
            nm.ClientManager.StartConnection();
    }

    static bool HasArg(string[] args, string flag)
    {
        for (int i = 0; i < args.Length; i++)
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    static string GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}
