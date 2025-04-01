using UnityEngine;

namespace Game.Driving
{
    [System.Serializable]
    public class WheelData
    {
        public Transform suspension;
        public Transform wheelGraphic;
        
        [Min(0.1f)]
        public float suspensionRestDistance = 1.0f;
        
        [Min(1.0f)]
        public float radius = 1.0f;

        //public bool allowSteering = false;

        [Range(0.0f, 1.0f)]
        public float tireGripFactor = 1.0f;

        public AnimationCurve steerCurve;

        [Min(1.0f)]
        public float mass = 10.0f;

        private float tireRotationX = 0.0f;

        public float TireRotationX { get => tireRotationX; set => tireRotationX = value; }
    }
}
