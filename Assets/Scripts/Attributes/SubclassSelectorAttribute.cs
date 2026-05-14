using System;
using UnityEngine;

namespace Nevergreen.Attributes
{
    /// <summary>
    /// Attribute to show a dropdown for selecting a subclass of a SerializeReference field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubclassSelectorAttribute : PropertyAttribute
    {
    }
}
