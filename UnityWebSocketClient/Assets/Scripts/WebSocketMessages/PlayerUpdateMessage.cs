using System;

[Serializable]
public class PlayerUpdateMesssage
{

    public string messageType = "CLIENT_MESSAGE_TYPE_PLAYER_UPDATE";
    public PlayerModel playerModel;

    public PlayerUpdateMesssage(PlayerModel playerModel)
    {
        this.playerModel = playerModel;
    }

}
