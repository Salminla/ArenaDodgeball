using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : NetworkBehaviour
{
    public static GameController Instance { private set; get; }
    
    public Button buttonHost;
    public Button buttonClient;

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
    
    private UNetTransport transport;
    
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

        spawnIndex = Random.Range(0, spawnPoints.Count);
        
        transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UNetTransport;
        
        RoundOverContainer.gameObject.SetActive(false);

        //Käynnistä host (joka siis server ja client), server tai client.
        buttonHost.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();          
            ShowStartContainer(false);
            LobbyControlRef.Init();
            
        });

        buttonClient.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            LobbyControlRef.Init();
            ShowStartContainer(false);
        });

        ShowStartContainer(true);
    }
    
    public void ShowStartContainer(bool _show)
    {
        StartContainer.gameObject.SetActive(_show);
        LobbyContainer.gameObject.SetActive(!_show);        
        textInfo.text = GetInfoText(_show);

        if (_show)
            RoundOverContainer.gameObject.SetActive(false);
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
        players.OrderByDescending(pt => pt.Health.Value).ToList().ForEach(pt => results += pt.playerName.Value + " health: " + pt.Health.Value + "\n");
        

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
