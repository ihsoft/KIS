using KSP.UI.Screens;
using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
using KSPDev.GUIUtils;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KIS {

[PersistentFieldsFile("KIS/settings.cfg", "KISConfig")]
public class KISAddonPickup : MonoBehaviour {
  /// <summary>A helper class to handle mouse clicks in the editor.</summary>
  private class EditorClickListener : MonoBehaviour, IBeginDragHandler,
                                      IDragHandler, IEndDragHandler {
    private EditorPartIcon partIcon;
    private bool dragStarted;
    private const PointerEventData.InputButton PartDragButton = PointerEventData.InputButton.Left;
    private Part preCreatedPart;

    public virtual void OnBeginDrag(PointerEventData eventData) {
      // Start dargging for KIS or delegate event to the editor.
      if (eventData.button == PartDragButton
          && EventChecker.IsModifierCombinationPressed(editorGrabPartModifiers)) {
        dragStarted = true;
        // Don't trust the parts provided by the editor. They may have uninitialized modules. Always
        // re-create them from prefab.
        if (!preCreatedPart) {
          preCreatedPart = (Part) UnityEngine.Object.Instantiate(partIcon.partInfo.partPrefab);
          preCreatedPart.gameObject.SetActive(true);
          preCreatedPart.name = partIcon.partInfo.name;
          preCreatedPart.InitializeModules();
        }
        KISAddonPickup.instance.OnMouseGrabPartClick(preCreatedPart);
      } else {
        EditorPartList.Instance.partListScrollRect.OnBeginDrag(eventData);
      }
    }
        
    public virtual void OnDrag(PointerEventData eventData) {
      // If not KIS dragging then delegate to the editor. KIS dragging is handled in the
      // KISAddonPickup.Update() method.
      // TODO: Handle KIS parts dragging here.
      if (!dragStarted) {
        EditorPartList.Instance.partListScrollRect.OnDrag(eventData);
      }
    }
        
    public virtual void OnEndDrag(PointerEventData eventData) {
      // If not KIS dragging then delegate to the editor. KIS dragging is handled in the
      // KISAddonPickup.Update() method.
      // TODO: Handle KIS parts dropping here.
      if (!dragStarted) {
        EditorPartList.Instance.partListScrollRect.OnEndDrag(eventData);
      } else if (eventData.button == PartDragButton) {
        dragStarted = false;
      }
    }
        
    /// <summary>Registers click handlers on the editor's category icon.</summary>
    void Start() {
      // Getting components is not a cheap operation so, cache anything we can.
      partIcon = GetComponent<EditorPartIcon>();              
    }
    
    void OnDestroy() {
      if (preCreatedPart) {
        UnityEngine.Object.Destroy(preCreatedPart.gameObject);
        preCreatedPart = null;
      }
    }
  }

  const string GrabIcon = "KIS/Textures/grab";
  const string GrabOkIcon = "KIS/Textures/grabOk";
  const string ForbiddenIcon = "KIS/Textures/forbidden";
  const string TooFarIcon = "KIS/Textures/tooFar";
  const string TooHeavyIcon = "KIS/Textures/tooHeavy";
  const string NeedToolIcon = "KIS/Textures/needtool";
  const string AttachOkIcon = "KIS/Textures/attachOk";

  // Cursor status strings.
  const string ReDockOkStatus = "Re-dock";
  const string ReDockIsNotPossibleStatus = "Can't re-dock";
  const string CannotGrabStatus = "Can't grab";
  const string CannotAttachStatus = "Can't attach";
  const string TooHeavyStatus = "Too heavy";
  const string TooFarStatus = "Too far";
  const string NotSupportedStatus = "Not supported";
  const string NeedToolStatus = "Tool needed";

  // Cursor hit text strings.
  const string ReDockStatusTextFmt = "Vessel: {1}, mass {0:F3}t";
  const string ReDockIsNotPossibleText = "No docked vessel found";
  const string ReDockSelectVesselText = "Select a vessel";
  const string CannotMoveKerbonautText =
      "Kerbonauts can move themselves using jetpacks. Try to ask.";
  const string TooHeavyTextFmt = "Bring more kerbal [{0:F3}t > {1:F3}t]";
  const string TooFarText = "Move closer to the part";
  const string NeedToolToAttachText = "This part can't be attached without a tool";
  const string NeedToolToDetachText = "This part can't be detached without a tool";
  const string NeedToolToDetachFromGroundText =
      "This part can't be detached from the ground without a tool";
  const string NotSupportedText = "The function is not supported on this part";
  const string CannotAttachText = "Attach function is not supported on this part";

  [PersistentField("Editor/partGrabModifiers")]
  static KeyModifiers editorGrabPartModifiers = KeyModifiers.None;

  public static string grabKey = "g";
  public static string attachKey = "h";
  public static string redockKey = "y";
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

  /// <summary>A number of parts inthe currently grabbed assembly.</summary>
  public static int grabbedPartsCount;
  /// <summary>The total mass of the grabbed assembly. Tons.</summary>
  public static float grabbedMass;
  /// <summary>A root part of the currently grabbed assembly.</summary>
  public static Part grabbedPart;

  private static Part redockTarget;
  private static string redockVesselName;
  private static bool redockOk;

  public enum PointerMode {
    Drop,
    Attach,
    ReDock
  }
  private PointerMode _pointerMode = PointerMode.Drop;

  public enum CursorMode {
    Nothing,
    Detach,
    Grab,
    ReDock
  }
  private CursorMode cursorMode = CursorMode.Nothing;

  public enum PickupMode {
    Nothing,
    GrabFromInventory,
    Move,
    Undock
  }
  private PickupMode pickupMode = PickupMode.Nothing;

  public PointerMode pointerMode {
    get {
      return this._pointerMode;
    }
    set {
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
      if (value == PointerMode.Drop) {
        texts.Add("[" + KISAddonPointer.offsetUpKey.ToUpper() + "]/["
                  + KISAddonPointer.offsetDownKey.ToUpper() + "] to move up/down");
      }
      if (value == PointerMode.Drop) {
        texts.Add("[" + attachKey.ToUpper() + "] to attach");
      }
      texts.Add("[Escape] to cancel");

      if (value == PointerMode.Drop) {
        KISAddonCursor.CursorEnable("KIS/Textures/drop", "Drop ("
                                    + KISAddonPointer.GetCurrentAttachNode().id + ")", texts);
        KISAddonPointer.allowPart = true;
        KISAddonPointer.allowStatic = true;
        KISAddonPointer.allowEva = true;
        KISAddonPointer.allowPartItself = true;
        KISAddonPointer.useAttachRules = false;
        KISAddonPointer.allowOffset = true;
        KISAddonPointer.colorOk = Color.green;
        KISAddonPointer.allowedAttachmentParts = null;
      }
      if (value == PointerMode.Attach) {
        KISAddonCursor.CursorEnable("KIS/Textures/attachOk", "Attach ("
                                    + KISAddonPointer.GetCurrentAttachNode().id + ")", texts);
        KISAddonPointer.allowPart = false;
        KISAddonPointer.allowStatic = false;
        KISAddonPointer.allowEva = false;
        KISAddonPointer.allowPartItself = false;
        KISAddonPointer.useAttachRules = true;
        KISAddonPointer.allowOffset = false;
        KISAddonPointer.colorOk = XKCDColors.Teal;
        KISAddonPointer.allowedAttachmentParts = null;

        ModuleKISItem item = null;
        Part attachPart = null;
        if (movingPart) {
          item = movingPart.GetComponent<ModuleKISItem>();
          attachPart = movingPart;
        }
        if (draggedItem != null) {
          item = draggedItem.prefabModule;
          attachPart = draggedItem.inventory.part;
        }

        if (item) {
          if (item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways) {
            KISAddonPointer.allowStatic = true;
          } else if (item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
            ModuleKISPickup pickupModule =
                GetActivePickupNearest(attachPart, canStaticAttachOnly: true);
            if (pickupModule) {
              KISAddonPointer.allowStatic = true;
            }
          }

          if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedAlways) {
            KISAddonPointer.allowPart = true;
          } else if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
            ModuleKISPickup pickupModule =
                GetActivePickupNearest(attachPart, canPartAttachOnly: true);
            if (pickupModule) {
              KISAddonPointer.allowPart = true;
            }
          }
        } else {
          ModuleKISPickup pickupModule =
              GetActivePickupNearest(attachPart, canPartAttachOnly: true);
          if (pickupModule) {
            KISAddonPointer.allowPart = true;
          }
          KISAddonPointer.allowStatic = false;
        }
      }
      if (value == PointerMode.ReDock) {
        KISAddonCursor.CursorEnable(AttachOkIcon,
                                    String.Format("Re-docking: {0}", redockVesselName),
                                    new List<string>() { "[Escape] to cancel" });
        KISAddonPointer.allowPart = false;
        KISAddonPointer.allowStatic = false;
        KISAddonPointer.allowEva = false;
        KISAddonPointer.allowPartItself = false;
        KISAddonPointer.useAttachRules = true;
        KISAddonPointer.allowOffset = false;
        KISAddonPointer.colorOk = XKCDColors.Teal;
        KISAddonPointer.allowedAttachmentParts = GetAllowedDockPorts();
      }
      Logger.logInfo("Set pointer mode to: {0}", value);
      this._pointerMode = value;
    }
  }

  void Awake() {
    instance = this;
    if (HighLogic.LoadedSceneIsEditor) {
      if (EditorPartList.Instance) {
        var iconPrefab = EditorPartList.Instance.partPrefab.gameObject;
        if (iconPrefab.GetComponent<EditorClickListener>() == null) {
          EditorPartList.Instance.partPrefab.gameObject.AddComponent<EditorClickListener>();
        } else {
          Logger.logWarning("Skip adding click listener because it exists");
        }
      }
    }
    GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(this.OnVesselChange));
    ConfigAccessor.ReadFieldsInType(typeof(KISAddonPickup), instance: this);
  }

  public void Update() {
    // Check if action key is pressed for an EVA kerbal. 
    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel.isEVA) {
      // Check if attach/detach key is pressed
      if (Input.GetKeyDown(attachKey.ToLower())) {
        EnableAttachMode();
      }
      if (Input.GetKeyUp(attachKey.ToLower())) {
        DisableAttachMode();
      }

      // Check if grab key is pressed.
      if (Input.GetKeyDown(grabKey.ToLower())) {
        EnableGrabMode();
      }
      if (Input.GetKeyUp(grabKey.ToLower())) {
        DisableGrabMode();
      }

      // Check if re-docking key is pressed.
      if (Input.GetKeyDown(redockKey.ToLower())) {
        EnableRedockingMode();
      }
      if (Input.GetKeyUp(redockKey.ToLower())) {
        DisableRedockingMode();
      }
    }

    // Drag editor parts
    if (HighLogic.LoadedSceneIsEditor && Input.GetMouseButtonDown(0)) {
      if (InputLockManager.IsUnlocked(ControlTypes.EDITOR_PAD_PICK_PLACE)) {
        Part part = Mouse.HoveredPart;
        if (part) {
          OnMouseGrabPartClick(part);
        }
      }
    }

    // On drag released
    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) {
      if (draggedPart && (Input.GetMouseButtonUp(0) || delayedButtonUp)) {
        // In slow scenes mouse button can be pressed and released in just one frame.
        // As a result UP event may get handled before DOWN handlers which leads to
        // false action triggering. So, just postpone UP even by one frame when it
        // happens in the same frame as the DOWN event.
        if (KISAddonCursor.partClickedFrame == Time.frameCount) {
          Logger.logWarning(
            "Postponing mouse button up event in frame {0}", Time.frameCount);
          delayedButtonUp = true;  // Event will be handled in the next frame.
        } else {
          delayedButtonUp = false;
          OnDragReleased();
        }
      }
    }
  }

  public void EnableGrabMode() {
    // Skip incompatible modes.
    if (KISAddonPointer.isRunning || cursorMode != CursorMode.Nothing || draggedPart) {
      return;
    }
    // Check if pickup module is present on the active vessel.
    var pickupModules =
      FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
    if (!pickupModules.Any()) {
      return;
    }
    KISAddonCursor.StartPartDetection(OnMouseGrabPartClick, OnMouseGrabEnterPart,
                                      null, OnMouseGrabExitPart);
    KISAddonCursor.CursorEnable("KIS/Textures/grab", "Grab");
    grabActive = true;
    cursorMode = CursorMode.Grab;
  }

  public void DisableGrabMode() {
    if (cursorMode == CursorMode.Grab) {
      grabActive = false;
      cursorMode = CursorMode.Nothing;
      KISAddonCursor.StopPartDetection();
      KISAddonCursor.CursorDefault();
    }
  }

  public void EnableAttachMode() {
    // Skip incompatible modes.
    if (pointerMode == PointerMode.ReDock) {
      return;
    }
    // Check if pickup module is present on the active vessel.
    List<ModuleKISPickup> pickupModules = 
      FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
    if (cursorMode != CursorMode.Nothing || !pickupModules.Any()) {
      return;
    }
    // Entering "detach parts" mode.
    if (!KISAddonPointer.isRunning && !draggedPart) {
      KISAddonCursor.StartPartDetection(OnMouseDetachPartClick, OnMouseDetachEnterPart,
                                        null, OnMouseDetachExitPart);
      KISAddonCursor.CursorEnable("KIS/Textures/detach", "Detach");
      detachActive = true;
      cursorMode = CursorMode.Detach;
    }
    // Entering "attach moving part" mode.
    if (KISAddonPointer.isRunning
        && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount
        && pointerMode == KISAddonPickup.PointerMode.Drop) {
      if (CheckIsAttachable(grabbedPart, reportToConsole: true)) {
        KIS_UISoundPlayer.instance.PlayClick();
        pointerMode = KISAddonPickup.PointerMode.Attach;
      }
    }
  }

  public void DisableAttachMode() {
    // Skip incompatible modes.
    if (pointerMode == PointerMode.ReDock) {
      return;
    }
    // Cancelling "detach parts" mode.
    if (!KISAddonPointer.isRunning) {
      detachActive = false;
      cursorMode = CursorMode.Nothing;
      KISAddonCursor.StopPartDetection();
      KISAddonCursor.CursorDefault();
    }
    // Cancelling "attach moving part" mode.
    if (KISAddonPointer.isRunning && pointerMode == PointerMode.Attach) {
      KIS_UISoundPlayer.instance.PlayClick();
      pointerMode = KISAddonPickup.PointerMode.Drop;
    }
  }

  void OnDestroy() {
    GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(this.OnVesselChange));
  }

  void OnVesselChange(Vessel vesselChange) {
    if (KISAddonPointer.isRunning)
      KISAddonPointer.StopPointer();
    grabActive = false;
    draggedItem = null;
    draggedPart = null;
    movingPart = null;
    redockTarget = null;
    cursorMode = CursorMode.Nothing;
    KISAddonCursor.StopPartDetection();
    KISAddonCursor.CursorDefault();
  }

  void OnDragReleased() {
    KISAddonCursor.CursorDefault();
    if (HighLogic.LoadedSceneIsFlight) {
      InputLockManager.RemoveControlLock("KISpickup");
      // Re-enable jetpack mouse control (workaround as SetControlLock didn't have any effect on
      // this).
      KerbalEVA kEva = FlightGlobals.ActiveVessel.rootPart.GetComponent<KerbalEVA>();
      if (kEva && jetpackLock) {
        kEva.JetpackDeployed = true;
        jetpackLock = false;
        Logger.logInfo("Jetpack mouse input re-enabled");
      }
    }
    if (hoverInventoryGui()) {
      // Couroutine to let time to KISModuleInventory to catch the draggedPart
      StartCoroutine(WaitAndStopDrag());
    } else {
      ModuleKISPartDrag pDrag = null;
      if (KISAddonCursor.hoveredPart && KISAddonCursor.hoveredPart != draggedPart) {
        pDrag = KISAddonCursor.hoveredPart.GetComponent<ModuleKISPartDrag>();
      }
      if (pDrag) {
        if (draggedItem != null) {
          draggedItem.DragToPart(KISAddonCursor.hoveredPart);
          pDrag.OnItemDragged(draggedItem);
        } else {
          pDrag.OnPartDragged(draggedPart);
        }
      } else {
        if (HighLogic.LoadedSceneIsEditor && draggedItem != null) {
          draggedItem.Delete();
        }
        if (HighLogic.LoadedSceneIsFlight) {
          if (draggedItem != null) {
            Drop(draggedItem);
          } else {
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

  void OnMouseGrabEnterPart(Part part) {
    if (!grabActive) {
      return;
    }
    grabOk = false;
    if (!HighLogic.LoadedSceneIsFlight
        || KISAddonPointer.isRunning
        || hoverInventoryGui()
        || draggedPart == part) {
      return;
    }
          
    ModuleKISPartDrag pDrag = part.GetComponent<ModuleKISPartDrag>();

    // Drag part over another one if possible (ex : mount)
    if (draggedPart && pDrag) {
      KISAddonCursor.CursorEnable(pDrag.dragIconPath, pDrag.dragText, '(' + pDrag.dragText2 + ')');
      return;
    }

    if (draggedPart) {
      KISAddonCursor.CursorDisable();
      return;
    }

    // Do nothing if part is EVA
    if (part.vessel.isEVA) {
      return;
    }

    KIS_Shared.SetHierarchySelection(part, true /* isSelected */);
    if (!CheckCanGrab(part)) {
      return;
    }

    // Grab icon.
    string cursorTitle = part.parent ? "Detach & Grab" : "Grab";
    string cursorText = grabbedPartsCount == 1
        ? String.Format("({0})", part.partInfo.title)
        : String.Format("({0} with {1} attached parts)",
                        part.partInfo.title, grabbedPartsCount - 1);
    KISAddonCursor.CursorEnable("KIS/Textures/grabOk", cursorTitle, cursorText);

    grabOk = true;
  }

  void OnMouseGrabPartClick(Part part) {
    if (KISAddonPointer.isRunning || hoverInventoryGui()) {
      return;
    }
    if (HighLogic.LoadedSceneIsFlight) {
      if (grabOk) {
        Pickup(part);
      }
    } else if (HighLogic.LoadedSceneIsEditor) {
      if (ModuleKISInventory.GetAllOpenInventories().Any()) {
        Pickup(part);
      }
    }
  }

  void OnMouseGrabExitPart(Part p) {
    if (grabActive) {
      KISAddonCursor.CursorEnable("KIS/Textures/grab", "Grab");
    } else {
      KISAddonCursor.CursorDefault();
    }

    KIS_Shared.SetHierarchySelection(p, false /* isSelected */);
    grabOk = false;
  }

  void OnMouseDetachEnterPart(Part part) {
    if (!detachActive) {
      return;
    }
    detachOk = false;
    if (!HighLogic.LoadedSceneIsFlight
        || KISAddonPointer.isRunning
        || hoverInventoryGui()
        || draggedPart) {
      return;
    }

    // Don't separate kerbals with their parts. They have a reason to be attached.
    if (part.name == "kerbalEVA" || part.name == "kerbalEVAfemale") {
      KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Can't detach",
                                  "(This kerbanaut looks too attached to the part)");
      return;
    }

    ModuleKISPartDrag pDrag = part.GetComponent<ModuleKISPartDrag>();
    ModuleKISItem item = part.GetComponent<ModuleKISItem>();
    ModuleKISPartMount parentMount = null;
    if (part.parent) {
      parentMount = part.parent.GetComponent<ModuleKISPartMount>();
    }

    // Do nothing if part is EVA
    if (part.vessel.isEVA) {
      return;
    }

    // Check part distance
    if (!HasActivePickupInRange(part)) {
      KISAddonCursor.CursorEnable("KIS/Textures/tooFar", "Too far", "(Move closer to the part)");
      return;
    }
          
    // Check if part is static attached
    if (item) {
      if (item.staticAttached) {
        ModuleKISPickup pickupModule = GetActivePickupNearest(part, canStaticAttachOnly: true);
        if ((item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways)
            || (pickupModule
                && item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool)) {
          part.SetHighlightColor(XKCDColors.Periwinkle);
          part.SetHighlight(true, false);
          KISAddonCursor.CursorEnable("KIS/Textures/detachOk", "Detach from ground",
                                      '(' + part.partInfo.title + ')');
          detachOk = true;
        } else {
          if (FlightGlobals.ActiveVessel.isEVA) {
            KISAddonCursor.CursorEnable(
                "KIS/Textures/needtool", "Tool needed",
                "(This part can't be detached from the ground without a tool)");
          } else {
            KISAddonCursor.CursorEnable(
                "KIS/Textures/forbidden", "Not supported",
                "(Detach from ground function is not supported on this part)");
          }
        }
      }
    }

    // Check if part can be detached
    if (!parentMount) {
      if (part.children.Count > 0 || part.parent) {
        //Part with a child or a parent
        if (item) {
          if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.Disabled) {
            KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Can't detach",
                                        "(This part can't be detached)");
          } else if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
            ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
            if (!pickupModule) {
              if (FlightGlobals.ActiveVessel.isEVA) {
                KISAddonCursor.CursorEnable("KIS/Textures/needtool", "Tool needed",
                                            "(Part can't be detached without a tool)");
              } else {
                KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Not supported",
                                            "(Detach function is not supported on this part)");
              }
            }
          }
          return;
        }
        if (!CheckCanDetach(part)) {
          return;
        }
      } else {
        // Part without childs and parent
        return;
      }
    }

    // Check if part is a root
    if (!part.parent) {
      KISAddonCursor.CursorEnable("KIS/Textures/forbidden", "Root part",
                                  "(Cannot detach a root part)");
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

  void OnMouseDetachPartClick(Part part) {
    if (KISAddonPointer.isRunning
        || hoverInventoryGui()
        || !HighLogic.LoadedSceneIsFlight
        || !detachOk
        || !HasActivePickupInRange(part)) {
      return;
    }
    detachActive = false;
    KISAddonCursor.StopPartDetection();
    KISAddonCursor.CursorDefault();

    // Get actor's pickup module. Not having one is very suspicious but not a blocker. 
    var pickupModule = FlightGlobals.ActiveVessel.GetComponent<ModuleKISPickup>();
    if (!pickupModule) {
      Logger.logError("Unexpected actor executed KIS action via UI: {0}",
                      FlightGlobals.ActiveVessel);
    }

    // Deatch part and play detach sound if one available.
    ModuleKISItem item = part.GetComponent<ModuleKISItem>();
    if (item && item.staticAttached) {
      item.GroundDetach();  // Parts attached to the ground need special attention.
    } else {
      part.decouple();  // Regular parts detach via regular methods.
    }
    if (pickupModule) {
      KIS_Shared.PlaySoundAtPoint(
          pickupModule.detachStaticSndPath, pickupModule.part.transform.position);
    }
  }

  void OnMouseDetachExitPart(Part p) {
    if (detachActive) {
      KISAddonCursor.CursorEnable("KIS/Textures/detach", "Detach");
    } else {
      KISAddonCursor.CursorDefault();
    }
    p.SetHighlight(false, false);
    p.SetHighlightDefault();
    if (p.parent) {
      p.parent.SetHighlight(false, false);
      p.parent.SetHighlightDefault();
    }
    detachOk = false;
  }

  public bool HasActivePickupInRange(Part p, bool canPartAttachOnly = false,
                                     bool canStaticAttachOnly = false) {
    return HasActivePickupInRange(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
  }

  public bool HasActivePickupInRange(Vector3 position, bool canPartAttachOnly = false,
                                     bool canStaticAttachOnly = false) {
    bool nearPickupModule = false;
    var pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
    foreach (var pickupModule in pickupModules) {
      float partDist = Vector3.Distance(pickupModule.part.transform.position, position);
      if (partDist <= pickupModule.maxDistance) {
        if (canPartAttachOnly == false && canStaticAttachOnly == false) {
          nearPickupModule = true;
        } else if (canPartAttachOnly == true && pickupModule.allowPartAttach) {
          nearPickupModule = true;
        } else if (canStaticAttachOnly == true && pickupModule.allowStaticAttach) {
          nearPickupModule = true;
        }
      }
    }
    return nearPickupModule;
  }

  public ModuleKISPickup GetActivePickupNearest(Part p, bool canPartAttachOnly = false,
                                                bool canStaticAttachOnly = false) {
    return GetActivePickupNearest(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
  }

  public ModuleKISPickup GetActivePickupNearest(Vector3 position, bool canPartAttachOnly = false,
                                                bool canStaticAttachOnly = false) {
    ModuleKISPickup nearestPModule = null;
    float nearestDistance = Mathf.Infinity;
    var pickupModules = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
    foreach (var pickupModule in pickupModules) {
      float partDist = Vector3.Distance(pickupModule.part.transform.position, position);
      if (partDist <= nearestDistance) {
        if (!canPartAttachOnly && !canStaticAttachOnly) {
          nearestDistance = partDist;
          nearestPModule = pickupModule;
        } else if (canPartAttachOnly && pickupModule.allowPartAttach) {
          nearestDistance = partDist;
          nearestPModule = pickupModule;
        } else if (canStaticAttachOnly && pickupModule.allowStaticAttach) {
          nearestDistance = partDist;
          nearestPModule = pickupModule;
        }
      }
    }
    return nearestPModule;
  }

  private float GetAllPickupMaxMassInRange(Part p) {
    float maxMass = 0;
    var allPickupModules = FindObjectsOfType(typeof(ModuleKISPickup)) as ModuleKISPickup[];
    foreach (ModuleKISPickup pickupModule in allPickupModules) {
      float partDist = Vector3.Distance(pickupModule.part.transform.position, p.transform.position);
      if (partDist <= pickupModule.maxDistance) {
        maxMass += pickupModule.grabMaxMass;
      }
    }
    return maxMass;
  }

  public void Pickup(Part part) {
    KIS_Shared.SetHierarchySelection(part, false /* isSelected */);
    draggedPart = part;
    draggedItem = null;
    if (cursorMode == CursorMode.Detach) {
      Logger.logError("Deatch mode is not expected in Pickup()");
    }
    Pickup(cursorMode == CursorMode.ReDock ? PickupMode.Undock : PickupMode.Move);
  }

  public void Pickup(KIS_Item item) {
    draggedPart = item.availablePart.partPrefab;
    draggedItem = item;
    Pickup(PickupMode.GrabFromInventory);
  }

  private void Pickup(PickupMode newPickupMode) {
    Logger.logInfo("Start pickup in mode {0} from part: {1}", newPickupMode, draggedPart);
    grabbedPart = null;
    pickupMode = newPickupMode;
    cursorMode = CursorMode.Nothing;
    icon = new KIS_IconViewer(draggedPart, draggedIconResolution);
    KISAddonCursor.StartPartDetection();
    grabActive = false;
    KISAddonCursor.CursorDisable();
    if (HighLogic.LoadedSceneIsFlight) {
      InputLockManager.SetControlLock(ControlTypes.VESSEL_SWITCHING, "KISpickup");
      // Disable jetpack mouse control (workaround as SetControlLock didn't have any effect on this)  
      KerbalEVA kEva = FlightGlobals.ActiveVessel.rootPart.GetComponent<KerbalEVA>();
      if (kEva && kEva.JetpackDeployed) {
        kEva.JetpackDeployed = false;
        jetpackLock = true;
        Logger.logInfo("Jetpack mouse input disabled");
      }
    }
  }

  public void Drop(KIS_Item item) {
    draggedItem = item;
    Drop(item.availablePart.partPrefab, item.inventory.part);
  }

  public void Drop(Part part, Part fromPart) {
    grabbedPart = part;
    Logger.logInfo("End pickup of {0} from part: {1}", part, fromPart);
    if (!KISAddonPointer.isRunning) {
      ModuleKISPickup pickupModule = GetActivePickupNearest(fromPart);
      int unusedPartsCount;
      if (pickupModule && CheckMass(part, out unusedPartsCount, reportToConsole: true)) {
        KISAddonPointer.allowPart = true;
        KISAddonPointer.allowEva = true;
        KISAddonPointer.allowMount = true;
        KISAddonPointer.allowStatic = true;
        KISAddonPointer.allowStack = pickupModule.allowPartStack;
        KISAddonPointer.maxDist = pickupModule.maxDistance;
        if (draggedItem != null) {
          KISAddonPointer.scale = draggedItem.GetScale();
        } else {
          KISAddonPointer.scale = 1;
        }
        KISAddonPointer.StartPointer(part, OnPointerAction, OnPointerState, pickupModule.transform);

        pointerMode = pickupMode == PickupMode.Undock
            ? PointerMode.ReDock
            : PointerMode.Drop;
      }
    }
    KISAddonCursor.StopPartDetection();
  }

  private bool hoverInventoryGui() {
    // Check if hovering an inventory GUI
    var inventories = FindObjectsOfType(typeof(ModuleKISInventory)) as ModuleKISInventory[];
    bool hoverInventory = false;
    foreach (var inventory in inventories) {
      if (!inventory.showGui) {
        continue;
      }
      if (inventory.guiMainWindowPos.Contains(Event.current.mousePosition)) {
        hoverInventory = true;
        break;
      }
    }
    return hoverInventory;
  }

  private void OnGUI() {
    if (draggedPart) {
      GUI.depth = 0;
      GUI.DrawTexture(new Rect(Event.current.mousePosition.x - (draggedIconSize / 2),
                               Event.current.mousePosition.y - (draggedIconSize / 2),
                               draggedIconSize,
                               draggedIconSize),
                      icon.texture, ScaleMode.ScaleToFit);
    }
  }

  private IEnumerator WaitAndStopDrag() {
    yield return new WaitForFixedUpdate();
    icon = null;
    draggedPart = null;
  }

  private void OnPointerState(KISAddonPointer.PointerTarget pTarget,
                              KISAddonPointer.PointerState pState,
                              Part hoverPart, AttachNode hoverNode) {
    if (pState == KISAddonPointer.PointerState.OnMouseEnterNode) {
      if (pTarget == KISAddonPointer.PointerTarget.PartMount) {
        string keyAnchor = "[" + GameSettings.Editor_toggleSymMethod.name + "]";
        KISAddonCursor.CursorEnable("KIS/Textures/mount", "Mount",
                                    "(Press " + keyAnchor + " to change node, [Escape] to cancel)");
      }
      if (pTarget == KISAddonPointer.PointerTarget.PartNode) {
        pointerMode = pointerMode;
      }
    }
    if (pState == KISAddonPointer.PointerState.OnMouseExitNode
        || pState == KISAddonPointer.PointerState.OnChangeAttachNode) {
      pointerMode = pointerMode;
    }
  }

  private void OnPointerAction(KISAddonPointer.PointerTarget pointerTarget, Vector3 pos,
                               Quaternion rot, Part tgtPart, string srcAttachNodeID = null,
                               AttachNode tgtAttachNode = null) {
    if (pointerTarget == KISAddonPointer.PointerTarget.PartMount) {
      if (movingPart) {
        MoveAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
      } else {
        CreateAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
      }
      ModuleKISPartMount pMount = tgtPart.GetComponent<ModuleKISPartMount>();
      if (pMount) {
        pMount.sndFxStore.audio.Play();
      }
    }

    if (pointerTarget == KISAddonPointer.PointerTarget.Part
        || pointerTarget == KISAddonPointer.PointerTarget.PartNode
        || pointerTarget == KISAddonPointer.PointerTarget.Static
        || pointerTarget == KISAddonPointer.PointerTarget.KerbalEva) {
      if (pointerMode == PointerMode.Drop) {
        if (movingPart) {
          MoveDrop(tgtPart, pos, rot);
        } else {
          CreateDrop(tgtPart, pos, rot);
        }
      }
      if (pointerMode == PointerMode.Attach || pointerMode == PointerMode.ReDock) {
        if (movingPart) {
          MoveAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
        } else {
          CreateAttach(tgtPart, pos, rot, srcAttachNodeID, tgtAttachNode);
        }
        KIS_UISoundPlayer.instance.PlayToolAttach();
      }
    }
    draggedItem = null;
    draggedPart = null;
    movingPart = null;
    KISAddonCursor.CursorDefault();
  }

  private void MoveDrop(Part tgtPart, Vector3 pos, Quaternion rot) {
    Logger.logInfo("Move part");
    ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
    if (modulePickup) {
      if (movingPart.parent) {
        bool movingPartMounted = false;
        ModuleKISPartMount partM = movingPart.parent.GetComponent<ModuleKISPartMount>();
        if (partM && partM.PartIsMounted(movingPart)) {
          movingPartMounted = true;
        }
        if (!movingPartMounted) {
          AudioSource.PlayClipAtPoint(
              GameDatabase.Instance.GetAudioClip(modulePickup.detachPartSndPath),
              movingPart.transform.position);
        }
      }
      AudioSource.PlayClipAtPoint(
          GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
    }
    KIS_Shared.DecoupleAssembly(movingPart);
    movingPart.vessel.SetPosition(pos);
    movingPart.vessel.SetRotation(rot);
    KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.DropEnd,
                              KISAddonPointer.GetCurrentAttachNode(), tgtPart);
    KISAddonPointer.StopPointer();
    movingPart = null;
  }

  private Part CreateDrop(Part tgtPart, Vector3 pos, Quaternion rot) {
    Logger.logInfo("Create & drop part");
    ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
    draggedItem.StackRemove(1);
    Part newPart =
        KIS_Shared.CreatePart(draggedItem.partNode, pos, rot, draggedItem.inventory.part);
    KIS_Shared.SendKISMessage(newPart, KIS_Shared.MessageAction.DropEnd,
                              KISAddonPointer.GetCurrentAttachNode(), tgtPart);
    KISAddonPointer.StopPointer();
    draggedItem = null;
    draggedPart = null;
    if (modulePickup) {
      AudioSource.PlayClipAtPoint(
          GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
    }
    return newPart;
  }

  private void MoveAttach(Part tgtPart, Vector3 pos, Quaternion rot, string srcAttachNodeID = null,
                          AttachNode tgtAttachNode = null) {
    Logger.logInfo("Move part & attach");
    KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.AttachStart,
                              KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
    KIS_Shared.DecoupleAssembly(movingPart);
    movingPart.vessel.SetPosition(pos);
    movingPart.vessel.SetRotation(rot);
          
    ModuleKISItem moduleItem = movingPart.GetComponent<ModuleKISItem>();
    bool useExternalPartAttach = false;
    useExternalPartAttach = moduleItem && moduleItem.useExternalPartAttach;
    if (tgtPart && !useExternalPartAttach) {
      KIS_Shared.CouplePart(movingPart, tgtPart, srcAttachNodeID, tgtAttachNode);
    }
    KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.AttachEnd,
                              KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
    KISAddonPointer.StopPointer();
    movingPart = null;
    draggedItem = null;
    draggedPart = null;
  }

  private Part CreateAttach(Part tgtPart, Vector3 pos, Quaternion rot,
                            string srcAttachNodeID = null, AttachNode tgtAttachNode = null) {
    Logger.logInfo("Create part & attach");
    Part newPart;
    draggedItem.StackRemove(1);
    bool useExternalPartAttach = false;
    if (draggedItem.prefabModule && draggedItem.prefabModule.useExternalPartAttach) {
      useExternalPartAttach = true;
    }
    if (tgtPart && !useExternalPartAttach) {
      newPart = KIS_Shared.CreatePart(draggedItem.partNode, pos, rot, draggedItem.inventory.part,
                                      tgtPart, srcAttachNodeID, tgtAttachNode, OnPartCoupled);
    } else {
      newPart = KIS_Shared.CreatePart(draggedItem.partNode, pos, rot, draggedItem.inventory.part);
      KIS_Shared.SendKISMessage(newPart, KIS_Shared.MessageAction.AttachEnd,
                                KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
    }
    KISAddonPointer.StopPointer();
    movingPart = null;
    draggedItem = null;
    draggedPart = null;
    return newPart;
  }

  public void OnPartCoupled(Part createdPart, Part tgtPart = null,
                            AttachNode tgtAttachNode = null) {
    KIS_Shared.SendKISMessage(createdPart, KIS_Shared.MessageAction.AttachEnd,
                              KISAddonPointer.GetCurrentAttachNode(), tgtPart, tgtAttachNode);
  }
      
  /// <summary>Enables mode that allows re-docking a vessel attached to a station.</summary>
  private void EnableRedockingMode() {
    if (KISAddonPointer.isRunning || cursorMode != CursorMode.Nothing) {
      return;
    }
    var pickupModules =
        FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
    if (pickupModules.Count > 0) {
      Logger.logInfo("Enable re-dock mode");
      KISAddonCursor.StartPartDetection(OnMouseRedockPartClick, OnMouseRedockEnterPart,
                                        null, OnMouseRedockExitPart);
      cursorMode = CursorMode.ReDock;
    }
  }

  /// <summary>Disables re-docking mode.</summary>
  private void DisableRedockingMode() {
    if (cursorMode == CursorMode.ReDock) {
      Logger.logInfo("Disable re-dock mode");
      if (redockTarget) {
        KIS_Shared.SetHierarchySelection(redockTarget, false /* isSelected */);
      }
      cursorMode = CursorMode.Nothing;
      KISAddonCursor.StopPartDetection();
      KISAddonCursor.CursorDefault();
    }
  }

  /// <summary>Deducts and selects a vessel form the hovered part.</summary>
  /// <remarks>The method goes up to the first parent docking port that is connected to a port
  /// of the same type. This point is considered a docking point, and from here detachment is
  /// started.</remarks>
  /// <param name="part">A child part to start scanning from.</param>
  private void OnMouseRedockEnterPart(Part part) {
    // Abort on an async state change.
    if (!HighLogic.LoadedSceneIsFlight || hoverInventoryGui() || cursorMode != CursorMode.ReDock) {
      return;
    }

    redockOk = false;
    redockTarget = null;
    redockVesselName = null;

    // Find vessel's docking port.
    for (var chkPart = part; chkPart; chkPart = chkPart.parent) {
      // Only consider a docking port that is connected to the same type docking port, and
      // has a vessel attached.
      var dockingModule = chkPart.GetComponent<ModuleDockingNode>();
      if (dockingModule && chkPart.parent && chkPart.parent.name == chkPart.name
          && dockingModule.vesselInfo != null) {
        redockTarget = chkPart;
        redockVesselName = dockingModule.vesselInfo.name;
        break;
      }
    }
    if (!redockTarget) {
      KISAddonCursor.CursorEnable(
          ForbiddenIcon, ReDockIsNotPossibleStatus, ReDockIsNotPossibleText);
      return;
    }
    KIS_Shared.SetHierarchySelection(redockTarget, true /* isSelected */);

    if (!CheckCanGrab(redockTarget) || !CheckCanDetach(redockTarget) ||
        !CheckIsAttachable(redockTarget)) {
      return;
    }

    // Re-docking is allowed.
    string cursorText = String.Format(ReDockStatusTextFmt, grabbedMass, redockVesselName);
    KISAddonCursor.CursorEnable(GrabOkIcon, ReDockOkStatus, cursorText);
    redockOk = true;
  }

  /// <summary>Grabs re-docking vessel and starts movement.</summary>
  /// <param name="part">Not used.</param>
  private void OnMouseRedockPartClick(Part part) {
    if (redockOk) {
      Pickup(redockTarget);
    }
  }
      
  /// <summary>Erases re-docking vessel selection.</summary>
  private void OnMouseRedockExitPart(Part unusedPart) {
    if (cursorMode != CursorMode.ReDock) {
      return;
    }
    redockOk = false;
    redockVesselName = null;
    if (redockTarget) {
      KIS_Shared.SetHierarchySelection(redockTarget, false /* isSelected */);
      redockTarget = null;
    }
    KISAddonCursor.CursorEnable(GrabIcon, ReDockOkStatus, ReDockSelectVesselText);
  }
  
  /// <summary>
  /// Checks if the part and its children can be grabbed and reports the errors.
  /// </summary>
  /// <remarks>Also, collects <seealso cref="grabbedMass"/> and
  /// <seealso cref="grabbedPartsCount"/> of the attempted hierarchy.</remarks>
  /// <param name="part">A hierarchy root being grabbed.</param>
  /// <returns><c>true</c> when the hierarchy can be grabbed.</returns>
  private bool CheckCanGrab(Part part) {
    // Don't grab kerbals. It's weird, and they don't have attachment nodes anyways.
    if (part.name == "kerbalEVA" || part.name == "kerbalEVAfemale") {
      ReportCheckError(CannotGrabStatus, CannotMoveKerbonautText);
      return false;
    }
    // Check there are kerbals in range.
    if (!HasActivePickupInRange(part)) {
      ReportCheckError(TooFarStatus, TooFarText, cursorIcon: TooFarIcon);
      return false;
    }
    // Check if attached part has acceptable mass and can be detached.
    return CheckMass(part, out grabbedPartsCount) && CheckCanDetach(part);
  }

  /// <summary>Calculates grabbed part/assembly mass and reports if it's too heavy.</summary>
  /// <param name="part">A part or assembly root to check mass for.</param>
  /// <param name="grabbedPartsCount">A return parameter to give number of parts in the assembly.
  /// </param>
  /// <param name="reportToConsole">If <c>true</c> then error is only reported on the screen (it's a
  /// game's "console"). Otherwise, excess of mass only results in changing cursor icon to
  /// <seealso cref="TooHeavyIcon"/>.</param>
  /// <returns><c>true</c> if total mass is within the limits.</returns>
  private bool CheckMass(Part part, out int grabbedPartsCount, bool reportToConsole = false) {
    grabbedMass = KIS_Shared.GetAssemblyMass(part, out grabbedPartsCount);
    float pickupMaxMass = GetAllPickupMaxMassInRange(part);
    if (grabbedMass > pickupMaxMass) {
      ReportCheckError(TooHeavyStatus,
                       String.Format(TooHeavyTextFmt, grabbedMass, pickupMaxMass),
                       cursorIcon: TooHeavyIcon,
                       reportToConsole: reportToConsole);
      return false;
    }
    return true;
  }

  /// <summary>Checks if an attached part can be detached and reports the errors.</summary>
  /// <remarks>If part has a parent it's attached. In order to detach the part there should be
  /// a kerbonaut in range with a tool equipped.</remarks>
  /// <param name="part">A hierarchy root being detached.</param>
  /// <returns><c>true</c> when the hierarchy can be detached.</returns>
  private bool CheckCanDetach(Part part) {
    // If part is attached then check if the right tool is equipped to detach it.
    if (!part.parent) {
      return true;  // No parent, no detach needed.
    }
          
    var item = part.GetComponent<ModuleKISItem>();
    string rejectText = null;  // If null then detach is allowed.
    if (item) {
      // Handle KIS items.
      if (part.parent.GetComponent<ModuleKISPartMount>() != null) {
        // Check if part is a ground base.
        if (item.staticAttached
            && item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool
            && !GetActivePickupNearest(part, canStaticAttachOnly: true)) {
          rejectText = NeedToolToDetachFromGroundText;
        }
      } else {
        // Check specific KIS items
        if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.Disabled) {
          // Part restricts attachments and detachments.
          //FIXME: Findout what part cannot be detached. And why.
          Logger.logError("Unknown item being detached: {0}", item);
          ReportCheckError("Can't detach", "(This part can't be detached)");
          return false;
        }
        if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
          // Part requires a tool to be detached.
          if (!GetActivePickupNearest(part, canPartAttachOnly: true)) {
            rejectText = NeedToolToDetachText;
          }
        }
      }
    } else {
      // Handle regular game parts.
      if (!GetActivePickupNearest(part, canPartAttachOnly: true)) {
        rejectText = NeedToolToDetachText;
      }
    }
    if (rejectText != null) {
      ReportCheckError(NeedToolStatus, rejectText, cursorIcon: NeedToolIcon);
      return false;
    }
          
    return true;
  }

  /// <summary>Checks if part can be attached. At least in theory.</summary>
  /// <remarks>This method doesn't say if part *will* be attached if such attempt is made.</remarks>   
  private bool CheckIsAttachable(Part part, bool reportToConsole = false) {
    var item = part.GetComponent<ModuleKISItem>();

    // Check if part has at least one free node.
    var nodes = KIS_Shared.GetAvailableAttachNodes(part, part.parent);
    if (!nodes.Any()) {
      // Check if it's a static attachable item. Those are not required to have nodes
      // since they attach to the ground.
      if (!item || item.allowStaticAttach == ModuleKISItem.ItemAttachMode.Disabled) {
        ReportCheckError(CannotAttachStatus, CannotAttachText, reportToConsole);
        return false;
      }
    }

    // Check if KISItem part is allowed for attach without a tool.
    if (item && (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedAlways
                 || item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways)) {
      return true;
    }

    // Check if there is a kerbonaut with a tool to handle the task.
    ModuleKISPickup pickupModule = GetActivePickupNearest(part, canPartAttachOnly: true);
    if (!pickupModule) {
      // Check if it's EVA engineer or a KAS item.
      if (FlightGlobals.ActiveVessel.isEVA) {
        ReportCheckError(NeedToolStatus, NeedToolToAttachText, reportToConsole, NeedToolIcon);
      } else {
        ReportCheckError(NotSupportedStatus, NotSupportedText, reportToConsole);
      }
      return false;
    }

    return true;
  }

  /// <summary>
  /// Finds and returns all docking ports that are allowed for the re-docking operation.
  /// </summary>
  /// <remarks>
  /// Re-docking must be started and <seealso cref="redockTarget"/> populated with the vessel
  /// root part.
  /// </remarks>
  /// <returns>A complete set of allowed docking ports.</returns>
  private static HashSet<Part> GetAllowedDockPorts() {
    var result = new HashSet<Part>();
    var compatiblePorts = redockTarget.vessel.parts.FindAll(p => p.name == redockTarget.name);
    foreach (var port in compatiblePorts) {
      if (KIS_Shared.IsSameHierarchyChild(redockTarget, port)) {
        // Skip ports of the moving vessel.
        continue;
      }

      var usedNodes = 0;
      if (port.attachRules.srfAttach && port.srfAttachNode.attachedPart != null) {
        ++usedNodes;
      }
      var nodesWithParts = port.attachNodes.FindAll(p => p.attachedPart != null);
      usedNodes += nodesWithParts.Count();
      if (usedNodes < 2) {
        // Usual port has three nodes: one surface and two stacks. When any two of them
        // are occupied the docking is not possible.
        result.Add(port);
      }
    }
    Logger.logInfo("Found {0} allowed docking ports", result.Count());
    return result;
  }

  /// <summary>
  /// Reports an action error either via cursor state or as a right screen message.
  /// </summary>
  /// <param name="error">A short error status.</param>
  /// <param name="reason">A (reasonably) verbose error description.</param>
  /// <param name="reportToConsole">If <c>true</c> then the error will be shown as a right
  /// side screen message. Otherwise, the cursor status and hint texts will be changed.
  /// </param>
  /// <param name="cursorIcon">Specifies which cursor icon to use. Only makes sense when
  /// <paramref name="reportToConsole"/> is <c>false</c>.</param>
  private void ReportCheckError(string error, string reason,
                                bool reportToConsole = false,
                                string cursorIcon = ForbiddenIcon) {
    if (reportToConsole) {
      KIS_Shared.ShowRightScreenMessage("{0}: {1}", error, reason);
      KIS_UISoundPlayer.instance.PlayBipWrong();
    } else {
      KISAddonCursor.CursorEnable(cursorIcon, error, reason);
    }
  }
}
  
// Create an instance for managing inventory in the editor.
[KSPAddon(KSPAddon.Startup.EditorAny, false /*once*/)]
internal class KISAddonPickupInEditor : KISAddonPickup {
}

// Create an instance for accessing inventory in EVA.
[KSPAddon(KSPAddon.Startup.Flight, false /*once*/)]
internal class KISAddonPickupInFlight : KISAddonPickup {
}

}  // namespace
