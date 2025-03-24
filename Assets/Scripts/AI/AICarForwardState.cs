using UnityEngine;
using UnityHFSM;

namespace Game.AI
{
    public class AICarForwardState : StateBase<string>
    {
        private AICarController controller = null;
        private Vector2 calculatedInput = Vector2.zero;
        private Transform carTransform;

        public AICarForwardState(AICarController controller) : base(false, false)
        {
            this.controller = controller;
            this.carTransform = controller.transform;
        }

        public override void Init()
        {
            
        }

        public override void OnEnter()
        {
            calculatedInput.x = 0.0f;
            calculatedInput.y = 0.0f;
        }

        public override void OnLogic()
        {
            if(controller.Waypoints.Length == 0)
            {
                return;
            }

            if(controller.CurrentWaypointIndex >= controller.Waypoints.Length)
            {
                controller.CurrentWaypointIndex = 0;
            }

            //First decide steering input, then accelerate input

            //Steering input.
            Vector3 currentWaypointPos = controller.Waypoints[controller.CurrentWaypointIndex].position;
            Vector3 direction = (currentWaypointPos - carTransform.position).normalized;
            //Vector3 currentWaypointForward = controller.Waypoints[controller.CurrentWaypointIndex].forward;

            float angle = Mathf.Clamp(Vector3.SignedAngle(carTransform.forward, direction, Vector3.up), -controller.WaypointCheckAngle/2.0f, controller.WaypointCheckAngle/2.0f);

            Debug.DrawLine(carTransform.position, carTransform.position + direction * 5.0f, Color.yellow);
            Debug.DrawLine(carTransform.position, carTransform.position + carTransform.forward * 5.0f, Color.red);

            //Debug.Log("Angle: " + angle);

            float steeringSign = Mathf.Sign(angle);

            float leftWheelAngle = controller.NormalLeftWheelAngle;
            float rightWheelAngle = controller.NormalRightWheelAngle;

            //Debug.Log("Left Wheel Angle: " + leftWheelAngle);
            //Debug.Log("Right Wheel Angle: " + rightWheelAngle);

            float steeringAmount = Mathf.Abs(angle) / Mathf.Abs(Mathf.Max(leftWheelAngle, 
                                                                          rightWheelAngle));

            float normalSteeringInput = steeringSign * steeringAmount;

            //Debug.Log("Normal steering input: " + normalSteeringInput);

            float alignedSteeringInput = controller.GetAlignedSteeringInput();
            
            //Debug.Log("Aligned steering input: " + alignedSteeringInput);

            calculatedInput.x = Mathf.Clamp(normalSteeringInput + alignedSteeringInput , -1.0f, 1.0f);
            Debug.Log("Steering input: " + calculatedInput.x);
            
            if(calculatedInput.x >= -controller.IgnoreInputRange && calculatedInput.x <= controller.IgnoreInputRange)
            {
                calculatedInput.x = 0.0f;
            }

            //Debug.Log("Steering input: " + calculatedInput.x);

            //Accelerate input

            //Check for goddamn corners, if there is not, then accelerate like the drunk driver.
            //Also check for next waypoint.

            bool reachingCorner = IsReachingCorner(out float slowdownAccInput);
            if (reachingCorner)
            {
                //Debug.Log("Reaching corner");   
            }

            calculatedInput.y = slowdownAccInput;

            float wayPtSqrDistance = Vector3.SqrMagnitude(currentWaypointPos - controller.FrontCheckPos);
            if(wayPtSqrDistance <= controller.WaypointChangeDist * controller.WaypointChangeDist)
            {
                controller.CurrentWaypointIndex++;
            }

            //calculatedInput.Normalize();
            controller.SetInput(calculatedInput.x, calculatedInput.y);
        }

        public override void OnExit()
        {
            
        }

        private bool IsReachingCorner(out float slowdownAcceleratingInput)
        {
            if(controller.CurrentWaypointIndex + 1 >= controller.Waypoints.Length)
            {
                slowdownAcceleratingInput = 1.0f;
                return false;
            }

            Vector3 currentWaypointPos = controller.Waypoints[controller.CurrentWaypointIndex].position;
            Vector3 nextWaypointPos = controller.Waypoints[controller.CurrentWaypointIndex + 1].position;

            Vector3 previousPos = (controller.CurrentWaypointIndex - 1 >= 0) ? 
                                  controller.Waypoints[controller.CurrentWaypointIndex - 1].position : 
                                  carTransform.position;

            Vector3 currentDir = (currentWaypointPos - previousPos).normalized;
            Vector3 nextWayPtdirection = (nextWaypointPos - currentWaypointPos).normalized;

            float angle = Vector3.SignedAngle(currentDir, nextWayPtdirection, Vector3.up);
            Debug.Log("Corner angle: " + angle);
            float speedReduceDistSqr = controller.SpeedReduceDistance * controller.SpeedReduceDistance;
            float sqrDistance = Vector3.SqrMagnitude(currentWaypointPos - controller.FrontCheckPos);

            
            if (!(angle >= -controller.CornerCheckMinAngle/2.0f && angle <= controller.CornerCheckMinAngle/2.0f) && sqrDistance <= speedReduceDistSqr)
            {
                slowdownAcceleratingInput = sqrDistance / speedReduceDistSqr;
                return true;
            }

            slowdownAcceleratingInput = 1.0f;
            return false;
        }
    }
}
