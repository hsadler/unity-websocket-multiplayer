using System;

[Serializable]
public class PlayerEnterMessage
{

    public string messageType = "CLIENT_MESSAGE_TYPE_PLAYER_ENTER";
    public Player player;

    public PlayerEnterMessage(Player playerModel)
    {
        this.player = playerModel;
    }

}

[Serializable]
public class PlayerUpdateMesssage
{

    public string messageType = "CLIENT_MESSAGE_TYPE_PLAYER_UPDATE";
    public Player player;

    public PlayerUpdateMesssage(Player playerModel)
    {
        this.player = playerModel;
    }

}

[Serializable]
public class GetGameStateMessage
{
    public string messageType = "CLIENT_MESSAGE_TYPE_GET_GAME_STATE";
}