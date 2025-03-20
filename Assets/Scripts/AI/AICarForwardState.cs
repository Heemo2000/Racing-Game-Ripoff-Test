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
            
        }

        public override void OnLogic()
        {
            if(controller.CurrentWaypointIndex >= controller.Waypoints.Length)
            {
                controller.CurrentWaypointIndex = 0;
            }

            //First decide steering input, then accelerate input

            //Steering input.
            Vector3 currentWaypointPos = controller.Waypoints[controller.CurrentWaypointIndex].position;
            Vector3 direction = controller.Waypoints[controller.CurrentWaypointIndex].forward;    //(currentWaypointPos - controller.FrontCheck.position).normalized;

            float dot = Vector3.Dot(carTransform.forward, direction);
            //Debug.Log("Dot: " + dot);

            float angle = Vector3.SignedAngle(direction, carTransform.forward, carTransform.up);
            Debug.Log("Angle: " + angle);
            float steeringSign = -Mathf.Sign(angle);
            
            //Debug.Log("Steering sign: " + steeringSign);

            float normalSteeringInput = steeringSign * (1.0f - dot); //Mathf.Clamp(angle / 30.0f, -1.0f , 1.0f); 

            //Debug.Log("Normal steering input: " + normalSteeringInput);

            float alignedSteeringInput = controller.GetAlignedSteeringInput();
            //Debug.Log("Aligned steering input: " + alignedSteeringInput);

            calculatedInput.x = normalSteeringInput + alignedSteeringInput;
            /*if(calculatedInput.x >= -controller.IgnoreInputRange && calculatedInput.x <= controller.IgnoreInputRange)
            {
                calculatedInput.x = 0.0f;
            }*/

            //Debug.Log("Steering input: " + calculatedInput.x);

            //Accelerate input

            //Check for goddamn corners, if there is not, then accelerate like the drunk driver.
            //Also check for next waypoint.

            if(IsReachingCorner())
            {
                calculatedInput.y = 0.2f;
            }
            else
            {
                calculatedInput.y = 1.0f;
            }

            float wayPtSqrDistance = Vector3.SqrMagnitude(currentWaypointPos - controller.FrontCheck.position);
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

        private bool IsReachingCorner()
        {
            if(controller.CurrentWaypointIndex + 1 >= controller.Waypoints.Length)
            {
                return false;
            }

            Vector3 currentWaypointPos = controller.Waypoints[controller.CurrentWaypointIndex].position;
            Vector3 nextWaypointPos = controller.Waypoints[controller.CurrentWaypointIndex + 1].position;

            Vector3 currentDir = (currentWaypointPos - controller.FrontCheck.position).normalized;
            Vector3 nextWayPtdirection = (nextWaypointPos - currentWaypointPos).normalized;

            float angle = Vector3.Angle(currentDir, nextWayPtdirection);
            //Debug.Log("corner angle: " + angle);
            float speedReduceDistSqr = controller.SpeedReduceDistance * controller.SpeedReduceDistance;
            if (angle >= controller.CornerCheckMinAngle/2f && 
               Vector3.SqrMagnitude(currentWaypointPos - controller.FrontCheck.position) <= speedReduceDistSqr)
            {
                return true;
            }

            return false;
        }
    }
}
