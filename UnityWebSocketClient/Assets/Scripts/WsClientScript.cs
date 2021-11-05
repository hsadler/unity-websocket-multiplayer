using UnityEngine;
using WebSocketSharp;

public class WsClientScript : MonoBehaviour
{
    WebSocket ws;
    
    private void Start()
    {
        this.ws = new WebSocket("ws://localhost:5000");
        this.ws.Connect();
        this.ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
        };
    }

    private void Update()
    {
        if(this.ws == null)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.ws.Send("Hello");
        }  
    }

    private void OnDestroy()
    {
        this.ws.Close(CloseStatusCode.Normal);
    }

}