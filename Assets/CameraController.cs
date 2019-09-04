using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public GameObject player;

    public float distance = 5;

    Vector3 localPosition = new Vector3(0, 10, -10);


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        float horizontal = Input.GetAxis("Mouse X");
        float vertical = Input.GetAxis("Mouse Y");

        Quaternion toRotate = angleAroundAxis(horizontal * Time.deltaTime, Vector3.up);
        localPosition =  toRotate * localPosition;
        

        float scalar = distance / localPosition.magnitude;
        Debug.Log("Scaling by " + scalar);
        Debug.Log("Should be distance: " + distance);
        Debug.Log("Current magnitude: " + localPosition.magnitude);
        localPosition *= scalar;

        transform.position = Vector3.MoveTowards(transform.position, localPosition + player.transform.position, Time.deltaTime*3f);
        transform.rotation = toRotate * transform.rotation;
    }

    Quaternion angleAroundAxis(float angle, Vector3 axis)
    {
        return new Quaternion(Mathf.Sin(angle) * axis.x, Mathf.Sin(angle) * axis.y, Mathf.Sin(angle) * axis.z, Mathf.Cos(angle));
    }
}
