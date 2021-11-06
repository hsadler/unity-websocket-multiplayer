import asyncio
from websockets import serve
import json
import logging


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


class GameState():
    def __init__(self):
        self.connections = set()
        self.players = []
    def get_api_formatted_data(self):
        return json.dumps(self.players)

class Player():
    def __init__(self):
        self.position = Position()
        self.size = 1

class Position():
    def __init__(self):
        self.x = 0
        self.y = 0

state = GameState()

async def game_handler(websocket, path):
    try:
        state.connections.add(websocket)
        logging.info('added websocket: ' + str(websocket))
        logging.info('total websockets: ' + str([str(x) for x in state.connections]))
        async for message in websocket:
            await websocket.send("hello: " + message)
    finally:
        state.connections.remove(websocket)
        logging.info('removed websocket: ' + str(websocket))
        logging.info('total websockets: ' + str([str(x) for x in state.connections]))

async def main():
    async with serve(
		game_handler, 
		host="0.0.0.0", 
		port=5000, 
		logger=LoggerAdapter(logging.getLogger("websockets.server"))
	):
        await asyncio.Future()


asyncio.run(main())