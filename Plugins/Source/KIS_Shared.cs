using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace KIS
{
    public class LinkedObject : MonoBehaviour
    {
        public Part part;
    }

    static public class KIS_Shared
    {
        public static bool debugLog = true;
        public static string bipWrongSndPath = "KIS/Sounds/bipwrong";
        public delegate void OnPartCoupled(Part createdPart, Part tgtPart = null, AttachNode tgtAttachNode = null);

        public enum MessageAction { DropEnd, AttachStart, AttachEnd, Store, Decouple }

        public static void SendKISMessage(Part destPart, MessageAction action, Part tgtPart = null, AttachNode tgtNode = null)
        {
            BaseEventData bEventData = new BaseEventData(BaseEventData.Sender.AUTO);
            bEventData.Set("action", action.ToString());
            bEventData.Set("targetPart", tgtPart);
            bEventData.Set("targetNode", tgtNode);
            destPart.SendMessage("OnKISAction", bEventData, SendMessageOptions.DontRequireReceiver);
        }

        public static void DebugLog(string text)
        {
            if (debugLog) Debug.Log("[KIS] " + text);
        }

        public static void DebugLog(string text, UnityEngine.Object context)
        {
            if (debugLog) Debug.Log("[KIS] " + text, context);
        }

        public static void DebugWarning(string text)
        {
            if (debugLog)
            {
                Debug.LogWarning("[KIS] " + text);
            }
        }

        public static void DebugError(string text)
        {
            if (debugLog)
            {
                Debug.LogError("[KIS] " + text);
            }
        }

        public static Part GetPartUnderCursor()
        {
            RaycastHit hit;
            Part part = null;
            Camera cam = null;
            if (HighLogic.LoadedSceneIsEditor) cam = EditorLogic.fetch.editorCamera;
            if (HighLogic.LoadedSceneIsFlight) cam = FlightCamera.fetch.mainCamera;

            if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, 1000, 557059))
            {
                //part = hit.transform.gameObject.GetComponent<Part>();
                part = (Part)UIPartActionController.GetComponentUpwards("Part", hit.collider.gameObject);
            }
            return part;
        }

        public static void PlaySoundAtPoint(string soundPath, Vector3 position)
        {
            AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(soundPath), position);
        }

        public static bool createFXSound(Part part, FXGroup group, string sndPath, bool loop, float maxDistance = 30f)
        {
            group.audio = part.gameObject.AddComponent<AudioSource>();
            group.audio.volume = GameSettings.SHIP_VOLUME;
            group.audio.rolloffMode = AudioRolloffMode.Linear;
            group.audio.dopplerLevel = 0f;
            group.audio.panLevel = 1f;
            group.audio.maxDistance = maxDistance;
            group.audio.loop = loop;
            group.audio.playOnAwake = false;
            if (GameDatabase.Instance.ExistsAudioClip(sndPath))
            {
                group.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
                return true;
            }
            else
            {
                KIS_Shared.DebugError("Sound not found in the game database !");
                ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check your KAS installation !", 10, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
        }

        public static void DecoupleFromAll(Part p)
        {
            SendKISMessage(p, MessageAction.Decouple);
            if (p.parent)
            {
                p.decouple();
                //name container if needed
                ModuleKISInventory inv = p.GetComponent<ModuleKISInventory>();
                if (inv)
                {
                    if (inv.invName != "")
                    {
                        p.vessel.vesselName = inv.part.partInfo.title + " | " + inv.invName;
                    }
                    else
                    {
                        p.vessel.vesselName = inv.part.partInfo.title;
                    }
                }
            }
            if (p.children.Count != 0)
            {
                DecoupleAllChilds(p);
            }
        }

        public static void DecoupleAllChilds(Part p)
        {
            List<Part> partList = new List<Part>();
            foreach (Part pc in p.children)
            {
                partList.Add(pc);
            }
            foreach (Part pc2 in partList)
            {
                if (pc2.parent) pc2.decouple();
            }
        }

        public static ConfigNode PartSnapshot(Part part)
        {
            ConfigNode node = new ConfigNode("PART");
            ProtoPartSnapshot snapshot = null;
            try
            {
                // Seems fine with a null vessel in 0.23 if some empty lists are allocated below
                snapshot = new ProtoPartSnapshot(part, null);
            }
            catch
            {
                // workaround for command module
                KIS_Shared.DebugWarning("Error during part snapshot, spawning part for snapshot (workaround for command module)");
                Part p = (Part)UnityEngine.Object.Instantiate(part.partInfo.partPrefab);
                p.gameObject.SetActive(true);
                p.name = part.partInfo.name;
                p.InitializeModules();
                snapshot = new ProtoPartSnapshot(p, null);
                UnityEngine.Object.Destroy(p.gameObject);
            }
            snapshot.attachNodes = new List<AttachNodeSnapshot>();
            snapshot.srfAttachNode = new AttachNodeSnapshot("attach,-1");
            snapshot.symLinks = new List<ProtoPartSnapshot>();
            snapshot.symLinkIdxs = new List<int>();
            snapshot.Save(node);

            // Prune unimportant data
            node.RemoveValues("parent");
            node.RemoveValues("position");
            node.RemoveValues("rotation");
            node.RemoveValues("istg");
            node.RemoveValues("dstg");
            node.RemoveValues("sqor");
            node.RemoveValues("sidx");
            node.RemoveValues("attm");
            node.RemoveValues("srfN");
            node.RemoveValues("attN");
            node.RemoveValues("connected");
            node.RemoveValues("attached");
            node.RemoveValues("flag");

            node.RemoveNodes("ACTIONS");

            // Remove modules that are not in prefab since they won't load anyway
            var module_nodes = node.GetNodes("MODULE");
            var prefab_modules = part.partInfo.partPrefab.GetComponents<PartModule>();
            node.RemoveNodes("MODULE");

            for (int i = 0; i < prefab_modules.Length && i < module_nodes.Length; i++)
            {
                var module = module_nodes[i];
                var name = module.GetValue("name") ?? "";

                node.AddNode(module);

                if (name == "KASModuleContainer")
                {
                    // Containers get to keep their contents
                    module.RemoveNodes("EVENTS");
                }
                else if (name.StartsWith("KASModule"))
                {
                    // Prune the state of the KAS modules completely
                    module.ClearData();
                    module.AddValue("name", name);
                    continue;
                }

                module.RemoveNodes("ACTIONS");
            }

            return node;
        }

        public static ConfigNode vesselSnapshot(Vessel vessel)
        {
            ProtoVessel snapshot = new ProtoVessel(vessel);
            ConfigNode node = new ConfigNode("VESSEL");
            snapshot.Save(node);
            return node;
        }

        public static Collider GetEvaCollider(Vessel evaVessel, string colliderName)
        {
            KerbalEVA kerbalEva = evaVessel.rootPart.gameObject.GetComponent<KerbalEVA>();
            Collider evaCollider = null;
            if (kerbalEva)
            {
                foreach (Collider col in kerbalEva.characterColliders)
                {
                    if (col.name == colliderName)
                    {
                        evaCollider = col;
                        break;
                    }
                }
            }
            return evaCollider;
        }

        public static Part CreatePart(AvailablePart avPart, Vector3 position, Quaternion rotation, Part fromPart, Part tgtPart = null, string srcAttachNodeID = null, AttachNode tgtAttachNode = null)
        {
            ConfigNode partNode = new ConfigNode();
            PartSnapshot(avPart.partPrefab).CopyTo(partNode);
            return CreatePart(partNode, position, rotation, fromPart);
        }

        public static Part CreatePart(ConfigNode partConfig, Vector3 position, Quaternion rotation, Part fromPart, Part coupleToPart = null, string srcAttachNodeID = null, AttachNode tgtAttachNode = null, OnPartCoupled onPartCoupled = null)
        {
            ConfigNode node_copy = new ConfigNode();
            partConfig.CopyTo(node_copy);
            ProtoPartSnapshot snapshot = new ProtoPartSnapshot(node_copy, null, HighLogic.CurrentGame);

            if (HighLogic.CurrentGame.flightState.ContainsFlightID(snapshot.flightID) || snapshot.flightID == 0)
            {
                snapshot.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
            }
            snapshot.parentIdx = 0;
            snapshot.position = position;
            snapshot.rotation = rotation;
            snapshot.stageIndex = 0;
            snapshot.defaultInverseStage = 0;
            snapshot.seqOverride = -1;
            snapshot.inStageIndex = -1;
            snapshot.attachMode = (int)AttachModes.SRF_ATTACH;
            snapshot.attached = true;
            snapshot.connected = true;
            snapshot.flagURL = fromPart.flagURL;

            Part newPart = snapshot.Load(fromPart.vessel, false);

            newPart.transform.position = position;
            newPart.transform.rotation = rotation;
            newPart.missionID = fromPart.missionID;

            fromPart.vessel.Parts.Add(newPart);

            newPart.physicalSignificance = Part.PhysicalSignificance.NONE;
            newPart.PromoteToPhysicalPart();
            newPart.Unpack();
            newPart.InitializeModules();

            if (coupleToPart)
            {
                newPart.rigidbody.velocity = coupleToPart.rigidbody.velocity;
                newPart.rigidbody.angularVelocity = coupleToPart.rigidbody.angularVelocity;
            }
            else
            {
                newPart.rigidbody.velocity = fromPart.rigidbody.velocity;
                newPart.rigidbody.angularVelocity = fromPart.rigidbody.angularVelocity;
            }

            newPart.decouple();

            if (coupleToPart)
            {
                newPart.StartCoroutine(WaitAndCouple(newPart, coupleToPart, srcAttachNodeID, tgtAttachNode, onPartCoupled));
            }
            else
            {
                newPart.vessel.vesselType = VesselType.Unknown;
                //name container
                ModuleKISInventory inv = newPart.GetComponent<ModuleKISInventory>();
                if (inv)
                {
                    if (inv.invName != "")
                    {
                        newPart.vessel.vesselName = inv.part.partInfo.title + " | " + inv.invName;
                    }
                    else
                    {
                        newPart.vessel.vesselName = inv.part.partInfo.title;
                    }
                }
            }
            return newPart;
        }

        private static IEnumerator WaitAndCouple(Part newPart, Part tgtPart = null, string srcAttachNodeID = null, AttachNode tgtAttachNode = null, OnPartCoupled onPartCoupled = null)
        {
            // Get relative position & rotation
            Vector3 toPartLocalPos = Vector3.zero;
            Quaternion toPartLocalRot = Quaternion.identity;
            if (tgtPart)
            {
                if (tgtAttachNode == null)
                {
                    // Local position & rotation from part
                    toPartLocalPos = tgtPart.transform.InverseTransformPoint(newPart.transform.position);
                    toPartLocalRot = Quaternion.Inverse(tgtPart.transform.rotation) * newPart.transform.rotation;
                }
                else
                {
                    // Local position & rotation from node (KAS winch connector)
                    toPartLocalPos = tgtAttachNode.nodeTransform.InverseTransformPoint(newPart.transform.position);
                    toPartLocalRot = Quaternion.Inverse(tgtAttachNode.nodeTransform.rotation) * newPart.transform.rotation;
                }
            }

            // Wait part to initialize
            while (!newPart.started && newPart.State != PartStates.DEAD)
            {
                KIS_Shared.DebugLog("CreatePart - Waiting initialization of the part...");
                if (tgtPart)
                {
                    // Part stay in position 
                    if (tgtAttachNode == null)
                    {
                        newPart.transform.position = tgtPart.transform.TransformPoint(toPartLocalPos);
                        newPart.transform.rotation = tgtPart.transform.rotation * toPartLocalRot;
                    }
                    else
                    {
                        newPart.transform.position = tgtAttachNode.nodeTransform.TransformPoint(toPartLocalPos);
                        newPart.transform.rotation = tgtAttachNode.nodeTransform.rotation * toPartLocalRot;
                    }
                }
                yield return null;
            }

            KIS_Shared.DebugLog("CreatePart - Coupling part...");
            CouplePart(newPart, tgtPart, srcAttachNodeID, tgtAttachNode);

            if (onPartCoupled != null)
            {
                onPartCoupled(newPart, tgtPart, tgtAttachNode);
            }
        }

        public static void CouplePart(Part srcPart, Part tgtPart, string srcAttachNodeID = null, AttachNode tgtAttachNode = null)
        {
            // Node links
            if (srcAttachNodeID != null)
            {
                if (srcAttachNodeID == "srfAttach")
                {
                    KIS_Shared.DebugLog("Attach type : " + srcPart.srfAttachNode.nodeType + " | ID : " + srcPart.srfAttachNode.id);
                    srcPart.attachMode = AttachModes.SRF_ATTACH;
                    srcPart.srfAttachNode.attachedPart = tgtPart;
                }
                else
                {
                    AttachNode srcAttachNode = srcPart.findAttachNode(srcAttachNodeID);
                    if (srcAttachNode != null)
                    {
                        KIS_Shared.DebugLog("Attach type : " + srcPart.srfAttachNode.nodeType + " | ID : " + srcAttachNode.id);
                        srcPart.attachMode = AttachModes.STACK;
                        srcAttachNode.attachedPart = tgtPart;
                        if (tgtAttachNode != null)
                        {
                            tgtAttachNode.attachedPart = srcPart;
                        }
                    }
                    else
                    {
                        KIS_Shared.DebugError("Source attach node not found !");
                    }
                }
            }
            else
            {
                KIS_Shared.DebugWarning("Missing source attach node !");
            }

            srcPart.Couple(tgtPart);
        }

        public static void MoveAlign(Transform source, Transform childNode, RaycastHit hit, Quaternion adjust)
        {
            Vector3 refDir = hit.transform.TransformDirection(Vector3.up);
            Quaternion rotation = Quaternion.LookRotation(hit.normal, refDir);
            source.rotation = (rotation * adjust) * childNode.localRotation;
            source.position = source.position - (childNode.position - hit.point);
        }

        public static void MoveAlign(Transform source, Transform childNode, Transform target, Quaternion adjust)
        {
            MoveAlign(source, childNode, target.position, target.rotation * adjust);
        }

        public static void MoveAlign(Transform source, Transform childNode, Transform target)
        {
            MoveAlign(source, childNode, target.position, target.rotation);
        }

        public static void MoveAlign(Transform source, Transform childNode, Vector3 targetPos, Quaternion targetRot)
        {
            source.rotation = targetRot * childNode.localRotation;
            source.position = source.position - (childNode.position - targetPos);
        }

        public static void ResetCollisionEnhancer(Part p, bool create_new = true)
        {
            if (p.collisionEnhancer)
            {
                UnityEngine.Object.DestroyImmediate(p.collisionEnhancer);
            }

            if (create_new)
            {
                p.collisionEnhancer = p.gameObject.AddComponent<CollisionEnhancer>();
            }
        }

        public static float GetPartVolume(Part partPrefab)
        {
            Bounds[] rendererBounds = PartGeometryUtil.GetRendererBounds(partPrefab);
            Vector3 boundsSize = PartGeometryUtil.MergeBounds(rendererBounds, partPrefab.transform).size;
            float volume = boundsSize.x * boundsSize.y * boundsSize.z;
            return volume * 1000;
        }

        public static ConfigNode GetBaseConfigNode(PartModule partModule)
        {
            UrlDir.UrlConfig pConfig = null;
            foreach (UrlDir.UrlConfig uc in GameDatabase.Instance.GetConfigs("PART"))
            {
                if (uc.name.Replace('_', '.') == partModule.part.partInfo.name)
                {
                    pConfig = uc;
                    break;
                }
            }
            if (pConfig != null)
            {
                foreach (ConfigNode cn in pConfig.config.GetNodes("MODULE"))
                {
                    if (cn.GetValue("name") == partModule.moduleName)
                    {
                        return cn;
                    }
                }
            }
            return null;
        }

        public static Quaternion GetNodeRotation(AttachNode attachNode)
        {
            Quaternion rotation;
            rotation = Quaternion.LookRotation(attachNode.orientation);
            return rotation;
        }

        public static void AssignAttachIcon(Part part, AttachNode node, Color iconColor, string name = null)
        {
            // Create NodeTransform if needed
            if (node.nodeTransform == null)
            {
                node.nodeTransform = new GameObject("KISNodeTransf").transform;
                node.nodeTransform.parent = part.transform;
                node.nodeTransform.localPosition = node.position;
                node.nodeTransform.localRotation = KIS_Shared.GetNodeRotation(node);
            }

            if (!node.icon)
            {
                node.icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (node.icon.collider) UnityEngine.Object.DestroyImmediate(node.icon.collider);
                if (node.icon.renderer)
                {
                    node.icon.renderer.material = new Material(Shader.Find("Transparent/Diffuse"));
                    iconColor.a = 0.5f;
                    node.icon.renderer.material.color = iconColor;
                }
                node.icon.transform.parent = part.transform;
                if (name != null) node.icon.name = name;
                double num;
                if (node.size == 0)
                {
                    num = (double)node.size + 0.5;
                }
                else num = (double)node.size;
                node.icon.transform.localScale = Vector3.one * node.radius * (float)num;
                node.icon.transform.parent = node.nodeTransform;
                node.icon.transform.localPosition = Vector3.zero;
                node.icon.transform.localRotation = Quaternion.identity;
            }
        }

        public static void EditField(string label, ref bool value, int maxLenght = 50)
        {
            value = GUILayout.Toggle(value, label);
        }

        public static Dictionary<string, string> editFields = new Dictionary<string, string>();

        public static bool EditField(string label, ref Vector3 value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label + "x")) editFields.Add(label + "x", value.x.ToString());
            if (!editFields.ContainsKey(label + "y")) editFields.Add(label + "y", value.y.ToString());
            if (!editFields.ContainsKey(label + "z")) editFields.Add(label + "z", value.z.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label + "x"] = GUILayout.TextField(editFields[label + "x"], maxLenght);
            editFields[label + "y"] = GUILayout.TextField(editFields[label + "y"], maxLenght);
            editFields[label + "z"] = GUILayout.TextField(editFields[label + "z"], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set vector"), GUILayout.Width(60f)))
            {
                Vector3 tmpVector3 = new Vector3(float.Parse(editFields[label + "x"]), float.Parse(editFields[label + "y"]), float.Parse(editFields[label + "z"]));
                value = tmpVector3;
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

        public static bool EditField(string label, ref string value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label)) editFields.Add(label, value.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set string"), GUILayout.Width(60f)))
            {
                value = editFields[label];
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

        public static bool EditField(string label, ref int value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label)) editFields.Add(label, value.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set int"), GUILayout.Width(60f)))
            {
                value = int.Parse(editFields[label]);
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

        public static bool EditField(string label, ref float value, int maxLenght = 50)
        {
            bool btnPress = false;
            if (!editFields.ContainsKey(label)) editFields.Add(label, value.ToString());
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " : " + value + "   ");
            editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
            if (GUILayout.Button(new GUIContent("Set", "Set float"), GUILayout.Width(60f)))
            {
                value = float.Parse(editFields[label]);
                btnPress = true;
            }
            GUILayout.EndHorizontal();
            return btnPress;
        }

    }
}