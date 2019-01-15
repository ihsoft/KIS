using System;
using System.Linq;
using UnityEngine;

namespace KIS {

public class KIS_IconViewer : IDisposable {
  float iconPosY = 0;
  int mask = 22;
  float lightIntensity = 0.4f;
  float zoom = 0.75f;

  Camera cam;
  static Light iconLight;
  static int camStaticIndex = 0;
  static int iconCount = 0;
  int camIndex;

  public GameObject iconPrefab;
  public Texture texture;

  public KIS_IconViewer(Part p, int resolution) {
    if (p.vessel != null && p.vessel.isEVA) {
      // Icon Camera
      GameObject camGo = new GameObject("KASCamItem" + camStaticIndex);
      camGo.transform.parent = p.transform;
      camGo.transform.localPosition = Vector3.zero + new Vector3(0, 0.35f, 0.7f);
      camGo.transform.localRotation = Quaternion.identity;
      camGo.transform.Rotate(0.0f, 180f, 0.0f);
      cam = camGo.AddComponent<Camera>();
      cam.orthographic = true;
      cam.orthographicSize = 0.35f;
      cam.clearFlags = CameraClearFlags.Color;
      // Render texture
      RenderTexture tex = new RenderTexture(resolution, resolution, 8);
      this.texture = tex;

      cam.cullingMask = Camera.main.cullingMask;
      cam.farClipPlane = 1f;

      // Texture
      cam.targetTexture = tex;
      cam.ResetAspect();
    } else {
      // Instantiate part icon
      iconPrefab = UnityEngine.Object.Instantiate((UnityEngine.Object)p.partInfo.iconPrefab)
          as GameObject;

      // Command Seat Icon Fix (Temporary workaround until squad fix the broken shader)
      Shader fixShader = Shader.Find("KSP/Alpha/Cutoff Bumped");
      foreach (Renderer r in iconPrefab.GetComponentsInChildren<Renderer>(true)) {
        foreach (Material m in r.materials) {
          if (m.shader.name == "KSP/Alpha/Cutoff") {
            m.shader = fixShader;
          }
        }
      }

      iconPrefab.SetActive(true);

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
      this.texture = tex;

      //light
      if (iconLight == null && HighLogic.LoadedSceneIsFlight) {
        GameObject lightGo = new GameObject("KASLight");
        iconLight = lightGo.AddComponent<Light>();
        iconLight.cullingMask = 1 << mask;
        iconLight.type = LightType.Directional;
        iconLight.intensity = lightIntensity;
      }

      // Layer
      cam.cullingMask = 1 << mask;
      SetLayerRecursively(iconPrefab, mask);

      // Texture
      cam.targetTexture = tex;
      cam.ResetAspect();

      // Cam index
      this.camIndex = camStaticIndex;
      camStaticIndex += 2;
      ResetPos();
    }
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
    if (cam) {
        cam.gameObject.DestroyGameObject();
    }
    if (iconPrefab) {
      iconPrefab.DestroyGameObject();
    }
    this.cam = null;
    this.iconPrefab = null;
    this.texture = null;
    iconCount -= 1;
    if (iconCount == 0) {
      camStaticIndex = 0;
    }
  }

  public void Rotate() {
    iconPrefab.transform.Rotate(0.0f, 1f, 0.0f);
    cam.Render();  // Update snapshot.
  }

  public void ResetPos() {
    iconPrefab.transform.position = new Vector3(camIndex, iconPosY, 2f);
    iconPrefab.transform.rotation = Quaternion.Euler(-15f, 0.0f, 0.0f);
    iconPrefab.transform.Rotate(0.0f, -30f, 0.0f);
    cam.Render();  // Update snapshot.
  }

  public static void ResetCamIndex() {
    camStaticIndex = 0;
  }

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
}

}  // namespace
