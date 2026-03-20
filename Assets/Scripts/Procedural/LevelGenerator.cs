using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Generates a floor layout by placing rooms and connecting them.
    /// Attach to an empty GameObject in your exploration scene.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] List<RoomTemplate> roomPool;
        [SerializeField] int roomCount = 8;
        [SerializeField] float hallwayWidth = 2f;
        [SerializeField] int seed = -1;

        [Header("Spacing")]
        [SerializeField] float gapBetweenRooms = 2f;

        private List<PlacedRoom> _rooms = new List<PlacedRoom>();

        private struct PlacedRoom
        {
            public RoomTemplate template;
            public GameObject instance;
            public Vector3 position;
            public Bounds bounds;
        }

        private void Start()
        {
            Generate();
        }

        public void Generate()
        {
            if (seed >= 0)
                Random.InitState(seed);
            else
                Random.InitState(System.Environment.TickCount);

            ClearLevel();

            for (int i = 0; i < roomCount; i++)
            {
                RoomTemplate template = roomPool[Random.Range(0, roomPool.Count)];
                Vector3 pos = FindRoomPosition(template, i);

                GameObject room = Instantiate(template.roomPrefab, pos, Quaternion.identity, transform);
                room.name = $"{template.roomName}_{i}";

                // Generate furniture inside the room
                RoomGenerator gen = room.GetComponent<RoomGenerator>();
                if (gen != null)
                    gen.Generate();

                _rooms.Add(new PlacedRoom
                {
                    template = template,
                    instance = room,
                    position = pos,
                    bounds = new Bounds(pos, new Vector3(template.roomSize.x, 3f, template.roomSize.y))
                });
            }
        }

        public void ClearLevel()
        {
            foreach (var r in _rooms)
                if (r.instance != null) Destroy(r.instance);
            _rooms.Clear();
        }

        private Vector3 FindRoomPosition(RoomTemplate template, int index)
        {
            if (index == 0)
                return Vector3.zero;

            // Try to place adjacent to an existing room without overlapping
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // Pick a random existing room to attach to
                PlacedRoom neighbor = _rooms[Random.Range(0, _rooms.Count)];

                // Pick a random side (0=right, 1=left, 2=front, 3=back)
                int side = Random.Range(0, 4);
                Vector3 offset = Vector3.zero;
                float halfW = neighbor.template.roomSize.x / 2f + template.roomSize.x / 2f + gapBetweenRooms;
                float halfD = neighbor.template.roomSize.y / 2f + template.roomSize.y / 2f + gapBetweenRooms;

                switch (side)
                {
                    case 0: offset = Vector3.right * halfW; break;
                    case 1: offset = Vector3.left * halfW; break;
                    case 2: offset = Vector3.forward * halfD; break;
                    case 3: offset = Vector3.back * halfD; break;
                }

                Vector3 candidate = neighbor.position + offset;
                Bounds newBounds = new Bounds(candidate, new Vector3(template.roomSize.x, 3f, template.roomSize.y));

                if (!OverlapsExisting(newBounds))
                    return candidate;
            }

            // Fallback: place far away
            return new Vector3(index * 15f, 0f, 0f);
        }

        private bool OverlapsExisting(Bounds b)
        {
            foreach (var r in _rooms)
            {
                if (r.bounds.Intersects(b))
                    return true;
            }
            return false;
        }
    }
}
