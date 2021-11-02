
build:
	cd GameServer && docker build -t game-server:latest .

up:	
	cd GameServer && docker run --rm -e PYTHONUNBUFFERED=1 -p 5000:5000 game-server