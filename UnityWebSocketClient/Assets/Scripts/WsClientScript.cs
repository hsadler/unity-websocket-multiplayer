using System;
using UnityEngine;
using WebSocketSharp;

public class WsClientScript : MonoBehaviour
{

    private WebSocket ws;
    private Player player;

    private const string SERVER_MESSAGE_TYPE_PLAYER_ENTER = "SERVER_MESSAGE_TYPE_PLAYER_ENTER";
    private const string SERVER_MESSAGE_TYPE_PLAYER_EXIT = "SERVER_MESSAGE_TYPE_PLAYER_EXIT";
    private const string SERVER_MESSAGE_TYPE_PLAYER_UPDATE = "SERVER_MESSAGE_TYPE_PLAYER_UPDATE";
    private const string SERVER_MESSAGE_TYPE_GAME_STATE = "SERVER_MESSAGE_TYPE_GAME_STATE";

    // UNITY HOOKS
    
    private void Start()
    {
        // create websocket connection
        this.ws = new WebSocket("ws://localhost:5000");
        this.ws.Connect();
        // add message handler callback
        this.ws.OnMessage += this.HandleServerMessages;
        // create player model
        string uuid = System.Guid.NewGuid().ToString();
        var pos = new Position(this.transform.position.x, this.transform.position.y);
        this.player = new Player(uuid, pos);
        // send "player enter" message to server
        var playerEnterMessage = new PlayerEnterMessage(this.player);
        this.ws.Send(JsonUtility.ToJson(playerEnterMessage));
    }

    private void Update()
    {
        if(this.ws == null)
        {
            return;
        }
        this.HandleMovement();
    }

    private void OnDestroy()
    {
        // close websocket connection
        this.ws.Close(CloseStatusCode.Normal);
    }

    // IMPLEMENTATION METHODS

    private void HandleMovement()
    {
        // left
        var targetPos = this.transform.position;
        if (Input.GetKey(KeyCode.A))
        {
            targetPos += Vector3.left;
        }
        // right
        if (Input.GetKey(KeyCode.D))
        {
            targetPos += Vector3.right;
        }
        // up
        if (Input.GetKey(KeyCode.W))
        {
            targetPos += Vector3.up;
        }
        // down
        if (Input.GetKey(KeyCode.S))
        {
            targetPos += Vector3.down;
        }
        if (targetPos != this.transform.position)
        {
            float moveSpeed = 2f;
            this.transform.position = Vector3.MoveTowards(
                this.transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            // send "player update" message to server
            this.player.position = new Position(this.transform.position.x, this.transform.position.y);
            var playerUpdateMessage = new PlayerUpdateMesssage(this.player);
            this.ws.Send(JsonUtility.ToJson(playerUpdateMessage));
        }
    }

    private void HandleServerMessages(object sender, MessageEventArgs e)
    {
        // TODO: implement message routing and handling
        string messageType = JsonUtility.FromJson<GenericMessage>(e.Data).messageType;
        Debug.Log("handling incoming message type: " + messageType);
        if (messageType == SERVER_MESSAGE_TYPE_PLAYER_ENTER) {
            var message = JsonUtility.FromJson<EnterPlayerMessage>(e.Data);
            Debug.Log("enter player message received: " + JsonUtility.ToJson(message));
        }
    }

}