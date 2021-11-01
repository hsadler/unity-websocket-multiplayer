from flask import Flask
from flask_socketio import SocketIO, emit

app = Flask(__name__)
socketio = SocketIO(app, cors_allowed_origins="*")


@app.route("/")
def hello_world():
    return "hello, world!"

@socketio.on('connect')
def test_connect():
	print('connected')
	emit('my response', {'data': 'Connected'})

@socketio.on('disconnect')
def test_disconnect():
	print('Client disconnected')


if __name__ == "__main__":
    socketio.run(app, host="0.0.0.0", port=5000, debug=True)
