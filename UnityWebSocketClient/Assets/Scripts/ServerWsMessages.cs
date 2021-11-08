using System;

[Serializable]
public class ServerMessageGeneric
{
    public string messageType;
}

[Serializable]
public class ServerMessagePlayerEnter
{
    public string messageType;
    public Player player;
}

[Serializable]
public class ServerMessagePlayerExit
{
    public string messageType;
    public Player player;
}

[Serializable]
public class ServerMessagePlayerUpdate
{
    public string messageType;
    public Player player;
}

[Serializable]
public class ServerMessageGameState
{
    // TODO: implement stub class
    public string messageType;
    public GameState gameState;
}