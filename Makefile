
build:
	docker build -t game-server:latest ./GameServer

up:	
	docker-compose -f ./GameServer/docker-compose.yaml up
