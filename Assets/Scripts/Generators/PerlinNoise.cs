using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

#pragma warning disable IDE1006 // Naming Styles

[ExecuteAlways]
public class PerlinNoise : MonoBehaviour
{
    public Vector3Int resolution
    {
        get { return _resolution; }
        set
        {
            _resolution = Vector3Int.Max(Vector3Int.zero, value);
        }
    }

    public Vector3Int scale = new Vector3Int(10, 10, 10);
    public uint octaves = 4;
    public float persistence = 0.5f;
    public float contrast = 0.5f;
    public int seed = 0;
    public Texture3D noiseTexture { get; private set; }
    public Texture2D previewTexture { get; private set; }

    private Vector3Int _resolution = new Vector3Int(100, 100, 100);
    private static Vector2Int previewResolution = new Vector2Int(128, 128);

    private Color[] previewPixels = new Color[previewResolution.x * previewResolution.y];
    private float[] previewPixelsFloats = new float[previewResolution.x * previewResolution.y];

    private void Awake()
    {
        previewTexture = new Texture2D(previewResolution.x, previewResolution.y, TextureFormat.ARGB32, true);
        if (noiseTexture == null)
        {
            GenerateTexture();
        }
    }

    private void Update()
    {
        GetComponent<Renderer>().sharedMaterial.SetTexture("_NoiseTex", noiseTexture);
    }

    public void GeneratePreview()
    {
        SeedGenerator((uint)seed);

        GeneratePerlinNoiseImage2D((uint)previewResolution.x, (uint)previewResolution.y,
            (uint)scale.x, (uint)scale.y,
            octaves, persistence, contrast,
            0.0f, 1.0f, 0.0f, 1.0f,
            previewPixelsFloats);

        for (int i = 0; i < previewPixelsFloats.Length; i++)
        {
            previewPixels[i].r = previewPixelsFloats[i];
            previewPixels[i].g = previewPixelsFloats[i];
            previewPixels[i].b = previewPixelsFloats[i];
            previewPixels[i].a = 1.0f;
        }

        previewTexture.SetPixels(previewPixels);
        previewTexture.Apply();
}

    public void GenerateTexture()
    {
        Profiler.BeginSample("Perlin Noise Generation");

        Color[] pixels = new Color[resolution.x * resolution.y * resolution.z];
        noiseTexture = new Texture3D(resolution.x, resolution.y, resolution.z, TextureFormat.ARGB32, true);

        SeedGenerator((uint)seed);

        float[] pixelsFloats = new float[resolution.x * resolution.y * resolution.z];
        GeneratePerlinNoiseImage3D((uint)resolution.x, (uint)resolution.y, (uint)resolution.z,
            (uint)scale.x, (uint)scale.y, (uint)scale.z,
            octaves, persistence, contrast,
            0.0f, 1.0f, 0.0f, 1.0f,
            pixelsFloats);

        for (int i = 0; i < pixelsFloats.Length; i++)
        {
            pixels[i].r = pixelsFloats[i];
            pixels[i].g = pixelsFloats[i];
            pixels[i].b = pixelsFloats[i];
            pixels[i].a = pixelsFloats[i];
        }

        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
        Texture3D.DontDestroyOnLoad(noiseTexture);

        GeneratePreview();

        Profiler.EndSample();
    }


    [DllImport("NoiseGeneratorPlugin")]
    public static extern float SeedGenerator(uint seed);

    [DllImport("NoiseGeneratorPlugin")]
    public static extern float SamplePerlinNoiseOctaves(float x, float y, float z,
        uint tilesX, uint tilesY, uint tilesZ,
        float octaves, float persistence);

    [DllImport("NoiseGeneratorPlugin")]
    public static extern void GeneratePerlinNoiseImage2D(uint resolutionX, uint resolutionY,
        uint scaleX, uint scaleY,
        float octaves, float persistence,
        float contrast,
        float valueMin, float valueMax, float remapMin, float remapMax,
        float[] data);

    [DllImport("NoiseGeneratorPlugin")]
    public static extern void GeneratePerlinNoiseImage3D(uint resolutionX, uint resolutionY, uint resolutionZ,
        uint scaleX, uint scaleY, uint scaleZ,
        float octaves, float persistence,
        float contrast,
        float valueMin, float valueMax, float remapMin, float remapMax,
        float[] data);


    [DllImport("NoiseGeneratorPlugin")]
    public static extern void GenerateWorleyNoiseImage2D(uint resolutionX, uint resolutionY,
        uint scaleX, uint scaleY,
        float octaves, float persistence,
        float contrast,
        float valueMin, float valueMax, float remapMin, float remapMax,
        float[] data);
}
