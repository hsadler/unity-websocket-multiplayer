# Unity Websocket Multiplayer

POC for a Unity game client and game server with websockets!

There are 2 implementations of the game server. One in Python and one in 
Golang. The APIs for both servers are identical.


### Requirements
- Unity Editor: `version 2020.3.21f1`
- Docker
- Docker Compose
- ngrok (optional)


### Running Locally

1. Build and spin-up one of the websocket servers.

python
```sh
docker build -t python-gameserver:latest ./PythonGameServer
docker-compose -f ./PythonGameServer/docker-compose.yaml up
```

golang
```sh
docker build -t golang-gameserver:latest ./GolangGameServer
docker-compose -f ./GolangGameServer/docker-compose.yaml up
```

2. Open the Unity editor and run the game.

*The players is simply a circle you can move around with WASD. Other connected*
*players are red and the main player is white.*


### Testing Multiple Client Connections

1. Build and spin-up one of the game servers locally.

2. Expose to the internet with ngrok.
```sh
ngrok tcp 5000
```

3. Copy the ngrok URL to the game client, but replace the prefix `tcp` with the
prefix `ws`.

*Now you can connect multiple players who can see each other's movement!*


### Notes

This is a very simple proof-of-concept and could be used as a starting point for 
making a multiplayer game with the Unity engine.

It demonstrates game-client to game-server connection establishment, message 
transmission, and connection termination with the websocket protocol.

The entire game-server's code resides in:  
`PythonGameServer/server.py` (python server)  
`GolangGameServer/server.go` (golang server)

The game-client's websocket management resides in:  
`UnityWebSocketClient/Assets/Scripts/SceneManagerScript.cs`

The Golang game server's architecture is inspired by [this](https://github.com/gorilla/websocket/tree/master/examples/chat) chat example.

Enjoy!
