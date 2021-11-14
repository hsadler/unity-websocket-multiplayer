
build-python-game-server:
	docker build -t python-gameserver:latest ./PythonGameServer

up-python-game-server:	
	docker-compose -f ./PythonGameServer/docker-compose.yaml up
