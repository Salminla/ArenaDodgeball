using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Button buttonHost;
    public Button buttonServer;
    public Button buttonClient;
    public Button buttonDisconnect;

    public Text textInfo;

    private UNetTransport transport;
    
    void Start()
    {
        transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UNetTransport;
        
        buttonHost.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            SetInfoText(true);
        });
        buttonServer.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
            SetInfoText(true);
        });
        buttonClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            SetInfoText(true);
        });
        buttonDisconnect.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            SetInfoText(true);
        });
    }

    void SetInfoText(bool show)
    {
        textInfo.text = GetInfoText(show);
    }
    string GetInfoText(bool show)
    {
        string hostServer = NetworkManager.Singleton.IsHost ? "Host" : "Server";
        string info = "";

        if (show)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
                info = "Connected to: " + transport.ConnectAddress + ":" + transport.ConnectPort;
            else if (NetworkManager.Singleton.IsServer)
                info = hostServer + " running on port " + transport.ConnectPort;
            else
                info = "Disconnected";
        }

        return info;
    }
}
