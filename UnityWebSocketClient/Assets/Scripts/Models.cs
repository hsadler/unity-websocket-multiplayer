using System;
using UnityEngine;

[Serializable]
public class Player
{

    public string id;
    public Position position;

    public Player(string id, Position position)
    {
        this.id = id;
        this.position = position;
    }

}

[Serializable]
public class Position
{

    public float x;
    public float y;

    public Position(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

}
