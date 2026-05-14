using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Nevergreen.Attributes;

namespace Nevergreen.Editor.Drawers
{
    /// <summary>
    /// Custom property drawer for SubclassSelectorAttribute.
    /// Provides a dropdown for types that can be assigned to a [SerializeReference] field.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private class TypeData
        {
            public Type Type;
            public string Name;
            public string FullName;

            public TypeData(Type type)
            {
                Type = type;
                Name = type.Name;
                FullName = type.FullName;
            }
        }

        private static Dictionary<Type, List<TypeData>> _typeCache = new Dictionary<Type, List<TypeData>>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "Use SubclassSelector with [SerializeReference]");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // 1. Get the field type (the base type or interface)
            Type fieldType = GetFieldType(property);
            if (fieldType == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndProperty();
                return;
            }

            // 2. Find all implementing types
            List<TypeData> types = GetImplementingTypes(fieldType);

            // 3. Draw the label and dropdown
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            Rect dropdownRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);

            // Get current type name
            string currentTypeName = "Null";
            if (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                string[] parts = property.managedReferenceFullTypename.Split(' ');
                if (parts.Length > 1)
                {
                    currentTypeName = parts[1].Split('.').Last();
                }
            }

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(currentTypeName), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Null"), currentTypeName == "Null", () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });

                foreach (var typeData in types)
                {
                    bool isSelected = currentTypeName == typeData.Name;
                    menu.AddItem(new GUIContent(typeData.Name), isSelected, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(typeData.Type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.DropDown(dropdownRect);
            }

            // 4. Draw the children (the actual fields of the selected type)
            EditorGUI.PropertyField(position, property, GUIContent.none, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private Type GetFieldType(SerializedProperty property)
        {
            string[] parts = property.managedReferenceFieldTypename.Split(' ');
            if (parts.Length < 2) return null;

            string assemblyName = parts[0];
            string typeName = parts[1];

            return Type.GetType($"{typeName}, {assemblyName}");
        }

        private List<TypeData> GetImplementingTypes(Type baseType)
        {
            if (_typeCache.TryGetValue(baseType, out List<TypeData> cached))
            {
                return cached;
            }

            List<TypeData> results = new List<TypeData>();

            // Use TypeCache for efficiency (Unity 2019.2+)
            var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType);

            foreach (var type in derivedTypes)
            {
                if (type.IsAbstract || type.IsInterface || !type.IsSerializable) continue;
                results.Add(new TypeData(type));
            }

            _typeCache[baseType] = results;
            return results;
        }
    }
}
