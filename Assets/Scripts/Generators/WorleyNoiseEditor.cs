using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorleyNoise))]
public class WorleyNoiseEditor : Editor
{
    WorleyNoise _worleyNoise;

    // Preview updater variables
    float _lastUpdateTime = 0.0f;
    float _timeSinceLastPreview = 0.0f;
    static float _previewPeriod = 1000.0f;

    private void OnEnable()
    {
        _worleyNoise = (WorleyNoise)target;
    }

    public override void OnInspectorGUI()
    {
        _worleyNoise.scale = EditorGUILayout.Vector2IntField("Scale: ", _worleyNoise.scale);
        _worleyNoise.octaves = EditorGUILayout.IntField("Octaves: ", _worleyNoise.octaves);
        _worleyNoise.persistence = EditorGUILayout.Slider("Persistence: ", _worleyNoise.persistence, 0.0f, 1.0f);
        _worleyNoise.contrast = EditorGUILayout.Slider("Contrast: ", _worleyNoise.contrast, -1.0f, 1.0f);
        _worleyNoise.mapLower = EditorGUILayout.Slider("Lower Bound: ", _worleyNoise.mapLower, 0.0f, 1.0f);
        _worleyNoise.seed = EditorGUILayout.IntField("Seed: ", _worleyNoise.seed);

        if (GUILayout.Button("Generate Preview"))
        {
            _worleyNoise.GeneratePreview();
        }

        GUILayout.Label("Texture Preview:");
        GUILayout.Label(_worleyNoise.previewTexture);
    }

    void CheckPreview()
    {
        _timeSinceLastPreview += ((float)EditorApplication.timeSinceStartup - _lastUpdateTime);

        if (_timeSinceLastPreview >= _previewPeriod)
        {
            _worleyNoise.GeneratePreview();
            _timeSinceLastPreview = 0.0f;
        }
    }
}