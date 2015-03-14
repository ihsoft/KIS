using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KIS
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KISAddonPointer : MonoBehaviour
    {
        public GameObject audioGo = new GameObject();
        public AudioSource audioBipWrong = new AudioSource();
        public static GameObject soundGo;

        // Pointer parameters
        public static bool allowPart = false;
        public static bool allowEva = false;
        public static bool allowStatic = false;
        public static bool allowStackSnap = false;
        public static Part partToAttach;
        private static float maxDist = 2f;
        private static Transform sourceTransform;

        private static bool running = false;
        private static GameObject pointer;
        private static List<MeshRenderer> allModelMr;
        private static Vector3 customRot = new Vector3(0f, 0f, 0f);
        private static Transform pointerNodeTransform;
        private static bool ignoreAttachRules = false;
        private static List<AttachNode> attachNodes = new List<AttachNode>();
        private static int attachNodeIndex;

        private static OnPointerAction pointerAction;
        public enum PointerAction { ClickValid, Cancel }
        public delegate void OnPointerAction(PointerAction pointerAction, Vector3 pos, Quaternion rot, Part pointerPart);


        public static bool isRunning
        {
            get { return running; }
        }

        void Awake()
        {
            audioBipWrong = audioGo.AddComponent<AudioSource>();
            audioBipWrong.volume = GameSettings.UI_VOLUME;
            audioBipWrong.panLevel = 0;  //set as 2D audiosource

            if (GameDatabase.Instance.ExistsAudioClip(KIS_Shared.bipWrongSndPath))
            {
                audioBipWrong.clip = GameDatabase.Instance.GetAudioClip(KIS_Shared.bipWrongSndPath);
            }
            else
            {
                KIS_Shared.DebugError("Awake(AttachPointer) Bip wrong sound not found in the game database !");
            }
        }

        public static void StartPointer(Part partToMoveAndAttach, OnPointerAction pClick, bool partIsValid, bool evaIsValid, bool staticIsValid, bool ignoreRule, float maxDistance = 0, Transform from = null)
        {
            if (!running)
            {
                KIS_Shared.DebugLog("StartPointer(pointer)");
                customRot = Vector3.zero;
                allowPart = partIsValid;
                allowEva = evaIsValid;
                allowStatic = staticIsValid;
                partToAttach = partToMoveAndAttach;
                if (maxDistance == 0)
                {
                    maxDist = Mathf.Infinity;
                }
                else
                {
                    maxDist = maxDistance;
                }
                ignoreAttachRules = ignoreRule;
                sourceTransform = from;
                running = true;
                pointerAction = pClick;
                // Set possible attach nodes
                attachNodes.Clear();
                if (partToAttach.attachRules.srfAttach)
                {
                    KIS_Shared.DebugLog("Surface node set to default");
                    attachNodes.Add(partToMoveAndAttach.srfAttachNode);
                }
                else if (partToAttach.attachNodes.Count == 0)
                {
                    KIS_Shared.DebugLog("No attach nodes found, surface node set to default");
                    attachNodes.Add(partToMoveAndAttach.srfAttachNode);
                }
                else if (partToAttach.findAttachNode("bottom") != null)
                {
                    KIS_Shared.DebugLog("Bottom node set to default");
                    attachNodes.Add(partToAttach.findAttachNode("bottom"));
                }
                else
                {
                    KIS_Shared.DebugLog(partToAttach.attachNodes[0].id + " node set to default");
                    attachNodes.Add(partToAttach.attachNodes[0]);
                }
                foreach (AttachNode an in partToMoveAndAttach.attachNodes)
                {
                    KIS_Shared.DebugLog("Node : " + an.id + " added");
                    attachNodes.Add(an);
                }
                attachNodeIndex = 0;
                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "KISpointer");
            }
        }

        public static void StopPointer()
        {
            KIS_Shared.DebugLog("StopPointer(pointer)");
            running = false;
            InputLockManager.RemoveControlLock("KISpointer");
        }

        void Update()
        {
            UpdatePointer();
            UpdateKey();
        }

        private void UpdateKey()
        {
            if (isRunning)
            {
                if (
                Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.Return)
                )
                {
                    KIS_Shared.DebugLog("Cancel key pressed, stop eva attach mode");
                    StopPointer();
                    pointerAction(PointerAction.Cancel, Vector3.zero, Quaternion.identity, null);
                }
                if (GameSettings.Editor_toggleSymMethod.GetKeyDown())
                {
                    if (pointer) UnityEngine.Object.Destroy(pointer);
                    attachNodeIndex++;
                    if (attachNodeIndex > (attachNodes.Count - 1))
                    {
                        attachNodeIndex = 0;
                    }
                    ScreenMessage scrMsg = new ScreenMessage("Attach pointer node changed to : " + GetCurrentAttachNode().id, 5, ScreenMessageStyle.UPPER_CENTER);
                    ScreenMessages.PostScreenMessage(scrMsg, true);
                }
            }
        }

        public void UpdatePointer()
        {
            if (running && MapView.MapIsEnabled)
            {
                StopPointer();
            }

            if (!running)
            {
                if (pointer) UnityEngine.Object.Destroy(pointer);
                return;
            }

            //Cast ray
            Ray ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 500, 557059))
            {
                if (pointer) UnityEngine.Object.Destroy(pointer);
                return;
            }

            //Create pointer if needed
            if (!pointer)
            {
                GameObject modelGo = partToAttach.FindModelTransform("model").gameObject;
                pointer = Mesh.Instantiate(modelGo, new Vector3(0, 0, 100), Quaternion.identity) as GameObject;
                foreach (Collider col in pointer.GetComponentsInChildren<Collider>())
                {

                    UnityEngine.Object.DestroyImmediate(col);
                }

                allModelMr = new List<MeshRenderer>();
                // Remove attached tube mesh renderer if any
                List<MeshRenderer> tmpAllModelMr = new List<MeshRenderer>(pointer.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]);
                foreach (MeshRenderer mr in tmpAllModelMr)
                {
                    if (mr.name == "KAStube" || mr.name == "KASsrcSphere" || mr.name == "KASsrcTube" || mr.name == "KAStgtSphere" || mr.name == "KAStgtTube")
                    {
                        Destroy(mr);
                        continue;
                    }
                    allModelMr.Add(mr);
                    mr.material = new Material(Shader.Find("Transparent/Diffuse"));
                }
                pointerNodeTransform = new GameObject("KASPointerPartNode").transform;
                pointerNodeTransform.parent = pointer.transform;
            }

            //Set default color
            Color color = Color.green;

            // Check if object is valid
            bool isValidObj = false;
            Part hitPart = null;
            KerbalEVA hitEva = null;
            if (hit.rigidbody)
            {
                hitPart = hit.rigidbody.GetComponent<Part>();
                hitEva = hit.rigidbody.GetComponent<KerbalEVA>();
                if (hitPart && allowPart && !hitEva & hitPart != partToAttach)
                {
                    if (ignoreAttachRules)
                    {
                        isValidObj = true;
                    }
                    else
                    {
                        if (partToAttach.attachRules.srfAttach && hitPart.attachRules.allowSrfAttach)
                        {
                            isValidObj = true;
                        }
                    }
                }
                if (hitEva && allowEva) isValidObj = true;
            }
            if (!hitPart && !hitEva && allowStatic) isValidObj = true;

            AttachNode targetAttachNode = null;
            if (allowStackSnap)
            {
                if (hitPart)
                {
                    foreach (AttachNode an in hitPart.attachNodes)
                    {
                        if (!an.icon)
                        {
                            an.icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            Destroy(an.icon.collider);
                            an.icon.renderer.material = new Material(Shader.Find("Transparent/Diffuse"));
                            Color colorNode = XKCDColors.GrassyGreen;
                            colorNode.a = 0.5f;
                            an.icon.renderer.material.color = colorNode;
                            an.icon.transform.parent = hitPart.transform;

                            double num;
                            if (an.size == 0)
                            {
                                num = (double)an.size + 0.5;
                            }
                            else num = (double)an.size;
                            an.icon.transform.localScale = Vector3.one * an.radius * (float)num;
                            an.icon.transform.position = hitPart.transform.TransformPoint(an.position);
                            an.icon.transform.up = hitPart.transform.TransformDirection(an.orientation);
                            KIS_Shared.AddNodeTransform(hitPart, an);

                            /*
                            switch (an.nodeType)
                            {
                                case AttachNode.NodeType.Stack:
                                    an.icon.renderer.material.color = XKCDColors.GrassyGreen;
                                    return;
                                case AttachNode.NodeType.Surface:
                                    return;
                                case AttachNode.NodeType.Dock:
                                    an.icon.renderer.material.color = XKCDColors.AquaBlue;
                                    return;
                                default:
                                    return;
                            }*/
                        }

                        if (an.icon.renderer.bounds.IntersectRay(FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition)))
                        {
                            targetAttachNode = an;
                            //current3.icon.renderer.bounds.Contains(selPart.transform.TransformPoint(current2.position)))
                        }
                    }
                }
            }

            // Set pointer attach node
            if (GetCurrentAttachNode() == partToAttach.srfAttachNode || GetCurrentAttachNode().id == "top")
            {
                pointerNodeTransform.localPosition = GetCurrentAttachNode().position;
                pointerNodeTransform.localRotation = Quaternion.Inverse(Quaternion.LookRotation(GetCurrentAttachNode().orientation, Vector3.up));
            }
            else
            {
                pointerNodeTransform.localPosition = GetCurrentAttachNode().position;
                pointerNodeTransform.localRotation = Quaternion.LookRotation(GetCurrentAttachNode().orientation, Vector3.up);
            }


            float rotDegree = 15;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                rotDegree = 1;
            }

            if (GameSettings.Editor_rollLeft.GetKeyDown())
            {
                customRot -= new Vector3(0, -1, 0) * rotDegree;
            }
            if (GameSettings.Editor_rollRight.GetKeyDown())
            {
                customRot += new Vector3(0, -1, 0) * rotDegree;
            }
            if (GameSettings.Editor_pitchDown.GetKeyDown())
            {
                customRot -= new Vector3(1, 0, 0) * rotDegree;
            }
            if (GameSettings.Editor_pitchUp.GetKeyDown())
            {
                customRot += new Vector3(1, 0, 0) * rotDegree;
            }
            if (GameSettings.Editor_yawLeft.GetKeyDown())
            {
                customRot -= new Vector3(0, 0, 1) * rotDegree;
            }
            if (GameSettings.Editor_yawRight.GetKeyDown())
            {
                customRot += new Vector3(0, 0, 1) * rotDegree;
            }
            if (GameSettings.Editor_resetRotation.GetKeyDown())
            {
                customRot = new Vector3(0, 0, 0);
            }

            Quaternion rotAdjust = Quaternion.Euler(0, 0, customRot.z) * Quaternion.Euler(customRot.x, customRot.y, 0);
            if (targetAttachNode != null)
            {
                //Stack attach (snap)
                KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, targetAttachNode.nodeTransform);
                pointer.transform.rotation *= Quaternion.Euler(targetAttachNode.orientation);
            }
            else
            {
                //Surface attach
                KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hit, rotAdjust);
            }

            //Check distance
            bool isValidSourceDist = true;
            if (sourceTransform)
            {
                isValidSourceDist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, sourceTransform.position) <= maxDist;
            }
            bool isValidTargetDist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, hit.point) <= maxDist;

            //Set color
            if (!isValidObj)
            {
                color = Color.red;
            }
            else if (!isValidSourceDist || !isValidTargetDist)
            {
                color = Color.yellow;
            }
            color.a = 0.5f;
            foreach (MeshRenderer mr in allModelMr) mr.material.color = color;

            //Attach on click
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                KIS_Shared.DebugLog("Attachment started...");
                if (!isValidObj)
                {
                    ScreenMessages.PostScreenMessage("Target is not allowed !");
                    audioBipWrong.Play();
                    return;
                }

                if (!isValidSourceDist)
                {
                    ScreenMessages.PostScreenMessage("Too far from source !");
                    audioBipWrong.Play();
                    return;
                }

                if (isValidObj && isValidSourceDist && isValidTargetDist)
                {
                    pointerAction(PointerAction.ClickValid, pointer.transform.position, pointer.transform.rotation, hitPart);
                }
            }
        }

        public static AttachNode GetCurrentAttachNode()
        {
            return attachNodes[attachNodeIndex];
        }

        private static void RotatePointer(float dist)
        {
            customRot.Set(customRot.x, customRot.y, customRot.z + dist);
            //battery orientation, illuminator (0.0, 0.0, -1.0) orient nok rotate nok
            //radial cport/pipe/strut/round rcs orientation (0.0, -1.3, 0.0) orient ok rotate ok
            //telus bay / rover  orientation (1.0, 0.0, 0.0) orient ok rotate nok
            // rcs block orientation (0.1, 0.0, 0.0) orient ok rotate nok
        }

    }
}
