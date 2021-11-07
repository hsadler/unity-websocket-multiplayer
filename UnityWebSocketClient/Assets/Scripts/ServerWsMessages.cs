using System;

[Serializable]
public class GenericMessage
{
    public string messageType;
}

[Serializable]
public class EnterPlayerMessage
{
    public string messageType;
    public Player player;
}