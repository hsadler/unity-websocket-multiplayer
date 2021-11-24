import asyncio
import websockets
import json
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
    def __init__(self, game_state_lock):
        self.game_state_lock = game_state_lock
        self.connections = set()
        self.players = set()
    async def add_player(self, player, websocket):
        async with self.game_state_lock:
            self.players.add(player)
            self.connections.add(websocket)
    async def remove_player(self, player):
        async with self.game_state_lock:
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

game_state = GameState(game_state_lock=asyncio.Lock())


### SERVER MESSAGES ###

SERVER_MESSAGE_TYPE_PLAYER_ENTER = 'SERVER_MESSAGE_TYPE_PLAYER_ENTER'
SERVER_MESSAGE_TYPE_PLAYER_EXIT = 'SERVER_MESSAGE_TYPE_PLAYER_EXIT'
SERVER_MESSAGE_TYPE_PLAYER_UPDATE = 'SERVER_MESSAGE_TYPE_PLAYER_UPDATE'
SERVER_MESSAGE_TYPE_GAME_STATE = 'SERVER_MESSAGE_TYPE_GAME_STATE'

def enter_player_message(player):
    json_payload = json.dumps({
        'messageType': SERVER_MESSAGE_TYPE_PLAYER_ENTER,
        'player': player.to_dict()
    })
    return json_payload

def exit_player_message(player):
    return json.dumps({
        'messageType': SERVER_MESSAGE_TYPE_PLAYER_EXIT,
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
        'gameState': game_state.to_dict()
    })


### CLIENT MESSAGE HANDLERS ###

CLIENT_MESSAGE_TYPE_PLAYER_ENTER = 'CLIENT_MESSAGE_TYPE_PLAYER_ENTER'
CLIENT_MESSAGE_TYPE_PLAYER_EXIT = 'CLIENT_MESSAGE_TYPE_PLAYER_EXIT'
CLIENT_MESSAGE_TYPE_PLAYER_UPDATE = 'CLIENT_MESSAGE_TYPE_PLAYER_UPDATE'

async def route_message(message, websocket):
    message = json.loads(message)
    message_type_to_handler = {
        CLIENT_MESSAGE_TYPE_PLAYER_ENTER: handle_player_enter,
        CLIENT_MESSAGE_TYPE_PLAYER_EXIT: handle_player_exit,
        CLIENT_MESSAGE_TYPE_PLAYER_UPDATE: handle_player_update,
    }
    if message['messageType'] in message_type_to_handler:
        await message_type_to_handler[message['messageType']](message, websocket)

async def handle_player_enter(message, websocket):
    player_dict = message['player']
    player = Player.from_dict(player_dict, websocket)
    await game_state.add_player(player=player, websocket=websocket)
    websockets.broadcast(game_state.connections, enter_player_message(player))
    logging.info('player entered: ' + json.dumps(player.to_dict(), indent=2))
    logging.info('player count: ' + str(len(game_state.connections)))
    logging.info('game state: ' + json.dumps(game_state.to_dict(), indent=2))

async def handle_player_exit(message, websocket):
    player = game_state.get_player_by_websocket(websocket)
    if player is not None:
        await game_state.remove_player(player)
        websockets.broadcast(game_state.connections, exit_player_message(player))
        logging.info('player exited: ' + json.dumps(player.to_dict(), indent=2))
        logging.info('player count: ' + str(len(game_state.connections)))
        logging.info('game state: ' + json.dumps(game_state.to_dict(), indent=2))
    else:
        logging.warning('player not found by websocket id: ' + str(websocket.id))

async def handle_player_update(message, websocket):
    new_player_position = Position.from_dict(message['player']['position'])
    player = game_state.get_player_by_websocket(websocket)
    async with game_state.game_state_lock:
        player.position = new_player_position
    websockets.broadcast(game_state.connections, player_update_message(player))
    logging.info('player update: ' + json.dumps(player.to_dict(), indent=2))


### RUN SERVER ###

async def handle_websocket(websocket, path):
    try:
        # sync server game state to newly connected game client
        await websocket.send(game_state_message(game_state))
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