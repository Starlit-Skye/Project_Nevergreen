using UnityEditor;
using UnityEngine;
using Nevergreen.Combat;
using Nevergreen.Data;

namespace Nevergreen.Editor
{
    [CustomEditor(typeof(StatusEffectOnSpawn))]
    public class StatusEffectOnSpawnEditor : UnityEditor.Editor
    {
        private SerializedProperty statusTypeProp;
        private SerializedProperty durationProp;
        private SerializedProperty amplitudeProp;
        private SerializedProperty amplitudeTypeProp;
        private SerializedProperty targetStatProp;
        private SerializedProperty livingFragmentPrefabsProp;

        private void OnEnable()
        {
            statusTypeProp = serializedObject.FindProperty("statusType");
            durationProp = serializedObject.FindProperty("duration");
            amplitudeProp = serializedObject.FindProperty("amplitude");
            amplitudeTypeProp = serializedObject.FindProperty("amplitudeType");
            targetStatProp = serializedObject.FindProperty("targetStat");
            livingFragmentPrefabsProp = serializedObject.FindProperty("livingFragmentPrefabs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(statusTypeProp);
            EditorGUILayout.PropertyField(durationProp);

            StatusType selectedType = (StatusType)statusTypeProp.enumValueIndex;

            if (selectedType == StatusType.LivingFragments)
            {
                EditorGUILayout.PropertyField(livingFragmentPrefabsProp, true);
            }
            else if (selectedType == StatusType.Stealth)
            {
                // Stealth only needs duration, hide amplitude and target stat
            }
            else
            {
                // Standard status fields
                EditorGUILayout.PropertyField(amplitudeProp);
                EditorGUILayout.PropertyField(amplitudeTypeProp);
                EditorGUILayout.PropertyField(targetStatProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
