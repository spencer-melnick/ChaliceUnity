using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PerlinNoise))]
public class PerlinNoiseEditor : Editor
{
    PerlinNoise _perlinNoise;
    float _lastUpdateTime = 0.0f;
    float _timeSinceLastPreview = 0.0f;
    static float _previewPeriod = 0.1f;

    private void OnEnable()
    {
        _perlinNoise = (PerlinNoise)target;
    }

    public override void OnInspectorGUI()
    {
        _timeSinceLastPreview += ((float)EditorApplication.timeSinceStartup - _lastUpdateTime);
        _lastUpdateTime = (float)EditorApplication.timeSinceStartup;

        if (_timeSinceLastPreview >= _previewPeriod)
        {
            _perlinNoise.GeneratePreview();
            _timeSinceLastPreview = 0.0f;
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        GUILayout.Label("Texture Preview:");
        EditorGUILayout.Space();
        GUILayout.Label(_perlinNoise.previewTexture, GUILayout.MaxHeight(200.0f));

        if (GUILayout.Button("Generate Texture"))
        {
            _perlinNoise.GenerateTexture();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical();
        _perlinNoise.resolution = EditorGUILayout.Vector3IntField("Resolution:", _perlinNoise.resolution);
        _perlinNoise.scale = EditorGUILayout.Vector3Field("Scale:", _perlinNoise.scale);

        int octaves = EditorGUILayout.IntField("Octaves:", (int)_perlinNoise.octaves);
        if (octaves <= 0)
        {
            octaves = 1;
        }
        _perlinNoise.octaves = (uint)octaves;
        _perlinNoise.persistence = EditorGUILayout.Slider("Persistance:", _perlinNoise.persistence, 0.0f, 1.0f);
        _perlinNoise.contrast = EditorGUILayout.Slider("Contrast:", _perlinNoise.contrast, -1.0f, 1.0f);
        _perlinNoise.seed = EditorGUILayout.IntField("Seed:", _perlinNoise.seed);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
}
