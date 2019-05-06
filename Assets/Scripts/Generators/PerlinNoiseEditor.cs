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
        _perlinNoise.resolution = EditorGUILayout.Vector2IntField("Resolution", _perlinNoise.resolution);
        _perlinNoise.scale = EditorGUILayout.Vector2Field("Scale", _perlinNoise.scale);

        int octaves = EditorGUILayout.IntField("Octaves", (int)_perlinNoise.octaves);
        if (octaves <= 0)
        {
            octaves = 1;
        }
        _perlinNoise.octaves = (uint)octaves;
        _perlinNoise.persistance = EditorGUILayout.Slider("Persistance", _perlinNoise.persistance, 0.0f, 1.0f);

        if (GUILayout.Button("Generate Texture"))
        {
            _perlinNoise.GenerateTexture();
        }

        EditorGUILayout.Separator();
        GUILayout.Label(_perlinNoise.noiseTexture, GUILayout.MaxHeight(200.0f));
    }
}
