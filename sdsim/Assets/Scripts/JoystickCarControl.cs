using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.UI;

public class JoystickCarControl : MonoBehaviour
{
	public GameObject carObj1;
	public GameObject carObj2;
	private ICar car1;
	private ICar car2;

	public float MaximumSteerAngle = 25.0f; //has to be kept in sync with the car, as that's a private var.

	public Text SteeringText;
	public Text speedText;

	void Awake()
	{
		if (carObj1 != null)
			car1 = carObj1.GetComponent<ICar>();
		if (carObj2 != null)
			car2 = carObj2.GetComponent<ICar>();

	}

	private void FixedUpdate()
	{
		// Debug.Log("UPDATEANDO EL JOYSTICK CAR CONTROL");
		// pass the input to the car!
		float h = CrossPlatformInputManager.GetAxis("Horizontal");
		float v = CrossPlatformInputManager.GetAxis("Vertical");
		float handbrake = CrossPlatformInputManager.GetAxis("Jump");

		//Debug.Log(h * MaximumSteerAngle);
		//Debug.Log("RequestSteering COCHE2");
		car2.RequestSteering(h * MaximumSteerAngle);
		//Debug.Log("RequestSteering COCHE1");
		car1.RequestSteering(h * MaximumSteerAngle);
		car1.RequestThrottle(v);
		car2.RequestThrottle(v);
		//car.RequestFootBrake(v);
		car1.RequestHandBrake(handbrake);
		car2.RequestHandBrake(handbrake);
		//Debug.Log("POTPOT");
		//car.RequestFootBrake(v);

		if (SteeringText != null)
			SteeringText.text = string.Format("Steering: {0}", h * MaximumSteerAngle);

		if (speedText != null)
			speedText.text = string.Format("Speed: {0}", v);
	}
}
