using UnityEngine;

namespace Game.TrackManagement
{
    [System.Serializable]
    public class RaceDriverData
    {

        [SerializeField] private string name;
        [SerializeField] private int ranking;
        [SerializeField] private float completeProgress;
        [SerializeField] private int completedLaps;
        [SerializeField] private int totalLaps;
        [SerializeField] private bool hasCompletedLap = false;
        [SerializeField] private bool reachedEndOfTrack = false;
        [SerializeField] private bool isPlayer = false;
        
        public string Name { get => name; set => name = value; }
        public int Ranking { get => ranking; set => ranking = value; }
        public float CompleteProgress { get => completeProgress; set => completeProgress = value; }
        public int CompletedLaps { get => completedLaps; set => completedLaps = value; }
        public bool HasCompletedLap { get => hasCompletedLap; set => hasCompletedLap = value; }
        public bool ReachedEndOfTrack { get => reachedEndOfTrack; set => reachedEndOfTrack = value; }
        public bool IsPlayer { get => isPlayer; set => isPlayer = value; }
        public int TotalLaps { get => totalLaps; }

        public RaceDriverData(string name, int initialRanking, int totalLaps)
        {
            this.name = name;
            this.ranking = initialRanking;
            this.completeProgress = 0.0f;
            this.completedLaps = 0;
            this.totalLaps = totalLaps;
            this.hasCompletedLap = false;
            this.reachedEndOfTrack = false;
            this.isPlayer = false;
        }
    }
}
