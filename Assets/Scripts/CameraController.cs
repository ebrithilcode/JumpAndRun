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

        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");

        distance -= mouseWheel * 2;

        float horizontal = Input.GetAxis("Mouse X");
        float vertical = Input.GetAxis("Mouse Y");

        Quaternion toRotate = angleAroundAxis(horizontal * Time.deltaTime, Vector3.up);
        localPosition =  toRotate * localPosition;
        

        float scalar = distance / localPosition.magnitude;
        Debug.Log("Scaling by " + scalar);
        Debug.Log("Should be distance: " + distance);
        Debug.Log("Current magnitude: " + localPosition.magnitude);
        localPosition *= scalar;

        Vector3 positionToBe = localPosition + player.transform.position;

        float camMoveSpeed = (positionToBe - transform.position).magnitude * Time.deltaTime * 6;

        transform.position = Vector3.MoveTowards(transform.position, positionToBe, camMoveSpeed);
        transform.rotation = toRotate * transform.rotation;

        Quaternion toRotateUp = angleAroundAxis(-vertical * Time.deltaTime, Vector3.right);
        transform.rotation *= toRotateUp;
    }

    Quaternion angleAroundAxis(float angle, Vector3 axis)
    {
        return new Quaternion(Mathf.Sin(angle) * axis.x, Mathf.Sin(angle) * axis.y, Mathf.Sin(angle) * axis.z, Mathf.Cos(angle));
    }
}
