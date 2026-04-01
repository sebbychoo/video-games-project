using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Spawns furniture inside a room based on its RoomTemplate rules.
    /// Attach to the room's root GameObject (the one with SpawnZone children).
    /// </summary>
    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] RoomTemplate template;
        [SerializeField] int seed = -1; // -1 = random

        private List<PlacedObject> _placed = new List<PlacedObject>();

        private struct PlacedObject
        {
            public GameObject obj;
            public string tag;
            public Vector3 position;
        }

        public void Generate()
        {
            if (seed >= 0)
                Random.InitState(seed);

            ClearSpawned();

            SpawnZone[] zones = GetComponentsInChildren<SpawnZone>();

            if (template == null) return;

            foreach (SpawnRule rule in template.spawnRules)
            {
                int count = Random.Range(rule.minCount, rule.maxCount + 1);

                for (int i = 0; i < count; i++)
                {
                    Vector3 pos;
                    Quaternion rot;

                    if (rule.placement == PlacementType.UnderFurniture)
                    {
                        if (!TryGetUnderFurniturePosition(rule, out pos))
                            continue;
                        rot = Quaternion.identity;
                    }
                    else
                    {
                        SpawnZone zone = PickZone(zones, rule);
                        if (zone == null) continue;

                        pos = FindValidPosition(zone, rule);
                        rot = GetRotation(rule);
                    }

                    if (rule.placement == PlacementType.AlongWall)
                    {
                        SpawnZone zone = PickZone(zones, rule);
                        if (zone != null)
                            pos = SnapToWall(pos, zone);
                    }

                    GameObject obj = Instantiate(rule.prefab, pos, rot, transform);

                    _placed.Add(new PlacedObject
                    {
                        obj = obj,
                        tag = rule.prefab.name,
                        position = pos
                    });
                }
            }
        }

        public void ClearSpawned()
        {
            foreach (var p in _placed)
                if (p.obj != null) Destroy(p.obj);
            _placed.Clear();
        }

        private SpawnZone PickZone(SpawnZone[] zones, SpawnRule rule)
        {
            // Filter zones by tag if AlongWall
            List<SpawnZone> valid = new List<SpawnZone>();
            foreach (var z in zones)
            {
                if (rule.placement == PlacementType.AlongWall)
                {
                    if (z.zoneTag == "wall") valid.Add(z);
                }
                else
                {
                    valid.Add(z);
                }
            }
            if (valid.Count == 0) return zones.Length > 0 ? zones[0] : null;
            return valid[Random.Range(0, valid.Count)];
        }

        private Vector3 FindValidPosition(SpawnZone zone, SpawnRule rule)
        {
            // Try up to 30 times to find a non-overlapping position
            for (int attempt = 0; attempt < 30; attempt++)
            {
                Vector3 candidate = zone.GetRandomPoint();
                if (IsSpacingValid(candidate, rule.minSpacing))
                    return candidate;
            }
            return zone.GetRandomPoint(); // fallback
        }

        private bool IsSpacingValid(Vector3 pos, float minSpacing)
        {
            foreach (var p in _placed)
            {
                if (Vector3.Distance(pos, p.position) < minSpacing)
                    return false;
            }
            return true;
        }

        private bool TryGetUnderFurniturePosition(SpawnRule rule, out Vector3 pos)
        {
            pos = Vector3.zero;
            List<PlacedObject> parents = new List<PlacedObject>();

            foreach (var p in _placed)
            {
                if (p.tag == rule.parentTag)
                    parents.Add(p);
            }

            if (parents.Count == 0) return false;

            PlacedObject parent = parents[Random.Range(0, parents.Count)];
            // Place slightly offset under the parent
            float offsetX = Random.Range(-0.3f, 0.3f);
            float offsetZ = Random.Range(-0.3f, 0.3f);
            pos = parent.position + new Vector3(offsetX, 0f, offsetZ);
            return true;
        }

        private Vector3 SnapToWall(Vector3 pos, SpawnZone zone)
        {
            Bounds b = zone.GetBounds();
            // Find nearest edge
            float distLeft   = Mathf.Abs(pos.x - b.min.x);
            float distRight  = Mathf.Abs(pos.x - b.max.x);
            float distFront  = Mathf.Abs(pos.z - b.min.z);
            float distBack   = Mathf.Abs(pos.z - b.max.z);

            float minDist = Mathf.Min(distLeft, distRight, distFront, distBack);

            if (minDist == distLeft)       pos.x = b.min.x;
            else if (minDist == distRight) pos.x = b.max.x;
            else if (minDist == distFront) pos.z = b.min.z;
            else                           pos.z = b.max.z;

            return pos;
        }

        private Quaternion GetRotation(SpawnRule rule)
        {
            if (rule.placement == PlacementType.Cardinal)
            {
                int[] angles = { 0, 90, 180, 270 };
                return Quaternion.Euler(0f, angles[Random.Range(0, 4)], 0f);
            }
            return Quaternion.identity;
        }
    }
}
