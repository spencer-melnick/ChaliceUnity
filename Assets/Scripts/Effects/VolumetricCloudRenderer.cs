﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ImageEffectAllowedInSceneView, ExecuteAlways]
public class VolumetricCloudRenderer : MonoBehaviour
{
    public float renderScale = 1.0f;

    public Material material;

    private Material _blendMaterial;

    private void OnEnable()
    {
        material = new Material(Shader.Find("Unlit/Clouds"));
        _blendMaterial = new Material(Shader.Find("Hidden/OverlayEffect"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int textureWidth = Mathf.FloorToInt(Screen.width * renderScale);
        int textureHeight = Mathf.FloorToInt(Screen.height * renderScale);

        RenderTexture renderTexture = RenderTexture.GetTemporary(textureWidth, textureHeight);
        Graphics.SetRenderTarget(renderTexture);

        Matrix4x4 frustumCorners = GetFrustumCorners(Camera.current);
        Matrix4x4 inverseViewMatrix = Camera.current.cameraToWorldMatrix;

        material.SetMatrix("_FrustumCorners", frustumCorners);
        material.SetMatrix("_InverseView", inverseViewMatrix);
        material.SetVector("_CameraPosition", Camera.current.transform.position);
        material.SetPass(0);

        DrawFullscreenQuad();

        _blendMaterial.SetTexture("_SecondTex", renderTexture);

        Graphics.SetRenderTarget(destination);
        Graphics.Blit(source, destination, _blendMaterial);
        
        renderTexture.Release();
    }

    // Frustrum corners from https://flafla2.github.io/2016/10/01/raymarching.html
    static Matrix4x4 GetFrustumCorners(Camera cam)
    {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tan_fov * camAspect;
        Vector3 toTop = Vector3.up * tan_fov;

        Vector3 topLeft = (-Vector3.forward - toRight + toTop);
        Vector3 topRight = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        return frustumCorners;
    }

    static void DrawFullscreenQuad()
    {
        GL.PushMatrix();
        GL.LoadOrtho();

        // Z coordinate corresponds to the frustum vector index

        GL.Begin(GL.QUADS);
        GL.Vertex3(-1, -1, 0);
        GL.Vertex3(1, -1, 1);
        GL.Vertex3(1, 1, 2);
        GL.Vertex3(-1, 1, 3);
        GL.End();

        GL.PopMatrix();
    }
}

[CustomEditor(typeof(VolumetricCloudRenderer))]
public class VolumetricCloudRendererEditor : Editor
{
    private VolumetricCloudRenderer _target;
    private MaterialEditor _materialEditor;

    public void OnEnable()
    {
        _target = (VolumetricCloudRenderer)target;
        _materialEditor = (MaterialEditor)Editor.CreateEditor(_target.material, typeof(MaterialEditor));
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        _materialEditor.PropertiesGUI();
    }
}