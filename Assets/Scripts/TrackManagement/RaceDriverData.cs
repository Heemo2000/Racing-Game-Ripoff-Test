

namespace Game.TrackManagement
{
    public struct RaceDriverData
    {
        private string name;
        private int ranking;
        private float completeProgress;
        private float completedLaps;
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
