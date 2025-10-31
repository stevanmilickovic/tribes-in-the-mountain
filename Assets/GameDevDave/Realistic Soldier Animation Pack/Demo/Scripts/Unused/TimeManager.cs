using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour {

	public float slowdownFactor = 0.25f;
	public float slowdownLength = 2f;
	private float originalFixedDeltaTime;

	void Start () {
		originalFixedDeltaTime = Time.fixedDeltaTime;
	}

	public void DoSlowmotion () {
		Time.timeScale = slowdownFactor;
		Time.fixedDeltaTime = Time.timeScale * .02f;
	}

	public void DoNormalmotion () {
		Time.timeScale = 1f;
		Time.fixedDeltaTime = originalFixedDeltaTime;
	}

	public void DoStopTime () 
	{
		Time.timeScale = 0.000001f;	
	} 
}
