using UnityEngine;

namespace Game.TrackManagement
{
    public class WaypointManager : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;

        public Transform[] Waypoints { get => waypoints; }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            for(int i = 1; i < waypoints.Length; i++)
            {
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            }
        }
    }
}
