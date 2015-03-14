using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KIS
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class KISAddonPickup : MonoBehaviour
    {
        class EditorClickListener : MonoBehaviour
        {
            AvailablePart editorPart;
            void Start()
            {
                GetComponent<UIButton>().AddInputDelegate(new EZInputDelegate(OnInput));
                editorPart = GetComponent<EditorPartIcon>().partInfo;
            }

            void OnInput(ref POINTER_INFO ptr)
            {
                if (ptr.evt == POINTER_INFO.INPUT_EVENT.PRESS)
                {
                    KISAddonPickup.instance.OnEditorPartClick(editorPart.partPrefab);
                }
            }
        }

        public static string grabKey = "g";
        public static KIS_IconViewer icon;
        public static Part draggedPart;
        public static KIS_Item draggedItem;
        public static int draggedIconSize = 50;
        public static int draggedIconResolution = 64;
        public static Part movingPart;
        public static KISAddonPickup instance;
        public static bool cursorShow = false;
        public static Texture2D cursorTexture = null;
        public static string cursorText, cursorText2, cursorText3 = "";
        public Part hoveredPart = null;
        public bool grabActive = false;
        private bool grabOk = false;

        public enum PointerMode { Drop, Attach }
        private PointerMode _pointerMode = PointerMode.Drop;
        public PointerMode pointerMode
        {
            get
            {
                return this._pointerMode;
            }
            set
            {
                string keyrl = "[" + GameSettings.Editor_rollLeft.name + "]";
                string keyrr = "[" + GameSettings.Editor_rollRight.name + "]";
                string keypd = "[" + GameSettings.Editor_pitchDown.name + "]";
                string keypu = "[" + GameSettings.Editor_pitchUp.name + "]";
                string keyyl = "[" + GameSettings.Editor_yawLeft.name + "]";
                string keyyr = "[" + GameSettings.Editor_yawRight.name + "]";
                string keyRotate = keyrl + keyrr + " / " + keypd + keypu + " / " + keyyl + keyyr;
                string keyResetRot = "[" + GameSettings.Editor_resetRotation.name + "]";
                string keyAnchor = "[" + GameSettings.Editor_toggleSymMethod.name + "]";

                if (value == PointerMode.Drop)
                {
                    KIS_Shared.DebugLog("Change pointer mode to drop");
                    CursorEnable("KIS/Textures/drop", "Drop", "(Press " + keyRotate + " to rotate, " + keyResetRot + " to reset orientation", keyAnchor + " to change anchor point, [echap] to cancel)");
                    KISAddonPointer.allowPart = true;
                    KISAddonPointer.allowStatic = true;
                    KISAddonPointer.allowEva = true;
                }
                if (value == PointerMode.Attach)
                {
                    KIS_Shared.DebugLog("Change pointer mode to attach");
                    CursorEnable("KIS/Textures/attachOk", "Attach", "(Press " + keyRotate + " to rotate, " + keyResetRot + " to reset orientation", keyAnchor + " to change anchor point, [echap] to cancel)");
                    KISAddonPointer.allowPart = true;
                    KISAddonPointer.allowStatic = false;
                    KISAddonPointer.allowEva = false;
                }
                this._pointerMode = value;
            }
        }

        void Awake()
        {
            instance = this;
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorPartList.Instance.iconPrefab.gameObject.AddComponent<EditorClickListener>();
            }
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(this.OnVesselChange));
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(this.OnVesselChange));
        }

        void OnVesselChange(Vessel vesselChange)
        {
            if (KISAddonPointer.isRunning) KISAddonPointer.StopPointer();
            hoveredPart = null;
            grabActive = false;
            draggedItem = null;
            draggedPart = null;
            movingPart = null;
            //grabMode = GrabMode.Default;
            CursorDefault();
        }

        public void OnEditorPartClick(Part part)
        {
            // Editor part pickup
            if (ModuleKISInventory.OpenInventory == 0) return;
            Pickup(part);
        }

        void Update()
        {
            // Check if grab key is pressed
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (Input.GetKeyDown(grabKey.ToLower()))
                {
                    if (!KISAddonPointer.isRunning)
                    {
                        List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
                        // Grab only if pickup module is present on vessel
                        if (pickupModules.Count > 0)
                        {
                            if (!draggedPart)
                            {
                                //grabMode = GrabMode.Default;
                                CursorDefaultGrab();
                                grabActive = true;
                            }
                        }
                    }
                }
                if (Input.GetKeyUp(grabKey.ToLower()))
                {
                    if (!KISAddonPointer.isRunning)
                    {
                        List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
                        if (pickupModules.Count > 0)
                        {
                            if (!draggedPart)
                            {
                                CursorDefault();
                                hoveredPart = null;
                                grabActive = false;
                            }
                        }
                    }
                }
            }

            if ((grabActive || draggedPart) && HighLogic.LoadedSceneIsFlight)
            {
                Part part = GetPartUnderCursor();
                // OnMouseDown
                if (Input.GetMouseButtonDown(0))
                {
                    if (part)
                    {
                        OnMousePartClick(part);
                    }
                }
                // OnMouseOver   
                if (part)
                {
                    OnMouseHoverPart(part);
                }

                if (part)
                {
                    // OnMouseEnter
                    if (part != hoveredPart)
                    {
                        if (hoveredPart)
                        {
                            OnMouseExitPart(part);
                        }
                        OnMouseEnterPart(part);
                        hoveredPart = part;
                    }
                }
                else
                {
                    // OnMouseExit
                    if (part != hoveredPart)
                    {
                        OnMouseExitPart(part);
                        hoveredPart = null;
                    }
                }

            }

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                // On drag released
                if (draggedPart && Input.GetMouseButtonUp(0))
                {
                    CursorDefault();
                    if (hoverInventoryGui())
                    {
                        // Couroutine to let time to KISModuleInventory to catch the draggedPart
                        StartCoroutine(WaitAndStopDrag());
                    }
                    else
                    {
                        ModuleKISPartDrag pDrag = null;
                        if (hoveredPart)
                        {
                            pDrag = hoveredPart.GetComponent<ModuleKISPartDrag>();
                        }
                        if (pDrag)
                        {
                            if (draggedItem != null)
                            {
                                draggedItem.DragToPart(hoveredPart);
                                pDrag.OnItemDragged(draggedItem);
                            }
                            else
                            {
                                pDrag.OnPartDragged(draggedPart);
                            }
                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                if (draggedItem != null)
                                {
                                    draggedItem.Delete();
                                }
                            }
                            if (HighLogic.LoadedSceneIsFlight)
                            {
                                if (draggedItem != null)
                                {
                                    Drop(draggedItem);
                                }
                                else
                                {
                                    movingPart = draggedPart;
                                    Drop(movingPart, movingPart);
                                }
                            }
                        }
                        icon = null;
                        draggedPart = null;
                    }
                }
            }
        }

        void OnMousePartClick(Part part)
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (KISAddonPointer.isRunning) return;
            if (hoverInventoryGui()) return;
            if (grabOk && HasActivePickupInRange(part))
            {
                Pickup(part);
            }
        }

        void OnMouseHoverPart(Part p)
        {

        }

        void OnMouseEnterPart(Part part)
        {
            grabOk = false;
            if (!KISAddonPointer.isRunning && HighLogic.LoadedSceneIsFlight)
            {
                if (!HighLogic.LoadedSceneIsFlight) return;
                if (KISAddonPointer.isRunning) return;
                if (hoverInventoryGui()) return;
                if (draggedPart == part) return;
                ModuleKISPickup pickupModule = GetActivePickupNearest(part);
                ModuleKISPartDrag pDrag = part.GetComponent<ModuleKISPartDrag>();
                ModuleKISPartMount parentMount = null;
                if (part.parent) parentMount = part.parent.GetComponent<ModuleKISPartMount>();

                // Drag part over another one if possible (ex : mount)
                if (draggedPart && pDrag)
                {
                    CursorEnable(pDrag.dragIconPath, pDrag.dragText, '(' + pDrag.dragText2 + ')');
                    return;
                }

                if (draggedPart)
                {
                    CursorDisable();
                    return;
                }

                // Do nothing if part is EVA
                if (part.vessel.isEVA) return;

                // Check part distance
                if (!HasActivePickupInRange(part))
                {
                    CursorEnable("KIS/Textures/tooFar", "Too far", "(Move closer to the part");
                    return;
                }

                // Check if part can be detached from parent with a tool
                if (!pickupModule.canDetach && !parentMount && part.parent)
                {
                    CursorEnable("KIS/Textures/forbidden", "Can't grab", "(Part can't be detached without a tool");
                    return;
                }

                // Check part childrens
                if (part.children.Count > 0)
                {
                    CursorEnable("KIS/Textures/forbidden", "Can't grab", "(Part can't be grabbed because " + part.children.Count + " part(s) is attached to it");
                    return;
                }

                // Check part mass
                float pMass = (part.mass + part.GetResourceMass());
                float pickupMaxMass = GetAllPickupMaxMassInRange(part);
                if (pMass > pickupMaxMass)
                {
                    CursorEnable("KIS/Textures/tooHeavy", "Too heavy", "(Bring more kerbal [" + pMass + " > " + pickupMaxMass + ")");
                    return;
                }

                // Detach icon
                if (pickupModule)
                {
                    if (pickupModule.canDetach && !parentMount && part.parent && part.children.Count == 0)
                    {
                        float partMass = part.mass + part.GetResourceMass();
                        if (partMass > pickupModule.detachMaxMass)
                        {
                            CursorEnable("KIS/Textures/tooHeavy", "Too heavy", "(Use a better tool for this [" + partMass + " > " + pickupModule.detachMaxMass + ")");
                            return;
                        }
                        else
                        {
                            CursorEnable("KIS/Textures/attachOk", "Detach", '(' + part.partInfo.title + ')');
                            grabOk = true;
                            return;
                        }
                    }
                }

                CursorEnable("KIS/Textures/grabOk", "Grab", '(' + part.partInfo.title + ')');
                grabOk = true;

            }
        }

        void OnMouseExitPart(Part p)
        {
            grabOk = false;
            if (grabActive)
            {
                CursorDefaultGrab();
            }
            else
            {
                CursorDisable();
            }
        }

        public bool HasActivePickupInRange(Part p)
        {
            return HasActivePickupInRange(p.transform.position);
        }

        public bool HasActivePickupInRange(Vector3 position)
        {
            bool nearPickupModule = false;
            List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
            foreach (ModuleKISPickup pickupModule in pickupModules)
            {
                float partDist = Vector3.Distance(pickupModule.part.transform.position, position);
                if (partDist <= pickupModule.maxDistance)
                {
                    nearPickupModule = true;
                }
            }
            return nearPickupModule;
        }

        public ModuleKISPickup GetActivePickupNearest(Part p)
        {
            return GetActivePickupNearest(p.transform.position);
        }

        public ModuleKISPickup GetActivePickupNearest(Vector3 position)
        {
            ModuleKISPickup nearestPModule = null;
            float nearestDistance = Mathf.Infinity;
            List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
            foreach (ModuleKISPickup pickupModule in pickupModules)
            {
                float partDist = Vector3.Distance(pickupModule.part.transform.position, position);
                if (partDist <= nearestDistance)
                {
                    nearestDistance = partDist;
                    nearestPModule = pickupModule;
                }
            }
            return nearestPModule;
        }

        private float GetAllPickupMaxMassInRange(Part p)
        {
            float maxMass = 0;
            ModuleKISPickup[] allPickupModules = FindObjectsOfType(typeof(ModuleKISPickup)) as ModuleKISPickup[];
            foreach (ModuleKISPickup pickupModule in allPickupModules)
            {
                float partDist = Vector3.Distance(pickupModule.part.transform.position, p.transform.position);
                if (partDist <= pickupModule.maxDistance)
                {
                    maxMass += pickupModule.maxMass;
                }
            }
            return maxMass;
        }

        public void CursorEnable(string texturePath, string text = "", string text2 = "", string text3 = "")
        {
            cursorShow = true;
            Screen.showCursor = false;
            cursorTexture = GameDatabase.Instance.GetTexture(texturePath, false);
            cursorText = text;
            cursorText2 = text2;
            cursorText3 = text3;
        }

        public void CursorDefault()
        {
            cursorShow = false;
            Screen.showCursor = true;
        }

        public void CursorDisable()
        {
            cursorShow = false;
            Screen.showCursor = false;
        }

        public void CursorDefaultGrab()
        {
            CursorEnable("KIS/Textures/grab", "Grab", "");
        }

        private Part GetPartUnderCursor()
        {
            RaycastHit hit;
            Part part = null;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000, 557059))
            {
                //part = hit.transform.gameObject.GetComponent<Part>();
                part = (Part)UIPartActionController.GetComponentUpwards("Part", hit.collider.gameObject);
            }
            return part;
        }

        public void Pickup(Part part)
        {
            draggedPart = part;
            draggedItem = null;
            icon = new KIS_IconViewer(part, draggedIconResolution);
            hoveredPart = null;
            grabActive = false;
            CursorDisable();
        }

        public void Pickup(KIS_Item item)
        {
            draggedPart = item.availablePart.partPrefab;
            draggedItem = item;
            icon = new KIS_IconViewer(item.availablePart.partPrefab, draggedIconResolution);
            hoveredPart = null;
            grabActive = false;
            CursorDisable();
        }

        public void Drop(KIS_Item item)
        {
            draggedItem = item;
            Drop(item.availablePart.partPrefab, item.inventory.part);
        }

        public void Drop(Part part, Part fromPart)
        {
            if (!KISAddonPointer.isRunning)
            {
                ModuleKISPickup pickupModule = GetActivePickupNearest(fromPart);
                if (pickupModule)
                {
                    KISAddonPointer.StartPointer(part, OnPointerAction, true, true, true, true, pickupModule.maxDistance, pickupModule.transform);
                    pointerMode = PointerMode.Drop;
                }
                else
                {
                    KIS_Shared.DebugError("No active pickup nearest !");
                }
            }
        }

        private bool hoverInventoryGui()
        {
            // Check if hovering an inventory GUI
            ModuleKISInventory[] inventories = FindObjectsOfType(typeof(ModuleKISInventory)) as ModuleKISInventory[];
            bool hoverInventory = false;
            foreach (ModuleKISInventory inventory in inventories)
            {
                if (!inventory.showGui) continue;
                if (inventory.guiMainWindowPos.Contains(Event.current.mousePosition))
                {
                    hoverInventory = true;
                    break;
                }
            }
            return hoverInventory;
        }

        private void OnGUI()
        {
            if (draggedPart)
            {
                GUI.depth = 0;
                GUI.DrawTexture(new Rect(Event.current.mousePosition.x - (draggedIconSize / 2), Event.current.mousePosition.y - (draggedIconSize / 2), draggedIconSize, draggedIconSize), icon.texture, ScaleMode.ScaleToFit);
            }

            if (cursorShow)
            {
                GUI.DrawTexture(new Rect(Event.current.mousePosition.x - 12, Event.current.mousePosition.y - 12, 24, 24), cursorTexture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(Event.current.mousePosition.x + 16, Event.current.mousePosition.y - 10, 400, 20), cursorText);

                GUIStyle StyleComments = new GUIStyle(GUI.skin.label);
                StyleComments.fontSize = 10;
                GUI.Label(new Rect(Event.current.mousePosition.x + 16, Event.current.mousePosition.y + 5, 400, 20), cursorText2, StyleComments);
                GUI.Label(new Rect(Event.current.mousePosition.x + 16, Event.current.mousePosition.y + 20, 400, 20), cursorText3, StyleComments);
            }
        }

        private IEnumerator WaitAndStopDrag()
        {
            yield return new WaitForFixedUpdate();
            icon = null;
            draggedPart = null;
        }

        private void OnPointerAction(KISAddonPointer.PointerAction pointerAction, Vector3 pos, Quaternion rot, Part hitPart)
        {
            if (pointerAction == KISAddonPointer.PointerAction.ClickValid)
            {
                ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
                if (pointerMode == PointerMode.Drop)
                {
                    if (movingPart)
                    {
                        KIS_Shared.DebugLog("Move part");
                        if (modulePickup)
                        {
                            if (movingPart.parent)
                            {
                                bool movingPartMounted = false;
                                ModuleKISPartMount partM = movingPart.parent.GetComponent<ModuleKISPartMount>();
                                if (partM)
                                {
                                    if (partM.GetPartMounted() == movingPart)
                                    {
                                        movingPartMounted = true;
                                    }
                                }
                                if (!movingPartMounted) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.detachSndPath), movingPart.transform.position);
                            }
                            AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
                        }
                        KIS_Shared.DecoupleFromAll(movingPart);
                        movingPart.transform.position = pos;
                        movingPart.transform.rotation = rot;
                        KISAddonPointer.StopPointer();
                        movingPart = null;
                    }
                    else
                    {
                        KIS_Shared.DebugLog("Create & drop part");
                        if (draggedItem.configNode.HasNode("PART"))
                        {
                            ConfigNode partNode = draggedItem.configNode.GetNode("PART");
                            KIS_Shared.CreatePart(partNode, pos, rot, draggedItem.inventory.part);
                        }
                        else
                        {
                            KIS_Shared.CreatePart(draggedItem.availablePart, pos, rot, draggedItem.inventory.part);
                        }
                        KISAddonPointer.StopPointer();
                        draggedItem.StackRemove(1);
                        draggedItem = null;
                        draggedPart = null;
                        if (modulePickup) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
                    }
                }


                if (pointerMode == PointerMode.Attach)
                {
                    if (movingPart)
                    {
                        KIS_Shared.DebugLog("Move part & attach");
                        if (hitPart)
                        {
                            KIS_Shared.DecoupleFromAll(movingPart);
                            movingPart.transform.position = pos;
                            movingPart.transform.rotation = rot;
                            CouplePart(movingPart, hitPart);
                            KISAddonPointer.StopPointer();
                            movingPart = null;
                            draggedItem = null;
                            draggedPart = null;
                        }
                    }
                    else
                    {
                        KIS_Shared.DebugLog("Create part & attach");
                        Part newPart;
                        if (hitPart)
                        {
                            // If attaching to a part, move part away waiting initialisation for coupling
                            if (draggedItem.configNode.HasNode("PART"))
                            {
                                ConfigNode partNode = draggedItem.configNode.GetNode("PART");
                                newPart = KIS_Shared.CreatePart(partNode, pos, rot, hitPart);
                            }
                            else
                            {
                                newPart = KIS_Shared.CreatePart(draggedItem.availablePart, pos, rot, hitPart);
                            }
                            StartCoroutine(WaitAndCouple(newPart, hitPart, pos,rot));
                        }
                        else
                        {
                            newPart = KIS_Shared.CreatePart(draggedItem.configNode, pos, rot, draggedItem.inventory.part);
                        }
                        KISAddonPointer.StopPointer();
                        draggedItem.StackRemove(1);
                        movingPart = null;
                        draggedItem = null;
                        draggedPart = null;
                    }
                    if (modulePickup) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.attachSndPath), pos);
                }
            }
            draggedItem = null;
            draggedPart = null;
            movingPart = null;
            CursorDefault();
        }

        private void CouplePart(Part srcPart, Part targetPart)
        {
            // Handle special case when you start constructing with an engine as root (fuel flow temp fix, waiting KSP 1.0 for fuel flow overhaul)
            ModuleEngines targetEngine = targetPart.GetComponent<ModuleEngines>();
            ModuleEnginesFX targetEngineFx = targetPart.GetComponent<ModuleEnginesFX>();
            if ((targetEngine || targetEngineFx) && targetPart.vessel.rootPart == targetPart)
            {
                KIS_Shared.DebugWarning("Target part is a root engine, invert couple action to enable fuel flow...");
                Part tmpPart = srcPart;
                srcPart = targetPart;
                targetPart = tmpPart;
            }
            
            GameEvents.onActiveJointNeedUpdate.Fire(srcPart.vessel);
            GameEvents.onActiveJointNeedUpdate.Fire(targetPart.vessel);
            // For fuel feed
            srcPart.attachMode = AttachModes.SRF_ATTACH;
            srcPart.srfAttachNode.attachedPart = targetPart;
 
            /* doesn't work
            if (KISAddonPointer.GetCurrentAttachNode().nodeType == AttachNode.NodeType.Surface)
            {
                KIS_Shared.DebugLog("Attach type : " + KISAddonPointer.GetCurrentAttachNode().nodeType);
                srcPart.attachMode = AttachModes.SRF_ATTACH;
                KISAddonPointer.GetCurrentAttachNode().attachedPart = targetPart;
            }
            else
            {
                KIS_Shared.DebugLog("Attach type : " + KISAddonPointer.GetCurrentAttachNode().nodeType);
                srcPart.attachMode = AttachModes.STACK;
                KISAddonPointer.GetCurrentAttachNode().attachedPart = targetPart;
            }*/
            srcPart.Couple(targetPart);
            KIS_Shared.ResetCollisionEnhancer(srcPart);
            GameEvents.onVesselWasModified.Fire(targetPart.vessel);
        }

        private IEnumerator WaitAndCouple(Part partToAttach, Part toPart, Vector3 pos, Quaternion rot)
        {
            Vector3 toPartLocalPos = toPart.transform.InverseTransformPoint(pos);
            Quaternion toPartLocalRot = Quaternion.Inverse(toPart.transform.rotation) * rot;
            while (!partToAttach.rigidbody || (!partToAttach.started && partToAttach.State != PartStates.DEAD))
            {
                KIS_Shared.DebugLog("WaitAndCouple - Waiting part to initialize...");
                partToAttach.transform.position = toPart.transform.TransformPoint(toPartLocalPos);
                partToAttach.transform.rotation = toPart.transform.rotation * toPartLocalRot;
                yield return new WaitForFixedUpdate();
            }
            partToAttach.transform.position = toPart.transform.TransformPoint(toPartLocalPos);
            partToAttach.transform.rotation = toPart.transform.rotation * toPartLocalRot;
            partToAttach.rigidbody.velocity = toPart.rigidbody.velocity;
            partToAttach.rigidbody.angularVelocity = toPart.rigidbody.angularVelocity;
            CouplePart(partToAttach, toPart);
        }

    }
}
