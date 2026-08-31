using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Utility for selecting random rooms based on their configured RoomSelectionRule weights.
    /// </summary>
    public static class WeightedRoomSelector
    {
        /// <summary>
        /// Selects 'count' distinct rooms from the pool using weighted random sampling.
        /// Rooms with weight <= 0 are excluded. If the pool has fewer valid rooms than 'count',
        /// it returns all valid rooms.
        /// </summary>
        public static List<RoomData> SelectRooms(List<RoomPoolEntry> pool, int count, System.Random rng)
        {
            var results = new List<RoomData>();
            if (pool == null || pool.Count == 0 || count <= 0)
                return results;

            // 1. Create a working list of candidates with their weights
            var candidates = new List<(RoomData room, float weight)>();
            float totalWeight = 0f;

            foreach (var entry in pool)
            {
                if (entry == null || entry.room == null) continue;

                // If rule is null, default to weight 1.0f
                float weight = 1f;
                if (entry.selectionRule != null)
                {
                    weight = entry.selectionRule.EvaluateWeight();
                }

                if (weight > 0f)
                {
                    candidates.Add((entry.room, weight));
                    totalWeight += weight;
                }
            }

            if (candidates.Count == 0)
                return results;

            // 2. Sample with replacement (independent rolls)
            for (int i = 0; i < count; i++)
            {
                float roll = (float)(rng.NextDouble() * totalWeight);
                float accumulated = 0f;

                for (int j = 0; j < candidates.Count; j++)
                {
                    accumulated += candidates[j].weight;
                    
                    if (roll <= accumulated || j == candidates.Count - 1)
                    {
                        // Selected this room
                        results.Add(candidates[j].room);
                        break;
                    }
                }
            }

            return results;
        }
    }
}
