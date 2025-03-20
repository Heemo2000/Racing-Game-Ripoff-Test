using UnityEngine;
using Game.Input;
using Game.Driving;

namespace Game.PlayerManagement
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private bool allowInput = true;
        private GameControls controls;
        private Car car;

        public bool AllowInput { get => allowInput; set => allowInput = value; }

        private void Awake()
        {
            controls = new GameControls();
            controls.Enable();
            car = GetComponent<Car>();
        }

        private void Update()
        {
            car.Input = (allowInput) ? controls.Car.Movement.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}
