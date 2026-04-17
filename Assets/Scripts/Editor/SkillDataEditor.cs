using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Nevergreen.Data;
using Nevergreen.Combat;

namespace Nevergreen.Editor
{
    [CustomEditor(typeof(SkillData))]
    public class SkillDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SkillData skillData = (SkillData)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Add Modular Effects", EditorStyles.boldLabel);

            if (GUILayout.Button("Add New Effect...", GUILayout.Height(30)))
            {
                var dropdown = new EffectTypeDropdown(new AdvancedDropdownState(), (type) =>
                {
                    AddEffect(skillData, type);
                });
                
                // Get the button rect to show the dropdown below it
                Rect rect = GUILayoutUtility.GetLastRect();
                dropdown.Show(rect);
            }
        }

        private void AddEffect(SkillData data, Type type)
        {
            Undo.RecordObject(data, "Add Skill Effect");
            
            ISkillEffect newEffect = (ISkillEffect)Activator.CreateInstance(type);
            data.effects.Add(newEffect);
            
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// Searchable dropdown for ISkillEffect types.
    /// </summary>
    public class EffectTypeDropdown : AdvancedDropdown
    {
        private Action<Type> _onTypeSelected;

        public EffectTypeDropdown(AdvancedDropdownState state, Action<Type> onSelected) : base(state)
        {
            _onTypeSelected = onSelected;
            this.minimumSize = new Vector2(270, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Effects");

            // Look for ISkillEffect types
            var effectTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISkillEffect).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                .OrderBy(t => t.Name);

            foreach (var type in effectTypes)
            {
                // Create item for each type
                var item = new TypeDropdownItem(type, type.Name);
                root.AddChild(item);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeDropdownItem typeItem)
            {
                _onTypeSelected?.Invoke(typeItem.Type);
            }
        }

        private class TypeDropdownItem : AdvancedDropdownItem
        {
            public Type Type { get; }
            public TypeDropdownItem(Type type, string name) : base(name)
            {
                Type = type;
            }
        }
    }
}
