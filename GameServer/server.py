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


### GAME STATE AND OBJECTS ###

class GameState():
    def __init__(self):
        self.connections = set()
        self.player_id_to_player = {}
    def add_player(self, player, websocket):
        self.player_id_to_player[player.id] = player
        self.connections.add(websocket)
    def remove_player(self, player):
        logging.info('removing player by id: ' + player.id)
        self.connections.remove(player.websocket)
        del self.player_id_to_player[player.id]
    def get_player_by_websocket(self, websocket):
        for player in self.player_id_to_player.values():
            if player.websocket is websocket:
                return player
        return None
    def to_dict(self):
        player_dict = {}
        for id, player in self.player_id_to_player.items():
            player_dict[id] = player.to_dict()
        return {
            'connection_ids': [str(c.id) for c in self.connections],
            'players': player_dict
        }
    
class Player():
    def __init__(self, websocket):
        self.websocket = websocket
        self.id = str(uuid.uuid4().hex)
        self.pos = Position()
        self.size = 1
    def to_dict(self):
        return {
            'player_id': self.id,
            'websocket_id': str(self.websocket.id),
            'pos': self.pos.to_dict(),
            'size': self.size
        }

class Position():
    def __init__(self):
        self.x = float(0)
        self.y = float(0)
    def to_dict(self):
        return {
            'x': self.x,
            'y': self.y
        }

state = GameState()


### EVENT HANDLING ###

EVENT_TYPE_ENTER_PLAYER = 'EVENT_TYPE_ENTER_PLAYER'
EVENT_TYPE_EXIT_PLAYER = 'EVENT_TYPE_EXIT_PLAYER'
EVENT_TYPE_GAME_STATE = 'EVENT_TYPE_GAME_STATE'

def enter_player_event(player):
    return json.dumps({
        'type': EVENT_TYPE_ENTER_PLAYER,
        'player': player.to_dict()
    })

def exit_player_event(player):
    return json.dumps({
        'type': EVENT_TYPE_EXIT_PLAYER,
        'player': player.to_dict()
    })

def game_state_event(state):
    return json.dumps({
        'type': EVENT_TYPE_GAME_STATE,
        'state': state.to_dict()
    })

async def game_handler(websocket, path):
    try:
        player = Player(websocket=websocket)
        state.add_player(player=player, websocket=websocket)
        websockets.broadcast(state.connections, enter_player_event(player))
        logging.info('player entered: ' + json.dumps(player.to_dict()))
        logging.info('player count: ' + str(len(state.connections)))
        logging.info('game state: ' + json.dumps(state.to_dict()))
        async for message in websocket:
            await websocket.send("hello: " + message)
    finally:
        player = state.get_player_by_websocket(websocket)
        if player is not None:
            state.remove_player(player)
            websockets.broadcast(state.connections, exit_player_event(player))
            logging.info('player exited: ' + json.dumps(player.to_dict()))
            logging.info('player count: ' + str(len(state.connections)))
            logging.info('game state: ' + json.dumps(state.to_dict()))
        else:
            logging.warn('player not found by websocket id: ' + str(websocket.id))


### RUN SERVER ###

async def main():
    async with websockets.serve(
		game_handler, 
		host="0.0.0.0", 
		port=5000, 
		logger=LoggerAdapter(logging.getLogger("websockets.server"))
	):
        await asyncio.Future()

asyncio.run(main())