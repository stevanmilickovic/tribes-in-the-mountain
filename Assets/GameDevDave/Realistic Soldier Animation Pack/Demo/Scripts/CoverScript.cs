using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Credits: https://www.habrador.com/tutorials/interpolation/2-bezier-curve/

public class CoverScript : MonoBehaviour 
{
    //Has to be at least 4 so-called control points
    public Transform startPoint, endPoint, controlPointStart, controlPointEnd;
    public Transform player, leftWallCoverLocator, rightWallCoverLocator; 
    public DemoSystemScript demo;
    public Animator animator;
    public RootMotionScript root;
    public bool generatePath, rightSide;
    Vector3 newPos;

    //Easier to use ABCD for the positions of the points so they are the same as in the tutorial image
    Vector3 A, B, C, D;

    // Methods that send us to the right/left side of the wall for cover
    public void RightSideOfWall () 
    {
        rightSide = true; 
        // This allows us to switch sides with the UI arrow buttons when we are in cover idle
        if (demo.tagName == "InCoverIdle") 
        StartCoroutine(GoIntoCorrectCoverSpot(rightWallCoverLocator));
    }
    public void LeftSideOfWall () 
    {
        rightSide = false;
        // This allows us to switch sides with the UI arrow buttons when we are in cover idle
        if (demo.tagName == "InCoverIdle") 
        StartCoroutine(GoIntoCorrectCoverSpot(leftWallCoverLocator));
    }

    IEnumerator GoIntoCorrectCoverSpot (Transform side) 
    {
        float t = 0;
        
        while (t <= 1) 
        {
            player.position = Vector3.Lerp(player.position, side.position, t += .003f);
            player.rotation = Quaternion.Lerp(player.rotation, side.rotation, t += .003f);
            // As long as the player is not in the correct position yet, we loop this coroutine
            yield return null; 
        }        
    }   

    void Update () 
    {
        // This can be done with a single execution too, but it's fun to see it happen in realtime in the editor
        // We shut this bool off as soon as we want the character to follow this path 
        if (generatePath) 
        {
            var W = demo.wall.transform.position;
            var A = startPoint.position;
            var B = controlPointStart.position;
            var C = controlPointEnd.position;
            var D = endPoint.position;

            // The starting point should always be the player of course
            startPoint.position = player.position;

            // This is hard to explain but you can understand it by seeing it work in the scene editor next to reading the code 
            if (A.z > W.z) 
            {
                if (A.x > W.x)
                controlPointStart.position = new Vector3 (A.z / 2, B.y, B.z);
                else 
                controlPointStart.position = new Vector3 (-A.z / 2, B.y, B.z);

                controlPointEnd.position = new Vector3 (C.x, C.y, -A.z / 2);
            }
            else 
            {
                controlPointStart.position = controlPointEnd.position = new Vector3 (D.x, 0, 0);
            }
        }
    }

    public IEnumerator SendPlayerToCoverSpot()
    {
        // Enable rootmotion control for cover
        root.cover = true;

        A = startPoint.position;
        B = controlPointStart.position;
        C = controlPointEnd.position;
        D = endPoint.position;

        // Quality, a higher number will result in more iterations
        float resolution = 0.01f;

        //How many loops?
        int loops = Mathf.FloorToInt(1f / resolution);

        for (int i = 1; i <= loops; i++)
        {
            //Which t position are we at?
            float t = i * resolution;

            //Find the coordinates between the control points with a Catmull-Rom spline
            newPos = DeCasteljausAlgorithm(t);

            // Send off the rotational data to the Rootmotion Script
            root.orderedTurnValue = RotateYToAimZ(player.position, newPos);

            // Wait until player has reached destination and then continue the loop 
            yield return new WaitUntil (PlayerArrived);
        }

        // Call the right or leftcover trigger so we're bashing against the wall at the right time
        if (rightSide)
        player.GetComponent<Animator>().SetTrigger("GoCoverRight");
        else
        player.GetComponent<Animator>().SetTrigger("GoCoverLeft");

        // Make 100% sure the character has the correct position & rotation (mends with animation well)
        float elapsedTime = 0; 
        while (elapsedTime < 2)
        {
            if (rightSide) 
            {
                player.position = Vector3.Slerp(player.position, rightWallCoverLocator.position, elapsedTime / 2);
                player.rotation = Quaternion.Slerp(player.rotation, rightWallCoverLocator.rotation, elapsedTime / 2);
            }
            else 
            {
                player.position = Vector3.Slerp(player.position, leftWallCoverLocator.position, elapsedTime / 2);
                player.rotation = Quaternion.Slerp(player.rotation, leftWallCoverLocator.rotation, elapsedTime / 2);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Start generating path again for potential new route 
        generatePath = true;
        // Disable the rootmotion rotation control
        root.cover = false;
    }

    // See if the player arrived at the new position so we can continue the coroutine 
    // We use this method in the coroutine above
    bool PlayerArrived()
    {
        return (Mathf.Abs(player.position.x - newPos.x) < 0.1f && Mathf.Abs(player.position.z - newPos.z) < 0.1f);
    }

    // A quick method that calculates a Quaternion angle based on a Vector3 for Y as rotation to aim the Z axis at the Target
    Quaternion RotateYToAimZ (Vector3 origin, Vector3 Target)     
    {				
		// Calculate vector direction
		Vector3 targetDirection = Target - origin;
		// Convert vector3 to radians and radians to degrees
		float targetAngle = Mathf.Atan2 (targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
		// Create an angle that rotates on y and aims the blue axis 
		Quaternion rotateYToAimZ = Quaternion.AngleAxis(targetAngle, Vector3.up);	
        // Return quaternion
		return rotateYToAimZ;
    }

    //Displays the curve in the editor
    void OnDrawGizmos()
    {
        A = startPoint.position;
        B = controlPointStart.position;
        C = controlPointEnd.position;
        D = endPoint.position;

	    //The Bezier curve's color
        Gizmos.color = Color.white;

        //The start position of the line
        Vector3 lastPos = A;

        //The resolution of the line
        //Make sure the resolution is adding up to 1, so 0.3 will give a gap at the end, but 0.2 will work
        float resolution = 0.01f;

        //How many loops?
        int loops = Mathf.FloorToInt(1f / resolution);

        for (int i = 1; i <= loops; i++)
        {
            //Which t position are we at?
            float t = i * resolution;

            //Find the coordinates between the control points with a Catmull-Rom spline
            Vector3 newPos = DeCasteljausAlgorithm(t);

            //Draw this line segment
            Gizmos.DrawLine(lastPos, newPos);

            //Save this pos so we can draw the next line segment
            lastPos = newPos;
        }
		
	    //Also draw lines between the control points and endpoints
        Gizmos.color = Color.green;

        Gizmos.DrawLine(A, B);
        Gizmos.DrawLine(C, D);
    }

    //The De Casteljau's Algorithm
    Vector3 DeCasteljausAlgorithm(float t)
    {
        //Linear interpolation = lerp = (1 - t) * A + t * B
        //Could use Vector3.Lerp(A, B, t)

        //To make it faster
        float oneMinusT = 1f - t;
        
        //Layer 1
        Vector3 Q = oneMinusT * A + t * B;
        Vector3 R = oneMinusT * B + t * C;
        Vector3 S = oneMinusT * C + t * D;

        //Layer 2
        Vector3 P = oneMinusT * Q + t * R;
        Vector3 T = oneMinusT * R + t * S;

        //Final interpolated position
        Vector3 U = oneMinusT * P + t * T;

        return U;
    }
}