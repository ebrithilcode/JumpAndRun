using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {

    enum State
    {
        Idle, Walking, Running, Jumping, Backwards, Undefined
    }

    State state = State.Idle;
    Dictionary<string, State> animationNameToState = new Dictionary<string, State>();
    
    
    Animator animator;
    SkinnedMeshRenderer meshRenderer;
    BoxCollider collider;

    Dictionary<State, List<System.Action>> onStateEndCallbackList = new Dictionary<State, List<System.Action>>();


    float speed = 0;

	// Use this for initialization
	void Start () {
        animationNameToState.Add("Base.Idle", State.Idle);
        animationNameToState.Add("Base.Walk", State.Walking);
        animationNameToState.Add("Base.Backwards", State.Backwards);
        animationNameToState.Add("Base.Run", State.Running);
        animationNameToState.Add("Base.IdleJump", State.Jumping);
        animationNameToState.Add("Base.WalkingJump", State.Jumping);
        animationNameToState.Add("Base.RunningJump", State.Jumping);
        animationNameToState.Add("Base.SlowUpJump", State.Jumping);
        animationNameToState.Add("Base.FastUpJump", State.Jumping);
        animationNameToState.Add("Base.WallRun", State.Jumping);

        
        

        animator = GetComponent<Animator>();
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        collider = GetComponentInChildren<BoxCollider>();
	}

	
	// Update is called once per frame
	void Update () {    

        


        state = getCurrentAnimatorState();

        //Turn only while walking or running
        float turningValue = Input.GetAxis("Horizontal");
        if (state==State.Walking || state==State.Running || state==State.Backwards)
        {
            float angularFrequency = 2 * Mathf.PI * 0.5f;
            transform.rotation *= eulerQuaternion(Vector3.up, turningValue * angularFrequency * Time.deltaTime / 2);
        }


        
        animator.SetFloat("Speed", Input.GetAxis("Vertical"));

        
        //Jump on space
        if (Input.GetKeyDown("space"))
        {
            float heightToJump = neededHeightForJump(3f);
            switch(state)
            {
                case State.Running:
                    if (heightToJump<0.5 || heightToJump>1.5)
                    {
                        deactivateGravityUntilLanding();
                        if (heightToJump < 0.5)
                        {
                            animator.SetTrigger(heightToJump < 0.1 ? "Jump" : "JumpWithObstacle");
                        } else
                        {
                            animator.SetTrigger("WallRun");
                        }
                    }
                    break;

                case State.Walking:
                    if (heightToJump<0.7)
                    {
                        deactivateGravityUntilLanding();
                        animator.SetTrigger(heightToJump < 0.1? "Jump" : "JumpWithObstacle");
                    }
                    break;

                case State.Idle:
                    deactivateGravityUntilLanding();
                    animator.SetTrigger("Jump");
                    break;
            }
            
        }

        //Run or run not
        if (Input.GetKeyDown("left shift"))
        {
            animator.SetBool("IsRunning", true);
        } else if (Input.GetKeyUp("left shift"))
        {
            animator.SetBool("IsRunning", false);
        }

	}

    private void deactivateGravityUntilLanding()
    {
        GetComponent<Rigidbody>().useGravity = false;
        OnStateFinish(State.Jumping, () => GetComponent<Rigidbody>().useGravity = true);
    }


    State getCurrentAnimatorState()
    {
        foreach (KeyValuePair<string, State> entry in animationNameToState)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(entry.Key)) {
                if (entry.Value != state)
                {
                    if (onStateEndCallbackList.ContainsKey(state))
                    {
                        foreach(System.Action action in onStateEndCallbackList[state])
                        {
                            action();
                        }
                        onStateEndCallbackList[state].Clear();
                    }
                }
                return entry.Value;
            }
        }
        Debug.Log("Help! Animation state undefined");
        return State.Undefined;
    }

    private float neededHeightForJump(float distance)
    {
        float maxHeight = -float.MaxValue;

        foreach(RaycastHit hit in Physics.BoxCastAll(collider.transform.position, collider.bounds.size, transform.forward, Quaternion.identity, distance))
        {
            if (hit.collider.gameObject != collider.gameObject) 
                maxHeight = Mathf.Max(maxHeight, hit.collider.transform.position.y + hit.collider.bounds.size.y/2);
        }

        Debug.Log("Heighest spot object: " + maxHeight);
        Debug.Log("Loweset spot player: " + (collider.center.y + collider.transform.position.y - collider.bounds.size.y / 2));
        
        return maxHeight - (collider.center.y + collider.transform.position.y - collider.bounds.size.y/2) ;
    }


    private void OnAnimatorMove()
    {
        transform.position += animator.deltaPosition;
        transform.rotation *= animator.deltaRotation;
    }

    private Quaternion eulerQuaternion(Vector3 axis, float angle)
    {
        return new Quaternion(Mathf.Sin(angle) * axis.x, Mathf.Sin(angle) * axis.y, Mathf.Sin(angle) * axis.z, Mathf.Cos(angle));
    }

    private void OnStateFinish(State state, System.Action action)
    {
        if (!onStateEndCallbackList.ContainsKey(state)) onStateEndCallbackList[state] = new List<System.Action>();

        onStateEndCallbackList[state].Add(action);
    }

    
}
