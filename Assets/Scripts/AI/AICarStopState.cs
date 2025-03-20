using UnityEngine;
using UnityHFSM;

namespace Game.AI
{
    public class AICarStopState : StateBase<string>
    {
        private AICarController controller;
        public AICarStopState(AICarController controller) : base(false, false)
        {
            this.controller = controller;
        }

        public override void Init()
        {
            
        }

        public override void OnEnter()
        {
            
        }

        public override void OnLogic()
        {
            controller.SetInput(0.0f, 0.0f);
        }

        public override void OnExit()
        {
            
        }
    }
}
