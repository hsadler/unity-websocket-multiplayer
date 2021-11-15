
build-python-game-server:
	docker build -t python-gameserver:latest ./PythonGameServer

up-python-game-server:	
	docker-compose -f ./PythonGameServer/docker-compose.yaml up

build-golang-game-server:
	docker build -t golang-gameserver:latest ./GolangGameServer

up-golang-game-server:
	docker-compose -f ./GolangGameServer/docker-compose.yaml up
