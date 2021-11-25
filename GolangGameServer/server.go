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

///////////////// HUB /////////////////

type Hub struct {
	Clients   map[*Client]bool
	Add       chan *Client
	Remove    chan *Client
	Broadcast chan []byte
}

func (h *Hub) Run() {
	for {
		select {
		case client := <-h.Add:
			fmt.Println("adding client from hub")
			h.Clients[client] = true
		case client := <-h.Remove:
			fmt.Println("removing client from hub")
			delete(h.Clients, client)
			client.Destroy()
		case message := <-h.Broadcast:
			for c := range h.Clients {
				c.Send <- message
			}
		default:
			// fmt.Println("nothing for hub to process...")
		}
	}
}

func NewHub() *Hub {
	return &Hub{
		Clients:   make(map[*Client]bool),
		Add:       make(chan *Client),
		Remove:    make(chan *Client),
		Broadcast: make(chan []byte),
	}
}

///////////////// CLIENT /////////////////

type Client struct {
	Hub    *Hub
	Ws     *websocket.Conn
	Player *Player
	Send   chan []byte
}

func (cl *Client) RecieveMessages() {
	// initialize client's game state by sending server's entire state
	cl.SendGameState()
	// do player removal from game state and websocket close on disconnect
	defer func() {
		fmt.Println("RecieveMessages goroutine stopping")
		cl.HandlePlayerExit(nil)
		cl.Ws.Close()
	}()
	for {
		// read message
		_, message, err := cl.Ws.ReadMessage()
		if err != nil {
			log.Println("read:", err)
			break
		}
		// log message received
		fmt.Println("message received:")
		ConsoleLogJsonByteArray(message)
		// route message to handler
		messageTypeToHandler := map[string]func(map[string]interface{}){
			"CLIENT_MESSAGE_TYPE_PLAYER_ENTER":  cl.HandlePlayerEnter,
			"CLIENT_MESSAGE_TYPE_PLAYER_UPDATE": cl.HandlePlayerUpdate,
			"CLIENT_MESSAGE_TYPE_PLAYER_EXIT":   cl.HandlePlayerExit,
		}
		var mData map[string]interface{}
		if err := json.Unmarshal(message, &mData); err != nil {
			panic(err)
		}
		// process message with handler
		messageTypeToHandler[mData["messageType"].(string)](mData)
	}
}

func (cl *Client) SendGameState() {
	allPlayers := make([]*Player, 0)
	for client := range cl.Hub.Clients {
		allPlayers = append(allPlayers, client.Player)
	}
	messageData := GameStateMessage{
		MessageType: "SERVER_MESSAGE_TYPE_GAME_STATE",
		GameState: &GameStateJsonSerializable{
			Players: allPlayers,
		},
	}
	serialized, _ := json.Marshal(messageData)
	cl.Send <- serialized
}

func (cl *Client) HandlePlayerEnter(mData map[string]interface{}) {
	player := NewPlayerFromMap(mData["player"].(map[string]interface{}), cl.Ws)
	cl.Player = player
	message := PlayerMessage{
		MessageType: "SERVER_MESSAGE_TYPE_PLAYER_ENTER",
		Player:      player,
	}
	serialized, _ := json.Marshal(message)
	cl.Hub.Broadcast <- serialized
}

func (cl *Client) HandlePlayerUpdate(mData map[string]interface{}) {
	player := NewPlayerFromMap(mData["player"].(map[string]interface{}), cl.Ws)
	cl.Player = player
	message := PlayerMessage{
		MessageType: "SERVER_MESSAGE_TYPE_PLAYER_UPDATE",
		Player:      player,
	}
	serialized, _ := json.Marshal(message)
	cl.Hub.Broadcast <- serialized
}

func (cl *Client) HandlePlayerExit(mData map[string]interface{}) {
	message := PlayerMessage{
		MessageType: "SERVER_MESSAGE_TYPE_PLAYER_EXIT",
		Player:      cl.Player,
	}
	serialized, _ := json.Marshal(message)
	cl.Hub.Broadcast <- serialized
	cl.Hub.Remove <- cl
}

func (cl *Client) SendMessages() {
	defer func() {
		fmt.Println("SendMessages goroutine stopping")
	}()
	for {
		select {
		case message, ok := <-cl.Send:
			if !ok {
				return
			}
			SendJsonMessage(cl.Ws, message)
		default:
			// fmt.Println("no message to send...")
		}
	}
}

func (cl *Client) Destroy() {
	fmt.Println("closing client send channel")
	close(cl.Send)
}

///////////////// PLAYER /////////////////

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
	// log that message was sent
	fmt.Println("message sent:")
	ConsoleLogJsonByteArray(messageJson)
}

type PlayerMessage struct {
	MessageType string  `json:"messageType"`
	Player      *Player `json:"player"`
}

type GameStateJsonSerializable struct {
	Players []*Player `json:"players"`
}

type GameStateMessage struct {
	MessageType string                     `json:"messageType"`
	GameState   *GameStateJsonSerializable `json:"gameState"`
}

///////////////// RUN SERVER /////////////////

func main() {
	flag.Parse()
	log.SetFlags(0)
	// create and run hub singleton
	h := NewHub()
	go h.Run()
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		upgrader := websocket.Upgrader{} // use default options
		ws, err := upgrader.Upgrade(w, r, nil)
		if err != nil {
			log.Print("upgrade:", err)
			return
		}
		// create client, run processes, and add to hub
		cl := &Client{
			Hub:    h,
			Ws:     ws,
			Player: nil,
			Send:   make(chan []byte, 256),
		}
		go cl.RecieveMessages()
		go cl.SendMessages()
		h.Add <- cl
	})
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
