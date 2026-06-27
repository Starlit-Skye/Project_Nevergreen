using UnityEditor;
using UnityEngine;
using Nevergreen.Combat.AI.Nodes;
using Nevergreen.Data;

namespace Nevergreen.Combat.AI.Editor
{
    [CustomPropertyDrawer(typeof(NotHasStatusCondition))]
    public class NotHasStatusConditionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // Type header/foldout (though usually handled by parent for SerializeReference, but let's be safe)
            
            if (!property.isExpanded) return height;

            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("target")) + EditorGUIUtility.standardVerticalSpacing;
            
            var statusTypeProp = property.FindPropertyRelative("statusType");
            height += EditorGUI.GetPropertyHeight(statusTypeProp) + EditorGUIUtility.standardVerticalSpacing;

            var statusType = (StatusType)statusTypeProp.enumValueIndex;
            if (statusType == StatusType.Buff || statusType == StatusType.Debuff)
            {
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("stat")) + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("amplitudeComparison")) + EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("targetAmplitude")) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var targetProp = property.FindPropertyRelative("target");
                EditorGUI.PropertyField(rect, targetProp);
                rect.y += EditorGUI.GetPropertyHeight(targetProp) + EditorGUIUtility.standardVerticalSpacing;

                var statusTypeProp = property.FindPropertyRelative("statusType");
                EditorGUI.PropertyField(rect, statusTypeProp);
                rect.y += EditorGUI.GetPropertyHeight(statusTypeProp) + EditorGUIUtility.standardVerticalSpacing;

                var statusType = (StatusType)statusTypeProp.enumValueIndex;
                if (statusType == StatusType.Buff || statusType == StatusType.Debuff)
                {
                    var statProp = property.FindPropertyRelative("stat");
                    EditorGUI.PropertyField(rect, statProp);
                    rect.y += EditorGUI.GetPropertyHeight(statProp) + EditorGUIUtility.standardVerticalSpacing;

                    var ampCompProp = property.FindPropertyRelative("amplitudeComparison");
                    EditorGUI.PropertyField(rect, ampCompProp);
                    rect.y += EditorGUI.GetPropertyHeight(ampCompProp) + EditorGUIUtility.standardVerticalSpacing;

                    var ampProp = property.FindPropertyRelative("targetAmplitude");
                    EditorGUI.PropertyField(rect, ampProp);
                    rect.y += EditorGUI.GetPropertyHeight(ampProp) + EditorGUIUtility.standardVerticalSpacing;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
