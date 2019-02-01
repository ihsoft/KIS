using KISAPIv1;
using KSPDev.LogUtils;
using KSPDev.PartUtils;
using System;
using System.Linq;
using UnityEngine;

namespace KIS {

  public sealed class KIS_IconViewer {
  const float IconPosY = 0;
  const int CameraLayer = 22;
  const float LightIntensity = 0.4f;
  const float CameraZoom = 0.75f;
  const float RotationsPerSecond = 0.20f;  // Full round in 5 seconds.

  Camera camera;
  float cameraShift;
  GameObject iconPrefab;
  bool disposed;

  static Light iconLight;
  static int iconCount;
  static float globalCameraShift;

  public Texture texture { get; private set; }

  public KIS_IconViewer(Part part, int resolution) {
    if (part.vessel != null && part.vessel.isEVA) {
      MakeKerbalAvatar(part, resolution);
    } else {
      MakePartIcon(part.partInfo, resolution, VariantsUtils.GetCurrentPartVariant(part));
    }
  }

  public KIS_IconViewer(AvailablePart avPart, int resolution, PartVariant variant) {
    MakePartIcon(avPart, resolution, variant);
  }

  /// <summary>Warns if the icon is not disposed properly.</summary>
  /// <remarks>
  /// This method cannot release the Unity resources since teh access to them is only allowed from
  /// the Unity main thread. The best thing this method can do is spamming log errors.
  /// </remarks>
  ~KIS_IconViewer() {
    if (!disposed) {
      DebugEx.Error("RESOURCES LEAK! The IconViewer was not disposed: camera={0}, iconPrefab={1}",
                    camera, iconPrefab);
    }
  }
    
  /// <summary>Releases all the used resources.</summary>
  /// <remarks>
  /// This method <i>must</i> be called if an icon becomes unusable. Otherwise, all the cached Unity
  /// objects will live and take memory till the scene is reloaded. Some of the internal counters
  /// will also not get updated as expected. Simply put, jut call it!
  /// <para>It's safe to call this method multiple times.</para>
  /// </remarks>
  public void Dispose() {
    if (!disposed) {
      if (camera != null) {
        UnityEngine.Object.Destroy(camera.gameObject);
      }
      if (iconPrefab != null) {
        UnityEngine.Object.Destroy(iconPrefab);
      }
      ReleaseCameraSpot();

      camera = null;
      iconPrefab = null;
      texture = null;
      disposed = true;
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
    cameraShift = ReserveCameraSpot();
    GameObject camGo = new GameObject("KASCamItem" + cameraShift);
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
    cameraShift = ReserveCameraSpot();
    GameObject camGo = new GameObject("KASCamItem" + cameraShift);
    camGo.transform.position = new Vector3(cameraShift, IconPosY, 0);
    camGo.transform.rotation = Quaternion.identity;
    camera = camGo.AddComponent<Camera>();
    camera.orthographic = true;
    camera.orthographicSize = CameraZoom;
    camera.clearFlags = CameraClearFlags.Color;
    camera.enabled = false;
    // Render texture
    RenderTexture tex = new RenderTexture(resolution, resolution, 8);
    texture = tex;

    // Layer
    camera.cullingMask = 1 << CameraLayer;
    SetLayerRecursively(iconPrefab, CameraLayer);

    // Texture
    camera.targetTexture = tex;
    camera.ResetAspect();

    ResetPos();
  }

  void ReleaseCameraSpot() {
    if (--iconCount == 0) {
      globalCameraShift = 0;
      DebugEx.Fine("Icon camera global shift is reset to zero");
    }
  }

  float ReserveCameraSpot() {
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

    iconCount++;
    globalCameraShift += 2.0f;
    
    return globalCameraShift;
  }
  #endregion
}

}  // namespace
