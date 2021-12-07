using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : NetworkBehaviour
{
    public static GameController Instance { private set; get; }
    
    public Button buttonHost;
    public Button buttonClient;

    public TMP_Dropdown DropDownRegions;
    public TMP_Text InputFieldJoinCode;
    
    public LobbyControl LobbyControlRef;

    public RectTransform StartContainer;
    public RectTransform LobbyContainer;
    public RectTransform DisconnectContainer;
    public RectTransform RoundOverContainer;

    public TMP_Text TextResults;
    public Text textInfo;
    
    public List<Transform> spawnPoints;
    private int spawnIndex;
    
    public NetworkObjectPool objectPool;
    
    public static NetworkVariable<bool> GameStarted = new NetworkVariable<bool>(false);
    
    private UnityTransport transport;
    
    void Start()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
        
        //Huom! NetworkManager.Singleton saa arvon OnEnabledissa,
        //joka tapahtuu Awaken jälkeen, joten kannattaa huomata,
        //että kutsuu sitä vasta Startissa!
        NetworkManager.Singleton.ConnectionApprovalCallback += ConnectionApproval;

        
        
        transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        
        RoundOverContainer.gameObject.SetActive(false);

        //Käynnistä host (joka siis server ja client), server tai client.
        buttonHost.onClick.AddListener(() =>
        {
            //NetworkManager.Singleton.StartHost();
            InitiateHosting(DropDownRegions.options[DropDownRegions.value].text);
            ShowStartContainer(false);
            //LobbyControlRef.Init();
            
        });

        buttonClient.onClick.AddListener(() =>
        {
            //NetworkManager.Singleton.StartClient();
            InitiateJoiningAsClient();
            //LobbyControlRef.Init();
            ShowStartContainer(false);
        });

        ShowStartContainer(true);
        
        GetRelayRegions();
    }
    async private void GetRelayRegions()
    {
        Task<List<Region>> regionsTask = RelaySetup.GetRegions();

        try
        {
            await regionsTask;
        }
        catch (AggregateException ae)
        {
            ae.InnerExceptions.ToList().ForEach(e => Debug.LogError(e.Message + "\n Stack trace: " + e.StackTrace));
        }

        //ListRegionsAsync palauttaa listan Region-objekteja:
        List<Region> regions = regionsTask.Result;

        //Regionin Id on string id, joka voidaan antaa parametrina CreateAllocationAsync-metodille:
        regions.ForEach(r => Debug.Log(r.Description + " - region ID string: " + r.Id + "\n\n"));

        //Käyttöliittymänä yksinkertainen Dropdown, johon filtteröidään lista pelkistä Id-arvoista:
        List<string> regionStringIDs = regions.Select(r => r.Id).ToList();
        DropDownRegions.ClearOptions();        
        DropDownRegions.AddOptions(regionStringIDs);
        DropDownRegions.onValueChanged.AddListener(HandleDropDownChange);

        //Ja sitten näytetään StartContainer, kun ollaan saatu regionit dropdowniin:
        StartContainer.gameObject.SetActive(true);
    }

    private void HandleDropDownChange(int i)
    {
        Debug.Log("selected region: " + DropDownRegions.value);
    }

    //Hostatessa kutsutaan RelaySetupin.HostGame()-metodia ja jos
    //homma onnistuu, palvelimelta saadaan RelayHostData, joka
    //passataan Unity Transportille SetRelayServerData-metodilla.
    //RelayHostDatassa tulee myös join code, jonka client antaa
    //parametriksi JoinGame()-metodiin.
    //Odotetaan tällä threadilla asynkronisen Taskin valmistumista
    //async-await -kuviolla.

    //CreateAllocationAsync-metodin region on optionaalinen, joten tässäkin default-arvona null
    async private void InitiateHosting(string regionStringID = null)
    {
        //Pass region to host if such exists:
        Task<RelayHostData> hostRelaySetup = RelaySetup.HostGame(4, regionStringID);

        try
        {
            await hostRelaySetup;
        }
        catch (AggregateException ae)
        {
            ae.InnerExceptions.ToList().ForEach(e => Debug.LogError(e.Message + "\n Stack trace: " + e.StackTrace));
        }

        RelayHostData relayHostData = hostRelaySetup.Result;
        transport.SetRelayServerData(
            relayHostData.IPv4Address,
            relayHostData.Port,
            relayHostData.AllocationIDBytes,
            relayHostData.Key,
            relayHostData.ConnectionData
            );

        Debug.Log("Relay IP: " + relayHostData.IPv4Address);

        Debug.Log("Join code: " + relayHostData.JoinCode);
        LobbyControlRef.SetJoinCodeInfoText(relayHostData.JoinCode);

        spawnIndex = Random.Range(0, spawnPoints.Count);

        NetworkManager.Singleton.StartHost();
        LobbyControlRef.Init();
        ShowStartContainer(false);        
    }

    //Client taas kutsuu RelaySetupin.JoinGame()-metodia ja jos
    //homma onnistuu, palvelimelta saadaan RelayJoinData, joka
    //passataan Unity Transportille SetRelayServerData-metodilla.
    //RelayHostData ja RelayJoinData ovat lähes samat, mutta client
    //antaa vielä viimeisenä parametrina hostin connection datan.

    async private void InitiateJoiningAsClient()
    {
        Task<RelayJoinData> joinRelaySetup = RelaySetup.JoinGame(InputFieldJoinCode.text);

        try
        {
            await joinRelaySetup;
        }
        catch (AggregateException ae)
        {
            ae.InnerExceptions.ToList().ForEach(e => Debug.LogError(e.Message + "\n Stack trace: " + e.StackTrace));
        }

        RelayJoinData relayJoinData = joinRelaySetup.Result;
        transport.SetRelayServerData(
            relayJoinData.IPv4Address,
            relayJoinData.Port,
            relayJoinData.AllocationIDBytes,
            relayJoinData.Key,
            relayJoinData.ConnectionData,
            relayJoinData.HostConnectionData
            );
        
        NetworkManager.Singleton.StartClient();
        LobbyControlRef.Init();
        ShowStartContainer(false);
    }
    
    public void ShowStartContainer(bool _show)
    {
        StartContainer.gameObject.SetActive(_show);
        LobbyContainer.gameObject.SetActive(!_show);        
       // textInfo.text = GetInfoText(_show);

        if (_show)
            RoundOverContainer.gameObject.SetActive(false);
    }
/*
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
    */
    Transform GetSpawnPoint()
    {
        Transform newSpawn = spawnPoints[0];
        newSpawn = spawnIndex > 0 ? spawnPoints[spawnIndex] : spawnPoints[0];
        spawnIndex++;
        return newSpawn;
    }
    void ConnectionApproval(byte[] payload, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        //ConnectionApprovedDelegate, jossa instantioidaan pelaaja spawn pointiin:
        Transform newSpawn = GetSpawnPoint();
        callback(true, null, true, newSpawn.position, newSpawn.rotation);
    }
    [ClientRpc]
    public void StartOrEndGameClientRpc(bool _start, string _results)
    {
        if (_start)
            LobbyContainer.gameObject.SetActive(false);
        else
        {
            RoundOverContainer.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            TextResults.text = _results;
        }
        
        GameStarted.Value = _start;
    }
    //Tässä tarkastellaan pelaajien tilaa ja muodostetaan string kierroksen tuloksista, joka näytetään pelin päättyessä.
    //ServerRPC:nä tässä voidaan tehdä kaikenlaista clienttien arvojen tarkastelua ja järjestelyä ja sitten jaella tulokset
    //clienteille. Tässä esimerkissä peli myös siis lopetetaan tätä kautta joka timerin mennessä nollaan (jolloin EI tarvitse
    //laskea monta pelaajaa on jäljellä) tai kun yhden pelaajan Health menee nolliin, tarkastetaan montako pelaaja on vielä
    //pelissä. Itse peli taas lopetetaan kaikilta StartOrEndGameClientRpc-metodissa, jossa näytetään oikeat käyttöliittymät
    //ja päivitetään onko peli alkanut.
    [ServerRpc(RequireOwnership = false)]
    public void CheckPlayerStatusServerRpc(bool _endGameImmediately)
    {  
        List<Player> players = new List<Player>();
        NetworkManager.Singleton.ConnectedClients.ToList().ForEach(cc => 
        {
            Player player = cc.Value.PlayerObject.GetComponent<Player>();
            if (player != null)
                players.Add(player);
        });

        string results = "";
        players.OrderByDescending(pt => pt.Score.Value).ToList().ForEach(pt => results += pt.playerName.Value + " health: " + pt.Health.Value + " score: " + pt.Score.Value + "\n");
        

        if (_endGameImmediately)
        {
            StartOrEndGameClientRpc(false, results);
        }
        else
        {
            int deadPlayersCount = NetworkManager.Singleton.ConnectedClients.ToList().
                Where(player => player.Value.PlayerObject.GetComponent<Player>().Health.Value <= 0).Count();

            if (NetworkManager.Singleton.ConnectedClients.Count() - deadPlayersCount == 1)
            {
                StartOrEndGameClientRpc(false, results);
            }
        }
    }
}
