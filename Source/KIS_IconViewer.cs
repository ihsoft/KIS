using KISAPIv1;
using System;
using System.Linq;
using UnityEngine;

namespace KIS {

public sealed class KIS_IconViewer : IDisposable {
  const float IconPosY = 0;
  const int CameraLayer = 22;
  const float LightIntensity = 0.4f;
  const float CameraZoom = 0.75f;
  const float RotationsPerSecond = 0.20f;  // Full round in 5 seconds.

  Camera camera;
  int cameraShift;
  GameObject iconPrefab;

  static Light iconLight;
  static int cameraGlobalShift;
  static int iconCount;

  public Texture texture { get; private set; }

  public KIS_IconViewer(Part part, int resolution) {
    if (part.vessel != null && part.vessel.isEVA) {
      MakeKerbalAvatar(part, resolution);
    } else {
      MakePartIcon(part.partInfo, resolution, KISAPI.PartUtils.GetCurrentPartVariant(part));
    }
    iconCount += 1;
  }

  public KIS_IconViewer(AvailablePart avPart, int resolution, PartVariant variant) {
    MakePartIcon(avPart, resolution, variant);
    iconCount += 1;
  }

  ~KIS_IconViewer() {
    if (camera != null) {
      Dispose();
    }
  }

  // The Dispose() method MUST be called instead of garbage-collecting icon instances
  // because we can only access the cam.gameObject member from the main thread.
  public void Dispose()
  {
    if (camera != null) {
      UnityEngine.Object.DestroyImmediate(camera.gameObject);
      camera = null;
    }
    if (iconPrefab != null) {
      UnityEngine.Object.DestroyImmediate(iconPrefab);
      iconPrefab = null;
    }
    if (texture != null) {
      (texture as RenderTexture).Release();
      texture = null;
    }
    iconCount -= 1;
    if (iconCount == 0) {
      cameraGlobalShift = 0;
    }
  }

  public void Rotate() {
    var step = 360.0f * RotationsPerSecond * Time.deltaTime;
    iconPrefab.transform.Rotate(0.0f, step, 0.0f);
    camera.Render();  // Update snapshot.
  }

  public void ResetPos() {
    iconPrefab.transform.position = new Vector3(cameraShift, IconPosY, 2f);
    iconPrefab.transform.rotation = Quaternion.Euler(-15f, 0.0f, 0.0f);
    iconPrefab.transform.Rotate(0.0f, -30f, 0.0f);
    camera.Render();  // Update snapshot.
  }

  static void ResetCamIndex() {
    cameraGlobalShift = 0;
  }

  #region Local utility methods
  void SetLayerRecursively(GameObject obj, int newLayer) {
    if (null == obj) {
      return;
    }
    obj.layer = newLayer;
    foreach (Transform child in obj.transform) {
      if (null == child) {
        continue;
      }
      SetLayerRecursively(child.gameObject, newLayer);
    }
  }

  void MakeKerbalAvatar(Part ownerPart, int resolution) {
    // Icon Camera
    GameObject camGo = new GameObject("KASCamItem" + cameraGlobalShift);
    camGo.transform.parent = ownerPart.transform;
    camGo.transform.localPosition = Vector3.zero + new Vector3(0, 0.35f, 0.7f);
    camGo.transform.localRotation = Quaternion.identity;
    camGo.transform.Rotate(0.0f, 180f, 0.0f);
    camera = camGo.AddComponent<Camera>();
    camera.orthographic = true;
    camera.orthographicSize = 0.35f;
    camera.clearFlags = CameraClearFlags.Color;
    // Render texture
    RenderTexture tex = new RenderTexture(resolution, resolution, 8);
    texture = tex;

    camera.cullingMask = Camera.main.cullingMask;
    camera.farClipPlane = 1f;

    // Texture
    camera.targetTexture = tex;
    camera.ResetAspect();
  }

  void MakePartIcon(AvailablePart avPart, int resolution, PartVariant variant) {
    // Instantiate part icon
    iconPrefab = KISAPI.PartUtils.GetIconPrefab(avPart, variant);

    // Command Seat Icon Fix (Temporary workaround until squad fix the broken shader)
    Shader fixShader = Shader.Find("KSP/Alpha/Cutoff Bumped");
    foreach (Renderer r in iconPrefab.GetComponentsInChildren<Renderer>(true)) {
      foreach (Material m in r.materials) {
        if (m.shader.name == "KSP/Alpha/Cutoff") {
          m.shader = fixShader;
        }
      }
    }

    // Icon Camera
    GameObject camGo = new GameObject("KASCamItem" + cameraGlobalShift);
    camGo.transform.position = new Vector3(cameraGlobalShift, IconPosY, 0);
    camGo.transform.rotation = Quaternion.identity;
    camera = camGo.AddComponent<Camera>();
    camera.orthographic = true;
    camera.orthographicSize = CameraZoom;
    camera.clearFlags = CameraClearFlags.Color;
    camera.enabled = false;
    // Render texture
    RenderTexture tex = new RenderTexture(resolution, resolution, 8);
    texture = tex;

    //light
    if (iconLight == null && HighLogic.LoadedSceneIsFlight) {
      GameObject lightGo = new GameObject("KASLight");
      iconLight = lightGo.AddComponent<Light>();
      iconLight.cullingMask = 1 << CameraLayer;
      iconLight.type = LightType.Directional;
      iconLight.intensity = LightIntensity;
      iconLight.shadows = LightShadows.None;
      iconLight.renderMode = LightRenderMode.ForcePixel;
    }

    // Layer
    camera.cullingMask = 1 << CameraLayer;
    SetLayerRecursively(iconPrefab, CameraLayer);

    // Texture
    camera.targetTexture = tex;
    camera.ResetAspect();

    // Cam index
    cameraShift = cameraGlobalShift;
    cameraGlobalShift += 2;
    ResetPos();
  }
  #endregion
}

}  // namespace
