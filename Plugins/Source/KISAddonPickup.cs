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
                    if (!editorPaction.isGrey) KISAddonPickup.instance.OnMouseGrabPartClick(editorPaction.partInfo.partPrefab);
                }
            }
        }

        public static string grabKey = "g";
        public static string attachKey = "h";
        public static KIS_IconViewer icon;
        public static Part draggedPart;
        public static KIS_Item draggedItem;
        public static int draggedIconSize = 50;
        public static int draggedIconResolution = 64;
        public static Part movingPart;
        public static KISAddonPickup instance;
        public bool grabActive = false;
        public bool detachActive = false;
        private bool grabOk = false;
        private bool detachOk = false;
        private bool jetpackLock = false;
        private bool delayedButtonUp = false;

        public static int grabbedPartsCount;
        public static float grabbedMass;  // Tons.

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

                List<String> texts = new List<String>();
                texts.Add(keyrl + keyrr + "/" + keypd + keypu + "/" + keyyl + keyyr + " to rotate");
                texts.Add("[" + GameSettings.Editor_resetRotation.name + "] to reset orientation & position");
                texts.Add("[" + GameSettings.Editor_toggleSymMethod.name + "] to change node");
                if (value == PointerMode.Drop) texts.Add("[" + KISAddonPointer.offsetUpKey.ToUpper() + "]/[" + KISAddonPointer.offsetDownKey.ToUpper() + "] to move up/down");
                if (value == PointerMode.Drop) texts.Add("[" + attachKey.ToUpper() + "] to attach");
                texts.Add("[Escape] to cancel");

                if (value == PointerMode.Drop)
                {
                    KISAddonCursor.CursorEnable("KIS/Textures/drop", "Drop (" + KISAddonPointer.GetCurrentAttachNode().id + ")", texts);
                    KISAddonPointer.allowPart = true;
                    KISAddonPointer.allowStatic = true;
                    KISAddonPointer.allowEva = true;
                    KISAddonPointer.useAttachRules = false;
                    KISAddonPointer.allowOffset = true;
                    KISAddonPointer.colorOk = Color.green;
                }
                if (value == PointerMode.Attach)
                {
                    KISAddonCursor.CursorEnable("KIS/Textures/attachOk", "Attach (" + KISAddonPointer.GetCurrentAttachNode().id + ")", texts);
                    KISAddonPointer.allowPart = false;
                    KISAddonPointer.allowStatic = false;
                    KISAddonPointer.allowEva = false;
                    KISAddonPointer.useAttachRules = true;
                    KISAddonPointer.allowOffset = false;
                    KISAddonPointer.colorOk = XKCDColors.Teal;

                    ModuleKISItem item = null;
                    Part attachPart = null;
                    if (movingPart)
                    {
                        item = movingPart.GetComponent<ModuleKISItem>();
                        attachPart = movingPart;
                    }
                    if (draggedItem != null)
                    {
                        item = draggedItem.prefabModule;
                        attachPart = draggedItem.inventory.part;
                    }

                    if (item)
                    {
                        if (item.allowStaticAttach == 1)
                        {
                            KISAddonPointer.allowStatic = true;
                        }
                        else if (item.allowStaticAttach == 2)
                        {
                            ModuleKISPickup pickupModule = GetActivePickupNearest(attachPart, canStaticAttachOnly: true);
                            if (pickupModule)
                            {
                                KISAddonPointer.allowStatic = true;
                            }
                        }

                        if (item.allowPartAttach == 1)
                        {
                            KISAddonPointer.allowPart = true;
                        }
                        else if (item.allowPartAttach == 2)
                        {
                            ModuleKISPickup pickupModule = GetActivePickupNearest(attachPart, canPartAttachOnly: true);
                            if (pickupModule)
                            {
                                KISAddonPointer.allowPart = true;
                            }
                        }
                    }
                    else
                    {
                        ModuleKISPickup pickupModule = GetActivePickupNearest(attachPart, canPartAttachOnly: true);
                        if (pickupModule)
                        {
                            KISAddonPointer.allowPart = true;
                        }
                        KISAddonPointer.allowStatic = false;
                    }
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

        public void Update() {
            try {
                Internal_Update();
            } catch (Exception e) {
                KIS_Shared.logExceptionRepeated(e);
            }
        }
        
        private void Internal_Update()
        {
            // Check if grab key is pressed
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (Input.GetKeyDown(grabKey.ToLower()))
                {
                    EnableGrabMode();
                }
                if (Input.GetKeyUp(grabKey.ToLower()))
                {
                    DisableGrabMode();
                }
            }
            // Check if attach/detach key is pressed
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (Input.GetKeyDown(attachKey.ToLower()))
                {
                    EnableAttachMode();
                }
                if (Input.GetKeyUp(attachKey.ToLower()))
                {
                    DisableAttachMode();
                }
            }
            // Drag editor parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!UIManager.instance.DidPointerHitUI(0) && InputLockManager.IsUnlocked(ControlTypes.EDITOR_PAD_PICK_PLACE))
                    {
                        Part part = KIS_Shared.GetPartUnderCursor();
                        if (part)
                        {
                            OnMouseGrabPartClick(part);
                        }
                    }
                }
            }
            // On drag released
            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                if (draggedPart && (Input.GetMouseButtonUp(0) || delayedButtonUp))
                {
                    // In slow scenes mouse button can be pressed and released in just one frame.
                    // As a result UP event may get handled before DOWN handlers which leads to
                    // false action triggering. So, just postpone UP even by one frame when it
                    // happens in the same frame as the DOWN event.
                    if (KISAddonCursor.partClickedFrame == Time.frameCount) {
                        KIS_Shared.logTrace(
                            "Postponing mouse button up event in frame {0}", Time.frameCount);
                        delayedButtonUp = true;  // Event will be handled in the next frame.
                    } else {
                        delayedButtonUp = false;
                        OnDragReleased();
                    }
                }
            }
        }

        public void EnableGrabMode()
        {
            if (!KISAddonPointer.isRunning)
            {
                List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
                // Grab only if pickup module is present on vessel
                if (pickupModules.Count > 0)
                {
                    if (!draggedPart)
                    {
                        KISAddonCursor.StartPartDetection(OnMouseGrabPartClick, OnMouseGrabEnterPart, null, OnMouseGrabExitPart);
                        KISAddonCursor.CursorEnable("KIS/Textures/grab", "Grab", "");
                        grabActive = true;
                    }
                }
            }
        }

        public void DisableGrabMode()
        {
            if (!KISAddonPointer.isRunning)
            {
                List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
                if (pickupModules.Count > 0)
                {
                    if (!draggedPart)
                    {
                        grabActive = false;
                        KISAddonCursor.StopPartDetection();
                        KISAddonCursor.CursorDefault();
                    }
                }
            }
        }

        public void EnableAttachMode()
        {
            List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
            // Attach/detach only if pickup module is present on vessel
            if (pickupModules.Count > 0)
            {
                if (!draggedPart && !grabActive && !KISAddonPointer.isRunning)
                {
                    KISAddonCursor.StartPartDetection(OnMouseDetachPartClick, OnMouseDetachEnterPart, null, OnMouseDetachExitPart);
                    KISAddonCursor.CursorEnable("KIS/Textures/detach", "Detach", "");
                    detachActive = true;
                }
                if (KISAddonPointer.isRunning && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount)
                {
                    KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Attach;
                    KIS_Shared.PlaySoundAtPoint("KIS/Sounds/click", FlightGlobals.ActiveVessel.transform.position);
                }
            }
        }

        public void DisableAttachMode()
        {
            List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
            if (pickupModules.Count > 0)
            {
                if (!draggedPart && !grabActive && !KISAddonPointer.isRunning)
                {
                    detachActive = false;
                    KISAddonCursor.StopPartDetection();
                    KISAddonCursor.CursorDefault();
                }
                if (KISAddonPointer.isRunning && KISAddonPickup.instance.pointerMode == KISAddonPickup.PointerMode.Attach)
                {
                    KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Drop;
                    KIS_Shared.PlaySoundAtPoint("KIS/Sounds/click", FlightGlobals.ActiveVessel.transform.position);
                }
            }
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(this.OnVesselChange));
        }

        void OnVesselChange(Vessel vesselChange)
        {
            if (KISAddonPointer.isRunning) KISAddonPointer.StopPointer();
            grabActive = false;
            draggedItem = null;
            draggedPart = null;
            movingPart = null;
            KISAddonCursor.StopPartDetection();
            KISAddonCursor.CursorDefault();
        }

        void OnDragReleased()
        {
            KISAddonCursor.CursorDefault();
            if (HighLogic.LoadedSceneIsFlight)
            {
                InputLockManager.RemoveControlLock("KISpickup");
                // Re-enable jetpack mouse control (workaround as SetControlLock didn't have any effect on this)  
                KerbalEVA Keva = FlightGlobals.ActiveVessel.rootPart.GetComponent<KerbalEVA>();
                if (Keva)
                {
                    if (jetpackLock)
                    {
                        Keva.JetpackDeployed = true;
                        jetpackLock = false;
                        KIS_Shared.DebugLog("Jetpack mouse input re-enabled");
                    }
                }
            }
            if (hoverInventoryGui())
            {
                // Couroutine to let time to KISModuleInventory to catch the draggedPart
                StartCoroutine(WaitAndStopDrag());
            }
            else
            {
                ModuleKISPartDrag pDrag = null;
                if (KISAddonCursor.hoveredPart)
                {
                    if (KISAddonCursor.hoveredPart != draggedPart)
                    {
                        pDrag = KISAddonCursor.hoveredPart.GetComponent<ModuleKISPartDrag>();
                    }
                }
                if (pDrag)
                {
                    if (draggedItem != null)
                    {
                        draggedItem.DragToPart(KISAddonCursor.hoveredPart);
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
            KISAddonCursor.StopPartDetection();
        }

        void OnMouseGrabEnterPart(Part part)
        {
            if (!grabActive) return;
            grabOk = false;
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (KISAddonPointer.isRunning) return;
            if (hoverInventoryGui()) return;
            if (draggedPart == part) return;
            ModuleKISPartDrag pDrag = part.GetComponent<ModuleKISPartDrag>();
            ModuleKISPartMount parentMount = null;
            if (part.parent) parentMount = part.parent.GetComponent<ModuleKISPartMount>();
            ModuleKISItem item = part.GetComponent<ModuleKISItem>();

            // Drag part over another one if possible (ex : mount)
            if (draggedPart && pDrag)
            {
                KISAddonCursor.CursorEnable(pDrag.dragIconPath, pDrag.dragText, '(' + pDrag.dragText2 + ')');
                return;
            }

            if (draggedPart)
            {
                KISAddonCursor.CursorDisable();
                return;
            }

            // Do nothing if part is EVA
            if (part.vessel.isEVA) return;

            // Check part distance
            if (!HasActivePickupInRange(part))
            {
                KISAddonCursor.CursorEnable("KIS/Textures/tooFar", "Too far", "(Move closer to the part)");
                return;
            }

            // Check part mass.
            grabbedMass = KIS_Shared.GetAssemblyMass(part, out grabbedPartsCount);
            part.SetHighlight(true, true);  // Highlight whole hierarchy.
                
            float pickupMaxMass = GetAllPickupMaxMassInRange(part);
            if (grabbedMass > pickupMaxMass)
            {
                KISAddonCursor.CursorEnable("KIS/Textures/tooHeavy", "Too heavy", "(Bring more kerbal [" + pMass + " > " + pickupMaxMass + ")");
                return;
            }

            // Check if part can be detached and grabbed
            if (!parentMount)
            {
                if (part.children.Count > 0 || part.parent)
                {
                    //Part with a child or a parent
                    if (item)
                    {
                        if (item.allowPartAttach == 0)
                        {
                            KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Can't grab", "(This part can't be detached)");
                            return;
                        }
                        else if (item.allowPartAttach == 2)
                        {
                            ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
                            if (!pickupModule)
                            {
                                if (FlightGlobals.ActiveVessel.isEVA)
                                {
                                    KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed", "(This part can't be detached without a tool)");
                                    return;
                                }
                                else
                                {
                                    KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported", "(Detach function is not supported on this part)");
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
                        if (!pickupModule)
                        {
                            if (FlightGlobals.ActiveVessel.isEVA)
                            {
                                KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed", "(This part can't be detached without a tool)");
                                return;
                            }
                            else
                            {
                                KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported", "(Detach function is not supported on this part)");
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // Part without childs and parent
                    if (item)
                    {
                        if (item.staticAttached && item.allowStaticAttach == 2)
                        {
                            ModuleKISPickup pickupModule = GetActivePickupNearest(part, canStaticAttachOnly: true);
                            if (!pickupModule)
                            {
                                if (FlightGlobals.ActiveVessel.isEVA)
                                {
                                    KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed", "(This part can't be detached from the ground without a tool)");
                                    return;
                                }
                                else
                                {
                                    KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported", "(Detach from ground function is not supported on this part)");
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // check number of part attached
            if (part.parent)
            {
                if (part.children.Count > 0)
                {
                    KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Can't grab", "(" + (part.children.Count + 1) + " parts is attached to it)");
                    return;
                }
            }
            else
            {
                if (part.children.Count > 1)
                {
                    KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Can't grab", "(" + part.children.Count + " parts is attached to it)");
                    return;
                }
            }

            // Grab icon.
            string cursorTitle = part.parent ? "Detach & Grab" : "Grab";
            string cursorText = grabbedPartsCount == 1
                ? String.Format("({0})", part.partInfo.title)
                : String.Format(
                    "({0} with {1} attached parts)", part.partInfo.title, grabbedPartsCount - 1);
            KISAddonCursor.CursorEnable("KIS/Textures/grabOk", cursorTitle, cursorText);

            grabOk = true;
        }

        void OnMouseGrabPartClick(Part part)
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

        void OnMouseGrabExitPart(Part p)
        {
            if (grabActive)
            {
                KISAddonCursor.CursorEnable("KIS/Textures/grab", "Grab", "");
            }
            else
            {
                KISAddonCursor.CursorDefault();
            }

            p.SetHighlight(false /* active */, true /* recursive */);
            // HACK: Game will remember "recursive" setting and continue selecting the
            // hierarchy on mouse hover. Do an explicit call with recusrive=false to reset it.
            p.SetHighlight(false /* active */, false /* recursive */);

            grabOk = false;
        }

        void OnMouseDetachEnterPart(Part part)
        {
            if (!detachActive) return;
            detachOk = false;
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (KISAddonPointer.isRunning) return;
            if (hoverInventoryGui()) return;
            if (draggedPart) return;
            ModuleKISPartDrag pDrag = part.GetComponent<ModuleKISPartDrag>();
            ModuleKISItem item = part.GetComponent<ModuleKISItem>();
            ModuleKISPartMount parentMount = null;
            if (part.parent) parentMount = part.parent.GetComponent<ModuleKISPartMount>();

            // Do nothing if part is EVA
            if (part.vessel.isEVA)
            {
                return;
            }

            // Check part distance
            if (!HasActivePickupInRange(part))
            {
                KISAddonCursor.CursorEnable("KIS/Textures/tooFar", "Too far", "(Move closer to the part)");
                return;
            }


            // Check if part is static attached
            if (item)
            {
                if (item.staticAttached)
                {
                    ModuleKISPickup pickupModule = GetActivePickupNearest(part, canStaticAttachOnly: true);
                    if ((item.allowStaticAttach == 1) || (pickupModule && item.allowStaticAttach == 2))
                    {
                        part.SetHighlightColor(XKCDColors.Periwinkle);
                        part.SetHighlight(true, false);
                        KISAddonCursor.CursorEnable("KIS/Textures/detachOk", "Detach from ground", '(' + part.partInfo.title + ')');
                        detachOk = true;
                        return;
                    }
                    else
                    {
                        if (FlightGlobals.ActiveVessel.isEVA)
                        {
                            KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed", "(This part can't be detached from the ground without a tool)");
                            return;
                        }
                        else
                        {
                            KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported", "(Detach from ground function is not supported on this part)");
                            return;
                        }
                    }
                }
            }


            // Check if part can be detached
            if (!parentMount)
            {
                if (part.children.Count > 0 || part.parent)
                {
                    //Part with a child or a parent
                    if (item)
                    {
                        if (item.allowPartAttach == 0)
                        {
                            KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Can't detach", "(This part can't be detached)");
                            return;
                        }
                        else if (item.allowPartAttach == 2)
                        {
                            ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
                            if (!pickupModule)
                            {
                                if (FlightGlobals.ActiveVessel.isEVA)
                                {
                                    KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed", "(Part can't be detached without a tool)");
                                    return;
                                }
                                else
                                {
                                    KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported", "(Detach function is not supported on this part)");
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
                        if (!pickupModule)
                        {
                            if (FlightGlobals.ActiveVessel.isEVA)
                            {
                                KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed", "(Part can't be detached without a tool)");
                                return;
                            }
                            else
                            {
                                KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported", "(Detach function is not supported on this part)");
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // Part without childs and parent
                    return;
                }
            }

            // Check if part is a root
            if (!part.parent)
            {
                KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Root part", "(Cannot detach a root part)");
                return;
            }

            // Detach icon
            part.SetHighlightColor(XKCDColors.Periwinkle);
            part.SetHighlight(true, false);
            part.parent.SetHighlightColor(XKCDColors.Periwinkle);
            part.parent.SetHighlight(true, false);
            KISAddonCursor.CursorEnable("KIS/Textures/detachOk", "Detach", '(' + part.partInfo.title + ')');
            detachOk = true;
        }

        void OnMouseDetachPartClick(Part part)
        {
            if (KISAddonPointer.isRunning) return;
            if (hoverInventoryGui()) return;
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (!detachOk) return;
            if (!HasActivePickupInRange(part)) return;
            detachActive = false;
            KISAddonCursor.StopPartDetection();
            KISAddonCursor.CursorDefault();
            ModuleKISItem item = part.GetComponent<ModuleKISItem>();

            if (item)
            {
                if (item.staticAttached)
                {
                    item.GroundDetach();
                    if (item.allowPartAttach == 1)
                    {
                        ModuleKISPickup pickupModule = GetActivePickupNearest(part);
                        KIS_Shared.PlaySoundAtPoint(pickupModule.detachStaticSndPath, pickupModule.part.transform.position);
                    }
                    if (item.allowPartAttach == 2)
                    {
                        ModuleKISPickup pickupModule = GetActivePickupNearest(part, canStaticAttachOnly: true);
                        KIS_Shared.PlaySoundAtPoint(pickupModule.detachStaticSndPath, pickupModule.part.transform.position);
                    }
                    return;
                }
            }

            part.decouple();

            if (item)
            {
                if (item.allowPartAttach == 1)
                {
                    ModuleKISPickup pickupModule = GetActivePickupNearest(part);
                    KIS_Shared.PlaySoundAtPoint(pickupModule.detachPartSndPath, pickupModule.part.transform.position);
                }
                if (item.allowPartAttach == 2)
                {
                    ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
                    KIS_Shared.PlaySoundAtPoint(pickupModule.detachPartSndPath, pickupModule.part.transform.position);
                }
            }
            else
            {
                ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
                KIS_Shared.PlaySoundAtPoint(pickupModule.detachPartSndPath, pickupModule.part.transform.position);
            }
        }

        void OnMouseDetachExitPart(Part p)
        {
            if (detachActive)
            {
                KISAddonCursor.CursorEnable("KIS/Textures/detach", "Detach", "");
            }
            else
            {
                KISAddonCursor.CursorDefault();
            }
            p.SetHighlight(false, false);
            p.SetHighlightDefault();
            if (p.parent)
            {
                p.parent.SetHighlight(false, false);
                p.parent.SetHighlightDefault();
            }
            detachOk = false;
        }

        public bool HasActivePickupInRange(Part p, bool canPartAttachOnly = false, bool canStaticAttachOnly = false)
        {
            return HasActivePickupInRange(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
        }

        public bool HasActivePickupInRange(Vector3 position, bool canPartAttachOnly = false, bool canStaticAttachOnly = false)
        {
            bool nearPickupModule = false;
            List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
            foreach (ModuleKISPickup pickupModule in pickupModules)
            {
                float partDist = Vector3.Distance(pickupModule.part.transform.position, position);
                if (partDist <= pickupModule.maxDistance)
                {
                    if (canPartAttachOnly == false && canStaticAttachOnly == false)
                    {
                        nearPickupModule = true;
                    }
                    else if (canPartAttachOnly == true && pickupModule.allowPartAttach)
                    {
                        nearPickupModule = true;
                    }
                    else if (canStaticAttachOnly == true && pickupModule.allowStaticAttach)
                    {
                        nearPickupModule = true;
                    }
                }
            }
            return nearPickupModule;
        }

        public ModuleKISPickup GetActivePickupNearest(Part p, bool canPartAttachOnly = false, bool canStaticAttachOnly = false)
        {
            return GetActivePickupNearest(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
        }

        public ModuleKISPickup GetActivePickupNearest(Vector3 position, bool canPartAttachOnly = false, bool canStaticAttachOnly = false)
        {
            ModuleKISPickup nearestPModule = null;
            float nearestDistance = Mathf.Infinity;
            List<ModuleKISPickup> pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
            foreach (ModuleKISPickup pickupModule in pickupModules)
            {
                float partDist = Vector3.Distance(pickupModule.part.transform.position, position);
                if (partDist <= nearestDistance)
                {
                    if (canPartAttachOnly == false && canStaticAttachOnly == false)
                    {
                        nearestDistance = partDist;
                        nearestPModule = pickupModule;
                    }
                    else if (canPartAttachOnly == true && pickupModule.allowPartAttach)
                    {
                        nearestDistance = partDist;
                        nearestPModule = pickupModule;
                    }
                    else if (canStaticAttachOnly == true && pickupModule.allowStaticAttach)
                    {
                        nearestDistance = partDist;
                        nearestPModule = pickupModule;
                    }
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
                    maxMass += pickupModule.grabMaxMass;
                }
            }
            return maxMass;
        }

        public void Pickup(Part part)
        {
            draggedPart = part;
            draggedItem = null;
            Pickup();
        }

        public void Pickup(KIS_Item item)
        {
            draggedPart = item.availablePart.partPrefab;
            draggedItem = item;
            Pickup();
        }

        private void Pickup()
        {
            icon = new KIS_IconViewer(draggedPart, draggedIconResolution);
            KISAddonCursor.StartPartDetection();
            grabActive = false;
            KISAddonCursor.CursorDisable();
            if (HighLogic.LoadedSceneIsFlight)
            {
                InputLockManager.SetControlLock(ControlTypes.VESSEL_SWITCHING, "KISpickup");
                // Disable jetpack mouse control (workaround as SetControlLock didn't have any effect on this)  
                KerbalEVA Keva = FlightGlobals.ActiveVessel.rootPart.GetComponent<KerbalEVA>();
                if (Keva)
                {
                    if (Keva.JetpackDeployed)
                    {
                        Keva.JetpackDeployed = false;
                        jetpackLock = true;
                        KIS_Shared.DebugLog("Jetpack mouse input disabled");
                    }
                }
            }
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
                    KISAddonPointer.allowStack = pickupModule.allowPartStack;
                    KISAddonPointer.maxDist = pickupModule.maxDistance;
                    if (draggedItem != null)
                    {
                        KISAddonPointer.scale = draggedItem.GetScale();
                    }
                    else
                    {
                        KISAddonPointer.scale = 1;
                    }
                    KISAddonPointer.StartPointer(part, OnPointerAction, OnPointerState, pickupModule.transform);
                    pointerMode = PointerMode.Drop;
                }
                else
                {
                    KIS_Shared.DebugError("No active pickup nearest !");
                }
            }
            KISAddonCursor.StopPartDetection();
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
                    KISAddonCursor.CursorEnable("KIS/Textures/mount", "Mount", "(Press " + keyAnchor + " to change node, [Escape] to cancel)");
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

        private void OnPointerAction(KISAddonPointer.PointerTarget pointerTarget, Vector3 pos, Quaternion rot, Part tgtPart, string srcAttachNodeID = null, AttachNode tgtAttachNode = null)
        {
            if (pointerTarget == KISAddonPointer.PointerTarget.PartMount)
            {
                if (movingPart)
                {
                    MoveAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
                }
                else
                {
                    CreateAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
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
                        MoveAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
                    }
                    else
                    {
                        CreateAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
                    }
                    // sound
                    ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
                    if (tgtPart)
                    {
                        if (modulePickup) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.attachPartSndPath), pos);
                    }
                }
            }
            draggedItem = null;
            draggedPart = null;
            movingPart = null;
            KISAddonCursor.CursorDefault();
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
                    if (!movingPartMounted) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.detachPartSndPath), movingPart.transform.position);
                }
                AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
            }
            KIS_Shared.DecoupleFromAll(movingPart);
            movingPart.transform.position = pos;
            movingPart.transform.rotation = rot;
            KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.DropEnd, KISAddonPointer.GetCurrentAttachNode(), tgtPart);
            KISAddonPointer.StopPointer();
            movingPart = null;
        }

        private Part CreateDrop(Part tgtPart, Vector3 pos, Quaternion rot)
        {
            KIS_Shared.DebugLog("Create & drop part");
            ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
            draggedItem.StackRemove(1);
            Part newPart = KIS_Shared.CreatePart(draggedItem.partNode, pos, rot, draggedItem.inventory.part);
            KIS_Shared.SendKISMessage(newPart, KIS_Shared.MessageAction.DropEnd, KISAddonPointer.GetCurrentAttachNode(), tgtPart);
            KISAddonPointer.StopPointer();
            draggedItem = null;
            draggedPart = null;
            if (modulePickup) AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
            return newPart;
        }

        private void MoveAttach(Part tgtPart, Vector3 pos, Quaternion rot, string srcAttachNodeID = null, AttachNode tgtAttachNode = null)
        {
            KIS_Shared.DebugLog("Move part & attach");
            KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.AttachStart, KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
            KIS_Shared.DecoupleFromAll(movingPart);
            movingPart.transform.position = pos;
            movingPart.transform.rotation = rot;

            ModuleKISItem moduleItem = movingPart.GetComponent<ModuleKISItem>();
            bool useExternalPartAttach = false;
            useExternalPartAttach = moduleItem && moduleItem.useExternalPartAttach;
            if (tgtPart && !useExternalPartAttach)
            {
                KIS_Shared.CouplePart(movingPart, tgtPart, srcAttachNodeID, tgtAttachNode);
            }
            KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.AttachEnd, KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
            KISAddonPointer.StopPointer();
            movingPart = null;
            draggedItem = null;
            draggedPart = null;
        }

        private Part CreateAttach(Part tgtPart, Vector3 pos, Quaternion rot, string srcAttachNodeID = null, AttachNode tgtAttachNode = null)
        {
            KIS_Shared.DebugLog("Create part & attach");
            Part newPart;
            draggedItem.StackRemove(1);
            bool useExternalPartAttach = false;
            if (draggedItem.prefabModule) if (draggedItem.prefabModule.useExternalPartAttach) useExternalPartAttach = true;
            if (tgtPart && !useExternalPartAttach)
            {
                newPart = KIS_Shared.CreatePart(draggedItem.partNode, pos, rot, draggedItem.inventory.part, tgtPart, srcAttachNodeID, tgtAttachNode, OnPartCoupled);
            }
            else
            {
                newPart = KIS_Shared.CreatePart(draggedItem.partNode, pos, rot, draggedItem.inventory.part);
                KIS_Shared.SendKISMessage(newPart, KIS_Shared.MessageAction.AttachEnd, KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
            }
            KISAddonPointer.StopPointer();
            movingPart = null;
            draggedItem = null;
            draggedPart = null;
            return newPart;
        }

        public void OnPartCoupled(Part createdPart, Part tgtPart = null, AttachNode tgtAttachNode = null)
        {
            KIS_Shared.SendKISMessage(createdPart, KIS_Shared.MessageAction.AttachEnd, KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
        }

    }
}
