using UnityEngine;

// Marks a [SerializeReference] field or list so the Inspector shows a menu to pick which class goes in
// it. Unity draws those fields but gives no way to choose the class; SubclassPickerDrawer, in
// Code/Editor, adds it. Only for the editor: in the game it does nothing.
public class SubclassPickerAttribute : PropertyAttribute
{
}
