using UnityEngine;
using WebSocketSharp;

public class WsClientScript : MonoBehaviour
{
    WebSocket ws;
    
    private void Start()
    {
        ws = new WebSocket("ws://localhost:5000");
        ws.Connect();
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
        };
    }

    private void Update()
    {
        if(ws == null)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ws.Send("Hello");
        }  
    }

    private void OnDestroy()
    {
        ws.Close(CloseStatusCode.Normal);
    }

}