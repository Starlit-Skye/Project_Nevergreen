using System.Collections.Generic;
using UnityEngine;

namespace Nevergreen.Data
{
    /// <summary>
    /// Database ScriptableObject that holds the pool of available room types
    /// for the room selection system.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomDatabase", menuName = "Nevergreen/Databases/Room Database")]
    public class RoomDatabase : ScriptableObject
    {
        [Tooltip("Pool of available room types the player can choose from.")]
        public List<RoomData> availableRooms = new List<RoomData>();
    }
}
