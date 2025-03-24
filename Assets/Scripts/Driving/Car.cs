using System.Collections;
using UnityEngine;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Driving
{
    [RequireComponent(typeof(Rigidbody))]
    public class Car : MonoBehaviour
    {
        private const float MIN_STEERING_INPUT = 0.0001f;
        private const float MAX_STEERING_INPUT = 0.999f;

        private const float MIN_ACCELERATING_INPUT = 0.0001f;
        private const float MAX_ACCELERATING_INPUT = 0.999f;

        [Header("Wheel Settings: ")]
        [SerializeField] private WheelData wheelFL;
        [SerializeField] private WheelData wheelFR;
        [SerializeField] private WheelData wheelRL;
        [SerializeField] private WheelData wheelRR;

        [Space(10.0f)]
        
        [Header("Damping while touching ground settings: ")]
        [Range(0.0f, 10.0f)]
        [SerializeField] private float linearDamp = 5.0f;
        [Range(0.0f, 10.0f)]
        [SerializeField] private float angularDamp = 5.0f;
        [Range(0.05f, 1.0f)]
        [SerializeField] private float dampingTime = 0.1f;
        
        [Space(10.0f)]
        
        [Header("Other Settings: ")]

        [Min(250.0f)]
        [SerializeField] private float mass = 1000.0f;
        [SerializeField] private LayerMask detectGroundLayerMask;
        
        [Min(0.1f)]
        [SerializeField] private float springStrength = 1.0f;
        
        [Min(0.1f)]
        [SerializeField] private float damperStrength = 1.0f;

        [SerializeField] private bool interpolatedInput = true;
        [Min(0.001f)]
        [SerializeField] private float steeringInputTime = 1.0f;

        [Min(0.001f)]
        [SerializeField] private float accelerateInputTime = 1.0f;

        [SerializeField] private float gravity = 10.0f;
        
        [Tooltip("Max torque of car in N-m")]
        [Min(1.0f)]
        [SerializeField] private float maxTorque = 1000.0f;

        [SerializeField] private AnimationCurve powerCurve;

        [Tooltip("Top speed of a car in m/s")]
        [Min(1.0f)]
        [SerializeField] private float forwardSpeed = 50.0f;

        [Tooltip("reverse speed of a car in m/s")]
        [Min(0.5f)]
        [SerializeField] private float reverseSpeed = 10.0f;
        [Min(0.1f)]
        [SerializeField] private float turnRadius = 5.0f;

        [Space(10.0f)]

        [Tooltip("Anti Rollbar Settings: ")]
        [SerializeField] private bool antiRollbarEnabled = false;
        
        [Min(1.0f)]
        [SerializeField] private float stiffness = 100.0f;
        
        [Min(0.1f)]
        [SerializeField] private float rollAngle = 1.0f;

        
        private Rigidbody carRB;
        private Collider carCollider;
        private RaycastHit hit;

        private Vector2 input = Vector2.zero;
        private Vector2 currentInput = Vector2.zero;
        
        private int groundChecksCount = 0;
        
        private float tempVelocityX = 0.0f;
        private float tempVelocityY = 0.0f;

        private float wheelBase = 0.0f;
        private float rearTrack = 0.0f;
        
        private Coroutine dampingCoroutine = null;

        public Vector2 Input { get => input; set => input = value; }

        public float GetNormalLeftWheelAngle()
        {
            float leftWheelAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + (rearTrack / 2.0f)));
            return leftWheelAngle;
        }

        public float GetNormalRightWheelAngle()
        {
            float rightWheelAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - (rearTrack / 2.0f)));
            return rightWheelAngle;
        }
        private float GetCurrentSpeed()
        {
            return Vector3.Dot(transform.forward, carRB.linearVelocity);
        }

        private float GetNormalizedSpeed()
        {
            return Mathf.Clamp01(Mathf.Abs(GetCurrentSpeed()) /
                                                 ((currentInput.y >= 0.0f) ? forwardSpeed : reverseSpeed));
        }

        private void ApplyGravity()
        {
            carRB.AddForce(-Vector3.up * mass * gravity);
        }

        private void HandleForces(WheelData data)
        {
            bool didHitGround = Physics.Raycast(data.suspension.position, 
                                               -data.suspension.up,
                                                out hit, 
                                                data.radius, 
                                                detectGroundLayerMask.value);

            //Debug.Log("Hitting ground: " + didHitGround);

            if(!didHitGround)
            {
                return;
            }

            groundChecksCount++;

            //Suspension Forces
            Vector3 springDir = data.suspension.up;

            Vector3 tireWorldVelocity = carRB.GetPointVelocity(data.suspension.position);

            float offset = data.suspensionRestDistance - hit.distance;

            float velocity = Vector3.Dot(springDir, tireWorldVelocity);

            float forceAmount = (offset * springStrength) - (velocity * damperStrength);

            Vector3 springForce = springDir * forceAmount;

            //Steering Forces
            Vector3 steeringDir = data.suspension.right; //(input.x == 0.0f || !data.allowSteering) ? data.suspension.right :
                                    //                      input.x * data.suspension.right;

            float steeringVelocity = Vector3.Dot(steeringDir, tireWorldVelocity);

            float desiredVelocityChange = -steeringVelocity * data.tireGripFactor;

            float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;

            Vector3 steeringForce = steeringDir * data.mass * desiredAcceleration;


            //Acceleration Forces
            Vector3 accelDir = data.suspension.forward;

            float carSpeed = GetCurrentSpeed();

            //Debug.Log("Car Speed: " + carSpeed);

            float normalizedSpeed = GetNormalizedSpeed();

            //Debug.Log("Normalized speed: " + normalizedSpeed);

            float availaibleTorque = (currentInput.y != 0.0f) ? powerCurve.Evaluate(normalizedSpeed) * maxTorque * currentInput.y : 0.0f;
            //Debug.Log("Availaible torque: " + availaibleTorque);

            Vector3 accelerationForce = (normalizedSpeed < 1.0f) ? accelDir * availaibleTorque : Vector3.zero;

            //Debug.Log("Acceleration Force: " + accelerationForce);

            //carRB.AddForceAtPosition(springForce + steeringForce + accelerationForce, data.suspension.position);
            carRB.AddForceAtPosition(springForce, data.suspension.position);
            carRB.AddForceAtPosition(steeringForce, data.suspension.position);
            carRB.AddForceAtPosition(accelerationForce, data.suspension.position);
        }

        private void HandleDampOnTouchingGround()
        {
            if(groundChecksCount == 0)
            {
                dampingCoroutine = null;
            }
            else if(groundChecksCount == 4 && dampingCoroutine == null)
            {
                dampingCoroutine = StartCoroutine(Damp());
            }
        }
        

        private void HandleSteering()
        {
            if(wheelFL != null && wheelFL.suspension != null)
            {
                float leftWheelAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + Mathf.Sign(input.x) * (rearTrack / 2.0f))) * currentInput.x;
                leftWheelAngle *= wheelFL.steerCurve.Evaluate(GetNormalizedSpeed());
                
                Vector3 leftWheelEulerAngles = wheelFL.suspension.localEulerAngles;
                leftWheelEulerAngles.y = leftWheelAngle;

                wheelFL.suspension.localEulerAngles = leftWheelEulerAngles;
            }

            if(wheelFR != null && wheelFR.suspension != null)
            {
                float rightWheelAngle = Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - Mathf.Sign(input.x) * (rearTrack / 2.0f))) * currentInput.x;
                rightWheelAngle *= wheelFR.steerCurve.Evaluate(GetNormalizedSpeed());

                Vector3 rightWheelEulerAngles = wheelFR.suspension.localEulerAngles;
                rightWheelEulerAngles.y = rightWheelAngle;

                wheelFR.suspension.localEulerAngles = rightWheelEulerAngles;
            }
        }

        private void HandleAntiRollbar()
        {
            if(!antiRollbarEnabled)
            {
                return;
            }


            RaycastHit leftWheelHit;
            RaycastHit rightWheelHit;
            

            float travelLeft = 1.0f;
            float travelRight = 1.0f;

            /*if (wheelFL != null && wheelFR != null && wheelFL.suspension != null && wheelFR.suspension != null)
            {
                
                bool wheelFLCheck = Physics.Raycast(wheelFL.suspension.position,
                                                          -wheelFL.suspension.up,
                                                           out leftWheelHit,
                                                           wheelFL.radius,
                                                           detectGroundLayerMask.value);

                bool wheelFRCheck = Physics.Raycast(wheelFR.suspension.position,
                                                          -wheelFR.suspension.up,
                                                           out rightWheelHit,
                                                           wheelFR.radius,
                                                           detectGroundLayerMask.value);

                

                if (wheelFLCheck)
                {
                    Debug.DrawLine(wheelFL.suspension.position,
                                   leftWheelHit.point, Color.white);

                    travelLeft = (wheelFL.suspension.InverseTransformPoint(leftWheelHit.point).y - wheelFL.radius) / wheelFL.suspensionRestDistance;
                }

                if (wheelFRCheck)
                {
                    Debug.DrawLine(wheelFR.suspension.position,
                                   rightWheelHit.point, Color.white);

                    travelRight = (wheelFR.suspension.InverseTransformPoint(rightWheelHit.point).y - wheelFR.radius) / wheelFR.suspensionRestDistance;
                }

                float antiRollbarForceAmount = (travelLeft - travelRight) * stiffness * rollAngle;



                carRB.AddForceAtPosition(wheelFL.suspension.up * -antiRollbarForceAmount,
                                          wheelFL.suspension.position);

                carRB.AddForceAtPosition(wheelFR.suspension.up * antiRollbarForceAmount,
                                          wheelFR.suspension.position);
            }*/

            if (wheelRL != null && wheelRR != null && wheelRL.suspension != null && wheelRR.suspension != null)
            {

                bool wheelRLCheck = Physics.Raycast(wheelRL.suspension.position,
                                                          -wheelRL.suspension.up,
                                                           out leftWheelHit,
                                                           wheelRL.radius,
                                                           detectGroundLayerMask.value);

                bool wheelRRCheck = Physics.Raycast(wheelRR.suspension.position,
                                                          -wheelRR.suspension.up,
                                                           out rightWheelHit,
                                                           wheelRR.radius,
                                                           detectGroundLayerMask.value);


                travelLeft = 1.0f;
                travelRight = 1.0f;

                if (wheelRLCheck)
                {
                    Debug.DrawLine(wheelRL.suspension.position,
                                   leftWheelHit.point, Color.white);

                    travelLeft = (wheelRL.suspension.InverseTransformPoint(leftWheelHit.point).y - wheelRL.radius) / wheelRL.suspensionRestDistance;
                }

                if (wheelRRCheck)
                {
                    Debug.DrawLine(wheelRR.suspension.position,
                                   rightWheelHit.point, Color.white);

                    travelRight = (wheelRR.suspension.InverseTransformPoint(rightWheelHit.point).y - wheelRR.radius) / wheelRR.suspensionRestDistance;
                }


                float antiRollbarForceAmount = (travelLeft - travelRight) * stiffness * rollAngle;



                carRB.AddForceAtPosition(wheelRL.suspension.up * -antiRollbarForceAmount,
                                          wheelRL.suspension.position);

                carRB.AddForceAtPosition(wheelRR.suspension.up * antiRollbarForceAmount,
                                          wheelRR.suspension.position);
            }

        }

        private void HandleWheelGraphics(WheelData data)
        {
            if (data != null && data.suspension != null)
            {
                Vector3 wheelAngle = data.suspension.localEulerAngles;
                wheelAngle.x = 0.0f;
                wheelAngle.z = 0.0f;
                data.wheelGraphic.localEulerAngles = wheelAngle;
            }
        }
        private void Setup()
        {
            carRB.isKinematic = false;
            carRB.mass = mass;
            carRB.useGravity = false;
            carRB.constraints = RigidbodyConstraints.None;
            carRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            carRB.interpolation = RigidbodyInterpolation.Interpolate;
            
            wheelBase = Vector3.Distance(wheelFL.suspension.position, wheelFR.suspension.position);
            rearTrack = Vector3.Distance(wheelFL.suspension.position, wheelRL.suspension.position);
        }

        private IEnumerator Damp()
        {
            carRB.linearDamping = linearDamp;
            carRB.angularDamping = angularDamp;
            yield return new WaitForSeconds(dampingTime);
            carRB.linearDamping = 0.0f;
            carRB.angularDamping = 0.05f;
        }

        private void Awake()
        {
            carRB = GetComponent<Rigidbody>();
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Setup();
        }

        private void Update()
        {
            if(carRB.isKinematic)
            {
                return; 
            }

            if(!interpolatedInput)
            {
                currentInput.x = input.x;
                currentInput.y = input.y;
                return;
            }


            currentInput.x = Mathf.SmoothDamp(currentInput.x, input.x, ref tempVelocityX, steeringInputTime);
            currentInput.y = Mathf.SmoothDamp(currentInput.y, input.y, ref tempVelocityY, accelerateInputTime);

            if ((currentInput.x > 0.0f && currentInput.x <= MIN_STEERING_INPUT) && 
                (currentInput.x < 0.0f && currentInput.x >= -MIN_STEERING_INPUT))
            {
                currentInput.x = 0.0f;
            }
            else if(currentInput.x > 0.0f && currentInput.x >= MAX_STEERING_INPUT)
            {
                currentInput.x = 1.0f;
            }
            else if(currentInput.x < 0.0f && currentInput.x <= -MAX_STEERING_INPUT)
            {
                currentInput.x = -1.0f;
            }

            if((currentInput.y > 0.0f && currentInput.y <= MIN_ACCELERATING_INPUT) &&
                (currentInput.y < 0.0f && currentInput.y >= -MIN_ACCELERATING_INPUT))
            {
                currentInput.y = 0.0f; 
            }
            else if(currentInput.y > 0.0f && currentInput.y >= MAX_ACCELERATING_INPUT)
            {
                currentInput.y = 1.0f;
            }
            else if(currentInput.y < 0.0f && currentInput.y <= -MAX_ACCELERATING_INPUT)
            {
                currentInput.y = -1.0f;
            }
        }
        private void FixedUpdate()
        {
            if(carRB.isKinematic)
            {
                return;
            }

            groundChecksCount = 0;


            HandleForces(wheelFL);
            HandleForces(wheelFR);
            HandleForces(wheelRL);
            HandleForces(wheelRR);

            ApplyGravity();

            HandleDampOnTouchingGround();

            HandleSteering();

            HandleWheelGraphics(wheelFL);
            HandleWheelGraphics(wheelFR);
            HandleWheelGraphics(wheelRL);
            HandleWheelGraphics(wheelRR);

            HandleAntiRollbar();
        }

        private void OnValidate()
        {
            wheelBase = Vector3.Distance(wheelFL.suspension.position, wheelFR.suspension.position);
            rearTrack = Vector3.Distance(wheelFL.suspension.position, wheelRL.suspension.position);

            if (turnRadius < rearTrack/2.0f)
            {
                turnRadius = rearTrack/2.0f;
            }
        }

        private void DrawWheelDetails(WheelData data)
        {
            if(data == null)
            {
                return;
            }

            #if UNITY_EDITOR
            
            if(data.suspension != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(data.suspension.position, 
                                data.suspension.position + Vector3.up * data.suspensionRestDistance);
            }

            if(data.wheelGraphic != null)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(data.wheelGraphic.position, data.wheelGraphic.right, data.radius);
            }

            #endif
        }
        private void OnDrawGizmosSelected()
        {
            DrawWheelDetails(wheelFL);
            DrawWheelDetails(wheelFR);
            DrawWheelDetails(wheelRL);
            DrawWheelDetails(wheelRR);

            if((wheelFL != null && wheelFR != null && wheelRL != null) && 
                (wheelFL.suspension != null && wheelFR.suspension != null && wheelRL.suspension != null))
            {
                Gizmos.color = Color.magenta;

                //For displaying wheel base
                Gizmos.DrawLine(wheelFL.suspension.position, wheelRL.suspension.position);

                //For displaying rear track
                Gizmos.DrawLine(wheelRL.suspension.position, wheelRR.suspension.position);
            }

            //For displaying turn radius.
            if(wheelRL != null && wheelRL.suspension != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(wheelRL.suspension.position,
                                wheelRL.suspension.position + wheelRL.suspension.right * turnRadius);
            }
        }
    }
}
