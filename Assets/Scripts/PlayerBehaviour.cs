using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {

    public enum State
    {
        Idle, Walking, Running, Jumping, Backwards, Falling, Rising, Landing, Undefined
    }

    State state = State.Idle;
    Dictionary<string, State> animationNameToState = new Dictionary<string, State>();
    
    
    Animator animator;
    SkinnedMeshRenderer meshRenderer;
    BoxCollider collider;

    Dictionary<State, List<System.Action>> onStateEnterCallbackList = new Dictionary<State, List<System.Action>>();
    Dictionary<State, List<System.Action>> onStateEndCallbackList = new Dictionary<State, List<System.Action>>();

    List<AnimationWithCondition> possibleAnimations = new List<AnimationWithCondition>();

    private float lastJump = 0;


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
        animationNameToState.Add("Base.Falling", State.Falling);
        animationNameToState.Add("Base.Landing", State.Landing);
        animationNameToState.Add("Base.StandUp", State.Rising);


        addAnimationTransitions();

        
        

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

        //Manage possible falling:
        if (state != State.Falling)
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up*2, Vector3.down, 1000f);
            float minDist = 1e10f;
            Debug.Log("Ray cast hit " + hits.Length + " colliders");
            foreach (RaycastHit hit in hits)
            {
                
                if (hit.collider != collider)
                {
                    minDist = Mathf.Min(minDist, (hit.point - transform.position).magnitude);
                }
            }
            if (minDist > 2f)
            {
                if (Time.time - lastJump > 1f)
                {
                    lastJump = Time.time;
                    Debug.LogWarning("The player is now falling");
                    animator.SetTrigger("StartFalling");
                }
            }
        }


        
        animator.SetFloat("Speed", Input.GetAxis("Vertical"));

        Vector2 distanceToObstacle = distanceAndHeightToCollider(6f);
        foreach (AnimationWithCondition possibleAnim in possibleAnimations) {
            possibleAnim.playAnimationIfValid(this, distanceToObstacle.x, distanceToObstacle.y);
        }
        if (animator.GetBool("CanWalk") != (distanceToObstacle.x > 0.3f) )
            animator.SetBool("CanWalk", distanceToObstacle.x > 0.3f);
            

        //Run or run not
        if (Input.GetKeyDown("left shift"))
        {
            animator.SetBool("IsRunning", true);
        } else if (Input.GetKeyUp("left shift"))
        {
            animator.SetBool("IsRunning", false);
        }


        //If the player drops below a specified height, he shall be respawned falling on the big red block

        if (transform.position.y < -25)
        {
            transform.position = new Vector3(10, 75, 23);
        }

	}

    private void deactivateGravityUntilLanding()
    {
        GetComponent<Rigidbody>().useGravity = false;
        //OnStateEnter(State.Jumping, () => GetComponent<Rigidbody>().useGravity = false);
        OnStateFinish(State.Jumping, () => GetComponent<Rigidbody>().useGravity = true);
    }

    private void setKinematicUntilLanding()
    {
        setKinematicUntilEndOf(State.Jumping);
        
    }

    private void setKinematicUntilEndOf(State state)
    {
        GetComponent<Rigidbody>().isKinematic = true;
        //OnStateEnter(State.Jumping, () => GetComponent<Rigidbody>().isKinematic = true);
        OnStateFinish(state, () => GetComponent<Rigidbody>().isKinematic = false);

    }

    //Manage possible landing
    private void OnCollisionEnter(Collision collision)
    {
        animator.SetTrigger("LandHard");
        OnStateFinish(State.Landing,
                () => setKinematicUntilEndOf(State.Rising));
            
    }


    State getCurrentAnimatorState()
    {
        foreach (KeyValuePair<string, State> entry in animationNameToState)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(entry.Key)) {
                if (entry.Value != state)
                {
                    if (state != State.Falling && state != State.Rising && state != State.Landing)
                    {
                        transform.rotation = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up);
                    }
                    if (onStateEndCallbackList.ContainsKey(state))
                    {
                        foreach(System.Action action in onStateEndCallbackList[state])
                        {
                            action();
                        }
                        onStateEndCallbackList[state].Clear();
                    }
                    if (onStateEnterCallbackList.ContainsKey(entry.Value))
                    {
                        foreach (System.Action action in onStateEnterCallbackList[entry.Value])
                        {
                            action();
                        }
                        onStateEnterCallbackList.Clear();
                    }
                }
                return entry.Value;
            }
        }
        Debug.Log("Help! Animation state undefined");
        return State.Undefined;
    }

    private Vector2 distanceAndHeightToCollider(float maxDistance)
    {

        float isFloorThreshold = 0.05f;
        float colliderFeetY = (collider.center.y + collider.transform.position.y - collider.bounds.size.y / 2);

        RaycastHit[] possibleColliderHits = Physics.BoxCastAll(collider.transform.position, collider.bounds.size, transform.forward, Quaternion.identity, maxDistance);
        float[] maxHeights = new float[possibleColliderHits.Length];
        float[] minDists = new float[possibleColliderHits.Length];
        for (int i= 0;i<minDists.Length;i++)
        {
            RaycastHit hit = possibleColliderHits[i];
            if (hit.collider.gameObject != collider.gameObject)
            {
                
                maxHeights[i] = hit.collider.transform.position.y + hit.collider.bounds.size.y / 2 - colliderFeetY;
                Vector3 hitPoint;
                if (hit.collider is BoxCollider)
                    hitPoint = ((BoxCollider)hit.collider).ClosestPoint(transform.position);
                else
                    hitPoint = hit.collider.ClosestPoint(transform.position);
                Vector3 posDif = hitPoint - collider.ClosestPoint(hitPoint);
                posDif.y = 0;

                minDists[i] = maxHeights[i] > isFloorThreshold ? posDif.magnitude : 1e10f;
            } else
            {
                maxHeights[i] = -1e10f;
                minDists[i] = 1e10f;
            }
        }

        int maxIndex = 0;
        float maxHeight = maxHeights[0];
        for (int i=0;i<maxHeights.Length;i++)
        {
            if (maxHeights[i] > maxHeight)
            {
                maxHeight = maxHeights[i];
                maxIndex = i;
            }
        }

        float minDist = minDists[maxIndex];

        Debug.Log("[" + Time.frameCount + "] Obstacle height: " + maxHeight);
        Debug.Log("[" + Time.frameCount + "] Distance to obstacle: " + minDist);

        return new Vector2(minDist, maxHeight);
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

    private void OnStateEnter(State state, System.Action action)
    {
        if (!onStateEnterCallbackList.ContainsKey(state)) onStateEnterCallbackList[state] = new List<System.Action>();

        onStateEnterCallbackList[state].Add(action);
    }

    private void addAnimationTransitions()
    {
        //Idle jump
        possibleAnimations.Add(
            new AnimationWithCondition(
                () =>
                {
                    setKinematicUntilLanding();
                    animator.SetTrigger("Jump");
                    lastJump = Time.time;
                })
                .addPrevState(State.Idle)
                .setPreCheck((script) => defaultJumpCondition())
                );

        //Butterfly and usual walking jump
        possibleAnimations.Add(
            new AnimationWithCondition(
                () =>
                {
                    //deactivateGravityUntilLanding();
                    setKinematicUntilLanding();
                    animator.SetTrigger("Jump");
                    lastJump = Time.time;
                })
                .setMinDistance(3)
                .setMaxHeight(0.05f)
                .addPrevState(State.Running).addPrevState(State.Walking)
                .setPreCheck((script) => defaultJumpCondition())
                );

        //Dash over obstacle
        possibleAnimations.Add(
            new AnimationWithCondition(
                () =>
                {
                    setKinematicUntilLanding();
                    animator.SetTrigger("JumpWithObstacle");
                    lastJump = Time.time;
                })
                .setMaxDistance(0.85f)
                .setMaxHeight(0.8f)
                .setMinHeight(0.1f)
                .addPrevState(State.Running)
                .setPreCheck((script) => defaultJumpCondition())
                );

        //Jump on obstacle
        possibleAnimations.Add(
            new AnimationWithCondition(
                () =>
                {

                    setKinematicUntilLanding();
                    animator.SetTrigger("JumpWithObstacle");
                    lastJump = Time.time;
                })
                .setMaxDistance(1f)
                .setMaxHeight(1f)
                .setMinHeight(0.1f)
                .addPrevState(State.Walking)
                .setPreCheck((script) => defaultJumpCondition())
                );

        //Wallflip
        possibleAnimations.Add(
            new AnimationWithCondition(
                () =>
                {
                   
                    Debug.LogWarning("Triggering wall jump");
                    setKinematicUntilLanding();
                    animator.SetTrigger("WallRun");
                    lastJump = Time.time;
                })
                .setMinDistance(1f)
                .setMaxDistance(2.5f)
                .setMinHeight(1.5f)
                .addPrevState(State.Running)
                .setPreCheck((script) => defaultJumpCondition())
                );

    }

    private bool defaultJumpCondition()
    {
        return (Time.time - lastJump) > 2 && Input.GetKey("space");
    }

    

    public class AnimationWithCondition
    {
        private List<State> previuosStates = new List<State>();
        private Vector2 distanceToObjectMinMax = new Vector2(-float.MaxValue, float.MaxValue);
        private Vector2 feedHeightDistanceToObjectMinMax = new Vector2(-float.MaxValue, float.MaxValue);
        private System.Action actionIfValid;
        private System.Predicate<PlayerBehaviour> preprocessingCheck;

        public AnimationWithCondition(System.Action onValidation) { this.actionIfValid = onValidation; }

        public AnimationWithCondition addPrevState(State state) { previuosStates.Add(state); return this; }
        public AnimationWithCondition setMinDistance(float value) { distanceToObjectMinMax.x = value; return this; }
        public AnimationWithCondition setMaxDistance(float value) { distanceToObjectMinMax.y = value; return this; }
        public AnimationWithCondition setMinHeight(float value) { feedHeightDistanceToObjectMinMax.x = value; return this; }
        public AnimationWithCondition setMaxHeight(float value) { feedHeightDistanceToObjectMinMax.y = value; return this; }
        public AnimationWithCondition setPreCheck(System.Predicate<PlayerBehaviour> predicate) { preprocessingCheck = predicate; return this; }



        public bool playAnimationIfValid(PlayerBehaviour playerScript, float distanceToObject, float heightDifference)
        {
            State currentState = playerScript.state;
            if (preprocessingCheck!=null && !preprocessingCheck(playerScript)) return false;
            if (isValid(currentState, distanceToObject, heightDifference))
            {
                actionIfValid();
                return true;
            }
            return false;
        }



        private bool isValid(State state, float distanceToObject, float heightDifference)
        {

            return (previuosStates.Contains(state)) && isInbetween(distanceToObject, distanceToObjectMinMax)
                && isInbetween(heightDifference, feedHeightDistanceToObjectMinMax);
        }

        private bool isInbetween(float value, Vector2 bounds)
        {
            return value >= bounds.x && value <= bounds.y;
        }
    }
    
}
