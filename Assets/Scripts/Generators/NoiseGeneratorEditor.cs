using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGeneratorEditor : Editor
{
    NoiseGenerator _noiseGenerator;
    float _lastUpdateTime = 0.0f;
    float _timeSinceLastPreview = 0.0f;
    static float _previewPeriod = 0.1f;

    private void OnEnable()
    {
        _noiseGenerator = (NoiseGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        _timeSinceLastPreview += ((float)EditorApplication.timeSinceStartup - _lastUpdateTime);
        _lastUpdateTime = (float)EditorApplication.timeSinceStartup;

        if (_timeSinceLastPreview >= _previewPeriod)
        {
            _noiseGenerator.GeneratePreview();
            _timeSinceLastPreview = 0.0f;
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        GUILayout.Label("Texture Preview:");
        EditorGUILayout.Space();
        GUILayout.Label(_noiseGenerator.previewTexture, GUILayout.MaxHeight(200.0f));

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical();
        _noiseGenerator.resolution = EditorGUILayout.Vector3IntField("Resolution:", _noiseGenerator.resolution);
        _noiseGenerator.scale = EditorGUILayout.Vector3IntField("Scale:", _noiseGenerator.scale);

        int octaves = EditorGUILayout.IntField("Octaves:", (int)_noiseGenerator.octaves);
        if (octaves <= 0)
        {
            octaves = 1;
        }
        _noiseGenerator.octaves = (uint)octaves;
        _noiseGenerator.persistence = EditorGUILayout.Slider("Persistance:", _noiseGenerator.persistence, 0.0f, 1.0f);
        EditorGUILayout.MinMaxSlider("Input Range: ", ref _noiseGenerator.inMapMin, ref _noiseGenerator.inMapMax, 0.0f, 1.0f);
        EditorGUILayout.MinMaxSlider("Output Range: ", ref _noiseGenerator.outMapMin, ref _noiseGenerator.outMapMax, 0.0f, 1.0f);
        _noiseGenerator.contrast = EditorGUILayout.Slider("Contrast:", _noiseGenerator.contrast, -1.0f, 1.0f);
        _noiseGenerator.seed = EditorGUILayout.IntField("Seed:", _noiseGenerator.seed);
        _noiseGenerator.noiseType = (NoiseGenerator.NoiseType)EditorGUILayout.EnumPopup("Noise Type: ", _noiseGenerator.noiseType);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Texture"))
        {
            _noiseGenerator.GenerateTexture();
        }
    }
}
