import asyncio
from os import stat
import websockets
import json
import uuid
import logging


### LOGGING ###

logging.basicConfig(
    format="%(asctime)s %(message)s",
    level=logging.INFO,
)
class LoggerAdapter(logging.LoggerAdapter):
    """Add connection ID and client IP address to websockets logs."""
    def process(self, msg, kwargs):
        try:
            websocket = kwargs["extra"]["websocket"]
        except KeyError:
            return msg, kwargs
        xff = websocket.request_headers.get("X-Forwarded-For")
        return f"{websocket.id} {xff} {msg}", kwargs


### GAME STATE ###

class GameState():
    def __init__(self):
        self.connections = set()
        self.players = set()
    def add_player(self, player, websocket):
        self.players.add(player)
        self.connections.add(websocket)
    def remove_player(self, player):
        self.connections.remove(player.websocket)
        self.players.remove(player)
    def get_player_by_websocket(self, websocket):
        for player in self.players:
            if player.websocket is websocket:
                return player
        return None
    def to_dict(self):
        return {
            'connectionIds': [str(c.id) for c in self.connections],
            'players': [p.to_dict() for p in self.players]
        }
    
class Player():
    def __init__(self, websocket, id, position):
        self.websocket = websocket
        self.id = id
        self.position = position
    @staticmethod
    def from_dict(player_dict, websocket):
        player_id = player_dict['id']
        player_pos_dict = player_dict['position']
        position = Position.from_dict(player_pos_dict)
        return Player(websocket=websocket, id=player_id, position=position)
    def to_dict(self):
        return {
            'id': self.id,
            'websocketId': str(self.websocket.id),
            'position': self.position.to_dict(),
        }

class Position():
    def __init__(self, x, y):
        self.x = x
        self.y = y
    @staticmethod
    def from_dict(position_dict):
        return Position(position_dict['x'], position_dict['y'])
    def to_dict(self):
        return {
            'x': self.x,
            'y': self.y
        }

state = GameState()


### SERVER MESSAGES ###

SERVER_MESSAGE_TYPE_ENTER_PLAYER = 'SERVER_MESSAGE_TYPE_ENTER_PLAYER'
SERVER_MESSAGE_TYPE_EXIT_PLAYER = 'SERVER_MESSAGE_TYPE_EXIT_PLAYER'
SERVER_MESSAGE_TYPE_PLAYER_ID = 'SERVER_MESSAGE_TYPE_PLAYER_ID'
SERVER_MESSAGE_TYPE_PLAYER_UPDATE = 'SERVER_MESSAGE_TYPE_PLAYER_UPDATE'
SERVER_MESSAGE_TYPE_GAME_STATE = 'SERVER_MESSAGE_TYPE_GAME_STATE'

def enter_player_message(player):
    json_payload = json.dumps({
        'messageType': SERVER_MESSAGE_TYPE_ENTER_PLAYER,
        'player': player.to_dict()
    })
    logging.info("enter_player_message: " + json_payload)
    return json_payload

def exit_player_message(player):
    return json.dumps({
        'messageType': SERVER_MESSAGE_TYPE_EXIT_PLAYER,
        'player': player.to_dict()
    })

def player_update_message(player):
    return json.dumps({
        'messageType': SERVER_MESSAGE_TYPE_PLAYER_UPDATE,
        'player': player.to_dict()
    })

def game_state_message(state):
    return json.dumps({
        'messageType': SERVER_MESSAGE_TYPE_GAME_STATE,
        'gameState': state.to_dict()
    })


### CLIENT MESSAGE HANDLERS ###

CLIENT_MESSAGE_TYPE_PLAYER_ENTER = 'CLIENT_MESSAGE_TYPE_PLAYER_ENTER'
CLIENT_MESSAGE_TYPE_PLAYER_EXIT = 'CLIENT_MESSAGE_TYPE_PLAYER_EXIT'
CLIENT_MESSAGE_TYPE_PLAYER_UPDATE = 'CLIENT_MESSAGE_TYPE_PLAYER_UPDATE'
CLIENT_MESSAGE_TYPE_GET_GAME_STATE = 'CLIENT_MESSAGE_TYPE_GET_GAME_STATE'

async def route_message(message, websocket):
    logging.info('message received from game client: ' + message)
    message = json.loads(message)
    message_type_to_handler = {
        CLIENT_MESSAGE_TYPE_PLAYER_ENTER: handle_player_enter,
        CLIENT_MESSAGE_TYPE_PLAYER_EXIT: handle_player_exit,
        CLIENT_MESSAGE_TYPE_PLAYER_UPDATE: handle_player_update,
        CLIENT_MESSAGE_TYPE_GET_GAME_STATE: handle_get_game_state
    }
    if message['messageType'] in message_type_to_handler:
        await message_type_to_handler[message['messageType']](message, websocket)

async def handle_player_enter(message, websocket):
    logging.info("handle_player_enter message received", message)
    player_dict = message['player']
    player = Player.from_dict(player_dict, websocket)
    state.add_player(player=player, websocket=websocket)
    websockets.broadcast(state.connections, enter_player_message(player))
    logging.info('player entered: ' + json.dumps(player.to_dict()))
    logging.info('player count: ' + str(len(state.connections)))
    logging.info('game state: ' + json.dumps(state.to_dict()))

async def handle_player_exit(message, websocket):
    player = state.get_player_by_websocket(websocket)
    if player is not None:
        state.remove_player(player)
        websockets.broadcast(state.connections, exit_player_message(player))
        logging.info('player exited: ' + json.dumps(player.to_dict()))
        logging.info('player count: ' + str(len(state.connections)))
        logging.info('game state: ' + json.dumps(state.to_dict()))
    else:
        logging.warning('player not found by websocket id: ' + str(websocket.id))

async def handle_player_update(message, websocket):
    new_player_position = Position.from_dict(message['player']['position'])
    player = state.get_player_by_websocket(websocket)
    player.position = new_player_position
    websockets.broadcast(state.connections, player_update_message(player))

async def handle_get_game_state(message, websocket):
    pass


### RUN SERVER ###

async def handle_websocket(websocket, path):
    try:
        # sync server game state to newly connected game client
        await websocket.send(game_state_message(state))
        # route and handle messages for duration of websocket connection
        async for message in websocket:
            await route_message(message, websocket)
    finally:
        # upon websocket disconnect remove client's player
        await handle_player_exit(None, websocket)
        

async def main():
    async with websockets.serve(
		handle_websocket,
		host="0.0.0.0", 
		port=5000, 
		logger=LoggerAdapter(logging.getLogger("websockets.server"))
	):
        await asyncio.Future()

asyncio.run(main())