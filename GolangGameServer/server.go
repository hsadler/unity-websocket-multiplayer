package main

import (
	"bytes"
	"encoding/json"
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"

	"github.com/gorilla/websocket"
)

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

type GameStateJsonSerializable struct {
	Players []*Player `json:"players"`
}

type Player struct {
	Id       string    `json:"id"`
	Position *Position `json:"position"`
}

type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
}

func NewPlayerFromMap(pData map[string]interface{}, ws *websocket.Conn) *Player {
	posMap := pData["position"].(map[string]interface{})
	pos := Position{
		X: posMap["x"].(float64),
		Y: posMap["y"].(float64),
	}
	player := Player{
		Id:       pData["id"].(string),
		Position: &pos,
	}
	return &player
}

///////////////// SERVER MESSAGE SENDING /////////////////

func SendJsonMessage(ws *websocket.Conn, messageJson []byte) {
	ws.WriteMessage(1, messageJson)
	// log message sent
	fmt.Println("message sent:")
	ConsoleLogJsonByteArray(messageJson)
}

func BroadcastMessage(connections []*websocket.Conn, messageJson []byte) {
	for _, ws := range connections {
		SendJsonMessage(ws, messageJson)
	}
}

func SendGameState(ws *websocket.Conn) {
	allPlayers := make([]*Player, 0)
	for _, player := range state.WebsocketToPlayer {
		allPlayers = append(allPlayers, player)
	}
	messageData := GameStateMessage{
		MessageType: "SERVER_MESSAGE_TYPE_GAME_STATE",
		GameState: &GameStateJsonSerializable{
			Players: allPlayers,
		},
	}
	messageJson, _ := json.Marshal(messageData)
	SendJsonMessage(ws, messageJson)
}

type PlayerMessage struct {
	MessageType string  `json:"messageType"`
	Player      *Player `json:"player"`
}

type GameStateMessage struct {
	MessageType string                     `json:"messageType"`
	GameState   *GameStateJsonSerializable `json:"gameState"`
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
	messageTypeToHandler[mData["messageType"].(string)](ws, mData)
}

func HandlePlayerEnter(ws *websocket.Conn, mData map[string]interface{}) {
	player := NewPlayerFromMap(mData["player"].(map[string]interface{}), ws)
	state.AddPlayer(player, ws)
	message := PlayerMessage{
		MessageType: "SERVER_MESSAGE_TYPE_ENTER_PLAYER",
		Player:      player,
	}
	serialized, _ := json.Marshal(message)
	BroadcastMessage(state.GetAllConnections(), serialized)
}

func HandlePlayerUpdate(ws *websocket.Conn, mData map[string]interface{}) {
	player := NewPlayerFromMap(mData["player"].(map[string]interface{}), ws)
	state.WebsocketToPlayer[ws] = player
	message := PlayerMessage{
		MessageType: "SERVER_MESSAGE_TYPE_PLAYER_UPDATE",
		Player:      player,
	}
	serialized, _ := json.Marshal(message)
	BroadcastMessage(state.GetAllConnections(), serialized)
}

func HandlePlayerExit(ws *websocket.Conn, mData map[string]interface{}) {
	playerToRemove := state.WebsocketToPlayer[ws]
	message := PlayerMessage{
		MessageType: "SERVER_MESSAGE_TYPE_PLAYER_EXIT",
		Player:      playerToRemove,
	}
	serialized, _ := json.Marshal(message)
	BroadcastMessage(state.GetAllConnections(), serialized)
	state.RemovePlayerByWebsocket(ws)
}

///////////////// CONNECTION AND INCOMING MESSAGES /////////////////

func HandleWebsocket(w http.ResponseWriter, r *http.Request) {
	upgrader := websocket.Upgrader{} // use default options
	ws, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Print("upgrade:", err)
		return
	}
	// send game state message to newly connected client
	SendGameState(ws)
	// do player removal from game state and websocket close on disconnect
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
		// log message received
		fmt.Println("message received:")
		ConsoleLogJsonByteArray(message)
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
	err := http.ListenAndServe(*addr, nil)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
	}
}

///////////////// HELPERS /////////////////

func ConsoleLogJsonByteArray(message []byte) {
	var out bytes.Buffer
	json.Indent(&out, message, "", "  ")
	out.WriteTo(os.Stdout)
}
