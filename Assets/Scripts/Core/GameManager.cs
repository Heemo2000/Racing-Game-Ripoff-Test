using UnityEngine;
using UnityEngine.Events;
using Game.TrackManagement;
using System.Collections;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private TrackManager trackManager;

        public UnityEvent OnBeforeGameStart;
        public UnityEvent<int> OnSecondsLeft;
        public UnityEvent OnGameStarted;

        private Coroutine startRaceCoroutine;

        private IEnumerator StartRace()
        {
            Debug.Log("Before Start");
            yield return new WaitForSeconds(0.1f);

            OnBeforeGameStart?.Invoke();
            yield return new WaitForSeconds(1.0f);

            int secondsLeft = 3;
            while(secondsLeft > 0)
            {
                
                OnSecondsLeft?.Invoke(secondsLeft);
                yield return new WaitForSeconds(1.0f);
                secondsLeft--;
            }

            Debug.Log("After Start");
            OnGameStarted?.Invoke();
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            OnBeforeGameStart.AddListener(trackManager.InitializeTrack);
            OnGameStarted.AddListener(trackManager.StartCheckingRacers);
            
            if (startRaceCoroutine == null)
            {
                startRaceCoroutine = StartCoroutine(StartRace());
            }
        }

        private void OnDestroy()
        {
            OnBeforeGameStart.RemoveListener(trackManager.InitializeTrack);
            OnGameStarted.RemoveListener(trackManager.StartCheckingRacers);
        }
    }
}
