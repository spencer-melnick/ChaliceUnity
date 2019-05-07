using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PerlinNoise))]
public class PerlinNoiseEditor : Editor
{
    PerlinNoise _perlinNoise;

    private void OnEnable()
    {
        _perlinNoise = (PerlinNoise)target;
    }

    public override void OnInspectorGUI()
    {
        _perlinNoise.resolution = EditorGUILayout.Vector3IntField("Resolution", _perlinNoise.resolution);
        _perlinNoise.scale = EditorGUILayout.Vector3Field("Scale", _perlinNoise.scale);

        int octaves = EditorGUILayout.IntField("Octaves", (int)_perlinNoise.octaves);
        if (octaves <= 0)
        {
            octaves = 1;
        }
        _perlinNoise.octaves = (uint)octaves;
        _perlinNoise.persistence = EditorGUILayout.Slider("Persistance", _perlinNoise.persistence, 0.0f, 1.0f);
        _perlinNoise.seed = EditorGUILayout.IntField("Seed", _perlinNoise.seed);

        if (GUILayout.Button("Generate Texture"))
        {
            _perlinNoise.GenerateTexture();
        }

        EditorGUILayout.Separator();
        GUILayout.Label(_perlinNoise.previewTexture, GUILayout.MaxHeight(200.0f));
    }
}
