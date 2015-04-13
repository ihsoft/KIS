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

        private static bool _allowMount = false;
        public static bool allowMount
        {
            get
            {
                return _allowMount;
            }
            set
            {
                ResetMouseOver();
                _allowMount = value;
            }
        }

        private static bool _allowStack = false;
        public static bool allowStack
        {
            get
            {
                return _allowStack;
            }
            set
            {
                ResetMouseOver();
                _allowStack = value;
            }
        }

        public static Part partToAttach;
        public static float maxDist = 2f;
        public static bool useAttachRules = false;
        private static Transform sourceTransform;
        private static RaycastHit hit;

        private static bool running = false;
        public static Part hoveredPart = null;
        public static AttachNode hoveredNode = null;
        private static GameObject pointer;
        private static List<MeshRenderer> allModelMr;
        private static Vector3 customRot = new Vector3(0f, 0f, 0f);
        private static Transform pointerNodeTransform;
        private static List<AttachNode> attachNodes = new List<AttachNode>();
        private static int attachNodeIndex;

        public static PointerTarget pointerTarget = PointerTarget.Nothing;
        public enum PointerTarget { Nothing, Static, StaticRb, Part, PartNode, PartMount, KerbalEva }
        private static OnPointerClick SendPointerClick;
        public delegate void OnPointerClick(PointerTarget pTarget, Vector3 pos, Quaternion rot, Part pointerPart, AttachNode SrcAttachNode = null, AttachNode tgtAttachNode = null);

        public enum PointerState { OnMouseEnterPart, OnMouseExitPart, OnMouseEnterNode, OnMouseExitNode }
        private static OnPointerState SendPointerState;
        public delegate void OnPointerState(PointerTarget pTarget, PointerState pState, Part hoverPart, AttachNode hoverNode);

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

        public static void StartPointer(Part partToMoveAndAttach, OnPointerClick pClick, OnPointerState pState, Transform from = null)
        {
            if (!running)
            {
                KIS_Shared.DebugLog("StartPointer(pointer)");
                customRot = Vector3.zero;
                partToAttach = partToMoveAndAttach;
                sourceTransform = from;
                running = true;
                SendPointerClick = pClick;
                SendPointerState = pState;
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
            ResetMouseOver();
            InputLockManager.RemoveControlLock("KISpointer");
        }

        void Update()
        {
            UpdateHoverDetect();
            UpdatePointer();
            UpdateKey();
        }

        public void UpdateHoverDetect()
        {
            if (isRunning)
            {
                //Cast ray
                Ray ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out hit, 500, 557059))
                {
                    pointerTarget = PointerTarget.Nothing;
                    ResetMouseOver();
                    return;
                }

                // Check target type
                Part tgtPart = null;
                KerbalEVA tgtKerbalEva = null;
                AttachNode tgtAttachNode = null;
                if (hit.rigidbody)
                {
                    tgtPart = hit.rigidbody.GetComponent<Part>();
                    tgtKerbalEva = hit.rigidbody.GetComponent<KerbalEVA>();
                    if (tgtKerbalEva)
                    {
                        pointerTarget = PointerTarget.KerbalEva;
                    }
                    else
                    {
                        pointerTarget = PointerTarget.StaticRb;
                        if (tgtPart)
                        {
                            float currentDist = Mathf.Infinity;
                            foreach (AttachNode an in tgtPart.attachNodes)
                            {
                                if (an.icon)
                                {
                                    float dist;
                                    if (an.icon.renderer.bounds.IntersectRay(FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition), out dist))
                                    {
                                        if (dist < currentDist)
                                        {
                                            tgtAttachNode = an;
                                            currentDist = dist;
                                        }
                                    }
                                }
                            }
                            if (tgtAttachNode != null)
                            {
                                if (tgtAttachNode.icon.name == "KISMount")
                                {
                                    pointerTarget = PointerTarget.PartMount;
                                }
                                else
                                {
                                    pointerTarget = PointerTarget.PartNode;
                                }
                            }
                            else
                            {
                                pointerTarget = PointerTarget.Part;
                            }
                        }
                    }
                }
                else
                {
                    pointerTarget = PointerTarget.Static;
                }

                if (tgtPart)
                {
                    if (tgtAttachNode != null)
                    {
                        // OnMouseEnter node
                        if (tgtAttachNode != hoveredNode)
                        {
                            if (hoveredNode != null)
                            {
                                OnMouseExitNode(hoveredNode);
                            }
                            OnMouseEnterNode(tgtAttachNode);
                            hoveredNode = tgtAttachNode;
                        }
                    }
                    else
                    {
                        // OnMouseExit node
                        if (tgtAttachNode != hoveredNode)
                        {
                            OnMouseExitNode(hoveredNode);
                            hoveredNode = null;
                        }
                    }

                    // OnMouseEnter part
                    if (tgtPart != hoveredPart)
                    {
                        if (hoveredPart)
                        {
                            OnMouseExitPart(hoveredPart);
                        }
                        OnMouseEnterPart(tgtPart);
                        hoveredPart = tgtPart;
                    }
                }
                else
                {
                    // OnMouseExit part
                    if (tgtPart != hoveredPart)
                    {
                        OnMouseExitPart(hoveredPart);
                        hoveredPart = null;
                    }
                }

            }
        }

        static void OnMouseEnterPart(Part hoverPart)
        {
            if (allowMount)
            {
                ModuleKISPartMount pMount = hoverPart.GetComponent<ModuleKISPartMount>();
                if (pMount)
                {
                    foreach (KeyValuePair<AttachNode, List<string>> mount in pMount.GetMounts())
                    {
                        if (!mount.Key.attachedPart)
                        {
                            KIS_Shared.AssignAttachIcon(hoverPart, mount.Key, XKCDColors.Teal, "KISMount");
                        }
                    }
                }
            }
            if (allowStack && GetCurrentAttachNode().nodeType != AttachNode.NodeType.Surface)
            {
                foreach (AttachNode an in hoverPart.attachNodes)
                {
                    if (!an.attachedPart)
                    {
                        KIS_Shared.AssignAttachIcon(hoverPart, an, XKCDColors.SeaGreen);
                    }
                }
            }
            SendPointerState(pointerTarget, PointerState.OnMouseEnterPart, hoverPart, null);
        }

        static void OnMouseExitPart(Part hoverPart)
        {
            foreach (AttachNode an in hoverPart.attachNodes)
            {
                if (an.icon)
                {
                    Destroy(an.icon);
                }
            }
            SendPointerState(pointerTarget, PointerState.OnMouseExitPart, hoverPart, null);
        }

        static void OnMouseEnterNode(AttachNode hoverNode)
        {
            SendPointerState(pointerTarget, PointerState.OnMouseEnterNode, hoverNode.owner, hoverNode);
        }

        static void OnMouseExitNode(AttachNode hoverNode)
        {
            SendPointerState(pointerTarget, PointerState.OnMouseExitNode, hoverNode.owner, hoverNode);
        }

        static void ResetMouseOver()
        {
            if (hoveredPart)
            {
                OnMouseExitPart(hoveredPart);
                hoveredPart = null;
            }
            if (hoveredNode != null)
            {
                OnMouseExitNode(hoveredNode);
                hoveredNode = null;
            }
        }

        public void UpdatePointer()
        {
            // Stop pointer on map
            if (running && MapView.MapIsEnabled)
            {
                StopPointer();
            }

            // Remove pointer if not running or if the raycast do not hit anything
            if (!running || pointerTarget == PointerTarget.Nothing)
            {
                if (pointer) UnityEngine.Object.Destroy(pointer);
                return;
            }

            //Create pointer if needed
            if (!pointer)
            {
                GameObject modelGo = partToAttach.FindModelTransform("model").gameObject;
                GameObject pointerModel = Mesh.Instantiate(modelGo, new Vector3(0, 0, 100), Quaternion.identity) as GameObject;
                foreach (Collider col in pointerModel.GetComponentsInChildren<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(col);
                }

                pointer = new GameObject("KISPointer");
                pointerModel.transform.parent = pointer.transform;
                pointerModel.transform.localPosition = modelGo.transform.localPosition;
                pointerModel.transform.localRotation = modelGo.transform.localRotation;

                allModelMr = new List<MeshRenderer>();
                // Remove attached tube mesh renderer if any
                List<MeshRenderer> tmpAllModelMr = new List<MeshRenderer>(pointerModel.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]);
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
                // Set pointer attach node
                pointerNodeTransform = new GameObject("KASPointerPartNode").transform;
                pointerNodeTransform.parent = pointer.transform;
                pointerNodeTransform.localPosition = GetCurrentAttachNode().position;
                pointerNodeTransform.localRotation = KIS_Shared.GetNodeRotation(GetCurrentAttachNode());
            }

            // Custom rotation
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

            // Move to position
            if (pointerTarget == PointerTarget.PartMount)
            {
                //Mount snap
                KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hoveredNode.nodeTransform);
                pointer.transform.rotation *= Quaternion.Euler(hoveredNode.orientation);
            }
            else if (pointerTarget == PointerTarget.PartNode)
            {
                //Part node snap
                KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hoveredNode.nodeTransform, rotAdjust);
            }
            else
            {
                //Surface attach
                if (GetCurrentAttachNode().nodeType == AttachNode.NodeType.Surface)
                {
                    KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hit, rotAdjust);
                }
                else
                {
                    KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hit, rotAdjust, true);
                }
            }

            //Check distance
            bool isValidSourceDist = true;
            if (sourceTransform)
            {
                isValidSourceDist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, sourceTransform.position) <= maxDist;
            }
            bool isValidTargetDist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, hit.point) <= maxDist;

            //Set color
            Color color = Color.red;
            bool valid = true;
            bool allowedOnMount = true;
            bool isValidSurfaceAttach = true;
            switch (pointerTarget)
            {
                case PointerTarget.Static:
                    if (allowStatic) color = Color.green;
                    else valid = false;
                    break;
                case PointerTarget.StaticRb:
                    if (allowStatic) color = Color.green;
                    else valid = false;
                    break;
                case PointerTarget.KerbalEva:
                    if (allowEva) color = Color.green;
                    else valid = false;
                    break;
                case PointerTarget.Part:
                    if (allowPart)
                    {
                        if (useAttachRules)
                        {
                            if (hoveredPart.attachRules.allowSrfAttach && partToAttach.attachRules.srfAttach && GetCurrentAttachNode().nodeType == AttachNode.NodeType.Surface)
                            {
                                color = Color.green;
                            }
                            else isValidSurfaceAttach = false;
                        }
                        else
                        {
                            color = Color.green;
                        }
                    }
                    else valid = false;
                    break;
                case PointerTarget.PartMount:
                    if (allowMount)
                    {
                        ModuleKISPartMount pMount = hoveredPart.GetComponent<ModuleKISPartMount>();
                        List<string> allowedPartNames = new List<string>();
                        pMount.GetMounts().TryGetValue(hoveredNode, out allowedPartNames);
                        if (allowedPartNames.Contains(partToAttach.partInfo.name))
                        {
                            color = XKCDColors.Teal;
                        }
                        else
                        {
                            color = XKCDColors.LightOrange;
                            allowedOnMount = false;
                        }
                    }
                    break;
                case PointerTarget.PartNode:
                    if (allowStack) color = XKCDColors.SeaGreen;
                    else valid = false;
                    break;
                default:
                    break;
            }
            if (!isValidSourceDist || !isValidTargetDist)
            {
                color = Color.yellow;
            }
            color.a = 0.5f;
            foreach (MeshRenderer mr in allModelMr) mr.material.color = color;


            //On click
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (!valid)
                {
                    ScreenMessages.PostScreenMessage("Target object is not allowed !");
                    audioBipWrong.Play();
                    return;
                }
                else if (!allowedOnMount)
                {
                    ScreenMessages.PostScreenMessage("This part is not allowed on the mount !");
                    audioBipWrong.Play();
                    return;
                }
                else if (!isValidSurfaceAttach)
                {
                    ScreenMessages.PostScreenMessage("This part cannot be surface attached on this part !");
                    audioBipWrong.Play();
                    return;
                }
                else if (!isValidSourceDist)
                {
                    ScreenMessages.PostScreenMessage("Too far from source !");
                    audioBipWrong.Play();
                    return;
                }
                else if (!isValidTargetDist)
                {
                    ScreenMessages.PostScreenMessage("Too far from target !");
                    audioBipWrong.Play();
                    return;
                }
                else
                {
                    SendPointerClick(pointerTarget, pointer.transform.position, pointer.transform.rotation, hoveredPart, GetCurrentAttachNode(), hoveredNode);
                }
            }
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
                    SendPointerClick(PointerTarget.Nothing, Vector3.zero, Quaternion.identity, null, null);
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
                    ResetMouseOver();
                    ScreenMessages.PostScreenMessage(scrMsg, true);
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
