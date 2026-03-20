using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Marks an area within a room where furniture can be placed.
    /// Place as a child of your room prefab. Uses the transform position + size as bounds.
    /// </summary>
    public class SpawnZone : MonoBehaviour
    {
        public Vector3 size = new Vector3(3f, 0f, 3f);
        public string zoneTag = ""; // e.g. "wall", "center", "under-desk"

        /// <summary>Returns a random point within this zone (Y = zone's Y).</summary>
        public Vector3 GetRandomPoint()
        {
            float x = Random.Range(-size.x / 2f, size.x / 2f);
            float z = Random.Range(-size.z / 2f, size.z / 2f);
            return transform.position + new Vector3(x, 0f, z);
        }

        /// <summary>Returns the world-space bounds of this zone.</summary>
        public Bounds GetBounds()
        {
            return new Bounds(transform.position, size);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawCube(transform.position, size);
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
