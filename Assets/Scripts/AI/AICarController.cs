using System;
using UnityEngine;
using Game.Driving;
using UnityHFSM;
using System.Collections;
using Game.Core;
using UnityHFSM.Visualization;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.AI
{
    public class AICarController : MonoBehaviour
    {
        private const string STOP_STATE = "Stop";
        private const string DRIVE_STATE = "Drive";
        private const string FORWARD_STATE = "Forward";
        private const string REVERSE_STATE = "Reverse";

        [Min(0.1f)]
        [SerializeField] private float waypointChangeDist = 5.0f;
        
        [Min(0.1f)]
        [SerializeField] private float speedReduceDistance = 10.0f;
        
        [Min(0.1f)]
        [SerializeField] private float cornerCheckMinAngle = 15.0f;

        [Range(1f, 90.0f)]
        [SerializeField] private float waypointCheckAngle = 5.0f;

        [SerializeField] private bool followWaypointEnabled = true;
        
        [SerializeField] private Transform[] waypoints = null;

        [Space(10.0f)]
        [Header("Align with track Settings: ")]

        [SerializeField] private bool allowAlignWithTrack = true;
        [Min(0.1f)]
        [SerializeField] private float alignWithTrackSpeed = 5.0f;
        [Min(0.1f)]
        [SerializeField] private float alignCheckDistance = 5.0f;

        [Min(0.1f)]
        [SerializeField] private float alignCheckWidth = 2.0f;

        [SerializeField] private LayerMask alignCheckLayerMask;
        [SerializeField] private CastType alignCastType = CastType.Ray;
        
        [Min(3)]
        [SerializeField] private int alignCheckRayCount = 3;

        [Range(0.1f, 2.0f)]
        [SerializeField] private float alignCheckTime = 0.5f;

        [Min(0.001f)]
        [SerializeField] private float alignCheckRadius = 0.5f;

        [Range(0.01f, 0.2f)]
        [SerializeField] private float ignoreInputRange = 0.15f;
        

        [Space(10.0f)]
        [Header("Block Settings: ")]
        
        [SerializeField] private Vector3 frontCheckOffset;
        [SerializeField] private Vector3 rearCheckOffset;
        
        [Min(0.1f)]
        [SerializeField] private float blockingCheckDistance = 5.0f;
        
        [SerializeField] private LayerMask blockCheckLayerMask;
        
        [Min(1)]
        [SerializeField] private int blockCheckRayCount = 3;
        
        [Range(0.1f, 30.0f)]
        [SerializeField] private float blockCheckWidth = 5.0f;

        [Min(0.01f)]
        [SerializeField] private float blockCheckTime = 2.0f;
        
        [Space(10.0f)]
        [Header("Deflection Settings: ")]

        [Min(60.0f)]
        [SerializeField] private float deflectionCheckAngle = 65.0f;
        [Min(0.01f)]
        [SerializeField] private float deflectionCheckTime = 2.0f;

        [Space(10.0f)]
        [SerializeField] private bool showStateHierarchy = false;
        
        private Car car;
        private Animator carAnimator;

        private StateMachine mainFSM;
        private StateMachine driveFSM;
        private AICarStopState stopState;
        private AICarForwardState forwardState;
        private AICarReverseState reverseState;


        private Vector2 input = Vector2.zero;
        private float requiredSteeringInput = 0.0f;
        private int currentWaypointIndex = -1;

        private float[] interest;
        private float[] danger;
        //private bool[] danger;

        private Coroutine interestAndDangerCoroutine = null;
        private RaycastHit alignHit;
        private float currentBlockTime = 0.0f;
        private float currentDeflectTime = 0.0f;
        private bool forwardBlocked = false;
        private bool backwardBlocked = false;
        private bool deflected = false;
        public Vector2 Input { get => input; }
        public int CurrentWaypointIndex { get => currentWaypointIndex; set => currentWaypointIndex = value; }
        public Transform[] Waypoints { get => waypoints; set => waypoints = value; }
        public float WaypointChangeDist { get => waypointChangeDist; }
        public float SpeedReduceDistance { get => speedReduceDistance; }
        public float CornerCheckMinAngle { get => cornerCheckMinAngle; }
        public Vector3 FrontCheckPos { get => transform.position +
                                              transform.right * frontCheckOffset.x +
                                              transform.up * frontCheckOffset.y +
                                              transform.forward * frontCheckOffset.z; }
        public Vector3 RearCheckPos { get => transform.position +
                                             transform.right * rearCheckOffset.x +
                                             transform.up * rearCheckOffset.y +
                                             transform.forward * rearCheckOffset.z; }
        public bool FollowWaypointEnabled { get => followWaypointEnabled; set => followWaypointEnabled = value; }
        public float IgnoreInputRange { get => ignoreInputRange; }
        public float NormalLeftWheelAngle { get => car.GetNormalLeftWheelAngle(); }
        public float NormalRightWheelAngle { get => car.GetNormalRightWheelAngle(); }
        public float WaypointCheckAngle { get => waypointCheckAngle; }

        public void SetInput(float steeringInput, float accelerateInput)
        {
            input.x = steeringInput;
            input.y = accelerateInput;
        }

        public float GetAlignedSteeringInput()
        {
            return requiredSteeringInput;
        }

        private float GetSegmentAngle(float maxAngle, int segmentIndex, int totalSegments)
        {
            float requiredAngle = Mathf.Lerp(-maxAngle / 2.0f,
                                              maxAngle / 2.0f,
                                              (totalSegments == 1) ? 0.5f : segmentIndex / (float)(totalSegments - 1));

            return requiredAngle;
        }

        private void HandleAI()
        {
            if(showStateHierarchy)
            {
                Debug.Log("Current State Hierarchy: " + mainFSM.GetActiveHierarchyPath());
            }

            mainFSM.OnLogic();

            #if UNITY_EDITOR
                HfsmAnimatorGraph.PreviewStateMachineInAnimator(mainFSM, carAnimator);
            #endif
        }

        private bool IsBlockingForward()
        {
            if(currentBlockTime < blockCheckTime)
            {
                currentBlockTime += Time.deltaTime;
                return forwardBlocked;
            }

            currentBlockTime = 0.0f;
            forwardBlocked = CheckObstacles(FrontCheckPos,
                                  transform.forward,
                                  blockCheckRayCount,
                                  blockCheckWidth,
                                  blockingCheckDistance,
                                  blockCheckLayerMask.value);
            return forwardBlocked && car.GetNormalizedSpeed() > 0.5f;
        }

        private bool IsBlockingBackward()
        {
            if (currentBlockTime < blockCheckTime)
            {
                currentBlockTime += Time.deltaTime;
                return backwardBlocked;
            }

            currentBlockTime = 0.0f;

            backwardBlocked = CheckObstacles(RearCheckPos,
                                  transform.forward,
                                  blockCheckRayCount,
                                  blockCheckWidth,
                                  blockingCheckDistance,
                                  blockCheckLayerMask.value);

            return backwardBlocked;
        }

        private bool IsTooDeflectedFromWaypoint()
        {
            if(currentWaypointIndex >= waypoints.Length)
            {
                return false;
            }

            if(currentDeflectTime < deflectionCheckTime)
            {
                currentDeflectTime += Time.deltaTime;
                return deflected;
            }

            currentDeflectTime = 0.0f;

            Vector3 direction = (waypoints[currentWaypointIndex].position - transform.position).normalized;
            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            //Debug.Log("Current deflection angle: " + angle);
            
            deflected = !(angle >= -deflectionCheckAngle / 2f && angle <= deflectionCheckAngle / 2.0f);
            return deflected;
        }

        private bool CheckObstacles(Vector3 origin, Vector3 direction, int rayCount, float checkWidth, 
                                    float checkDistance, LayerMask obstacleLayerMask)
        {
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 startPosition = Vector3.Lerp(origin - transform.right * checkWidth / 2.0f,
                                                     origin + transform.right * checkWidth / 2.0f,
                                                     (i == rayCount / 2) ? 0.5f : 
                                                     (float)i / (float)(rayCount - 1));

                if (Physics.Raycast(startPosition, direction, checkDistance, obstacleLayerMask.value))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetInterestAndDanger()
        {
            interest = new float[alignCheckRayCount];
            danger = new float[alignCheckRayCount];

            for(int i = 0; i < alignCheckRayCount; i++)
            {
                interest[i] = 0.0f;
                danger[i] = 0.0f;
                
            }
        }

        private IEnumerator CheckInterestAndDanger()
        {
            WaitForSeconds wait = new WaitForSeconds(alignCheckTime);

            while(this.enabled)
            {
                if(currentWaypointIndex == -1 || !followWaypointEnabled)
                {

                    yield return null;
                    continue;
                }
                int middleRayIndex = (alignCheckRayCount == 1) ? 0 : Mathf.CeilToInt(alignCheckRayCount / 2.0f);

                for (int i = 0; i < alignCheckRayCount; i++)
                {
                    if(!allowAlignWithTrack || driveFSM.ActiveState.name.Equals(REVERSE_STATE))
                    {
                        interest[i] = 0.0f;
                        yield return null;
                        continue;
                    }

                    else if(i == middleRayIndex && (alignCheckRayCount % 2) == 1)
                    {
                        interest[i] = 0.0f;
                        continue;
                    }

                    interest[i] = 0.0f;

                    Vector3 requiredPosition = Vector3.Lerp(FrontCheckPos - transform.right * alignCheckWidth / 2.0f,
                                                            FrontCheckPos + transform.right * alignCheckWidth / 2.0f,
                                                            (alignCheckRayCount == 1) ? 0.5f : i / (float)(alignCheckRayCount - 1));
                    Vector3 forwardVec = transform.forward;

                    bool hitSomething = false;

                    switch(alignCastType)
                    {
                        case CastType.Ray:
                                            hitSomething = Physics.Raycast(requiredPosition, forwardVec, out alignHit ,alignCheckDistance, alignCheckLayerMask.value);
                                            break;

                        case CastType.Sphere:
                                            hitSomething = Physics.SphereCast(new Ray(requiredPosition, forwardVec), alignCheckRadius, out alignHit, alignCheckDistance, alignCheckLayerMask.value);
                                            break;
                    }

                    Vector3 direction = (waypoints.Length == 0 || 
                                         currentWaypointIndex == -1 || 
                                         currentWaypointIndex >= waypoints.Length) ? transform.forward : 
                                         (waypoints[currentWaypointIndex].position - transform.position).normalized;
                    
                    float steeringSign = Mathf.Sign(i - middleRayIndex);

                    float angle = Vector3.SignedAngle(forwardVec, direction, transform.up);
                    float actualSteerAmount = Mathf.Clamp(Mathf.Abs(angle) / Mathf.Max(NormalLeftWheelAngle, NormalRightWheelAngle), -1.0f, 1.0f); //Mathf.Clamp(1.0f - Vector3.Dot(forwardVec, direction), -1.0f, 1.0f);
                    float avoidSteerAmount = (hitSomething == true) ? Mathf.Clamp01(1.0f - alignHit.distance/alignCheckDistance) : 0.0f;

                    danger[i] = (avoidSteerAmount >= 0.5f) ? 1.0f : 0.0f;
                    float requiredInterest = steeringSign * actualSteerAmount;
                    
                    if (danger[i] > 0.0f)
                    {
                        requiredInterest = 0.0f;
                    }

                    interest[i] = requiredInterest;
                }

                requiredSteeringInput = 0.0f;

                for (int i = 0; i < alignCheckRayCount; i++)
                {
                    float currentInterest = interest[i];
                    requiredSteeringInput += currentInterest;
                }

                yield return wait;
            }
        }

        private void DrawSensorRangeGizmos(Vector3 origin, Vector3 direction, int rayCount, float checkWidth,
                                    float checkDistance, float endSphereRadius = 0.0f)
        {
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 startPos = Vector3.Lerp(origin - transform.right * checkWidth / 2.0f,
                                                     origin + transform.right * checkWidth / 2.0f,
                                                     (i == rayCount / 2) ? 0.5f :
                                                     (float)i / (float)(rayCount - 1));

                Vector3 endPos = startPos + direction * checkDistance;
                Gizmos.DrawLine(startPos, endPos);
            }
        }



        private void Awake()
        {
            car = GetComponent<Car>();
            carAnimator = GetComponent<Animator>();

            mainFSM = new StateMachine();
            driveFSM = new StateMachine();
            stopState = new AICarStopState(this);

            forwardState = new AICarForwardState(this); 
            reverseState = new AICarReverseState(this);
            
            mainFSM.AddState(STOP_STATE, stopState);
            mainFSM.AddState(DRIVE_STATE, driveFSM);
            
            driveFSM.AddState(FORWARD_STATE, forwardState);
            driveFSM.AddState(REVERSE_STATE, reverseState);

            mainFSM.AddTransition(new Transition(STOP_STATE, DRIVE_STATE, (transition)=> followWaypointEnabled == true));
            mainFSM.AddTransition(new Transition(DRIVE_STATE, STOP_STATE, (transition)=> followWaypointEnabled == false));

            driveFSM.AddTransition(new Transition(FORWARD_STATE, REVERSE_STATE, (transition) => (IsTooDeflectedFromWaypoint() || IsBlockingForward())));
            driveFSM.AddTransition(new Transition(REVERSE_STATE, FORWARD_STATE, (transition) => (!IsTooDeflectedFromWaypoint() || IsBlockingBackward())));

            #if UNITY_EDITOR
            
                HfsmAnimatorGraph.CreateAnimatorFromStateMachine(mainFSM, 
                                                                 "Assets/DebugAnimators", 
                                                                 "AIControllerAnimator.controller");
            #endif
        }

        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            currentWaypointIndex = 0;
            ResetInterestAndDanger();
            if(interestAndDangerCoroutine == null)
            {
                interestAndDangerCoroutine = StartCoroutine(CheckInterestAndDanger());
            }

            mainFSM.SetStartState(STOP_STATE);
            mainFSM.Init();
        }

        private void FixedUpdate()
        {
            HandleAI();
            car.Input = input;
        }

        private void OnDisable()
        {
            if(interestAndDangerCoroutine != null)
            {
                StopCoroutine(interestAndDangerCoroutine);
                interestAndDangerCoroutine = null;
            }
        }

        private void OnValidate()
        {
            if(interest != null && alignCheckRayCount != interest.Length)
            {
                ResetInterestAndDanger();
            }
        }

        private void OnDrawGizmosSelected()
        {
            //Show block gizmos.

            Gizmos.color = Color.red;
            DrawSensorRangeGizmos(FrontCheckPos,
                                  transform.forward,
                                  blockCheckRayCount,
                                  blockCheckWidth,
                                  blockingCheckDistance);

            DrawSensorRangeGizmos(RearCheckPos,
                                  -transform.forward,
                                  blockCheckRayCount,
                                  blockCheckWidth,
                                  blockingCheckDistance);

            //Show align with track gizmos.

            Gizmos.color = Color.yellow;
            DrawSensorRangeGizmos(FrontCheckPos + transform.up * 0.1f,
                                  transform.forward,
                                  alignCheckRayCount,
                                  alignCheckWidth,
                                  alignCheckDistance,
                                  alignCheckRadius);

            #if UNITY_EDITOR

            //Show waypoint check distance and slow down distance
            Handles.color = Color.blue;
            Handles.DrawWireDisc(FrontCheckPos, Vector3.up, waypointChangeDist);

            Handles.color = Color.black;
            Handles.DrawWireDisc(FrontCheckPos, Vector3.up, speedReduceDistance);

            #endif

            

        }
    }
}
