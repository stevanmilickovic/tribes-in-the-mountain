using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine;

/// <summary>
/// A simple free camera to be added to a Unity game object.
/// 
/// Keys:
///	wasd / arrows	- movement
///	q/e or space/c	- up/down (local space)
///	r/f 			- up/down (world space)
///	pageup/pagedown	- up/down (world space)
///	hold shift		- enable fast movement mode
///	right mouse  	- enable free look
///	mouse			- free look / rotation
///     
/// </summary>

public class FreeCam : MonoBehaviour
{
    public GameObject ball;
    public GameObject playerLocator;
    public bool camEngaged;
    public bool shootBallOn;
    /// <summary>
    /// Normal speed of camera movement.
    /// </summary>
    public float movementSpeed = 10f;

    /// <summary>
    /// Speed of camera movement when shift is held down,
    /// </summary>
    public float fastMovementSpeed = 100f;

    /// <summary>
    /// Sensitivity for free look.
    /// </summary>
    public float freeLookSensitivity = 3f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    public float zoomSensitivity = 10f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    public float fastZoomSensitivity = 50f;

    /// <summary>
    /// Set to true when free looking (on right mouse button).
    /// </summary>
    public bool looking, orbiting;
    [HideInInspector]
    public bool orbitalFocus;

    void Start () 
    {
        EnableOrbitMode();
    }

    void Update()
    {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var movementSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            transform.position = transform.position + (transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            transform.position = transform.position + (-transform.forward * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Q))
        {
            transform.position = transform.position + (transform.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.LeftControl))
        {
            transform.position = transform.position + (-transform.up * movementSpeed * Time.deltaTime);
        }

        if (looking)
        {
            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        if (orbiting)
        {
            float newRotationX =+ Input.GetAxis("Mouse X");
            float newRotationY =+ Input.GetAxis("Mouse Y");
            transform.RotateAround (playerLocator.transform.position, playerLocator.transform.up, newRotationX);
            transform.RotateAround (playerLocator.transform.position, transform.right, -newRotationY);                   
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            var zoomSensitivity = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            transform.position = transform.position + transform.forward * axis * zoomSensitivity;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLookingOrOrbiting();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLookingOrOrbiting();
        }

        // Enable/Disable orbital focus
        if (Input.GetKeyDown(KeyCode.F))
        {
            EnableOrbitMode();
        }

        if (shootBallOn && looking) 
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                ball = Instantiate(ball, transform.position, Quaternion.identity);
                ball.GetComponent<Rigidbody>().velocity = transform.forward * 50;
            }
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                fireBalls = AutoFireBalls();
                StartCoroutine(fireBalls);
                
            }
            if (Input.GetKeyUp(KeyCode.Mouse2))
            {
                StopCoroutine(fireBalls);
                
            }
        }
    }

    void EnableOrbitMode () 
    {
        orbitalFocus = !orbitalFocus;

        if (orbitalFocus) 
        {
            transform.LookAt(playerLocator.transform.position);
            var dir = playerLocator.transform.position - transform.position;
            transform.position = playerLocator.transform.position - dir.normalized * 3;
            transform.SetParent(playerLocator.transform);
        }
        else transform.SetParent(null);
    }

    public IEnumerator fireBalls;
    IEnumerator AutoFireBalls () 
    {
        for (int i = 0; i < 3; i++)
        {
            if (i > 1) {i = 0;}
            yield return new WaitForSecondsRealtime(0.02f);
            ball = Instantiate(ball, transform.position, Quaternion.identity);
            ball.GetComponent<Rigidbody>().velocity = transform.forward * 50;
        }
    }

    void OnDisable()
    {
        StopLookingOrOrbiting();
    }

    /// <summary>
    /// Enable free looking or free Orbiting.
    /// </summary>
    public void StartLookingOrOrbiting()
    {
        if (orbitalFocus) {orbiting = true;}
        if (!orbitalFocus) {looking = true;}
        // Disable visibility of the cursor and keep it locked
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // So we know we are using the camera right now 
        camEngaged = true;
    }

    /// <summary>
    /// Disable free looking or free Orbiting.
    /// </summary>
    public void StopLookingOrOrbiting()
    {
        orbiting = false;
        looking = false;
        // Enable visibility of the cursor and unlock it
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // So we know we are not using the camera right now 
        camEngaged = false;
    }
}