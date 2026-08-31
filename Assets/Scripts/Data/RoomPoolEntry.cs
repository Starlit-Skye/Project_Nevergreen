using System;
using UnityEngine;
using Nevergreen.Attributes;

namespace Nevergreen.Data
{
    /// <summary>
    /// Pairs a RoomData asset with its selection rule within the RoomDatabase.
    /// This allows designers to configure room weights in one centralized location.
    /// </summary>
    [Serializable]
    public class RoomPoolEntry
    {
        [Tooltip("The room asset.")]
        public RoomData room;

        [Tooltip("Rule that determines how likely this room is to appear as a choice.")]
        [SerializeReference]
        [SubclassSelector]
        public RoomSelectionRule selectionRule;
    }
}
