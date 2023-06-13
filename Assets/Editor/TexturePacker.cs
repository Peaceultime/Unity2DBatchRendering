using UnityEditor;
using UnityEngine;

public class TexturePacker : EditorWindow
{
    public string filename;

    public Texture2D[] textures;
    public TextureFormat format;
    public bool mipmaps;

    [MenuItem("Window/Texture Array Packer")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TexturePacker));
    }
    void OnGUI()
    {
        filename = EditorGUILayout.TextField("File Name", filename);
        format = (TextureFormat)EditorGUILayout.EnumPopup("Format", format);
        mipmaps = EditorGUILayout.Toggle("Create Mipmaps", mipmaps);

        SerializedObject serializedObject = new SerializedObject(this);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("textures"), true);
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Generate"))
        {
            AssetDatabase.CreateAsset(CreateArray(textures, format), "Assets/Atlas/" + filename + ".asset");

            ShowNotification(new GUIContent("Asset created"));
        }
    }

    private Texture2DArray CreateArray(Texture2D[] textures, TextureFormat format)
    {
        Texture2DArray array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, format, mipmaps);

        for (int i = textures.Length - 1; i >= 0; --i)
            Graphics.CopyTexture(textures[i], 0, array, i);

        array.Apply();

        return array;
    }
}