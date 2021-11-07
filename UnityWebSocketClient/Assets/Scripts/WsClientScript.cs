using UnityEngine;
using WebSocketSharp;

public class WsClientScript : MonoBehaviour
{

    private WebSocket ws;
    private PlayerModel playerModel;

    private const string SERVER_MESSAGE_TYPE_ENTER_PLAYER = "SERVER_MESSAGE_TYPE_ENTER_PLAYER";
    private const string SERVER_MESSAGE_TYPE_EXIT_PLAYER = "SERVER_MESSAGE_TYPE_EXIT_PLAYER";
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
        this.playerModel = new PlayerModel(this.transform.position);
        // TODO: register new player on server
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
        // TODO: finish implementation

        // logic:
        // gather movement inputs
        // move player in game
        // get new position and update server

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
        }
        //var serverMessage = new PlayerUpdateMesssage();
    }

    private void HandleServerMessages(object sender, MessageEventArgs e)
    {
        // TODO: implement message routing and handling
        Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
    }

}