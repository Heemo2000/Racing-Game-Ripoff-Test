using UnityEngine;
using UnityHFSM;

namespace Game.AI
{
    public class AICarReverseState : StateBase<string>
    {
        private AICarController controller;
        private Transform carTransform;
        private Vector2 calculatedInput = Vector2.zero;

        public AICarReverseState(AICarController controller) : base(false, false)
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
            if(controller.Waypoints == null || controller.Waypoints.Length == 0)
            {
                return;
            }

            Vector3 currentWayPtPos = controller.Waypoints[controller.CurrentWaypointIndex].position;
            Vector3 direction = (currentWayPtPos - carTransform.position).normalized;

            float angle = Mathf.Clamp(Vector3.SignedAngle(carTransform.forward, direction, Vector3.up), -controller.WaypointCheckAngle / 2.0f, controller.WaypointCheckAngle / 2.0f);

            float steeringSign = Mathf.Sign(angle);

            float steeringAmount = Mathf.Abs(angle) / Mathf.Abs(Mathf.Max(controller.NormalLeftWheelAngle,
                                                                          controller.NormalRightWheelAngle));

            calculatedInput.x = -steeringSign * steeringAmount;
            calculatedInput.y = -1.0f;

            controller.SetInput(calculatedInput.x, calculatedInput.y);
        }

        public override void OnExit()
        {
            
        }
    }
}
