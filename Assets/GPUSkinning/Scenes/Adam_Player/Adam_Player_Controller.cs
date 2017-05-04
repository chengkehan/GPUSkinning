using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Adam_Player_Controller : MonoBehaviour
{
    private GPUSkinningPlayer player = null;

    private Transform camTransform = null;

    private Transform thisTransform = null;

    private Vector3 camOffsetPos;

    private bool isForward = false;

    private float forwardSpeed = 0;

    private bool isBackward = false;

    private bool isTurningRight = false;

    private bool isTurningLeft = false;

	private void Start ()
    {
        thisTransform = transform;
        camTransform = Camera.main.transform;

        camOffsetPos = camTransform.position - thisTransform.position;

        player = GetComponent<GPUSkinningPlayerMono>().Player;
        player.Play("Idle");
	}
	
	private void Update ()
    {
        if(Input.GetKeyDown(KeyCode.W))
        {
            isForward = true;
        }
        if(Input.GetKeyUp(KeyCode.W))
        {
            isForward = false;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            isBackward = true;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            isBackward = false;
        }
        if(Input.GetKeyDown(KeyCode.A))
        {
            isTurningLeft = true;
        }
        if(Input.GetKeyUp(KeyCode.A))
        {
            isTurningLeft = false;
        }
        if(Input.GetKeyDown(KeyCode.D))
        {
            isTurningRight = true;
        }
        if(Input.GetKeyUp(KeyCode.D))
        {
            isTurningRight = false;
        }

        if (isForward)
        {
			forwardSpeed += 0.5f * Time.deltaTime;
        }
		if(!isForward)
        {
			if(isBackward)
			{
				forwardSpeed -= 0.2f * Time.deltaTime;
			}
			else
			{
				forwardSpeed -= 1.25f * Time.deltaTime;
			}
        }
		if(isTurningLeft || isTurningRight)
		{
			forwardSpeed -= 0.8f * Time.deltaTime;
		}
        forwardSpeed = Mathf.Clamp01(forwardSpeed);

		if(forwardSpeed == 0)
        {
            if(isTurningLeft)
            {
                player.CrossFade("TurnOnSpotLeftB", 0.2f);
            }
            else if(isTurningRight)
            {
                player.CrossFade("TurnOnSpotRightB", 0.2f);
            }
            else if (isBackward)
            {
                player.CrossFade("TurnOnSpotRightD", 0.2f);
            }
            else
            {
                player.CrossFade("Idle", 0.2f);
            }
        }
        if(forwardSpeed > 0 && forwardSpeed <= 0.4f)
        {
            if(isTurningLeft)
            {
                player.CrossFade("PlantNTurneft90", 0.2f);
            }
            else if(isTurningRight)
            {
                player.CrossFade("PlantNTurnRigtht90", 0.2f);
            }
            else if (isBackward)
            {
                player.CrossFade("PlantNTurnRight180", 0.2f);
            }
            else
            {
                player.CrossFade("Walk", 0.2f);
            }
        }
        if(forwardSpeed > 0.4f)
        {
            if (isTurningLeft)
            {
                player.CrossFade("PlantNTurneft90", 0.2f);
            }
            else if (isTurningRight)
            {
                player.CrossFade("PlantNTurnRigtht90", 0.2f);
            }
            else if (isBackward)
            {
                player.CrossFade("PlantNTurnRight180", 0.2f);
            }
            else
            {
                player.CrossFade("Run", 0.2f);
            }
        }
	}

    private void LateUpdate()
    {
        CameraFollow();
    }

    private void CameraFollow()
    {
        camTransform.position = thisTransform.position + camOffsetPos;
    }

    private void OnGUI()
    {
        float boxSize = Screen.height / 8;
        DrawGUI_ASDW(new Rect(boxSize, 0, boxSize, boxSize), isForward, "W");
        DrawGUI_ASDW(new Rect(boxSize, boxSize, boxSize, boxSize), isBackward, "S");
        DrawGUI_ASDW(new Rect(0, boxSize, boxSize, boxSize), isTurningLeft, "A");
        DrawGUI_ASDW(new Rect(boxSize * 2, boxSize, boxSize, boxSize), isTurningRight, "D");
    }

    private void DrawGUI_ASDW(Rect rect, bool isDoing, string label)
    {
        if (isDoing)
        {
            GUI.Button(rect, string.Empty);
        }
        GUI.Button(rect, label);
    }
}
