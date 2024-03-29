﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

#pragma warning disable IDE1006 // Naming Styles

[ExecuteAlways]
public class NoiseGenerator : MonoBehaviour
{
    public enum NoiseType
    {
        PerlinNoise,
        WorleyNoise,
        InverseWorleyNoise,
        PerlinWorleyNoise
    }

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

    public float inMapMin = 0.0f;
    public float inMapMax = 1.0f;
    public float outMapMin = 0.0f;
    public float outMapMax = 1.0f;

    public NoiseType noiseType = NoiseType.PerlinNoise;
    public float blendFactor = 0.5f;

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
        GetComponent<VolumetricCloudRenderer>().material.SetTexture("_NoiseTex", noiseTexture);
    }

    public void GeneratePreview()
    {
        SeedGenerator((uint)seed);

        switch (noiseType)
        {
            case NoiseType.PerlinNoise:
                GeneratePerlinNoiseImage2D((uint)previewResolution.x, (uint)previewResolution.y,
                    (uint)scale.x, (uint)scale.y,
                    octaves, persistence, contrast,
                    inMapMin, inMapMax, outMapMin, outMapMax,
                    previewPixelsFloats);
                break;

            case NoiseType.WorleyNoise:
            case NoiseType.InverseWorleyNoise:
                GenerateWorleyNoiseImage2D((uint)previewResolution.x, (uint)previewResolution.y,
                    (uint)scale.x, (uint)scale.y,
                    octaves, persistence, contrast,
                    inMapMin, inMapMax, outMapMin, outMapMax,
                    previewPixelsFloats);
                break;

            case NoiseType.PerlinWorleyNoise:
                GeneratePerlinWorleyNoiseImage2D((uint)previewResolution.x, (uint)previewResolution.y,
                    (uint)scale.x, (uint)scale.y,
                    octaves, persistence, contrast,
                    inMapMin, inMapMax, outMapMin, outMapMax,
                    previewPixelsFloats);
                break;

            default:
                break;
        }

        for (int i = 0; i < previewPixelsFloats.Length; i++)
        {
            float value = previewPixelsFloats[i];
            
            if (noiseType == NoiseType.InverseWorleyNoise)
            {
                value = 1.0f - value;
            }

            previewPixels[i].r = value;
            previewPixels[i].g = value;
            previewPixels[i].b = value;
            previewPixels[i].a = 1.0f;
        }

        if (previewTexture == null)
        {
            previewTexture = new Texture2D(previewResolution.x, previewResolution.y, TextureFormat.ARGB32, true);
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

        switch (noiseType)
        {
            case NoiseType.PerlinNoise:
                GeneratePerlinNoiseImage3D((uint)resolution.x, (uint)resolution.y, (uint)resolution.z,
                    (uint)scale.x, (uint)scale.y, (uint)scale.z,
                    octaves, persistence, contrast,
                    inMapMin, inMapMax, outMapMin, outMapMax,
                    pixelsFloats);
                break;

            case NoiseType.WorleyNoise:
            case NoiseType.InverseWorleyNoise:
                GenerateWorleyNoiseImage3D((uint)resolution.x, (uint)resolution.y, (uint)resolution.z,
                    (uint)scale.x, (uint)scale.y, (uint)scale.z,
                    octaves, persistence, contrast,
                    inMapMin, inMapMax, outMapMin, outMapMax,
                    pixelsFloats);
                break;

            case NoiseType.PerlinWorleyNoise:
                GeneratePerlinWorleyNoiseImage3D((uint)resolution.x, (uint)resolution.y, (uint)resolution.z,
                    (uint)scale.x, (uint)scale.y, (uint)scale.z,
                    octaves, persistence, contrast,
                    inMapMin, inMapMax, outMapMin, outMapMax,
                    pixelsFloats);
                break;

            default:
                break;
        }

        for (int i = 0; i < pixelsFloats.Length; i++)
        {
            float value = pixelsFloats[i];

            if (noiseType == NoiseType.InverseWorleyNoise)
            {
                value = 1.0f - value;
            }

            pixels[i].r = value;
            pixels[i].g = value;
            pixels[i].b = value;
            pixels[i].a = value;
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

    [DllImport("NoiseGeneratorPlugin")]
    public static extern void GenerateWorleyNoiseImage3D(uint resolutionX, uint resolutionY, uint resolutionZ,
    uint scaleX, uint scaleY, uint scaleZ,
    float octaves, float persistence,
    float contrast,
    float valueMin, float valueMax, float remapMin, float remapMax,
    float[] data);


    [DllImport("NoiseGeneratorPlugin")]
    public static extern void GeneratePerlinWorleyNoiseImage2D(uint resolutionX, uint resolutionY,
        uint scaleX, uint scaleY,
        float octaves, float persistence,
        float contrast,
        float valueMin, float valueMax, float remapMin, float remapMax,
        float[] data);

    [DllImport("NoiseGeneratorPlugin")]
    public static extern void GeneratePerlinWorleyNoiseImage3D(uint resolutionX, uint resolutionY, uint resolutionZ,
        uint scaleX, uint scaleY, uint scaleZ,
        float octaves, float persistence,
        float contrast,
        float valueMin, float valueMax, float remapMin, float remapMax,
        float[] data);
}
