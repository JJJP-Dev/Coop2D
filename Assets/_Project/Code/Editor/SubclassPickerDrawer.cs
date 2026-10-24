using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Draws a [SerializeReference, SubclassPicker] field: a button with the class it holds and a menu
// with every class it can hold, then its fields as usual. It's how an upgrade's effect blocks are added
// in the Inspector. Editor only: it lives in a folder called Editor, so Unity leaves it out of builds.
[CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
public class SubclassPickerDrawer : PropertyDrawer
{
    // The classes that can go in each kind of field, found once per base class.
    private static readonly Dictionary<Type, List<Type>> Candidates = new();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        // The button goes on the first line, right of the label, and is drawn first so it gets the click
        // before the foldout under it. Then the fields, as Unity draws them (drawing the same property
        // inside its own drawer falls back to Unity's drawing, so this doesn't loop).
        Rect button = new(position.x + EditorGUIUtility.labelWidth + 2f, position.y,
                          position.width - EditorGUIUtility.labelWidth - 2f, EditorGUIUtility.singleLineHeight);

        object value = property.managedReferenceValue;
        string current = value != null ? ObjectNames.NicifyVariableName(value.GetType().Name) : "(vacío: elige un bloque)";

        if (EditorGUI.DropdownButton(button, new GUIContent(current), FocusType.Keyboard))
            ShowMenu(property);

        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndProperty();
    }

    private void ShowMenu(SerializedProperty property)
    {
        // A copy: the one passed to OnGUI is reused by Unity for the next element.
        SerializedProperty target = property.Copy();
        GenericMenu menu = new();

        menu.AddItem(new GUIContent("(vacío)"), target.managedReferenceValue == null, () => Set(target, null));
        menu.AddSeparator("");

        foreach (Type type in GetCandidates(GetBaseType()))
        {
            Type chosen = type;
            bool selected = target.managedReferenceValue != null && target.managedReferenceValue.GetType() == type;
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), selected, () => Set(target, chosen));
        }

        menu.ShowAsContext();
    }

    private static void Set(SerializedProperty property, Type type)
    {
        property.serializedObject.Update();
        property.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
        property.serializedObject.ApplyModifiedProperties();
    }

    // The class the field holds: for a list or an array, the class of its elements.
    private Type GetBaseType()
    {
        Type type = fieldInfo.FieldType;

        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
            return type.GetGenericArguments()[0];

        return type;
    }

    private static List<Type> GetCandidates(Type baseType)
    {
        if (Candidates.TryGetValue(baseType, out List<Type> types))
            return types;

        types = new List<Type>();
        foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
        {
            // Unity can only create classes it can serialize with an empty constructor.
            if (!type.IsAbstract && !type.IsGenericType && !typeof(UnityEngine.Object).IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) != null)
                types.Add(type);
        }

        types.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        Candidates.Add(baseType, types);
        return types;
    }
}
