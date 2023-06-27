using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
public class MinMaxSliderDrawer : PropertyDrawer
{
    private const int FLOAT_WIDTH = 36;
    private const int SPACING = 6;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxSliderAttribute range = attribute as MinMaxSliderAttribute;

        SerializedProperty copy = property.Copy();
        float2 value;

        copy.Next(true);
        value.x = copy.floatValue;
        copy.Next(true);
        value.y = copy.floatValue;

        label = EditorGUI.BeginProperty(position, label, property);

        Rect pos = EditorGUI.PrefixLabel(position, label);
        value.x = EditorGUI.FloatField(new Rect(pos.x, pos.y, FLOAT_WIDTH, pos.height), value.x);
        EditorGUI.MinMaxSlider(new Rect(pos.x + FLOAT_WIDTH + SPACING, pos.y, pos.width - (FLOAT_WIDTH + SPACING) * 2, pos.height), GUIContent.none, ref value.x, ref value.y, range.min, range.max);
        value.y = EditorGUI.FloatField(new Rect(pos.x + pos.width - (FLOAT_WIDTH), pos.y, FLOAT_WIDTH, pos.height), value.y);

        EditorGUI.EndProperty();

        property.Next(true);
        property.floatValue = math.clamp(value.x, range.min, value.y);
        property.Next(true);
        property.floatValue = math.clamp(value.y, value.x, range.max);
    }
}