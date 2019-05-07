using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Runtime.InteropServices;

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

    public Vector3 scale = new Vector3(10.0f, 10.0f, 10.0f);
    public uint octaves = 4;
    public float persistence = 0.5f;
    public int seed = 0;
    public Texture3D noiseTexture { get; private set; }
    public Texture2D previewTexture { get; private set; }

    private Vector3Int _resolution = new Vector3Int(100, 100, 100);

    // private int[] permutations;

    private void Awake()
    {
        // permutations = new int[512];

        if (noiseTexture == null)
        {
            GenerateTexture();
        }
    }

    private void Update()
    {
        GetComponent<Renderer>().sharedMaterial.SetTexture("_NoiseTex", noiseTexture);
    }



    public void GenerateTexture()
    {
        Profiler.BeginSample("Perlin Noise Generation");

        Color[] pixels = new Color[resolution.x * resolution.y * resolution.z];
        int pixelIndex = 0;
        noiseTexture = new Texture3D(resolution.x, resolution.y, resolution.z, TextureFormat.ARGB32, true);

        SeedGenerator((uint)seed);

        for (int k = 0; k < resolution.z; k ++)
        {
            for (int j = 0; j < resolution.y; j++)
            {
                for (int i = 0; i < resolution.x; i++)
                {
                    Vector3 coord = new Vector3(i * scale.x / resolution.x, j * scale.y / resolution.y, k * scale.z / resolution.z);
                    float value = SamplePerlinNoiseOctaves(coord.x, coord.y, coord.z, octaves, persistence);
                    pixels[pixelIndex++] = new Color(value, value, value, 1.0f);
                }
            }
        }

        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
        Texture3D.DontDestroyOnLoad(noiseTexture);

        Color[] previewPixels = new Color[resolution.x * resolution.y];
        System.Array.Copy(pixels, previewPixels, previewPixels.Length);
        previewTexture = new Texture2D(resolution.x, resolution.y, TextureFormat.ARGB32, true);
        previewTexture.SetPixels(previewPixels);
        previewTexture.Apply();

        Profiler.EndSample();
    }

    public static void GeneratePermutations(int seed, int[] permutations)
    {
        List<int> possibleValues = new List<int>(256);
        System.Random random = new System.Random(seed);
        
        for (int i = 0; i < 256; i++)
        {
            possibleValues.Add(i);
        }

        // Randomly place the numbers from possible values into the first 256 indices
        for (int i = 0; i < 256; i++)
        {
            int index = random.Next(0, possibleValues.Count);
            permutations[i] = possibleValues[index];
            possibleValues.RemoveAt(index);
        }

        // Last 256 indices are duplicates of the first 256
        for (int i = 256; i < 512; i++)
        {
            permutations[i] = permutations[i % 256];
        }
    }

    /*
    
    struct HashResult
    {
        public int aaa;
        public int aab;
        public int aba;
        public int abb;
        public int baa;
        public int bab;
        public int bba;
        public int bbb;
    }
    
    public static float Fade(float t)
    {
        Profiler.BeginSample("Fade");

        float value = t * t * t * (t * (t * 6 - 15) + 10);

        Profiler.EndSample();

        return value;
    }

    public static Vector3 Fade(Vector3 t)
    {
        return new Vector3(Fade(t.x), Fade(t.y), Fade(t.z));
    }

    static HashResult HashCube(Vector3Int coord, int[] permutations, bool repeat = true)
    {
        Profiler.BeginSample("Hash Cube");

        Vector3Int c = coord;
        Vector3Int ci = c + Vector3Int.one;
        HashResult result;

        if (repeat)
        {
            ci.x %= 256;
            ci.y %= 256;
            ci.z %= 256;
        }

        result.aaa = permutations[permutations[permutations[ c.x] +  c.y] +  c.z];
        result.aab = permutations[permutations[permutations[ c.x] +  c.y] + ci.z];
        result.aba = permutations[permutations[permutations[ c.x] + ci.y] +  c.z];
        result.abb = permutations[permutations[permutations[ c.x] + ci.y] + ci.z];
        result.baa = permutations[permutations[permutations[ci.x] +  c.y] +  c.z];
        result.bab = permutations[permutations[permutations[ci.x] +  c.y] + ci.z];
        result.bba = permutations[permutations[permutations[ci.x] + ci.y] +  c.z];
        result.bbb = permutations[permutations[permutations[ci.x] + ci.y] + ci.z];

        Profiler.EndSample();

        return result;
    }

    public static float PerlinGradient(int hashValue, Vector3 coord)
    {
        Profiler.BeginSample("Gradient");

        float value = 0;

        // Switch using the first 4 bits of the hash value
        switch (hashValue & 0xF)
        {
            case 0x0: value =  coord.x + coord.y; break;
            case 0x1: value = -coord.x + coord.y; break;
            case 0x2: value =  coord.x - coord.y; break;
            case 0x3: value = -coord.x - coord.y; break;
            case 0x4: value =  coord.x + coord.z; break;
            case 0x5: value = -coord.x + coord.z; break;
            case 0x6: value =  coord.x - coord.z; break;
            case 0x7: value = -coord.x - coord.z; break;
            case 0x8: value =  coord.y + coord.z; break;
            case 0x9: value = -coord.y + coord.z; break;
            case 0xA: value =  coord.y - coord.z; break;
            case 0xB: value = -coord.y - coord.z; break;
            case 0xC: value =  coord.y + coord.x; break;
            case 0xD: value = -coord.y + coord.z; break;
            case 0xE: value =  coord.y - coord.x; break;
            case 0xF: value = -coord.y - coord.z; break;
            default: break;
        }

        Profiler.EndSample();

        return value;
    }

    public static float Lerp(float a, float b, float t)
    {
        Profiler.BeginSample("Lerp");
        float y = a + (b - a) * t;
        Profiler.EndSample();
        return y;
    }

    public static float SamplePerlinNoise(Vector3 coord, int[] permutations, bool repeat = true)
    {
        Profiler.BeginSample("Sampling Noise");

        if (permutations.Length < 512)
        {
            Debug.LogWarning("Cannot sample Perlin Noise: 512 permutations expected, " + permutations.Length + " provided");
            return 0.0f;
        }

        // Wrap coords if we are repeating
        if (repeat)
        {
            coord.x %= 256.0f;
            coord.y %= 256.0f;
            coord.z %= 256.0f;
        }

        Vector3Int coordInt = Vector3Int.FloorToInt(coord);
        Vector3 deltaCoord = coord - coordInt;

        Vector3 fade = Fade(deltaCoord);
        HashResult hashResult = HashCube(coordInt, permutations);

        float x1, x2, y1, y2, value;

        x1 = Lerp(PerlinGradient(hashResult.aaa, deltaCoord - new Vector3(0.0f, 0.0f, 0.0f)),
                  PerlinGradient(hashResult.baa, deltaCoord - new Vector3(1.0f, 0.0f, 0.0f)),
                  fade.x);

        x2 = Lerp(PerlinGradient(hashResult.aba, deltaCoord - new Vector3(0.0f, 1.0f, 0.0f)),
                  PerlinGradient(hashResult.bba, deltaCoord - new Vector3(1.0f, 1.0f, 0.0f)),
                  fade.x);

        y1 = Lerp(x1, x2, fade.y);

        x1 = Lerp(PerlinGradient(hashResult.aab, deltaCoord - new Vector3(0.0f, 0.0f, 1.0f)),
                  PerlinGradient(hashResult.bab, deltaCoord - new Vector3(1.0f, 0.0f, 1.0f)),
                  fade.x);

        x2 = Lerp(PerlinGradient(hashResult.abb, deltaCoord - new Vector3(0.0f, 1.0f, 1.0f)),
                  PerlinGradient(hashResult.bbb, deltaCoord - new Vector3(1.0f, 1.0f, 1.0f)),
                  fade.x);

        y2 = Lerp(x1, x2, fade.y);

        value = Lerp(y1, y2, fade.z);
        value = (value + 1.0f) / 2.0f;

        Profiler.EndSample();

        return value;
    }

    public static float PerlinNoiseOctaves(Vector3 coord, int[] permutations, uint octaves, float persistence)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += SamplePerlinNoise(coord * frequency, permutations) * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    } */

    [DllImport("PerlinNoisePlugin")]
    public static extern float SamplePerlinNoiseOctaves(float x, float y, float z, float octaves, float persistence);

    [DllImport("PerlinNoisePlugin")]
    public static extern float SeedGenerator(uint seed);
}
