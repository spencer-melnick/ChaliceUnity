using UnityEngine;
using System.Collections;
using System;

[ExecuteAlways]
public class WorleyNoise : MonoBehaviour
{
    public Texture2D previewTexture { get; private set; }
    public Vector2Int scale = new Vector2Int(1, 1);
    public int octaves = 5;
    public float persistence = 0.5f;
    public float contrast = 0.5f;
    public float mapLower = 0.2f;
    public int seed = 0;

    const uint _numPermutations = 10;
    uint[] _permutations = new uint[_numPermutations * 2];
    static int[] _coordConstants = { -1, 0, 1 };
    Vector2Int[] _coordOffsets;

    static Vector2Int _previewResolution = new Vector2Int(128, 128);
    Color[] _previewPixels = new Color[_previewResolution.x * _previewResolution.y];

    private void Awake()
    {
        GenerateCoordOffsets();
        previewTexture = new Texture2D(_previewResolution.x, _previewResolution.y, TextureFormat.ARGB32, true);
    }

    public void GeneratePreview()
    {
        int pixelIndex = 0;
        GeneratePermutations();
        GenerateCoordOffsets();

        for (int j = 0; j < _previewResolution.y; j++)
        {
            float yCoord = (float)j * scale.y / (float)_previewResolution.y;

            for (int i = 0; i < _previewResolution.x; i++)
            {
                float xCoord = (float)i * scale.x / (float)_previewResolution.x;
                Vector2 coord = new Vector2(xCoord, yCoord);

                float value = 1.0f - SampleWorleyNoiseOctaves(coord, scale, octaves, persistence);
                value = Remap(value, mapLower, 1.0f, 0.0f, 1.0f);
                value = ApplyContrast(value, contrast);

                Color color = new Color(value, value, value, 1.0f);
                _previewPixels[pixelIndex++] = color;
            }
        }

        previewTexture.SetPixels(_previewPixels);
        previewTexture.Apply();
    }

    float SampleWorleyNoiseOctaves(Vector2 coord, Vector2Int numTiles, float octaves, float persistence)
    {
        float total = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1.0f;
        float maxValue = 1.0f;

        for (int i = 0; i < octaves; i++)
        {
            total += SampleWorleyNoise(coord * frequency, numTiles * Mathf.FloorToInt(frequency)) * amplitude;

            maxValue += amplitude;

            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    float SampleWorleyNoise(Vector2 coord, Vector2Int numTiles)
    {
        Vector2Int cell = Vector2Int.FloorToInt(coord);
        Vector2 localCoord = coord - cell;

        float minDistance = 1.0f;

        foreach (Vector2Int cellOffset in _coordOffsets)
        {
            Vector2Int checkedCell = cell + cellOffset;
            Vector2 checkedOffset = GetOffset(checkedCell, numTiles) + cellOffset;

            float checkedDistance = (checkedOffset - localCoord).magnitude;
            minDistance = Mathf.Min(minDistance, checkedDistance);
        }

        return minDistance;
    }

    void GenerateCoordOffsets()
    {
        _coordOffsets = new Vector2Int[3 * 3 + 1];
        int index = 0;

        foreach (int x in _coordConstants)
        {
            foreach (int y in _coordConstants)
            {
                _coordOffsets[index++] = new Vector2Int(x, y);
            }
        }
    }

    void GeneratePermutations()
    {
        UnityEngine.Random.InitState(seed);

        // Generate range from 0 to num permutations
        for (uint i = 0; i < _numPermutations; i++)
        {
            _permutations[i] = i;
        }

        for (uint i = 0; i < _numPermutations; i++)
        {
            // Generate two random indices
            uint randIndex1 = (uint)Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, (float)(_numPermutations - 1)));
            uint randIndex2 = (uint)Mathf.FloorToInt(UnityEngine.Random.Range(0.0f, (float)(_numPermutations - 1)));

            // Swap values
            uint tempValue = _permutations[randIndex1];
            _permutations[randIndex1] = _permutations[randIndex2];
            _permutations[randIndex2] = tempValue;
        }

        Array.Copy(_permutations, 0, _permutations, _numPermutations, _numPermutations);
    }

    Vector2 GetOffset(Vector2Int coord, Vector2Int numTiles)
    {
        coord.x = WrapValue(coord.x, numTiles.x);
        coord.y = WrapValue(coord.y, numTiles.y);

        Vector2 offset = new Vector2();

        offset.x = (float)HashCoord(coord.x, coord.y, 0) / (float)_numPermutations;
        offset.y = (float)HashCoord(coord.x, coord.y, 1) / (float)_numPermutations;

        return offset;
    }

    uint HashCoord(int coordX, int coordY, int offset)
    {
        coordX = WrapValue(coordX, (int)_numPermutations);
        coordY = WrapValue(coordY, (int)_numPermutations);

        return _permutations[_permutations[_permutations[coordX] + coordY] + offset];
    }

    int WrapValue(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        float normalizedValue = (value - oldMin) / (oldMax - oldMin);
        return normalizedValue * (newMax - newMin) + newMin;
    }

    float ApplyContrast(float input, float contrast)
    {
        input *= 2.0f;
        input -= 1.0f;
        contrast = -contrast;
        return 0.5f + ((input - input * contrast) / (contrast - Mathf.Abs(input) * contrast + 1.0f)) / 2.0f;
    }
}
