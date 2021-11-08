using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class SceneManagerScript : MonoBehaviour
{

    public GameObject playerPrefab;

    private WebSocket ws;

    private Player mainPlayerModel;
    private GameObject mainPlayerGO;

    private IDictionary<string, GameObject> playerIdToOtherPlayerGO;

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
        this.ws = new WebSocket("ws://localhost:5000");
        this.ws.Connect();
        // add message handler callback
        this.ws.OnMessage += this.HandleServerMessages;
    }

    private void InitMainPlayer()
    {
        // create player game object
        var playerPos = Vector3.zero;
        this.mainPlayerGO = Instantiate(this.playerPrefab, playerPos, Quaternion.identity);
        this.mainPlayerGO.GetComponent<PlayerScript>().sceneManager = this;
        // create player model
        string uuid = System.Guid.NewGuid().ToString();
        var pos = new Position(this.transform.position.x, this.transform.position.y);
        this.mainPlayerModel = new Player(uuid, pos);
        // send "player enter" message to server
        var playerEnterMessage = new ClientMessagePlayerEnter(this.mainPlayerModel);
        this.ws.Send(JsonUtility.ToJson(playerEnterMessage));
    }

    private void HandleServerMessages(object sender, MessageEventArgs e)
    {
        // parse message type
        string messageType = JsonUtility.FromJson<ServerMessageGeneric>(e.Data).messageType;
        Debug.Log("handling incoming message type: " + messageType);
        // route message to handler based on message type
        if (messageType == SERVER_MESSAGE_TYPE_PLAYER_ENTER)
        {
            var message = JsonUtility.FromJson<ServerMessagePlayerEnter>(e.Data);
            //Debug.Log("player enter message received: " + JsonUtility.ToJson(message));
        }
        else if (messageType == SERVER_MESSAGE_TYPE_PLAYER_EXIT)
        {
            var message = JsonUtility.FromJson<ServerMessagePlayerExit>(e.Data);
            //Debug.Log("player exit message received: " + JsonUtility.ToJson(message));
        }
        else if (messageType == SERVER_MESSAGE_TYPE_PLAYER_UPDATE)
        {
            var message = JsonUtility.FromJson<ServerMessagePlayerUpdate>(e.Data);
            //Debug.Log("player update message received: " + JsonUtility.ToJson(message));
        }
        else if (messageType == SERVER_MESSAGE_TYPE_GAME_STATE)
        {
            this.HandleGameStateServerMessage(e.Data);
        }
    }

    private void HandleGameStateServerMessage(string messageJSON) {
        var message = JsonUtility.FromJson<ServerMessageGameState>(messageJSON);
        Debug.Log("game state message received: " + JsonUtility.ToJson(message));
    }

}
