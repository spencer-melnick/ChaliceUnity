using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable IDE1006 // Naming Styles

[ExecuteAlways]
public class PerlinNoise : MonoBehaviour
{
    public Vector2Int resolution
    {
        get { return _resolution; }
        set
        {
            _resolution = Vector2Int.Max(Vector2Int.zero, value);
        }
    }

    public Vector2 scale = new Vector2(10.0f, 10.0f);
    public uint octaves = 4;
    public float persistance = 1.0f;
    public Texture2D noiseTexture { get; private set; }

    private Vector2Int _resolution = new Vector2Int(1024, 1024);

    private void Awake()
    {
        GenerateTexture();
    }

    private void Update()
    {
        GetComponent<Renderer>().sharedMaterial.SetTexture("_NoiseTex", noiseTexture);
    }



    public void GenerateTexture()
    {
        Color[] pixels = new Color[resolution.x * resolution.y];
        noiseTexture = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, true);

        for (int i = 0; i < resolution.x; i ++)
        {
            for (int j = 0; j < resolution.y; j++)
            {
                Vector2 coord = new Vector2(i, j) * scale / resolution;
                float value = PerlinNoiseOctaves(coord, octaves, persistance);
                pixels[i + j * resolution.y] = new Color(value, value, value, 1.0f);
            }
        }

        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
    }


    public static float SamplePerlinNoise()
    {
        return 0.0f;
    }

    public static float PerlinNoiseOctaves(Vector2 coord, uint octaves, float persistence)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(coord.x * frequency, coord.y * frequency) * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }
}
