﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.UI;

public class JoystickCarControl : MonoBehaviour 
{
	public GameObject carObj;
	private ICar car;

	public float MaximumSteerAngle = 25.0f; //has to be kept in sync with the car, as that's a private var.

	public Text SteeringText;
	public Text speedText;

	void Awake()
	{
		if(carObj != null)
			car = carObj.GetComponent<ICar>();

	}

	private void FixedUpdate()
	{
		// Debug.Log("UPDATEANDO EL JOYSTICK CAR CONTROL");
		// pass the input to the car!
		float h = CrossPlatformInputManager.GetAxis("Horizontal");
		float v = CrossPlatformInputManager.GetAxis("Vertical");
		float handbrake = CrossPlatformInputManager.GetAxis("Jump");
		//Debug.Log(h * MaximumSteerAngle);
		car.RequestSteering(h * MaximumSteerAngle);
		car.RequestThrottle(v);
		//car.RequestFootBrake(v);
		car.RequestHandBrake(handbrake);
		//Debug.Log("POTPOT");

		if (SteeringText != null)
			SteeringText.text = string.Format("Steering: {0}", h * MaximumSteerAngle);

		if (speedText != null)
			speedText.text = string.Format("Speed: {0}", v);
	}
}
