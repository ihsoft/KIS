using KISAPIv1;
using System;
using System.Linq;
using UnityEngine;

namespace KIS {

public sealed class KIS_IconViewer : IDisposable {
  const float iconPosY = 0;
  const int mask = 22;
  const float lightIntensity = 0.4f;
  const float zoom = 0.75f;
  const float rotationsPerSecond = 0.20f;  // Full round in 5 seconds.

  Camera cam;
  static Light iconLight;
  static int camStaticIndex;
  static int iconCount;
  int camIndex;
  GameObject iconPrefab;

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
    if (cam) {
        Dispose();
    }
  }

  // The Dispose() method MUST be called instead of garbage-collecting icon instances
  // because we can only access the cam.gameObject member from the main thread.
  public void Dispose()
  {
    if (cam != null) {
      cam.gameObject.DestroyGameObject();
      cam = null;
    }
    if (iconPrefab != null) {
      iconPrefab.DestroyGameObject();
      iconPrefab = null;
    }
    texture = null;
    iconCount -= 1;
    if (iconCount == 0) {
      camStaticIndex = 0;
    }
  }

  public void Rotate() {
    var step = 360.0f * rotationsPerSecond * Time.deltaTime;
    iconPrefab.transform.Rotate(0.0f, step, 0.0f);
    cam.Render();  // Update snapshot.
  }

  public void ResetPos() {
    iconPrefab.transform.position = new Vector3(camIndex, iconPosY, 2f);
    iconPrefab.transform.rotation = Quaternion.Euler(-15f, 0.0f, 0.0f);
    iconPrefab.transform.Rotate(0.0f, -30f, 0.0f);
    cam.Render();  // Update snapshot.
  }

  static void ResetCamIndex() {
    camStaticIndex = 0;
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
    GameObject camGo = new GameObject("KASCamItem" + camStaticIndex);
    camGo.transform.parent = ownerPart.transform;
    camGo.transform.localPosition = Vector3.zero + new Vector3(0, 0.35f, 0.7f);
    camGo.transform.localRotation = Quaternion.identity;
    camGo.transform.Rotate(0.0f, 180f, 0.0f);
    cam = camGo.AddComponent<Camera>();
    cam.orthographic = true;
    cam.orthographicSize = 0.35f;
    cam.clearFlags = CameraClearFlags.Color;
    // Render texture
    RenderTexture tex = new RenderTexture(resolution, resolution, 8);
    texture = tex;

    cam.cullingMask = Camera.main.cullingMask;
    cam.farClipPlane = 1f;

    // Texture
    cam.targetTexture = tex;
    cam.ResetAspect();
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
    GameObject camGo = new GameObject("KASCamItem" + camStaticIndex);
    camGo.transform.position = new Vector3(camStaticIndex, iconPosY, 0);
    camGo.transform.rotation = Quaternion.identity;
    cam = camGo.AddComponent<Camera>();
    cam.orthographic = true;
    cam.orthographicSize = zoom;
    cam.clearFlags = CameraClearFlags.Color;
    cam.enabled = false;
    // Render texture
    RenderTexture tex = new RenderTexture(resolution, resolution, 8);
    texture = tex;

    //light
    if (iconLight == null && HighLogic.LoadedSceneIsFlight) {
      GameObject lightGo = new GameObject("KASLight");
      iconLight = lightGo.AddComponent<Light>();
      iconLight.cullingMask = 1 << mask;
      iconLight.type = LightType.Directional;
    }

    // Layer
    cam.cullingMask = 1 << mask;
    SetLayerRecursively(iconPrefab, mask);

    // Texture
    cam.targetTexture = tex;
    cam.ResetAspect();

    // Cam index
    camIndex = camStaticIndex;
    camStaticIndex += 2;
    ResetPos();
  }
  #endregion
}

}  // namespace
