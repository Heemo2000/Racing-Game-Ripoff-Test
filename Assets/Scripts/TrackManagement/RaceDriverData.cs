using UnityEngine;

namespace Game.TrackManagement
{
    [System.Serializable]
    public class RaceDriverData
    {

        [SerializeField] private string name;
        [SerializeField] private int ranking;
        [SerializeField] private float completeProgress;
        [SerializeField] private float completedLaps;
        public string Name { get => name; set => name = value; }
        public int Ranking { get => ranking; set => ranking = value; }

        public float CompleteProgress { get => completeProgress; set => completeProgress = value; }
        public float CompletedLaps { get => completedLaps; set => completedLaps = value; }

        public RaceDriverData(string name, int initialRanking)
        {
            this.name = name;
            this.ranking = initialRanking;
            this.completeProgress = 0.0f;
            this.completedLaps = 0;
        }
    }
}
