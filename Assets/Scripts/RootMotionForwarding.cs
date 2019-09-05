using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionForwarding : MonoBehaviour {

    PlayerBehaviour player;

	// Use this for initialization
	void Start () {
        player = GetComponentInParent<PlayerBehaviour>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnAnimatorMove()
    {
        Debug.LogWarning("Moving the Animator");
        player.OnAnimatorMove();
    }
}
