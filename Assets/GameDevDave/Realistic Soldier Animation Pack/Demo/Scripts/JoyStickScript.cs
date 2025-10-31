using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class JoyStickScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    bool HoldingJoyStick;
    Vector3 mouse;
    public float deadzone;
    public Vector2 direction;
    
    // Single frame OnPointer clicks for the UI button this script is attached to
    public void OnPointerDown(PointerEventData eventData)
    {
        JoyStickEnabled();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        JoyStickDisabled();
    }

    void Update () 
    {
        if (HoldingJoyStick) 
        {
            // Get the Mouse's x axis and y axis Delta's (speed of mouse x,y)
            mouse = new Vector3 (Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            // Add that to the position of the Handle 
            transform.position += mouse * 40;

            // Make sure the Handle doesn't leave a certain radius
            // By clamping its position inside a certain vector length (the vector's magnitude)
            // We add +1 to the length so we can reach a whole number, we're clamping it when we configure the direction
            transform.localPosition = Vector3.ClampMagnitude (transform.localPosition, 100 + 1);
        }

        // The direction modified to give us a 0 - 1 value in both axes 
        ConfigureDirectionVector(); 
    }

    void JoyStickEnabled () 
    {
        // Initiate Hold
        HoldingJoyStick = true;
        // Disable visibility of the cursor
        Cursor.visible = false;
    }
    void JoyStickDisabled () 
    {
        // Initiate Release
        HoldingJoyStick = false;
        // Enable visibility of the cursor
        Cursor.visible = true;
    }
    public void ResetJoystick () 
    {
        // Reset the joystick handle back to its original position when we release 
        transform.localPosition = Vector2.zero;
    }
    void ConfigureDirectionVector () 
    {
        // Establish both x and y values of the handle's localposition
        float x = Mathf.Clamp (transform.localPosition.x / 100, -1, 1);
        float y = Mathf.Clamp (transform.localPosition.y / 100, -1, 1);

        // Declare new x and y values to modify them
        float xNew = 0, yNew = 0;

        // The Absolute values of x and y (no matter if eg. x is negative or positive, Mathf.Abs will return a positive value)
        // We use this to configure a deadzone
        if (Mathf.Abs (x) > deadzone)
        xNew = x;
        if (Mathf.Abs (y) > deadzone)
        yNew = y;

        // Insert the new x and y variables into a new vector
        Vector2 dir = new Vector2 (xNew, yNew); 

        // Assign our modified direction dir to public Vector2 direction for use outside of this script
        direction = dir;
    }
}
