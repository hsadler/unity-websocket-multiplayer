using System;
using UnityEngine;

[Serializable]
public class PlayerModel
{

    public string uuid;
    public Vector3 position;

    public PlayerModel(Vector3 position)
    {
        this.uuid = System.Guid.NewGuid().ToString();
        this.position = position;
    }

}
