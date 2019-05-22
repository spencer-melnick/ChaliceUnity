using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class VolumetricClouds : MonoBehaviour
{
    public Vector4 scrollSpeed = new Vector4(0.5f, 0.0f, 0.0f, 0.0f);

    private Material _material;

    private void Update()
    {
        _material = GetComponent<Renderer>().sharedMaterial;

        Vector4 noiseOffset = _material.GetVector("_NoiseOffset");
        Vector4 noiseScale = _material.GetVector("_NoiseScale");
        noiseOffset += scrollSpeed * Time.deltaTime;

        noiseOffset.x %= noiseScale.x;
        noiseOffset.y %= noiseScale.y;
        noiseOffset.z %= noiseScale.z;
        noiseOffset.w %= noiseScale.w;

        _material.SetVector("_NoiseOffset", noiseOffset);
    }
}
