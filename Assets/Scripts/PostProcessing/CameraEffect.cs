using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ImageEffectAllowedInSceneView]
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraEffect : MonoBehaviour
{
    [SerializeField] private Material material;

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }   

        Graphics.Blit(source, destination, material);
    }
}
