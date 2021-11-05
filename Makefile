
build:
	cd GameServer && docker build -t game-server:latest .

up:	
	cd GameServer && docker-compose -f docker-compose.yaml up
