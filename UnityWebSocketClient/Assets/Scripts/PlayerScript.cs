using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{

    public SceneManagerScript sceneManager;

    private float moveSpeed = 5f;

    // UNITY HOOKS

    void Start()
    {

    }

    void Update()
    {
        this.HandleMovement();
    }

    // IMPLEMENTATION METHODS

    private void HandleMovement()
    {
        var targetPos = this.transform.position;
        // left
        if (Input.GetKey(KeyCode.A))
        {
            targetPos += Vector3.left;
        }
        // right
        if (Input.GetKey(KeyCode.D))
        {
            targetPos += Vector3.right;
        }
        // up
        if (Input.GetKey(KeyCode.W))
        {
            targetPos += Vector3.up;
        }
        // down
        if (Input.GetKey(KeyCode.S))
        {
            targetPos += Vector3.down;
        }
        if (targetPos != this.transform.position)
        {
            
            this.transform.position = Vector3.MoveTowards(
                this.transform.position,
                targetPos,
                Time.deltaTime * this.moveSpeed
            );
            this.sceneManager.SyncPlayerState(this.gameObject);
        }
    }

}
