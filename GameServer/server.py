import asyncio
from websockets import serve
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

async def echo(websocket, path):
    async for message in websocket:
        logging.info('message: ' + message)
        await websocket.send(message)

async def main():
    async with serve(
		echo, 
		host="0.0.0.0", 
		port=5000, 
		logger=LoggerAdapter(logging.getLogger("websockets.server"))
	):
        await asyncio.Future()

asyncio.run(main())