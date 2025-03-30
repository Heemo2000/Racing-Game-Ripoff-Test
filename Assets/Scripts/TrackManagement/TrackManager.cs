using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;
using Game.AI;
using Game.PlayerManagement;
using Game.Driving;
using AYellowpaper.SerializedCollections;
using System.Linq;

namespace Game.TrackManagement
{
    public class TrackManager : MonoBehaviour
    {
        [SerializeField] private WaypointManager waypointManager;
        [SerializeField] private AICarSpawner carSpawner;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Player playerPrefab;
        [SerializeField] private CinemachineCamera playerFollowCameraPrefab;
        [SerializeField] private RaceType raceType;
        [Min(2)]
        [SerializeField] private int lapsCount = 2;
        [Min(0.01f)]
        [SerializeField] private float checkRacersInterval = 0.5f;
        
        [SerializeField]
        [SerializedDictionary("ID", "Racer Data")]
        private SerializedDictionary<int, RaceDriverData> raceDriverDatas;

        public UnityEvent<SerializedDictionary<int, RaceDriverData>.ValueCollection> OnCheckingRacersStarted;
        public UnityEvent<SerializedDictionary<int, RaceDriverData>.ValueCollection> OnRacerDataUpdated;
        private List<Car> racers;
        
        private List<string> randomRacerNames;
        private Transform[] waypoints = null;
        private int tempRanking = 1;

        public void InitializeTrack()
        {
            waypoints = waypointManager.Waypoints;

            for(int i = 0; i < spawnPoints.Length; i++)
            {
                Transform spawnPoint = spawnPoints[i];
                Car car = null;
                if(i == 0)
                {
                    Player player = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
                    player.transform.forward = spawnPoint.forward;
                    player.AllowInput = false;
                    var playerFollowCamera = Instantiate(playerFollowCameraPrefab,
                                                          spawnPoint.position,
                                                          Quaternion.identity);
                    playerFollowCamera.Follow = player.transform;
                    playerFollowCamera.LookAt = player.transform;
                    car = player.GetComponent<Car>();
                }
                else
                {
                    var aiCar = carSpawner.SpawnAICar(waypointManager.Waypoints, spawnPoint);
                    aiCar.FollowWaypointEnabled = false;
                    car = aiCar.GetComponent<Car>();
                }

                if(car == null)
                {
                    Debug.LogError("Cannot initialize race, car reference is null");
                    return;
                }
                car.enabled = false;
                racers.Add(car);

                var data = new RaceDriverData(randomRacerNames[Random.Range(0, randomRacerNames.Count)], i, lapsCount);
                data.IsPlayer = i == 0;
                raceDriverDatas.Add(car.gameObject.GetInstanceID(), data);
                
            }

            StartCoroutine(EnableCarComponent());
        }

        public void StartCheckingRacers()
        {
            StartCoroutine(CheckRacers());
            OnCheckingRacersStarted?.Invoke(raceDriverDatas.Values);
        }

        private IEnumerator EnableCarComponent()
        {
            yield return new WaitForSeconds(0.1f);
            foreach(var car in racers)
            {
                car.enabled = true;
            }
        }
        private IEnumerator CheckRacers()
        {
            foreach(var car in racers)
            {
                //car.enabled = true;
                if(car.TryGetComponent<Player>(out Player player))
                {
                    Debug.Log("Enable player component");
                    player.AllowInput = true;
                }
                else if(car.TryGetComponent<AICarController>(out AICarController controller))
                {
                    Debug.Log("Enable controller component");
                    controller.FollowWaypointEnabled = true;
                }
            }

            var wait = new WaitForSeconds(checkRacersInterval);
            while(!DoAllRacersReachedEnd())
            {
                for(int i = 0; i < racers.Count; i++)
                {
                    Car car = racers[i];
                    var id = car.gameObject.GetInstanceID();
                    var data = raceDriverDatas[id];
                    if(data.ReachedEndOfTrack)
                    {
                        yield return null;
                        continue;
                    }
                    float percent = FindRiderCompleteProgress(car);

                    Debug.Log("Complete Percent for " + car.transform.name + " :" + percent);

                    if(percent == 1.0f)
                    {
                        if(car.TryGetComponent<Player>(out Player player))
                        {
                            player.AllowInput = false;
                        }
                        else if(car.TryGetComponent<AICarController>(out AICarController controller))
                        {
                            controller.FollowWaypointEnabled = false;
                        }
                        data.ReachedEndOfTrack = true;
                        raceDriverDatas[id] = data;
                    }

                    yield return null;
                }

                var rankQuery = raceDriverDatas.
                                OrderByDescending(data => data.Value.CompleteProgress);

                tempRanking = 1;

                foreach(var result in rankQuery)
                {
                    raceDriverDatas[result.Key].Ranking = tempRanking;
                    tempRanking++;
                }

                OnRacerDataUpdated?.Invoke(raceDriverDatas.Values);
                yield return wait;
            }

            
            Debug.Log("All racers reached end line");
        }

        private bool DoAllRacersReachedEnd()
        {
            foreach(var data in raceDriverDatas.Values)
            {
                if(!data.ReachedEndOfTrack)
                {
                    return false;
                }
            }

            return true;
        }

        private float FindRiderCompleteProgress(Car car)
        {
            Vector3 carPosition = car.transform.position;

            int closestWayPtIndex = -1;
            float closestSqrDistance = Mathf.Infinity;

            for(int i = 0; i < waypoints.Length; i++)
            {
                var current = waypoints[i];
                float sqrDistance = Vector3.SqrMagnitude(carPosition - current.position);
                if(sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestWayPtIndex = i;
                }
            }

            var data = raceDriverDatas[car.gameObject.GetInstanceID()];

            int lastIndex = waypoints.Length - 1;

            Vector3 direction = (waypoints[closestWayPtIndex].position - car.transform.position).normalized;
            int backIndex = (Vector3.Dot(car.transform.forward, direction) < 0) ? closestWayPtIndex : closestWayPtIndex - 1;
            backIndex = Mathf.Max(0, backIndex);

            int frontIndex = (backIndex + 1 >= waypoints.Length) ? 0 : backIndex + 1;

            float lerpDelta = Vector3.SqrMagnitude(car.transform.position - waypoints[backIndex].position) / Vector3.SqrMagnitude(car.transform.position - waypoints[frontIndex].position);
            float lerpedIndex = Mathf.Lerp(backIndex, frontIndex, lerpDelta);

            if (closestWayPtIndex == lastIndex && car.Input.y < 0.0f)
            {
                return -1.0f;
            }

            switch (raceType)
            {
                case RaceType.Sprint:
                                      float individualSprintPercent = lerpedIndex / (float)lastIndex;
                                      
                                      data.CompleteProgress = individualSprintPercent;
                                      raceDriverDatas[car.gameObject.GetInstanceID()] = data;
                                        
                                      return individualSprintPercent;
                case RaceType.Circuit:

                                      float individualLapPercent = lerpedIndex / 
                                                                (float)lastIndex;

                                      Debug.Log("Individual Lap Percent: " + individualLapPercent);
                                      
                                      if(data.HasCompletedLap == true && individualLapPercent != 1.0f && individualLapPercent >= 0.5f)
                                      {
                                          data.HasCompletedLap = false;
                                      }

                                      if(!data.HasCompletedLap && individualLapPercent >= 0.99f)
                                      {
                                        data.CompletedLaps++;
                                        data.HasCompletedLap = true;
                                      }

                                      float totalPercent = ((float)(data.CompletedLaps * lastIndex) + lerpedIndex) /
                                                           (float)((lapsCount + 1) * lastIndex);

                                      data.CompleteProgress = totalPercent;
                                      raceDriverDatas[car.gameObject.GetInstanceID()] = data;
                                      
                                      return totalPercent;
                    
            }

            return 0.0f;
        }

        private void Awake()
        {
            racers = new List<Car>();
            raceDriverDatas = new SerializedDictionary<int, RaceDriverData>();

            randomRacerNames = new List<string>();
            randomRacerNames.Add("Chungu");
            randomRacerNames.Add("Mangu");
            randomRacerNames.Add("Timka");
            randomRacerNames.Add("Timki");
        }
    }
}
