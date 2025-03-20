using System;
using UnityEngine;
using Game.Driving;
using UnityHFSM;

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

        [Range(0.1f, 20.0f)]
        [SerializeField] private float waypointCheckAngle = 5.0f;

        [SerializeField] private bool followWaypointEnabled = true;
        
        [SerializeField] private Transform[] waypoints = null;

        [Space(10.0f)]
        [Header("Align with track Settings: ")]
        
        [SerializeField] private Transform alignCheckPoint;
        
        [Min(0.1f)]
        [SerializeField] private float alignCheckDistance = 5.0f;

        [Min(30.0f)]
        [SerializeField] private float alignCheckAngle = 60.0f;

        [SerializeField] private LayerMask alignCheckLayerMask;
        
        [Min(3)]
        [SerializeField] private int alignCheckRayCount = 3;

        [Range(0.01f, 0.2f)]
        [SerializeField] private float ignoreInputRange = 0.15f;

        [Space(10.0f)]
        [Header("Block Settings: ")]
        
        [SerializeField] private Transform frontCheck;
        [SerializeField] private Transform rearCheck;
        
        [Min(0.1f)]
        [SerializeField] private float blockingCheckDistance = 5.0f;
        
        [SerializeField] private LayerMask blockCheckLayerMask;
        
        [Min(1)]
        [SerializeField] private int blockCheckRayCount = 3;
        
        [Range(60.0f, 180.0f)]
        [SerializeField] private float blockCheckAngle = 90.0f;
        
        [Space(10.0f)]
        [Header("Deflection Settings: ")]

        [Min(60.0f)]
        [SerializeField] private float deflectionCheckAngle = 65.0f;
        
        private Car car;

        private StateMachine mainFSM;
        private StateMachine driveFSM;
        private AICarStopState stopState;
        private AICarForwardState forwardState;
        private AICarReverseState reverseState;


        private Vector2 input = Vector2.zero;
        private float requiredSteeringInput = 0.0f;
        private int currentWaypointIndex = -1;

        public Vector2 Input { get => input; }
        public int CurrentWaypointIndex { get => currentWaypointIndex; set => currentWaypointIndex = value; }
        public Transform[] Waypoints { get => waypoints; set => waypoints = value; }
        public float WaypointChangeDist { get => waypointChangeDist; }
        public float SpeedReduceDistance { get => speedReduceDistance; }
        public float CornerCheckMinAngle { get => cornerCheckMinAngle; }
        public Transform FrontCheck { get => frontCheck;}
        public Transform RearCheck { get => rearCheck;}
        public bool FollowWaypointEnabled { get => followWaypointEnabled; set => followWaypointEnabled = value; }
        public float IgnoreInputRange { get => ignoreInputRange; }

        public void SetInput(float steeringInput, float accelerateInput)
        {
            input.x = steeringInput;
            input.y = accelerateInput;
        }

        public float GetAlignedSteeringInput()
        {
            requiredSteeringInput = 0.0f;

            int middleRayIndex = (alignCheckRayCount == 1) ? 0 : Mathf.CeilToInt(alignCheckRayCount / 2.0f);

            for (int i = 0; i < alignCheckRayCount; i++)
            {
                float requiredAngle = Mathf.Lerp(-alignCheckAngle / 2.0f, 
                                                  alignCheckAngle / 2.0f, 
                                                  (alignCheckRayCount == 1) ? 0.5f : i / (float)(alignCheckRayCount - 1));

                Vector3 forwardVec = Quaternion.AngleAxis(requiredAngle, transform.up) * frontCheck.forward;

                if (Physics.Raycast(frontCheck.position, forwardVec, alignCheckDistance, alignCheckLayerMask.value))
                {
                    float turnDirection = (middleRayIndex != i) ? Mathf.Sign(middleRayIndex - i) : 0f;
                    requiredSteeringInput += turnDirection * 1.0f / (float)alignCheckRayCount;

                }
            }

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
            //Debug.Log("Current State Hierarchy: " + mainFSM.GetActiveHierarchyPath());
            
            mainFSM.OnLogic();
        }

        private bool IsBlockingForward()
        {
            return CheckObstacles(frontCheck.position, 
                                  transform.forward, 
                                  blockCheckRayCount,
                                  blockCheckAngle, 
                                  blockingCheckDistance, 
                                  blockCheckLayerMask.value);
        }

        private bool IsBlockingBackward()
        {
            return CheckObstacles(rearCheck.position,
                                  transform.forward,
                                  blockCheckRayCount,
                                  blockCheckAngle,
                                  blockingCheckDistance,
                                  blockCheckLayerMask.value);
        }

        private bool IsTooDeflectedFromWaypoint()
        {
            if(currentWaypointIndex >= waypoints.Length)
            {
                return false;
            }

            
            float angle = Vector3.Angle(frontCheck.forward, waypoints[currentWaypointIndex].forward);
            //Debug.Log("Current deflection angle: " + angle);
            return Mathf.Abs(angle) > deflectionCheckAngle/2f;
        }

        private bool CheckObstacles(Vector3 origin, Vector3 direction, int rayCount, float checkAngle, 
                                    float checkDistance, LayerMask obstacleLayerMask)
        {
            for (int i = 0; i < rayCount; i++)
            {
                float requiredAngle = Mathf.Lerp(-checkAngle / 2.0f, checkAngle / 2.0f,
                                                 (rayCount == 1) ? 0.5f : i / (float)(rayCount - 1));

                Vector3 angleDirection = Quaternion.AngleAxis(requiredAngle, transform.up) * direction;

                if (Physics.Raycast(origin, angleDirection, checkDistance, obstacleLayerMask.value))
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            car = GetComponent<Car>();
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

            driveFSM.AddTransition(new Transition(FORWARD_STATE, REVERSE_STATE, (transition) => input.y > 0.0f && (IsTooDeflectedFromWaypoint() || IsBlockingForward())));
            driveFSM.AddTransition(new Transition(REVERSE_STATE, FORWARD_STATE, (transition) => input.y < 0.0f && (!IsTooDeflectedFromWaypoint() || IsBlockingBackward())));
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            currentWaypointIndex = 0;
            mainFSM.SetStartState(STOP_STATE);
            mainFSM.Init();
        }

        private void FixedUpdate()
        {
            HandleAI();
            car.Input = input;
        }


        private void DrawSensorRangeGizmos(Vector3 origin, Vector3 direction, int rayCount, float checkAngle,
                                    float checkDistance)
        {
            for (int i = 0; i < rayCount; i++)
            {
                float requiredAngle = Mathf.Lerp(-checkAngle / 2.0f, checkAngle / 2.0f,
                                                 (rayCount == 1) ? 0.5f : i / (float)(rayCount - 1));

                Vector3 angleDirection = Quaternion.AngleAxis(requiredAngle, transform.up) * direction;

                Gizmos.DrawLine(origin, origin + angleDirection * checkDistance);
            }
        }

        private void OnDrawGizmosSelected()
        {
            //Show block gizmos.
            if(frontCheck != null && rearCheck != null)
            {
                Gizmos.color = Color.red;
                DrawSensorRangeGizmos(frontCheck.position,
                                      frontCheck.forward,
                                      blockCheckRayCount,
                                      blockCheckAngle,
                                      blockingCheckDistance);

                DrawSensorRangeGizmos(rearCheck.position,
                                      rearCheck.forward,
                                      blockCheckRayCount,
                                      blockCheckAngle,
                                      blockingCheckDistance);
            }

            //Show align with track gizmos.

            if(alignCheckPoint != null)
            {
                Gizmos.color = Color.yellow;
                DrawSensorRangeGizmos(alignCheckPoint.position + transform.up * 0.1f,
                                      alignCheckPoint.forward,
                                      alignCheckRayCount,
                                      alignCheckAngle,
                                      alignCheckDistance);
            }

            if(frontCheck != null)
            {
                #if UNITY_EDITOR

                //Show waypoint check distance and slow down distance
                Handles.color = Color.blue;
                Handles.DrawWireDisc(frontCheck.position, Vector3.up, waypointChangeDist);

                Handles.color = Color.black;
                Handles.DrawWireDisc(frontCheck.position, Vector3.up, speedReduceDistance);
                
                #endif

            }

            if(waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.magenta;

                for(int i = 1; i < waypoints.Length; i++)
                {
                    Vector3 currentWayPt = waypoints[i].position;
                    Vector3 previousWayPt = waypoints[i - 1].position;

                    Gizmos.DrawLine(previousWayPt, currentWayPt);
                }
            }

        }
    }
}
