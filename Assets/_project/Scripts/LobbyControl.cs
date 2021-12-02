using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Linq;
using TMPro;
using UnityEngine.Serialization;

public class LobbyControl : NetworkBehaviour
{
    //Disconnect buttonin tilaa vaihdetaan vähän hassusti lobbyn
    //skriptistä, kun mitään kunnollista UI:n hallintaa ei ole.
    public Button ButtonDisconnect;
    //Button, jolla client ilmoittaa olevansa valmis aloittamaan:
    public Button ButtonReady;
   
    public Button ButtonPlayerColor;
    //Infoteksti
    public TMP_Text LobbyText;

    //Minimipelaajamäärä pelin aloittamiseksi
    [SerializeField]
    private int MinimumPlayerCount = 2;

    //Onko lobbyssa tarpeeksi pelaajia
    private bool EnoughPlayersInLobby;

    //Kirjanpito clientId:istä, jotka ovat lobbyssa. Value on "ready"-tila
    private Dictionary<ulong, bool> ClientsInLobby;

    private void Start()
    {
        ButtonDisconnect.gameObject.SetActive(false);
    }

    public void Init()
    {
        ClientsInLobby = new Dictionary<ulong, bool>();

        //Sinänsä turha tarkastus, koska palvelin on käynnissä tällä UI-logiikalla:
        if (NetworkManager.Singleton.IsListening)
        {
            //Hostina lisätään oma client id suoraan ClientsInLobby-dictionaryyn ("ready"-napin painallusta
            //odotetaan kuten muillakin clienteilla) ja aloitetaan kuuntelemaan muita peliin liittyviä
            //clientteja:
            if (IsServer)
            {
                //Lisätään host suoraan
                ClientsInLobby.Add(NetworkManager.Singleton.LocalClientId, false);

                EnoughPlayersInLobby = false;

                //Aloitetaan kuuntelemaan peliin liittyviä clientteja:
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
                
                ButtonDisconnect.gameObject.SetActive(true);
            }

            //Lobbyssa olevien pelaajien tilanne päivitetään UI:n Text-objektiin:
            UpdateLobbyText();
        }

        ButtonReady.onClick.AddListener(() => HandlePlayerIsReadyButtonClick());
        ButtonPlayerColor.onClick.AddListener(() => HandleTankColorButtonClick());
        ButtonDisconnect.onClick.AddListener(() => HandleDisconnectButtonClick());
    }

    private void HandleDisconnectButtonClick()
    {
        //Host käskyttää clientit kutsumaan Shutdown-metodin disconnectaamiseksi
        //ja vasta sen jälkeen kutsuu itse Shutdownia. 
        if (IsHost)
        {
            ShutdownClientRpc();
            //Lopetetaan eventtien kuuntelu:
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            //client sen sijaan voi kutsua suoraan Shutdown-metodia:
            ExecuteShutDown();
            Cursor.lockState = CursorLockMode.None;
        }
    }

    [ClientRpc]
    private void ShutdownClientRpc()
    {
        //En löytänyt mistään tietoa, miten Shutdown tulisi varsinaisesti suorittaa,
        //että hostaamisen voisi lopettaa virheettömästi, mutta ainakin tällaisella
        //pienellä purkkavirityksellä se toimii. Ensin siis clientit tekevät ShutDownin
        //ja sitten odotetaan vielä hetki ennen serverin Shutdownia.

        if (NetworkManager.Singleton.LocalClientId != NetworkManager.Singleton.ServerClientId)        
            StartCoroutine(DelayedShutDown(.1f));        
        else        
            StartCoroutine(DelayedShutDown(.2f));        
    }

    IEnumerator DelayedShutDown(float _wait)
    {
        yield return new WaitForSeconds(_wait);
        Debug.Log("Waited for " + _wait + " seconds and now the actual shutdown...");
        ExecuteShutDown();
    }

    private void ExecuteShutDown()
    {

        if (IsServer)
            GameController.Instance.StartOrEndGameClientRpc(true, "");

        //Tässä toteutuksessa laitoin paikallisen kameran GameObjectin
        //NetworkObjectin childiksi ja se tuhoutuu hetken päästä,
        //joten asetetaan parentiksi takaisin root:
        // Camera.main.transform.SetParent(null);
        
        //Shutdown-metodiin on ilmeisesti päätetty yhdistää StopHost, StopServer ja StopClient
        //metodit, joista hämmentävästi puhutaan dokumentaatiossa ja jopa NetworkManagerin 
        //suoritettavassa koodissa (ks. NetworkManager rivi 1436):
        NetworkManager.Singleton.Shutdown();

        //Ajastin ei voi myöskään jäädä käyntiin, joten lopetetaan sen coroutine
        //(tai oikeastaan kaikki):
        // RoundTimer.Instance.StopTimerCoroutine();

        //Ja lopuksi näytetään taas alkuvalikko:
        GameController.Instance.ShowStartContainer(true);
    }

    //Clienttien connected/ready tilanne kerrotaan UI:ssa yksinkertaisessa Text-komponentissa:
    private void UpdateLobbyText()
    {
        string userLobbyStatus = string.Empty;
        foreach (var clientLobbyStatus in ClientsInLobby)
        {
            userLobbyStatus += "Player " + clientLobbyStatus.Key + " ";
            if (clientLobbyStatus.Value)
                userLobbyStatus += "[Ready]\n";
            else
                userLobbyStatus += "[Not Ready]\n";
        }

        LobbyText.text = userLobbyStatus;
    }

    private void UpdateAndCheckPlayersInLobby()
    {
        //Tarkastetaan onko MinimumPlayerCount täynnä...
        EnoughPlayersInLobby = ClientsInLobby.Count >= MinimumPlayerCount;

        //mutta clientin status ei välttämättä ole vielä ConnectedClient,
        //joten tehdään ready-statuksen päivitys ja tarkastetaan ConnectedClients
        //dictionarysta tilanne:
        foreach (var clientLobbyStatus in ClientsInLobby)
        {
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value);

            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))
                EnoughPlayersInLobby = false;
        }

        //ja sitten katsotaan ovatko kaikki jo klikanneet "ready"-nappia:
        CheckForAllPlayersReady();
    }

    //Kuunnellaan peliin liittyviä clientteja. Parametrina tulee clientId ja
    //lisätään siis lobby-kirjanpitoon ne clientit, joita siellä ei vielä ole.
    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log("Client connected: " + clientId);

        if (IsServer)
        {
            if (!ClientsInLobby.ContainsKey(clientId)) ClientsInLobby.Add(clientId, false);
            UpdateLobbyText();
            UpdateAndCheckPlayersInLobby();

            //Aktivoi käyttäjien disconnect buttonit:
            SetDisconnectButtonStateClientRpc(true);
        }
    }

    //Kuunnellaan clienttien disconnectia ja päivitetään lobbyn
    //tilanne:
    private void OnClientDisconnectedCallback(ulong clientId)
    {

        Debug.Log("Client disconnected: " + clientId);

        if (IsServer)
            RemoveClientFromLobbyClientRpc(clientId);                
    }

    [ClientRpc]
    private void SetDisconnectButtonStateClientRpc(bool active)
    {
        ButtonDisconnect.gameObject.SetActive(active);
    }

    [ClientRpc]
    private void RemoveClientFromLobbyClientRpc(ulong clientId)
    {
        if (ClientsInLobby.ContainsKey(clientId))
            ClientsInLobby.Remove(clientId);

        UpdateLobbyText();        
    }

    //Pävitetään serveriltä clienteille tilanne lobbyssa olevista clienteista. Host lisättiin
    //jo Init-metodissa.
    [ClientRpc]
    private void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
    {
        if (!IsServer)
        {
            if (!ClientsInLobby.ContainsKey(clientId))
                ClientsInLobby.Add(clientId, isReady);
            else
                ClientsInLobby[clientId] = isReady;
            UpdateLobbyText();
        }
    }

    //Tarkastetaan onko pelin aloittamiseen vaadittava pelaajamäärä täynnä ja
    //ovatko kaikki ilmoittaneet olevansa valmiita aloittamaan. Jos ovat,
    //aloitetaan peli:
    private void CheckForAllPlayersReady()
    {
        if (EnoughPlayersInLobby)
        {
            //Linq-kysely käy läpi dictionaryn ja katsoo ovatko kaikki
            //sieltä löytyvät clientit klikanneet ready-nappia:
            if (ClientsInLobby.All(client => client.Value == true))
            {
                //Kun tarvittava pelaajamäärä on tullut täyteen, ei uusia yhteyksiä enää kuunnella:
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                //Aloitetaan peli suoraan, kun pelaajamäärä on täynnä:
                GameController.Instance.StartOrEndGameClientRpc(true, "");
            }
        }
    }

    //Ready-napin painalluksella serverillä päivitetään pelaajatilanne ja
    //client pyytää päivittämään oman id:nsä (dictionaryn key) ClientInLobby dictionaryn
    //value boolean-arvon trueksi (olen valmis).
    private void HandlePlayerIsReadyButtonClick()
    {
        if (IsServer)
        {
            ClientsInLobby[NetworkManager.Singleton.ServerClientId] = true;
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            ClientsInLobby[NetworkManager.Singleton.LocalClientId] = true;
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        UpdateLobbyText();
    }

    private void HandleTankColorButtonClick()
    {
        //Random color rajattuna tummempiin sävyihin:
        Color randomColor = Random.ColorHSV(.2f, 0.6f, .2f, .6f);
        ButtonPlayerColor.GetComponent<Image>().color = randomColor;
        UpdatePlayerColorServerRpc(NetworkManager.Singleton.LocalClientId, randomColor);
    }

    //Valitaan ConnectedClienteista oma ClientId ja päivitetään tankin väri valituksi väriksi.
    //Tämä siis ServerRpc, joka ei vaadi ownershippia, jotta sitä voidaan kutsua, mutta ServerRpc
    //siis siksi, että vain server pääsee käsiksi ConnectecClientsiin, josta tässä kaivetaan
    //clientilta tulleella id:llä oikean pelaajan tankki ja pyydetään clienttia päivittämään sen väri.
    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerColorServerRpc(ulong clientId, Color color)
    {
        //Samaa ideaa voi käyttää myös jos valitaan pelin alus/hahmo.
        //Prefabiin voi rakentaa valmiiksi spritet/mallit, jotka
        //yksinkertaisesti vaihdetaan näkyviin, kun pelaaja tekee
        //valinnan esim. lobbyssa (tai pelin aikana).
        
        // NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerTank>().PlayerColor.Value = color;
    }

    //Annetaan serverille tieto, että pelaaja "clientId" on klikannut "ready"-nappia.
    [ServerRpc(RequireOwnership = false)]
    private void OnClientIsReadyServerRpc(ulong clientId)
    {
        if (ClientsInLobby.ContainsKey(clientId))
        {
            ClientsInLobby[clientId] = true;
            UpdateAndCheckPlayersInLobby();
            UpdateLobbyText();
        }
    }
}
