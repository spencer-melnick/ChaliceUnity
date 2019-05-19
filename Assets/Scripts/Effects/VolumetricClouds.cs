﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class VolumetricClouds : MonoBehaviour
{
    private void Start()
    {
        Camera.main.depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnEnable()
    {
        // Camera.onPreRender += EnableDepthTexture;
    }

    private void OnDisable()
    {
        // Camera.onPreRender -= EnableDepthTexture;
    }

    void EnableDepthTexture(Camera camera)
    {
        camera.depthTextureMode |= DepthTextureMode.Depth;
    }

    void UpdateMVP(Camera camera)
    {
        Matrix4x4 m = transform.GetComponent<Renderer>().localToWorldMatrix;
        Matrix4x4 v = camera.worldToCameraMatrix;
        Matrix4x4 p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        Matrix4x4 mvp = p * v * m;
        Matrix4x4 inverseMvp = mvp.inverse;
        GetComponent<Renderer>().sharedMaterial.SetMatrix("_MVP", mvp);
        GetComponent<Renderer>().sharedMaterial.SetMatrix("_InverseMVP", inverseMvp);
    }
}
