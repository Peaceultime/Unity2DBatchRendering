using UnityEditor;
using UnityEngine;

//[CustomPropertyDrawer(typeof(Noiser))]
public class NoiserDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = GUIContent.none;
        bool active = property.FindPropertyRelative("active").boolValue;

        if (active)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("active"), GUIContent.none);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("type"), GUIContent.none);
                EditorGUILayout.PropertyField(property.FindPropertyRelative("shape"), GUIContent.none);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(property.FindPropertyRelative("amplitude").displayName);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("amplitude"), GUIContent.none);
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(property.FindPropertyRelative("frequency").displayName);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("frequency"), GUIContent.none);
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(property.FindPropertyRelative("addition").displayName);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("addition"), GUIContent.none);
                }
            }

            EditorGUILayout.PropertyField(property.FindPropertyRelative("clamp"));
        }
        else
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative("active"), GUIContent.none);
        }
    }
}