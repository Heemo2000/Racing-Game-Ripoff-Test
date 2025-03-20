using UnityEngine;
using UnityEngine.Pool;

namespace Game.AI
{
    public class AICarSpawner : MonoBehaviour
    {
        [SerializeField] private AICarController carControllerPrefab;
        [Min(1)]
        [SerializeField] private int maxControllerCount = 2;

        private ObjectPool<AICarController> controllerPool;
        private GameObject spawnedCarsHolder;

        public AICarController SpawnAICar(Transform[] waypoints, Transform spawnPoint)
        {
            var controller = controllerPool.Get();
            controller.transform.position = spawnPoint.position;
            controller.transform.forward = spawnPoint.forward;
            controller.Waypoints = waypoints;

            return controller;
        }

        private AICarController CreateAICar()
        {
            var controller = Instantiate(carControllerPrefab, transform.position, Quaternion.identity);
            controller.transform.parent = spawnedCarsHolder.transform;
            controller.gameObject.SetActive(false);
            return controller;
        }

        private void OnGetAICar(AICarController controller)
        {
            controller.gameObject.SetActive(true);
        }

        private void OnReleaseAICar(AICarController controller)
        {
            controller.gameObject.SetActive(false);
        }

        private void OnDestroyAICar(AICarController controller)
        {
            Destroy(controller.gameObject);
        }

        private void Awake()
        {
            spawnedCarsHolder = new GameObject("Spawned cars Holder");
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if(controllerPool == null)
            {
                controllerPool = new ObjectPool<AICarController>(CreateAICar,
                                                                 OnGetAICar,
                                                                 OnReleaseAICar,
                                                                 OnDestroyAICar,
                                                                 true,
                                                                 1,
                                                                 maxControllerCount);
            }
        }
    }
}
