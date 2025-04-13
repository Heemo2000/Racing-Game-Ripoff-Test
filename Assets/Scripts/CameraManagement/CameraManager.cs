using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Game.PlayerManagement;
using Game.Driving;
using Game.Input;
using UnityEngine.InputSystem;
using System;

namespace Game.CameraManagement
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera playerFollowCameraPrefab;
        [SerializeField] private CinemachineCamera aiCarCameraPrefab;

        private List<CinemachineCamera> cameras;
        private int currentCameraIndex = 0;
        private ExplicitCameraControls controls;
        public void SetupCameras(List<Car> racers)
        {
            for(int i = 0; i < racers.Count; i++)
            {
                var car = racers[i];
                Vector3 spawnPosition = car.transform.position;
                if (car.TryGetComponent<Player>(out Player player))
                {
                    var playerFollowCamera = Instantiate(playerFollowCameraPrefab,
                                                         spawnPosition,
                                                         Quaternion.identity);
                    
                    playerFollowCamera.Follow = player.transform;
                    playerFollowCamera.LookAt = player.transform;
                    playerFollowCamera.Priority.Enabled = true;
                    playerFollowCamera.Priority.Value = 10;
                    cameras.Add(playerFollowCamera);
                }
                else
                {
                    var aiCarCamera = Instantiate(aiCarCameraPrefab, 
                                                  spawnPosition, 
                                                  Quaternion.identity);

                    aiCarCamera.Follow = car.transform;
                    aiCarCamera.Priority.Enabled = true;
                    aiCarCamera.Priority.Value = 0;
                    cameras.Add(aiCarCamera);
                }
            }
        }

        public void MakeCameraImportant(int index)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                if (i == index)
                {
                    cameras[i].Priority.Value = 10;
                }
                else
                {
                    cameras[i].Priority.Value = 0;
                }
            }
        }


        private void SwitchCamera()
        {
            currentCameraIndex = (currentCameraIndex + 1) % cameras.Count;
            MakeCameraImportant(currentCameraIndex);
        }

        private void OnCameraSwitched(InputAction.CallbackContext context)
        {
            SwitchCamera();
        }

        private void Awake()
        {
            cameras = new List<CinemachineCamera>();
            controls = new ExplicitCameraControls();
        }

        private void Start()
        {
            controls.Enable();
            controls.Camera.ChangeCamera.performed += OnCameraSwitched;
        }

        

        private void OnDestroy()
        {
            controls.Disable();
            controls.Camera.ChangeCamera.performed -= OnCameraSwitched;
        }


    }
}
