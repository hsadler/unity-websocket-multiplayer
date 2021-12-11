using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{

    public SceneManagerScript sceneManager;
    public bool isMainPlayer;

    // use for testing if you want to connect multiple players to the server and
    // see them moving
    public bool autopilotOn = false;

    private float moveSpeed = 5f;

    // autopilot movement for testing
    private List<Vector3> moveDirections = new List<Vector3> {
        Vector3.up,
        Vector3.right,
        Vector3.down,
        Vector3.left
    };
    private int currMoveDirIndex = 0;

    // UNITY HOOKS

    void Start()
    {
        if (!this.isMainPlayer)
        {
            this.gameObject.GetComponent<SpriteRenderer>().color = Color.red;
        }
        // autopilot movement for testing
        InvokeRepeating("SetNextMoveDirectionIndex", 0f, 1f);
    }

    void Update()
    {
        if (this.isMainPlayer)
        {
            this.HandleMovement();
        }
    }

    // IMPLEMENTATION METHODS

    private void HandleMovement()
    {
        var targetPos = this.transform.position;
        if (Input.anyKey)
        {
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
        }
        else if (this.autopilotOn)
        {
            targetPos += this.moveDirections[this.currMoveDirIndex];
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

    // autopilot movement for testing
    private void SetNextMoveDirectionIndex()
    {
        if (this.currMoveDirIndex == 3)
        {
            this.currMoveDirIndex = 0;
        }
        else
        {
            this.currMoveDirIndex += 1;
        }
    }

}
