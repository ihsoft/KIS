using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KIS
{
    public class KIS_IconViewer
    {
        private float iconPosY = 0;
        private int mask = 22;
        private float lightIntensity = 0.4f;
        private float zoom = 0.75f;

        private Camera cam;
        private static Light iconLight;
        private static int camStaticIndex = 0;
        private static int iconCount = 0;
        private int camIndex;
        public GameObject iconPrefab;
        public Texture texture;

        public KIS_IconViewer(Part p, int resolution)
        {
            if (p.partInfo.name != "kerbalEVA")
            {
                // Instantiate part icon
                iconPrefab = UnityEngine.Object.Instantiate((UnityEngine.Object)p.partInfo.iconPrefab) as GameObject;
                iconPrefab.SetActive(true);

                // Icon Camera
                GameObject camGo = new GameObject("KASCamItem" + camStaticIndex);
                camGo.transform.position = new Vector3(camStaticIndex, iconPosY, 0);
                camGo.transform.rotation = Quaternion.identity;
                cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = zoom;
                cam.clearFlags = CameraClearFlags.Color;
                // Render texture
                RenderTexture tex = new RenderTexture(resolution, resolution, 8);
                this.texture = tex;

                //light
                if (iconLight == null)
                {
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
            else
            {
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
            }
            iconCount += 1;
        }

        ~KIS_IconViewer()
        {
            UnityEngine.Object.Destroy(cam.gameObject);
            if (iconPrefab) UnityEngine.Object.Destroy(iconPrefab);
            this.iconPrefab = null;
            this.texture = null;
            iconCount -= 1;
            if (iconCount == 0)
            {
                camStaticIndex = 0;
            }
        }

        public void Rotate()
        {
            iconPrefab.transform.Rotate(0.0f, 1f, 0.0f);
        }

        public void ResetPos()
        {
            iconPrefab.transform.position = new Vector3(camIndex, iconPosY, 2f);
            iconPrefab.transform.rotation = Quaternion.Euler(-15f, 0.0f, 0.0f);
            iconPrefab.transform.Rotate(0.0f, -30f, 0.0f);
        }

        public static void ResetCamIndex()
        {
            camStaticIndex = 0;
        }

        void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (null == obj)
            {
                return;
            }
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (null == child)
                {
                    continue;
                }
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}
