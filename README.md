# Unity Websocket Multiplayer

POC for a Unity game client and python server with websockets!


### Requirements
- Unity Editor: `version 2020.3.21f1`
- Docker
- Docker Compose


### Running Locally

1. Build the python websocket server image
```sh
docker build -t game-server:latest ./GameServer
```

2. Spin-up the server
```sh
docker-compose -f ./GameServer/docker-compose.yaml up
```

3. Open the Unity editor and run the game


### Notes

This is a very simple proof-of-concept and could be used as a starting point for 
making a multiplayer game with the Unity engine.

It demonstrates game-client to game-server connection establishment, message 
transmission, and connection termination with the websocket protocol.

The entire game-server's code resides in:
`GameServer/server.py`

The game-client's websocket management resides in:
`UnityWebSocketClient/Assets/Scripts/SceneManagerScript.cs`

Enjoy!
