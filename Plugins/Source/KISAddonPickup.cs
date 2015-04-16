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
            EditorPartIcon editorPaction;
            void Start()
            {
                GetComponent<UIButton>().AddInputDelegate(new EZInputDelegate(OnInput));
                editorPaction = GetComponent<EditorPartIcon>();

            }

            void OnInput(ref POINTER_INFO ptr)
            {
                if (ptr.evt == POINTER_INFO.INPUT_EVENT.PRESS)
                {
                    if (!editorPaction.isGrey) KISAddonPickup.instance.OnMousePartClick(editorPaction.partInfo.partPrefab);
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
                    CursorEnable("KIS/Textures/drop", "Drop (" + KISAddonPointer.GetCurrentAttachNode().id + ")", "(Press " + keyRotate + " to rotate, " + keyResetRot + " to reset orientation", keyAnchor + " to change node, [echap] to cancel)");
                    KISAddonPointer.allowPart = true;
                    KISAddonPointer.allowStatic = true;
                    KISAddonPointer.allowEva = true;
                    KISAddonPointer.allowPartItself = true;
                    KISAddonPointer.useAttachRules = false;
                }
                if (value == PointerMode.Attach)
                {
                    CursorEnable("KIS/Textures/attachOk", "Attach (" + KISAddonPointer.GetCurrentAttachNode().id + ")", "(Press " + keyRotate + " to rotate, " + keyResetRot + " to reset orientation", keyAnchor + " to change node, [echap] to cancel)");
                    KISAddonPointer.allowPart = true;
                    KISAddonPointer.allowStatic = false;
                    KISAddonPointer.allowEva = false;
                    KISAddonPointer.allowPartItself = false;
                    KISAddonPointer.useAttachRules = true;
                }
                this._pointerMode = value;
            }
        }

        void Awake()
        {
            instance = this;
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorPartList.Instance)
                {
                    EditorPartList.Instance.iconPrefab.gameObject.AddComponent<EditorClickListener>();
                }
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
                Part part = KIS_Shared.GetPartUnderCursor();
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
                            OnMouseExitPart(hoveredPart);
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
                        OnMouseExitPart(hoveredPart);
                        hoveredPart = null;
                    }
                }

            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!UIManager.instance.DidPointerHitUI(0) && InputLockManager.IsUnlocked(ControlTypes.EDITOR_PAD_PICK_PLACE))
                    {
                        Part part = KIS_Shared.GetPartUnderCursor();
                        if (part)
                        {
                            OnMousePartClick(part);
                        }
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
                            if (hoveredPart != draggedPart)
                            {
                                pDrag = hoveredPart.GetComponent<ModuleKISPartDrag>();
                            }
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
            if (KISAddonPointer.isRunning) return;
            if (hoverInventoryGui()) return;
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (grabOk && HasActivePickupInRange(part))
                {
                    Pickup(part);
                }
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (ModuleKISInventory.GetAllOpenInventories().Count == 0) return;
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
                    KISAddonPointer.allowPart = KISAddonPointer.allowEva = KISAddonPointer.allowMount = KISAddonPointer.allowStatic = true;
                    KISAddonPointer.allowStack = false;
                    KISAddonPointer.maxDist = pickupModule.maxDistance;
                    KISAddonPointer.StartPointer(part, OnPointerAction, OnPointerState, pickupModule.transform);
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

        private void OnPointerState(KISAddonPointer.PointerTarget pTarget, KISAddonPointer.PointerState pState, Part hoverPart, AttachNode hoverNode)
        {
            if (pState == KISAddonPointer.PointerState.OnMouseEnterNode)
            {
                if (pTarget == KISAddonPointer.PointerTarget.PartMount)
                {
                    string keyAnchor = "[" + GameSettings.Editor_toggleSymMethod.name + "]";
                    CursorEnable("KIS/Textures/mount", "Mount", "(Press " + keyAnchor + " to change node, [echap] to cancel)");
                }
                if (pTarget == KISAddonPointer.PointerTarget.PartNode)
                {
                    pointerMode = pointerMode;
                }
            }
            if (pState == KISAddonPointer.PointerState.OnMouseExitNode || pState == KISAddonPointer.PointerState.OnChangeAttachNode)
            {
                pointerMode = pointerMode;
            }
        }

        private void OnPointerAction(KISAddonPointer.PointerTarget pointerTarget, Vector3 pos, Quaternion rot, Part tgtPart, AttachNode srcAttachNode = null, AttachNode tgtAttachNode = null)
        {
            if (pointerTarget == KISAddonPointer.PointerTarget.PartMount)
            {
                if (movingPart)
                {
                    MoveAttach(tgtPart, pos, rot, srcAttachNode, tgtAttachNode);
                }
                else
                {
                    CreateAttach(tgtPart, pos, rot, srcAttachNode, tgtAttachNode);
                }
                ModuleKISPartMount pMount = tgtPart.GetComponent<ModuleKISPartMount>();
                if (pMount) pMount.sndFxStore.audio.Play();
            }

            if (pointerTarget == KISAddonPointer.PointerTarget.Part
                || pointerTarget == KISAddonPointer.PointerTarget.PartNode
                || pointerTarget == KISAddonPointer.PointerTarget.Static
                || pointerTarget == KISAddonPointer.PointerTarget.KerbalEva)
            {
                if (pointerMode == PointerMode.Drop)
                {
                    if (movingPart)
                    {
                        MoveDrop(tgtPart, pos, rot);
                    }
                    else
                    {
                        CreateDrop(tgtPart, pos, rot);
                    }
                }
                if (pointerMode == PointerMode.Attach)
                {
                    if (movingPart)
                    {
                        MoveAttach(tgtPart, pos, rot, srcAttachNode, tgtAttachNode);
                    }
                    else
                    {
                        CreateAttach(tgtPart, pos, rot, srcAttachNode, tgtAttachNode);
                    }
                    ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
                    if (modulePickup) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.attachSndPath), pos);
                }
            }
            draggedItem = null;
            draggedPart = null;
            movingPart = null;
            CursorDefault();
        }

        private void MoveDrop(Part tgtPart, Vector3 pos, Quaternion rot)
        {
            KIS_Shared.DebugLog("Move part");
            ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
            if (modulePickup)
            {
                if (movingPart.parent)
                {
                    bool movingPartMounted = false;
                    ModuleKISPartMount partM = movingPart.parent.GetComponent<ModuleKISPartMount>();
                    if (partM)
                    {
                        if (partM.PartIsMounted(movingPart))
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

        private void CreateDrop(Part tgtPart, Vector3 pos, Quaternion rot)
        {
            KIS_Shared.DebugLog("Create & drop part");
            ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
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

        private void MoveAttach(Part tgtPart, Vector3 pos, Quaternion rot, AttachNode srcAttachNode = null, AttachNode tgtAttachNode = null)
        {
            KIS_Shared.DebugLog("Move part & attach");
            if (tgtPart)
            {
                KIS_Shared.DecoupleFromAll(movingPart);
                movingPart.transform.position = pos;
                movingPart.transform.rotation = rot;
                CouplePart(movingPart, tgtPart, srcAttachNode, tgtAttachNode);
                KISAddonPointer.StopPointer();
                movingPart = null;
                draggedItem = null;
                draggedPart = null;
            }
        }

        private void CreateAttach(Part tgtPart, Vector3 pos, Quaternion rot, AttachNode srcAttachNode = null, AttachNode tgtAttachNode = null)
        {
            KIS_Shared.DebugLog("Create part & attach");
            Part newPart;
            if (tgtPart)
            {
                // If attaching to a part, move part away waiting initialisation for coupling
                if (draggedItem.configNode.HasNode("PART"))
                {
                    ConfigNode partNode = draggedItem.configNode.GetNode("PART");
                    newPart = KIS_Shared.CreatePart(partNode, pos, rot, tgtPart);
                }
                else
                {
                    newPart = KIS_Shared.CreatePart(draggedItem.availablePart, pos, rot, tgtPart);
                }
                StartCoroutine(WaitAndCouple(newPart, tgtPart, pos, rot, srcAttachNode, tgtAttachNode));
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

        private IEnumerator WaitAndCouple(Part srcPart, Part tgtPart, Vector3 pos, Quaternion rot, AttachNode srcAttachNode = null, AttachNode tgtAttachNode = null)
        {
            Vector3 toPartLocalPos = tgtPart.transform.InverseTransformPoint(pos);
            Quaternion toPartLocalRot = Quaternion.Inverse(tgtPart.transform.rotation) * rot;
            while (!srcPart.rigidbody || (!srcPart.started && srcPart.State != PartStates.DEAD) || srcPart.packed)
            {
                KIS_Shared.DebugLog("WaitAndCouple - Waiting initialization of the part...");
                srcPart.transform.position = tgtPart.transform.TransformPoint(toPartLocalPos);
                srcPart.transform.rotation = tgtPart.transform.rotation * toPartLocalRot;
                yield return new WaitForFixedUpdate();
            }
            srcPart.transform.position = tgtPart.transform.TransformPoint(toPartLocalPos);
            srcPart.transform.rotation = tgtPart.transform.rotation * toPartLocalRot;
            srcPart.rigidbody.velocity = tgtPart.rigidbody.velocity;
            srcPart.rigidbody.angularVelocity = tgtPart.rigidbody.angularVelocity;
            CouplePart(srcPart, tgtPart, srcAttachNode, tgtAttachNode);
        }

        private void CouplePart(Part srcPart, Part tgtPart, AttachNode srcAttachNode = null, AttachNode tgtAttachNode = null)
        {
            // Handle special case when you start constructing with an engine as root (fuel flow temp fix, waiting KSP 1.0 for fuel flow overhaul)
            ModuleEngines targetEngine = tgtPart.GetComponent<ModuleEngines>();
            ModuleEnginesFX targetEngineFx = tgtPart.GetComponent<ModuleEnginesFX>();
            if ((targetEngine || targetEngineFx) && tgtPart.vessel.rootPart == tgtPart)
            {
                KIS_Shared.DebugWarning("Target part is a root engine, invert couple action to enable fuel flow...");
                Part tmpPart = srcPart;
                srcPart = tgtPart;
                tgtPart = tmpPart;
            }

            GameEvents.onActiveJointNeedUpdate.Fire(srcPart.vessel);
            GameEvents.onActiveJointNeedUpdate.Fire(tgtPart.vessel);

            // For fuel feed
            srcPart.attachMode = AttachModes.SRF_ATTACH;
            srcPart.srfAttachNode.attachedPart = tgtPart;

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
            srcPart.Couple(tgtPart);
            if (tgtAttachNode != null) tgtAttachNode.attachedPart = srcPart;
            if (srcAttachNode != null) srcAttachNode.attachedPart = tgtPart;
            KIS_Shared.ResetCollisionEnhancer(srcPart);
            GameEvents.onVesselWasModified.Fire(tgtPart.vessel);
        }

    }
}
