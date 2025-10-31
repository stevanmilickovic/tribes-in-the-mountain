using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class DemoSystemScript : MonoBehaviour
{
    [Header("Main Components")]
    public FreeCam freeCam;
    public GameObject player, gun, raycast, target, wall;
    [HideInInspector]
    public Vector3 gunOrgPos;
    [HideInInspector]
    public Quaternion gunOrgRot;
    Animator animator;
    RootMotionScript rootMotion;
    CoverScript cover;
    TimeManager timeManager;

    [Header("Limb Components")]
    public GameObject hand_Right;
    public GameObject hand_Left;

    [Header("UI")]
    public JoyStickScript joyStick;
    AnimatorStateInfo animatorBaseStateInfo, animatorGunStateInfo;
    AnimatorClipInfo animatorClipInfo;
    public Slider timeSpeedSlider;
    public Button runResetButton, coverLeft, coverRight;
    public Text runText, grenadeText;
    public Toggle gunAimToggle, combatToggle;
    public Text animationClipName, StateName, pressFInstruction;

    [Header("Animation State Tags")]
    public string[] tagNameList;
    public string tagName;

    // Other
    float t; 
    float layerWeightAim;
    float angle;

    // Start is called before the first frame update
    void Start()
    {
        animator = player.GetComponent<Animator>();
        rootMotion = player.GetComponent<RootMotionScript>();    
        cover = GetComponent<CoverScript>();    
        timeManager = GetComponent<TimeManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Get the current info on the animator's State and the State's animation clip (Base) [state]
        animatorBaseStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        animatorClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];

        // Fetch name of current clip playing and display on top-left corner of game view
        animationClipName.text = "Clip: " + animatorClipInfo.clip.name;

        // Return the state's tag name from the TagNameList compared with the CurrentAnimatorStateInfo tag
        StateName.text = "State: " + GetCurrentStateTag();

        // Communicate the state info back as an integer to a mecanim parameter for more control
        animator.SetInteger("State", GetCurrentStateValue());

        // Time Control for slow-motion animation previewing: Linking the Time to the Time Slider's value
        Time.timeScale = timeSpeedSlider.value;

        // Shift the weight to 1 on the "Aim" Animation layer when we turn on Combat Mode
        if (combatToggle.isOn) {CombatTransition(true);} else CombatTransition(false);

        // Let's us know if we are in Orbit mode or Flymode 
        if (freeCam.orbitalFocus) {pressFInstruction.text = "Press F: Character Focus";} else pressFInstruction.text = "Press F: FlyCam";

        // Logic for Movement, if we are not using the FreeCam
        MovementLogic();

        // Resetting all Animator Triggers 
        if (Input.GetKeyUp (KeyCode.Mouse0)) 
        {
            StartCoroutine(ResetAllAnimatorParameters());
        }
    }    

    // We execute this method through CallBack event (OnValueChanged) when the "Combat" Toggle gets clicked
    public void CombatOnValueChanged () 
    {
        // Communicate over the Toggle state to the animator
        animator.SetBool("CombatMode", combatToggle.isOn);
        // Let animator know we fired off the combat toggle through a trigger
        animator.SetTrigger("Combat");
    }

    void CombatTransition (bool shiftCombat) 
    {
        // A clamped increment/decrement of layerWeightAim variable
        if (shiftCombat) 
        layerWeightAim = Mathf.Clamp (layerWeightAim += Time.deltaTime * 3, 0, 1);
        else 
        layerWeightAim = Mathf.Clamp (layerWeightAim -= Time.deltaTime * 3, 0, 1);
        // Apply layerWeightAim to set the layerweight of Aim layer (1)
        animator.SetLayerWeight(1, layerWeightAim);
    }

    void MovementLogic () 
    {
        // Linking the joystick value to the animator's speed
        animator.SetFloat("Speed", joyStick.direction.magnitude);

        // Showing what Run clip we are using through the Button's Text 
        if (joyStick.direction.magnitude >= 0.5f) {runText.text = "Sprint";} else runText.text = "Run";

        // Visibility of the RunReset Button, we can reset the joystick with this to a zero position
        if (joyStick.direction.magnitude != 0) {runResetButton.gameObject.SetActive(true);} else runResetButton.gameObject.SetActive(false);

        // ROOT MOTION ROTATION: Using the Joystick's direction value to control Rootmotion's rotation:  

        // Convert Vector2 to Radians 
        angle = Mathf.Atan2 (joyStick.direction.x, joyStick.direction.y);
        // Convert Radians to Degrees
        angle = Mathf.Rad2Deg * angle;
        // Add the rotation.y value of the camera to this angle
        angle += Camera.main.transform.eulerAngles.y;
        // Send the value off to the Root Motion's script's turnValue through Euler Function (y-axis)
        rootMotion.controlledTurnValue = Quaternion.Euler (0, angle, 0);
    }

    public void Cover () 
    {
        // Reset the joystick script to prevent override and so we don't double up on Rootmotion speed
        joyStick.ResetJoystick();
        // Set the path in place so our character can follow it
        cover.generatePath = false;
        // Send the character to the cover spot
        StartCoroutine(cover.SendPlayerToCoverSpot());
        // The mecanim trigger for the cover animations 
        animator.SetTrigger("Cover");
    }

    // We execute this method through CallBack event (OnClick) to "Reset" the character into its original state
    public void Reset() 
    {
        // Normal Speed
        timeSpeedSlider.value = 1;
        // Combat off
        combatToggle.isOn = false;
        // Update Animator's Combat Parameter
        CombatOnValueChanged();
        // Player back to original position
        player.transform.position = Vector3.zero;
        // Player back to original rotation
        player.transform.rotation = Quaternion.identity;
        // Play a random idle animation
        animator.SetInteger("RandomIdle", Random.Range(0, 5));
        // Reset the Run System
        joyStick.ResetJoystick();
    }

    // Animator's triggers need to be reset after they are enabled, else if they don't trigger a transition,
    // they will stay enabled until a clip is played that has a transition with that trigger as a condition 
    IEnumerator ResetAllAnimatorParameters () 
    {
        // We wait a moment to allow the OnClick event of the button to execute
        yield return new WaitForSecondsRealtime (0.1f);

        // Now we reset everything
        for (int i = 0; i < animator.parameterCount; i++)
        {
            // Check if it is a trigger
            if (animator.GetParameter(i).type == AnimatorControllerParameterType.Trigger)
            // Returns the name of the parameter through its hash id as a string
            animator.ResetTrigger(animator.GetParameter(i).nameHash);
        }
    }

    // This is totally unnecessary to create functionality and is strictly for visual purposes to show text telling us the State we're in 
    // We compare our tagNameList to the tag of the current animator state and return a string tagName
    string GetCurrentStateTag () 
    {
        // We look through every layer
        for (int i = 0; i < animator.layerCount; i++)
        {   // Go through every layer's tags
            foreach (string tag in tagNameList)
            {   // Compare and see if one of the tag's matches the string in our tagNameList
                if (animator.GetCurrentAnimatorStateInfo(i).IsTag(tag)) 
                {   // Assign
                    tagName = tag;
                }
            }
        }
        // Return
        return tagName;
    } 
    
    // We send mecanim's parameter "State" a value to tell what animation state our character is in
    int GetCurrentStateValue () 
    {
        switch (tagName)
        {
            case "Standing":
            return 3;
            case "Crouching":
            return 2;
            case "Proning":
            return 1;        
            case "InCover":
            return 4;    
            case "InCoverIdle":
            return 4;  
            default:
            return 0;
        }

        // If you want strictly functionality with clip tags:
        // A better way (programmatically) would be: 

        // if (animatorStateInfo.IsTag("Standing")) {return 3;}
        // else if (animatorStateInfo.IsTag("Crouching")) {return 2;}
        // else if (animatorStateInfo.IsTag("Proning")) {return 1;}
        // else if (animatorStateInfo.IsTag("InCover")) {return 4;}
        // else 
        //     return 0;
    }
}
