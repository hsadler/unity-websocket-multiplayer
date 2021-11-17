package main

import (
	"encoding/json"
	"flag"
	"fmt"
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

var state GameState = GameState{
	WebsocketToPlayer: make(map[*websocket.Conn]*Player),
}

type GameState struct {
	WebsocketToPlayer map[*websocket.Conn]*Player
}

func (gs *GameState) AddPlayer(player *Player, ws *websocket.Conn) {
	gs.WebsocketToPlayer[ws] = player
}

func (gs *GameState) RemovePlayerByWebsocket(ws *websocket.Conn) {
	delete(gs.WebsocketToPlayer, ws)
}

func (gs *GameState) GetAllConnections() []*websocket.Conn {
	var connections []*websocket.Conn
	for ws := range gs.WebsocketToPlayer {
		connections = append(connections, ws)
	}
	return connections
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

func NewPlayerFromMap(pData map[string]interface{}, ws *websocket.Conn) *Player {
	posMap := pData["position"].(map[string]interface{})
	pos := Position{
		x: posMap["x"].(float64),
		y: posMap["y"].(float64),
	}
	// fmt.Printf("pData: %v\n", pData)
	// fmt.Printf("deserialized pos: %v\n", pos)
	player := Player{
		websocket: ws,
		id:        pData["id"].(string),
		position:  &pos,
	}
	return &player
}

///////////////// SERVER MESSAGE SENDING /////////////////

func BroadcastMessage(connections []*websocket.Conn, message_json string) {
	for _, ws := range connections {
		ws.WriteMessage(1, []byte(message_json))
	}
}

///////////////// CLIENT MESSAGE HANDLING /////////////////

func RouteMessage(ws *websocket.Conn, message []byte) {
	messageTypeToHandler := map[string]func(*websocket.Conn, map[string]interface{}){
		"CLIENT_MESSAGE_TYPE_PLAYER_ENTER":  HandlePlayerEnter,
		"CLIENT_MESSAGE_TYPE_PLAYER_UPDATE": HandlePlayerUpdate,
		"CLIENT_MESSAGE_TYPE_PLAYER_EXIT":   HandlePlayerExit,
	}
	var mData map[string]interface{}
	if err := json.Unmarshal(message, &mData); err != nil {
		panic(err)
	}
	fmt.Printf("message received: %s\n", mData)
	messageTypeToHandler[mData["messageType"].(string)](ws, mData)
}

func HandlePlayerEnter(ws *websocket.Conn, mData map[string]interface{}) {
	player := NewPlayerFromMap(mData["player"].(map[string]interface{}), ws)
	state.AddPlayer(player, ws)
	BroadcastMessage(state.GetAllConnections(), "mock player enter")
}

func HandlePlayerUpdate(ws *websocket.Conn, mData map[string]interface{}) {
	player := NewPlayerFromMap(mData["player"].(map[string]interface{}), ws)
	state.WebsocketToPlayer[ws] = player
	BroadcastMessage(state.GetAllConnections(), "mock player update")
}

func HandlePlayerExit(ws *websocket.Conn, mData map[string]interface{}) {
	state.RemovePlayerByWebsocket(ws)
	BroadcastMessage(state.GetAllConnections(), "mock player exit")
}

///////////////// CONNECTION AND INCOMING MESSAGES /////////////////

func HandleWebsocket(w http.ResponseWriter, r *http.Request) {
	upgrader := websocket.Upgrader{} // use default options
	ws, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Print("upgrade:", err)
		return
	}
	defer func() {
		HandlePlayerExit(ws, nil)
		ws.Close()
	}()
	for {
		// read message
		_, message, err := ws.ReadMessage()
		if err != nil {
			log.Println("read:", err)
			break
		}
		// process message
		RouteMessage(ws, message)
	}
}

///////////////// RUN SERVER /////////////////

func main() {
	flag.Parse()
	log.SetFlags(0)
	http.HandleFunc("/", HandleWebsocket)
	addr := flag.String("addr", "0.0.0.0:5000", "http service address")
	log.Fatal(http.ListenAndServe(*addr, nil))
}

///////////////// UTIL FUNCTIONS /////////////////
