using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragTarget : MonoBehaviour
{
    private Vector3 mOffset;
    private float mZCoord;
    void OnMouseDown()
    {
        // "World to Screen" z coordinate of the location of this gameObject relevant to our MainCamera
        mZCoord = Camera.main.WorldToScreenPoint(transform.position).z;
        // Store difference to compensate later = gameobject world pos - mouse world pos
        mOffset = gameObject.transform.position - GetMouseAsWorldPoint();
        // Change the material of the object as visual feedback
        GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        // Pixel coordinates of mouse (x,y)
        Vector3 mousePoint = Input.mousePosition;
        // z coordinate of game object on relevant to screen
        mousePoint.z = mZCoord;
        // Convert it back to world points
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    void OnMouseDrag()
    {
        // Assign it to the object's position this script's attached to and add the offset distance
        transform.position = GetMouseAsWorldPoint() + mOffset;
    }

    void OnMouseUp () 
    {
        // Change the material back of the object as visual feedback
        GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
    }
}