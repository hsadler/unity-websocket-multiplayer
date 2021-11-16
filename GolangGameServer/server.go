package main

import (
	"flag"
	"log"
	"net/http"

	"github.com/gorilla/websocket"
)

// TODO:
// - add game state:
//// - track connections
//// - track player states
// - add server message classes
// - add client message handlers
// - add routing of client messages to respective handler
// - add ability to broadcast server message

///////////////// GAME STATE /////////////////

type GameState struct {
	connections []*websocket.Conn
	players     []*Player
}

func (gs *GameState) add_player(player *Player, ws *websocket.Conn) {
	gs.players = append(gs.players, player)
	gs.connections = append(gs.connections, ws)
}

func (gs *GameState) remove_player(player *Player) {
	// stub
}

func (gs *GameState) find_player_by_websocket(ws *websocket.Conn) Player {
	// stub
	return Player{}
}

type Player struct {
	websocket *websocket.Conn
	id        string
	position  *Position
}

type Position struct {
	x float64
	y float64
}

///////////////// SERVER MESSAGES /////////////////

///////////////// CLIENT MESSAGE HANDLERS /////////////////

///////////////// CONNECTION HANDLING /////////////////

func broadcast_message(connections []*websocket.Conn, message_json string) {
	for _, ws := range connections {
		ws.WriteMessage(1, []byte(message_json))
	}
}

func handle_websocket(w http.ResponseWriter, r *http.Request) {
	upgrader := websocket.Upgrader{} // use default options
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Print("upgrade:", err)
		return
	}
	defer conn.Close()
	for {
		mt, message, err := conn.ReadMessage()
		if err != nil {
			log.Println("read:", err)
			break
		}
		log.Printf("recv: %s", message)
		err = conn.WriteMessage(mt, message)
		if err != nil {
			log.Println("write:", err)
			break
		}
	}
}

// func echo(w http.ResponseWriter, r *http.Request) {
// 	upgrader := websocket.Upgrader{} // use default options
// 	c, err := upgrader.Upgrade(w, r, nil)
// 	if err != nil {
// 		log.Print("upgrade:", err)
// 		return
// 	}
// 	defer c.Close()
// 	for {
// 		mt, message, err := c.ReadMessage()
// 		if err != nil {
// 			log.Println("read:", err)
// 			break
// 		}
// 		log.Printf("recv: %s", message)
// 		err = c.WriteMessage(mt, message)
// 		if err != nil {
// 			log.Println("write:", err)
// 			break
// 		}
// 	}
// }

// func home(w http.ResponseWriter, r *http.Request) {
// 	homeTemplate.Execute(w, "ws://"+r.Host+"/echo")
// }

func main() {
	flag.Parse()
	log.SetFlags(0)
	http.HandleFunc("/", handle_websocket)
	// http.HandleFunc("/echo", echo)
	// http.HandleFunc("/home", home)
	addr := flag.String("addr", "0.0.0.0:5000", "http service address")
	log.Fatal(http.ListenAndServe(*addr, nil))
}

// var homeTemplate = template.Must(template.New("").Parse(`
// <!DOCTYPE html>
// <html>
// <head>
// <meta charset="utf-8">
// <script>
// window.addEventListener("load", function(evt) {

//     var output = document.getElementById("output");
//     var input = document.getElementById("input");
//     var ws;

//     var print = function(message) {
//         var d = document.createElement("div");
//         d.textContent = message;
//         output.appendChild(d);
//         output.scroll(0, output.scrollHeight);
//     };

//     document.getElementById("open").onclick = function(evt) {
//         if (ws) {
//             return false;
//         }
//         ws = new WebSocket("{{.}}");
//         ws.onopen = function(evt) {
//             print("OPEN");
//         }
//         ws.onclose = function(evt) {
//             print("CLOSE");
//             ws = null;
//         }
//         ws.onmessage = function(evt) {
//             print("RESPONSE: " + evt.data);
//         }
//         ws.onerror = function(evt) {
//             print("ERROR: " + evt.data);
//         }
//         return false;
//     };

//     document.getElementById("send").onclick = function(evt) {
//         if (!ws) {
//             return false;
//         }
//         print("SEND: " + input.value);
//         ws.send(input.value);
//         return false;
//     };

//     document.getElementById("close").onclick = function(evt) {
//         if (!ws) {
//             return false;
//         }
//         ws.close();
//         return false;
//     };

// });
// </script>
// </head>
// <body>
// <table>
// <tr><td valign="top" width="50%">
// <p>Click "Open" to create a connection to the server,
// "Send" to send a message to the server and "Close" to close the connection.
// You can change the message and send multiple times.
// <p>
// <form>
// <button id="open">Open</button>
// <button id="close">Close</button>
// <p><input id="input" type="text" value="Hello world!">
// <button id="send">Send</button>
// </form>
// </td><td valign="top" width="50%">
// <div id="output" style="max-height: 70vh;overflow-y: scroll;"></div>
// </td></tr></table>
// </body>
// </html>
// `))
