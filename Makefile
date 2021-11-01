
build:
	cd GameServer && docker build -t game-server:latest .

up:	
	cd GameServer && docker run --rm -p 5000:5000 game-server