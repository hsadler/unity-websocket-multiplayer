using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class SceneManagerScript : MonoBehaviour
{

    public GameObject playerPrefab;

    private WebSocket ws;
    private string gameServerUrl = "ws://localhost:5000";
    //private string gameServerUrl = "ws://0.tcp.ngrok.io:16554"; // example ngrok URL

    private Player mainPlayerModel;
    private GameObject mainPlayerGO;

    private IDictionary<string, GameObject> playerIdToOtherPlayerGO =
            new Dictionary<string, GameObject>();

    private Queue<string> gameServerMessageQueue = new Queue<string>();

    private const string SERVER_MESSAGE_TYPE_PLAYER_ENTER = "SERVER_MESSAGE_TYPE_PLAYER_ENTER";
    private const string SERVER_MESSAGE_TYPE_PLAYER_EXIT = "SERVER_MESSAGE_TYPE_PLAYER_EXIT";
    private const string SERVER_MESSAGE_TYPE_PLAYER_UPDATE = "SERVER_MESSAGE_TYPE_PLAYER_UPDATE";
    private const string SERVER_MESSAGE_TYPE_GAME_STATE = "SERVER_MESSAGE_TYPE_GAME_STATE";

    // UNITY HOOKS

    private void Start()
    {
        this.InitWebSocketClient();
        this.InitMainPlayer();
    }

    private void Update()
    {
        // process all queued server messages
        while (this.gameServerMessageQueue.Count > 0) 
        {
            this.HandleServerMessage(this.gameServerMessageQueue.Dequeue());
        }
    }

    private void OnDestroy()
    {
        // close websocket connection
        this.ws.Close(CloseStatusCode.Normal);
    }

    // INTERFACE METHODS

    public void SyncPlayerState(GameObject playerGO)
    {
        // send "player update" message to server
        this.mainPlayerModel.position = new Position(
            playerGO.transform.position.x,
            playerGO.transform.position.y
        );
        var playerUpdateMessage = new ClientMessagePlayerUpdate(this.mainPlayerModel);
        this.ws.Send(JsonUtility.ToJson(playerUpdateMessage));
    }

    // IMPLEMENTATION METHODS

    private void InitWebSocketClient()
    {
        // create websocket connection
        this.ws = new WebSocket(this.gameServerUrl);
        this.ws.Connect();
        // add message handler callback
        this.ws.OnMessage += this.QueueServerMessage;
    }

    private void InitMainPlayer()
    {
        // create player game object
        var playerPos = Vector3.zero;
        this.mainPlayerGO = Instantiate(this.playerPrefab, playerPos, Quaternion.identity);
        var mainPlayerScript = this.mainPlayerGO.GetComponent<PlayerScript>();
        mainPlayerScript.sceneManager = this;
        mainPlayerScript.isMainPlayer = true;
        // create player model
        string uuid = System.Guid.NewGuid().ToString();
        var pos = new Position(this.transform.position.x, this.transform.position.y);
        this.mainPlayerModel = new Player(uuid, pos);
        // send "player enter" message to server
        var playerEnterMessage = new ClientMessagePlayerEnter(this.mainPlayerModel);
        this.ws.Send(JsonUtility.ToJson(playerEnterMessage));
    }

    private void QueueServerMessage(object sender, MessageEventArgs e) 
    {
        Debug.Log("Server message received: " + e.Data);
        this.gameServerMessageQueue.Enqueue(e.Data);
    }

    private void HandleServerMessage(string messageJSON)
    {
        // parse message type
        string messageType = JsonUtility.FromJson<ServerMessageGeneric>(messageJSON).messageType;
        // route message to handler based on message type
        if (messageType == SERVER_MESSAGE_TYPE_PLAYER_ENTER)
        {
            this.HandlePlayerEnterServerMessage(messageJSON);
        }
        else if (messageType == SERVER_MESSAGE_TYPE_PLAYER_EXIT)
        {
            this.HandlePlayerExitServerMessage(messageJSON);
        }
        else if (messageType == SERVER_MESSAGE_TYPE_PLAYER_UPDATE)
        {
            this.HandlePlayerUpdateServerMessage(messageJSON);
        }
        else if (messageType == SERVER_MESSAGE_TYPE_GAME_STATE)
        {
            this.HandleGameStateServerMessage(messageJSON);
        }
    }

    private void HandlePlayerEnterServerMessage(string messageJSON)
    {
        var playerEnterMessage = JsonUtility.FromJson<ServerMessagePlayerEnter>(messageJSON);
        this.AddOtherPlayerFromPlayerModel(playerEnterMessage.player);
    }

    private void HandlePlayerExitServerMessage(string messageJSON)
    {
        var playerExitMessage = JsonUtility.FromJson<ServerMessagePlayerExit>(messageJSON);
        string playerId = playerExitMessage.player.id;
        if (this.playerIdToOtherPlayerGO.ContainsKey(playerId)) 
        {
            Object.Destroy(this.playerIdToOtherPlayerGO[playerId]);
            this.playerIdToOtherPlayerGO.Remove(playerId);
        }
    }

    private void HandlePlayerUpdateServerMessage(string messageJSON)
    {
        var playerUpdateMessage = JsonUtility.FromJson<ServerMessagePlayerUpdate>(messageJSON);
        Player playerModel = playerUpdateMessage.player;
        if (this.playerIdToOtherPlayerGO.ContainsKey(playerModel.id))
        {
            var newPosition = new Vector3(
                playerModel.position.x,
                playerModel.position.y,
                0
            );
            this.playerIdToOtherPlayerGO[playerModel.id].transform.position = newPosition;
        }
    }

    private void HandleGameStateServerMessage(string messageJSON)
    {
        var gameStateMessage = JsonUtility.FromJson<ServerMessageGameState>(messageJSON);
        foreach (Player player in gameStateMessage.gameState.players) 
        {
            this.AddOtherPlayerFromPlayerModel(player);   
        }
    }

    private void AddOtherPlayerFromPlayerModel(Player otherPlayerModel)
    {
        // player is not main player and player is not currently tracked
        if (
            otherPlayerModel.id != this.mainPlayerModel.id
            && !this.playerIdToOtherPlayerGO.ContainsKey(otherPlayerModel.id)
        )
        {
            var otherPlayerPosition = new Vector3(
                otherPlayerModel.position.x,
                otherPlayerModel.position.y,
                0
            );
            GameObject otherPlayerGO = Instantiate(
                this.playerPrefab,
                otherPlayerPosition,
                Quaternion.identity
            );
            var otherPlayerScript = otherPlayerGO.GetComponent<PlayerScript>();
            otherPlayerScript.sceneManager = this;
            otherPlayerScript.isMainPlayer = false;
            this.playerIdToOtherPlayerGO.Add(otherPlayerModel.id, otherPlayerGO);
        }
    }

}
