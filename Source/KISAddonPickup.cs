// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSP.UI.Screens;
using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using KSPDev.GUIUtils.TypeFormatters;
using KSPDev.InputUtils;
using KSPDev.LogUtils;
using KSPDev.ModelUtils;
using KSPDev.PartUtils;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KIS {

// Next localization ID: #kisLOC_01041.
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
sealed class KISAddonPickup : MonoBehaviour {

  #region Localizable GUI strings.
  static readonly Message ReDockOkStatusTooltipTxt = new Message(
      "#kisLOC_01000",
      defaultTemplate: "Re-dock",
      description: "The action status to show in the tooltip when the player presses the re-dock"
      + " action key and the action is allowed.");

  static readonly Message ReDockNotOkStatusTooltipTxt = new Message(
      "#kisLOC_01001",
      defaultTemplate: "Can't re-dock",
      description: "The action status to show in the tooltip when the player presses the re-dock"
      + " action key and the action is not allowed.");

  static readonly Message GrabOkStatusTooltipTxt = new Message(
      "#kisLOC_01002",
      defaultTemplate: "Grab",
      description: "The action status to show in the tooltip when the player presses the grab"
      + " action key while pointing on a root part, and the action is allowed.");

  static readonly Message DetachAndGrabOkStatusTooltipTxt = new Message(
      "#kisLOC_01003",
      defaultTemplate: "Detach & Grab",
      description: "The action status to show in the tooltip when the player presses the grab"
      + " action key while pointing on a child part, and the action is allowed.");

  static readonly Message GrabNotOkStatusTooltipTxt = new Message(
      "#kisLOC_01004",
      defaultTemplate: "Can't grab",
      description: "The action status to show in the tooltip when the player presses the grab"
      + " action key and the action is not allowed.");

  static readonly Message DetachOkStatusTooltipTxt = new Message(
      "#kisLOC_01005",
      defaultTemplate: "Detach",
      description: "The action status to show in the tooltip when the player presses the detach"
      + " action key and the action is allowed.");

  static readonly Message DetachStaticOkStatusTooltipTxt = new Message(
      "#kisLOC_01006",
      defaultTemplate: "Detach from ground",
      description: "The action status to show in the tooltip when the player presses the detach"
      + " action key when a statically attached (to the ground) part is targeted, and the action is"
      + " allowed.");

  static readonly Message DetachNotOkStatusTootltipTxt = new Message(
      "#kisLOC_01007",
      defaultTemplate: "Can't detach",
      description: "The action status to show in the tooltip when the player attempts to detach a"
      + " part that cannot be detached for any reason.");

  static readonly Message AttachNotOkStatusTooltipTxt = new Message(
      "#kisLOC_01008",
      defaultTemplate: "Can't attach",
      description: "The action status to show in the tooltip when the player attempts to attach a"
      + " part that cannot be attached for any reason.");

  static readonly Message TooHeavyStatusTooltipTxt = new Message(
      "#kisLOC_01009",
      defaultTemplate: "Too heavy",
      description: "The action status to show in the tooltip when the player attempts to grab or"
      + " move a too heavy part or assembly.");

  static readonly Message TooFarStatusTooltipTxt = new Message(
      "#kisLOC_01010",
      defaultTemplate: "Too far",
      description: "The action status to show in the tooltip when the player attempts to grab or"
      + " move a part or assembly which is too far from the acting part (e.g. an EVA kerbal).");

  static readonly Message NotSupportedStatusTooltipTxt = new Message(
      "#kisLOC_01011",
      defaultTemplate: "Not supported",
      description: "The action status to show in the tooltip when the action cannot complete due to"
      + " the unexpected reasons.");

  static readonly Message NeedToolStatusTooltipTxt = new Message(
      "#kisLOC_01012",
      defaultTemplate: "Tool needed",
      description: "The action status to show in the tooltip when the requested action requires an"
      + " equipped tool on the EVA kerbal, but there was none.");

  static readonly Message<string, MassType> ReDockStatusTooltipTxt = new Message<string, MassType>(
      "#kisLOC_01013",
      defaultTemplate: "Vessel: <<1>>\nMass: <<2>>",
      description: "The action status to show in the tooltip when the re-dock action is in the" 
      + " process."
      + "\nArgument <<1>> is a name of the vessel being re-docked."
      + "\nArgument <<2>> is the total mass of the vessel. Format: MassType.");

  static readonly Message RootPartStatusTooltipTxt = new Message(
      "#kisLOC_01014",
      defaultTemplate: "Root part",
      description: "The action status to show in the tooltip when a root part of the vessel is"
      + " targeted for an action.");

  static readonly Message<string> DropActionStatusTooltipTxt = new Message<string>(
      "#kisLOC_01015",
      defaultTemplate: "Drop (<<1>>)",
      description: "The tooltip help string in case of the current action is dropping of a grabbed"
      + " part."
      + "\nArgument <<1>> is the name of the node at which the part will be acted.");

  static readonly Message<string> AttachActionStatusTooltipTxt = new Message<string>(
      "#kisLOC_01016",
      defaultTemplate: "Attach (<<1>>)",
      description: "The tooltip help string in case of the current action is attaching of a grabbed"
      + " part."
      + "\nArgument <<1>> is the name of the node at which the part will be acted.");

  static readonly Message<string> RedockActionStatusTooltipTxt = new Message<string>(
      "#kisLOC_01017",
      defaultTemplate: "Re-dock (<<1>>)",
      description: "The tooltip help string in case of the current action is re-docking of a"
      + " grabbed vessel."
      + "\nArgument <<1>> is the name of the vessel which will be re-docked.");

  static readonly Message MountActionStatusTooltipTxt = new Message(
      "#kisLOC_01018",
      defaultTemplate: "Mount",
      description: "The tooltip help string in case of the current action is mounting a KIS item.");

  static readonly Message ReDockIsNotPossibleTooltipTxt = new Message(
      "#kisLOC_01019",
      defaultTemplate: "No docked vessel found",
      description: "The tooltip help string to display when the re-dock action is selected but the"
      + " mouse cursor is not pointing to a valid docked vessel.");

  static readonly Message ReDockSelectVesselText = new Message(
      "#kisLOC_01020",
      defaultTemplate: "Select a vessel",
      description: "The tooltip help string to display when the re-dock action selected but the"
      + " mouse cursor is not pointing any part.");

  static readonly Message CannotMoveKerbonautTooltipTxt = new Message(
      "#kisLOC_01021",
      defaultTemplate: "Kerbonauts can move themselves using jetpacks. Try to ask.",
      description: "The tooltip help string to display when the player attempts to grab a kerbal");

  static readonly Message<MassType, MassType> TooHeavyTooltipTxt = new Message<MassType, MassType>(
      "#kisLOC_01022",
      defaultTemplate: "Bring more kerbals [<<1>> > <<2>>]",
      description: "The tooltip help string to display when the player attempts to grab a too heavy"
      + " object."
      + "\nArgument <<1>> is the total mass of the target object. Format: MassType."
      + "\nArgument <<2>> is the maximum allowed mass. Format: MassType.");

  static readonly Message TooFarTooltipTxt = new Message(
      "#kisLOC_01023",
      defaultTemplate: "Move closer to the part",
      description: "The tooltip help string to display when the player attempts to grab an object"
      + " which is too far away.");

  static readonly Message NeedToolToAttachTooltipTxt = new Message(
      "#kisLOC_01024",
      defaultTemplate: "This part can't be attached without a tool",
      description: "The tooltip help string to display when the player attempts to perform an"
      + " attach action without the proper tool equipped.");

  static readonly Message NeedToolToDetachTooltipTxt = new Message(
      "#kisLOC_01025",
      defaultTemplate: "This part can't be detached without a tool",
      description: "The tooltip help string to display when the player attempts to perform a"
      + " detach action without the proper tool equipped.");

  static readonly Message NeedToolToStaticDetachTooltipTxt = new Message(
      "#kisLOC_01026",
      defaultTemplate: "This part can't be detached from the ground without a tool",
      description: "The tooltip help string to display when the player attempts to perform a"
      + " detach action on a ground attached part without the proper tool equipped.");

  static readonly Message NotSupportedTooltipTxt = new Message(
      "#kisLOC_01027",
      defaultTemplate: "The function is not supported on this part",
      description: "The tooltip help string in the case when the action cannot complete due to"
      + " the unexpected reasons.");

  static readonly Message CannotAttachTooltipTxt = new Message(
      "#kisLOC_01028",
      defaultTemplate: "Attach function is not supported on this part",
      description: "The tooltip help string in the case when an attach action is attempted on a"
      + " part which is not designed for that.");

  static readonly Message<string> LonePartTargetTooltipTxt = new Message<string>(
      "#kisLOC_01029",
      defaultTemplate: "<<1>>",
      description: "The tooltip help string to display when a single part was targeted for an"
      + " action."
      + "\nArgument <<1>> is a name of the target part.");

  static readonly Message<string, int> AssemblyTargetTooltipTxt = new Message<string, int>(
      "#kisLOC_01030",
      defaultTemplate: "<<1>>\nAttached parts: <<2>>",
      description: "The tooltip help string to display when multiple parts was targeted for an"
      + " action."
      + "\nArgument <<1>> is a name of the target part."
      + "\nArgument <<2>> is the number of the children parts attached to the target.");

  static readonly Message<string, string> RollRotateKeysTooltipTxt = new Message<string, string>(
      "#kisLOC_01031",
      defaultTemplate: "[<<1>>][<<2>>]",
      description: "The tooltip help string for the key bindings to adjust the part's roll."
      + "\nArgument <<1>> is the \"adjust left\" key name."
      + "\nArgument <<2>> is the \"adjust right\" key name");

  static readonly Message<string, string> PitchRotateKeysTooltipTxt = new Message<string, string>(
      "#kisLOC_01032",
      defaultTemplate: "[<<1>>][<<2>>]",
      description: "The tooltip help string for the key bindings to adjust the part's pitch."
      + "\nArgument <<1>> is the \"adjust left\" key name."
      + "\nArgument <<2>> is the \"adjust right\" key name");

  static readonly Message<string, string> YawRotateKeysTooltipTxt = new Message<string, string>(
      "#kisLOC_01033",
      defaultTemplate: "[<<1>>][<<2>>]",
      description: "The tooltip help string for the key bindings to adjust the part's yaw."
      + "\nArgument <<1>> is the \"adjust left\" key name."
      + "\nArgument <<2>> is the \"adjust right\" key name");

  static readonly Message<string, string, string> PartRotateKeysTooltipTxt =
      new Message<string, string, string>(
          "#kisLOC_01034",
          defaultTemplate: "<<1>>/<<2>>/<<3>> to rotate",
          description: "The tooltip help string for the key bindings to adjust the part's rotation."
          + "\nArgument <<1>> is the keys for the roll adjustment."
          + "\nArgument <<2>> is the keys for the pitch adjustment."
          + "\nArgument <<3>> is the keys for the yaw adjustment.");

  static readonly Message<string> PartResetKeyTooltipTxt = new Message<string>(
      "#kisLOC_01035",
      defaultTemplate: "[<<1>>] to reset orientation & position",
      description: "The tooltip help string for the key binding to reset all the roll and offset"
      + " adjustments.");

  static readonly Message<string> PartChangeModeKeyTooltipTxt = new Message<string>(
      "#kisLOC_01036",
      defaultTemplate: "[<<1>>] to change node",
      description: "The tooltip help string for the key binding to change the node snap mode.");

  static readonly Message<string, string> PartOffsetKeysTooltipTxt = new Message<string, string>(
      "#kisLOC_01037",
      defaultTemplate: "[<<1>>]/[<<2>>] to move up/down",
      description: "The tooltip help string for the key bindings to adjust the part's offset."
      + "\nArgument <<1>> is the \"adjust up\" key name."
      + "\nArgument <<2>> is the \"adjust down\" key name");

  static readonly Message<string> PartAttachKeyTooltipTxt = new Message<string>(
      "#kisLOC_01038",
      defaultTemplate: "[<<1>>] to attach",
      description: "The tooltip help string for the key binding to switch to the attach mode.");

  static readonly Message PartCancelModeKeyTooltipTxt = new Message(
      "#kisLOC_01039",
      defaultTemplate: "[Escape] to cancel",
      description: "The tooltip help string for the key binding to cancel the operation.");

  static readonly Message CannotPickupGroundExperimentTooltipTxt = new Message(
      "#kisLOC_01040",
      defaultTemplate: "KIS cannot pickup deployed ground experiments.\nAccess the part's menu!",
      description: "The action status to show in the tooltip when the player tries to pick up a"
      + " ground experment from the scene. These parts must be handled via their menus.");
  #endregion

  /// <summary>A helper class to handle mouse clicks in the editor.</summary>
  class EditorClickListener : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    EditorPartIcon partIcon;
    bool dragStarted;

    public virtual void OnBeginDrag(PointerEventData eventData) {
      // Start dargging for KIS or delegate event to the editor.
      if (EventChecker.CheckClickEvent(editorPartGrabEvent, eventData.button)) {
        dragStarted = true;
        KISAddonPickup.instance.OnMouseGrabPartClick(partIcon.partInfo.partPrefab);
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
      } else {
        dragStarted = false;
      }
    }
        
    /// <summary>Registers click handlers on the editor's category icon.</summary>
    void Start() {
      // Getting components is not a cheap operation so, cache anything we can.
      partIcon = GetComponent<EditorPartIcon>();              
    }
  }

  #region Icons paths
  const string GrabIcon = "KIS/Textures/grab";
  const string GrabOkIcon = "KIS/Textures/grabOk";
  const string ForbiddenIcon = "KIS/Textures/forbidden";
  const string TooFarIcon = "KIS/Textures/tooFar";
  const string TooHeavyIcon = "KIS/Textures/tooHeavy";
  const string NeedToolIcon = "KIS/Textures/needtool";
  const string AttachOkIcon = "KIS/Textures/attachOk";
  const string DropIcon = "KIS/Textures/drop";
  const string DetachIcon = "KIS/Textures/detach";
  const string DetachOkIcon = "KIS/Textures/detachOk";
  const string MountIcon = "KIS/Textures/mount";
  #endregion

  [PersistentField("Editor/editorPartGrabAction")]
  public static string editorPartGrabAction = "mouse0";

  [PersistentField("EvaPickup/grabKey")]
  public static string grabKey = "g";

  [PersistentField("EvaPickup/attachKey")]
  public static string attachKey = "h";

  [PersistentField("EvaPickup/redockKey")]
  public static string redockKey = "y";

  [PersistentField("EvaPickup/draggedIconResolution")]
  public static int draggedIconResolution = 64;

  public static KIS_IconViewer icon = null;
  public static Part draggedPart;
  public static KIS_Item draggedItem;
  public static int draggedIconSize = 50;
  public static Part movingPart;
  public static KISAddonPickup instance;
  public bool grabActive;
  public bool detachActive;

  bool grabOk;
  bool detachOk;
  bool jetpackLock;
  bool delayedButtonUp;

  /// <summary>Mouse/keyboard event that grabs a part from the editor's panel.</summary>
  /// <seealso cref="editorPartGrabAction"/>
  static Event editorPartGrabEvent;

  /// <summary>A number of parts in the currently grabbed assembly.</summary>
  public static int grabbedPartsCount;
  /// <summary>The total mass of the grabbed assembly. Tons.</summary>
  public static double grabbedMass;
  /// <summary>A root part of the currently grabbed assembly.</summary>
  public static Part grabbedPart;

  static Part redockTarget;
  static string redockVesselName;
  static bool redockOk;

  public enum PointerMode {
    Nothing,
    Drop,
    Attach,
    ReDock
  }
  PointerMode _pointerMode = PointerMode.Drop;

  public enum CursorMode {
    Nothing,
    Detach,
    Grab,
    ReDock
  }
  CursorMode cursorMode = CursorMode.Nothing;

  public enum PickupMode {
    Nothing,
    GrabFromInventory,
    Move,
    Undock
  }
  PickupMode pickupMode = PickupMode.Nothing;

  public PointerMode pointerMode {
    get {
      return _pointerMode;
    }
    set {
      List<String> texts = new List<String>();
      texts.Add(PartRotateKeysTooltipTxt.Format(
          RollRotateKeysTooltipTxt.Format(
              GameSettings.Editor_rollLeft.name, GameSettings.Editor_rollRight.name),
          PitchRotateKeysTooltipTxt.Format(
              GameSettings.Editor_pitchDown.name, GameSettings.Editor_pitchUp.name),
          YawRotateKeysTooltipTxt.Format(
              GameSettings.Editor_yawLeft.name, GameSettings.Editor_yawRight.name)));
      texts.Add(PartResetKeyTooltipTxt.Format(GameSettings.Editor_resetRotation.name));
      texts.Add(PartChangeModeKeyTooltipTxt.Format(GameSettings.Editor_toggleSymMethod.name));
      if (value == PointerMode.Drop) {
        texts.Add(PartOffsetKeysTooltipTxt.Format(
            KISAddonPointer.offsetUpKey.ToUpper(), KISAddonPointer.offsetDownKey.ToUpper()));
      }
      if (value == PointerMode.Drop) {
        texts.Add(PartAttachKeyTooltipTxt.Format(attachKey.ToUpper()));
      }
      texts.Add(PartCancelModeKeyTooltipTxt);

      if (value == PointerMode.Drop) {
        KISAddonCursor.CursorEnable(
            DropIcon,
            DropActionStatusTooltipTxt.Format(KISAddonPointer.currentAttachNode.id),
            texts);
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
        KISAddonCursor.CursorEnable(
            AttachOkIcon,
            AttachActionStatusTooltipTxt.Format(KISAddonPointer.currentAttachNode.id),
            texts);
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
            KISAddonPointer.allowStatic =
                HasActivePickupInRange(attachPart, canStaticAttachOnly: true);
          }

          if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedAlways) {
            KISAddonPointer.allowPart = true;
          } else if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
            KISAddonPointer.allowPart = HasActivePickupInRange(attachPart, canPartAttachOnly: true);
          }
        } else {
          KISAddonPointer.allowPart = HasActivePickupInRange(attachPart, canPartAttachOnly: true);
          KISAddonPointer.allowStatic = false;
        }
      }
      if (value == PointerMode.ReDock) {
        KISAddonCursor.CursorEnable(AttachOkIcon,
                                    RedockActionStatusTooltipTxt.Format(redockVesselName),
                                    new List<string>() { PartCancelModeKeyTooltipTxt });
        KISAddonPointer.allowPart = false;
        KISAddonPointer.allowStatic = false;
        KISAddonPointer.allowEva = false;
        KISAddonPointer.allowPartItself = false;
        KISAddonPointer.useAttachRules = true;
        KISAddonPointer.allowOffset = false;
        KISAddonPointer.colorOk = XKCDColors.Teal;
        KISAddonPointer.allowedAttachmentParts = GetAllowedDockPorts();
      }
      DebugEx.Info("Set pointer mode to: {0}", value);
      _pointerMode = value;
    }
  }

  void Awake() {
    instance = this;
    editorPartGrabEvent = Event.KeyboardEvent(editorPartGrabAction);
    if (HighLogic.LoadedSceneIsEditor) {
      if (EditorPartList.Instance) {
        var iconPrefab = EditorPartList.Instance.partPrefab.gameObject;
        if (iconPrefab.GetComponent<EditorClickListener>() == null) {
          EditorPartList.Instance.partPrefab.gameObject.AddComponent<EditorClickListener>();
        } else {
          DebugEx.Warning("Skip adding click listener because it exists");
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
      if (KIS_Shared.IsKeyDown(attachKey)) {
        EnableAttachMode();
      }
      if (KIS_Shared.IsKeyUp(attachKey)) {
        DisableAttachMode();
      }

      // Check if grab key is pressed.
      if (KIS_Shared.IsKeyDown(grabKey)) {
        EnableGrabMode();
      }
      if (KIS_Shared.IsKeyUp(grabKey)) {
        DisableGrabMode();
      }

      // Check if re-docking key is pressed.
      if (KIS_Shared.IsKeyDown(redockKey)) {
        EnableRedockingMode();
      }
      if (KIS_Shared.IsKeyUp(redockKey)) {
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
        // false action triggering. So, just postpone UP event by one frame when it
        // happens in the same frame as the DOWN event.
        if (KISAddonCursor.partClickedFrame == Time.frameCount) {
          DebugEx.Warning("Postponing mouse button up event in frame {0}", Time.frameCount);
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
    KISAddonCursor.StartPartDetection(
        OnMouseGrabPartClick, OnMouseGrabEnterPart, null, OnMouseGrabExitPart);
    KISAddonCursor.CursorEnable(GrabIcon, GrabOkStatusTooltipTxt);
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
      // Indicate the detach mode is active, but don't yet allow it to happen. The part focus
      // callback will determine it.
      KISAddonCursor.CursorEnable(DetachIcon, DetachOkStatusTooltipTxt);
      detachActive = true;
      cursorMode = CursorMode.Detach;
      return;
    }
    // Entering "attach moving part" mode.
    if (KISAddonPointer.isRunning
        && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount
        && pointerMode == KISAddonPickup.PointerMode.Drop) {
      var checkPart = grabbedPart ?? draggedItem.availablePart.partPrefab;
      var refPart = grabbedPart ?? draggedItem.inventory.part;
      Func<Part, bool> checkNodesFn = p =>
          KIS_Shared.GetAvailableAttachNodes(p, ignoreAttachedPart: p.parent).Any();
      if (CheckIsAttachable(checkPart, refPart, checkNodesFn, reportToConsole: true)) {
        UISounds.PlayClick();
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
      UISounds.PlayClick();
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
        DebugEx.Info("Jetpack mouse input re-enabled");
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
            Drop(null, item: draggedItem);
          } else {
            movingPart = draggedPart;
            Drop(movingPart);
          }
        }
      }
      DisableIcon();
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
      KISAddonCursor.CursorEnable(pDrag.dragIconPath, pDrag.dragText, pDrag.dragText2);
      return;
    }

    if (draggedPart) {
      KISAddonCursor.CursorDisable();
      return;
    }

    KIS_Shared.SetHierarchySelection(part, true /* isSelected */);
    if (!CheckCanGrabRealPart(part)) {
      return;
    }

    // Grab icon.
    KISAddonCursor.CursorEnable(
        GrabOkIcon,
        part.parent != null
            ? DetachAndGrabOkStatusTooltipTxt.Format()
            : GrabOkStatusTooltipTxt.Format(),
        grabbedPartsCount == 1
            ? LonePartTargetTooltipTxt.Format(part.partInfo.title)
            : AssemblyTargetTooltipTxt.Format(part.partInfo.title, grabbedPartsCount - 1));

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
      if (ModuleKISInventory.GetAllOpenInventories().Count > 0) {
        Pickup(part);
      }
    }
  }

  void OnMouseGrabExitPart(Part p) {
    if (grabActive) {
      KISAddonCursor.CursorEnable(GrabIcon, GrabOkStatusTooltipTxt);
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
    if (part.vessel.isEVA) {
      KISAddonCursor.CursorEnable(
          ForbiddenIcon, DetachNotOkStatusTootltipTxt, NotSupportedTooltipTxt);
      return;
    }

    // Check if it's a KIS item that can be static attached.
    var item = part.GetComponent<ModuleKISItem>();
    if (item != null && item.staticAttached) {
      if ((item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways)
          || (HasActivePickupInRange(part, canStaticAttachOnly: true)
              && item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool)) {
        part.SetHighlightColor(XKCDColors.Periwinkle);
        part.SetHighlight(true, false);
        KISAddonCursor.CursorEnable(DetachOkIcon, DetachStaticOkStatusTooltipTxt,
                                    LonePartTargetTooltipTxt.Format(part.partInfo.title));
        detachOk = true;
        return;
      }
      // Cannot static attach.
      if (FlightGlobals.ActiveVessel.isEVA) {
        KISAddonCursor.CursorEnable(
            NeedToolIcon, NeedToolStatusTooltipTxt, NeedToolToStaticDetachTooltipTxt);
      } else {
        KISAddonCursor.CursorEnable(
            ForbiddenIcon, NotSupportedStatusTooltipTxt, NotSupportedTooltipTxt);
      }
      return;
    }

    // Check if part is a root. The root parts cannot be detached.
    if (part.parent == null) {
      KISAddonCursor.CursorEnable(ForbiddenIcon, RootPartStatusTooltipTxt, NotSupportedTooltipTxt);
      return;
    }

    // Check if part is not mounted and can be detached.
    var parentMount = part.parent.GetComponent<ModuleKISPartMount>();
    if (parentMount != null) {
      if (item != null && item.allowPartAttach == ModuleKISItem.ItemAttachMode.Disabled) {
        KISAddonCursor.CursorEnable(
            ForbiddenIcon, DetachNotOkStatusTootltipTxt, NotSupportedTooltipTxt);
        return;
      }
      if ((item != null && item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool
           || item == null)
          && !HasActivePickupInRange(part, canPartAttachOnly: true)) {
        if (FlightGlobals.ActiveVessel.isEVA) {
          KISAddonCursor.CursorEnable(
              NeedToolIcon, NeedToolStatusTooltipTxt, NeedToolToDetachTooltipTxt);
        } else {
          KISAddonCursor.CursorEnable(
              ForbiddenIcon, NotSupportedStatusTooltipTxt, NotSupportedTooltipTxt);
        }
        return;
      }
    }

    // Detach icon
    part.SetHighlightColor(XKCDColors.Periwinkle);
    part.SetHighlight(true, false);
    part.parent.SetHighlightColor(XKCDColors.Periwinkle);
    part.parent.SetHighlight(true, false);
    KISAddonCursor.CursorEnable(DetachOkIcon, DetachOkStatusTooltipTxt,
                                LonePartTargetTooltipTxt.Format(part.partInfo.title));
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
      DebugEx.Error("Unexpected actor executed KIS action via UI: {0}", FlightGlobals.ActiveVessel);
    }

    // Detach part and play a detach sound if one available.
    ModuleKISItem item = part.GetComponent<ModuleKISItem>();
    if (item && item.staticAttached) {
      item.GroundDetach();  // Parts attached to the ground need special attention.
    } else {
      KIS_Shared.DecoupleAssembly(part);
    }
    if (pickupModule) {
      KIS_Shared.PlaySoundAtPoint(
          pickupModule.detachStaticSndPath, pickupModule.part.transform.position);
    }
  }

  void OnMouseDetachExitPart(Part p) {
    if (detachActive) {
      KISAddonCursor.CursorEnable(DetachIcon, DetachOkStatusTooltipTxt);
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

  /// <summary>
  /// Checks if there is at least one actor in range that can interact with the part, given the
  /// capabilities restrictions.
  /// </summary>
  /// <param name="refPart">The real part in the scene to check the bounds for.</param>
  /// <param name="canPartAttachOnly">Set <c>true</c> to check the part attach capability.</param>
  /// <param name="canStaticAttachOnly">
  /// Set <c>true</c> to check the surafce attach capability.
  /// </param>
  /// <returns><c>true</c> if at least one actor found.</returns>
  /// <seealso cref="GetActivePickupNearest"/>
  bool HasActivePickupInRange(
      Part refPart, bool canPartAttachOnly = false, bool canStaticAttachOnly = false) {
    var actor = GetActivePickupNearest(
        refPart, canPartAttachOnly: canPartAttachOnly, canStaticAttachOnly: canStaticAttachOnly);
    return actor != null;
  }

  /// <summary>Returns the nearest actor module that has the required capabilities.</summary>
  /// <remarks>
  /// All actors on the active vessel are checked. The actor's range limit must be satisfied for it
  /// to be considered in the check.
  /// </remarks>
  /// <param name="refPart">
  /// The part that is being checked. Its mesh will be used to determine the exact distance.
  /// </param>
  /// <param name="canPartAttachOnly">
  /// Tells that the actor must be able to attach items to parts. This parameter is mutual exclusive
  /// with <paramref name="canStaticAttachOnly"/>.
  /// </param>
  /// <param name="canStaticAttachOnly">
  /// Tells that the actor must be able to attach items to the surface. This parameter is mutual
  /// exclusive with <paramref name="canPartAttachOnly"/>.
  /// </param>
  /// <param name="probePosition">
  /// Optional new position of the part. The distance will be checked, assuming the part is located
  /// there. The actual position of the part won't change.
  /// </param>
  /// <param name="probeRotation">
  /// Optional new rotation of the part. The distance will be checked, assuming the part is rotated
  /// as specified. The actual rotation of the part won't change.
  /// </param>
  /// <param name="testFunc">The function to verify if the module qualifies for the check.</param>
  /// <returns>
  /// The nearest actor from the active vessel or <c>null</c> if no matching candidates found.
  /// </returns>
  /// TODO(ihsoft): Redesign this method. Instead of awkward "can" parameters, simply provide a filter function.
  ModuleKISPickup GetActivePickupNearest(Part refPart,
                                         bool canPartAttachOnly = false,
                                         bool canStaticAttachOnly = false,
                                         Vector3? probePosition = null,
                                         Quaternion? probeRotation = null,
                                         Func<ModuleKISPickup, bool> testFunc = null) {
    // Temporarily relocate the ref part to probe the distance at the new location.
    Vector3 oldPos = refPart.transform.position;
    Quaternion oldRot = refPart.transform.rotation;

    // Legacy code compatibility.
    if (testFunc == null) {
      if (!canPartAttachOnly && !canStaticAttachOnly) {
        testFunc = x => true;
      } else if (canPartAttachOnly) {
        testFunc = x => x.allowPartAttach;
      } else if (canStaticAttachOnly) {
        testFunc = x => x.allowStaticAttach;
      } else {
        throw new ArgumentException(
            "canPartAttachOnly and canStaticAttachOnly are mutual exclusive");
      }
    }

    refPart.transform.position = probePosition ?? oldPos;
    refPart.transform.rotation = probeRotation ?? oldRot;
    var pickup = FlightGlobals.ActiveVessel
        .FindPartModulesImplementing<ModuleKISPickup>()
        .Select(x => new {
            module = x,
            sqrDist = Colliders.GetSqrDistanceToPartOrDefault(x.part.transform.position, refPart),
            sqrRange = x.maxDistance * x.maxDistance
        })
        .OrderBy(x => x.sqrDist)
        .FirstOrDefault(x => x.sqrDist <= x.sqrRange && testFunc(x.module));
    refPart.transform.position = oldPos;
    refPart.transform.rotation = oldRot;
    return pickup != null ? pickup.module : null;
  }

  /// <summary>Calculates the maximum mass that actor(s) can lift.</summary>
  /// <param name="refPart">
  /// The part that is being checked. Its mesh will be used to determine the exact distance.
  /// </param>
  /// <param name="probePosition">
  /// Optional new position of the part. The distance will be checked, assuming the part is located
  /// there. The actual position of the part won't change.
  /// </param>
  /// <param name="probeRotation">
  /// Optional new rotation of the part. The distance will be checked, assuming the part is rotated
  /// as specified. The actual rotation of the part won't change.
  /// </param>
  /// <returns>
  /// The nearest actor from the active vessel or <c>null</c> if no matching candidates found.
  /// </returns>
  /// <returns>The maximum possible mas, considering all the actors in range.</returns>
  float GetAllPickupMaxMassInRange(Part refPart,
                                   Vector3? probePosition = null,
                                   Quaternion? probeRotation = null) {
    // Temporarily relocate the ref part to probe the distance at the new location.
    Vector3 oldPos = refPart.transform.position;
    Quaternion oldRot = refPart.transform.rotation;
    refPart.transform.position = probePosition ?? oldPos;
    refPart.transform.rotation = probeRotation ?? oldRot;

    float maxMass = 0;
    var allPickupModules = FindObjectsOfType(typeof(ModuleKISPickup)).Cast<ModuleKISPickup>();
    foreach (var pickupModule in allPickupModules) {
      var partDist = Colliders.GetSqrDistanceToPartOrDefault(
          pickupModule.part.transform.position, refPart);
      if (partDist <= pickupModule.maxDistance * pickupModule.maxDistance) {
        HostedDebugLog.Fine(
            pickupModule, "Contribute into mass capability: mass={0}, distance={1}",
            pickupModule.grabMaxMass, pickupModule.maxDistance);
        maxMass += pickupModule.grabMaxMass;
      }
    }

    refPart.transform.position = oldPos;
    refPart.transform.rotation = oldRot;
    return maxMass;
  }

  public void Pickup(Part part) {
    KIS_Shared.SetHierarchySelection(part, false /* isSelected */);
    draggedPart = part;
    draggedItem = null;
    if (cursorMode == CursorMode.Detach) {
      DebugEx.Warning("Detach mode is not expected in Pickup()");
    }
    HandlePickup(cursorMode == CursorMode.ReDock ? PickupMode.Undock : PickupMode.Move);
  }

  public void Pickup(KIS_Item item) {
    draggedPart = item.availablePart.partPrefab;
    draggedItem = item;
    HandlePickup(PickupMode.GrabFromInventory);
  }

  void HandlePickup(PickupMode newPickupMode) {
    DebugEx.Info("Start pickup in mode {0} from part: {1}", newPickupMode, draggedPart);
    grabbedPart = null;
    pickupMode = newPickupMode;
    cursorMode = CursorMode.Nothing;
    EnableIcon();
    KISAddonCursor.AbortPartDetection();
    grabActive = false;
    KISAddonCursor.CursorDisable();
    if (HighLogic.LoadedSceneIsFlight) {
      InputLockManager.SetControlLock(ControlTypes.VESSEL_SWITCHING, "KISpickup");
      // Disable jetpack mouse control (workaround as SetControlLock didn't have any effect on this)  
      KerbalEVA kEva = FlightGlobals.ActiveVessel.rootPart.GetComponent<KerbalEVA>();
      if (kEva && kEva.JetpackDeployed) {
        kEva.JetpackDeployed = false;
        jetpackLock = true;
        DebugEx.Info("Jetpack mouse input disabled");
      }
    }
  }

  public void Drop(KIS_Item item) {
    Drop(null, item: item);
  }

  /// <summary>Handles part drop action.</summary>
  /// <param name="fromPart">
  /// The part that was the source of the dragging action. If a world's part is grabbed than it will
  /// be that part. This part must have active physical colliders! As a last resort, leave it
  /// <c>null</c>, and the root part of the currently active vessel will be used.
  /// </param>
  /// <param name="item">The item being dragged.</param>
  void Drop(Part fromPart, KIS_Item item = null) {
    fromPart = fromPart ?? FlightGlobals.ActiveVessel.rootPart;  // A very bad workaround!
    if (item != null) {
      draggedItem = item;
      HostedDebugLog.Info(
          item.inventory,
          "End item pickup: item={0}, fromPart={1}", item.availablePart.title, fromPart);
    } else {
      grabbedPart = fromPart;
      HostedDebugLog.Info(fromPart, "End part pickup");
    }
    var pickupModule = GetActivePickupNearest(fromPart);
    if (!KISAddonPointer.isRunning && pickupModule != null) {
      var grabPosition = fromPart.transform.position;
      int unusedPartsCount;
      if (item == null && CheckMass(fromPart, out unusedPartsCount, reportToConsole: true)
          || item != null && CheckItemMass(item, reportToConsole: true)) {
        KISAddonPointer.allowPart = true;
        KISAddonPointer.allowEva = true;
        KISAddonPointer.allowMount = true;
        KISAddonPointer.allowStatic = true;
        KISAddonPointer.allowStack = pickupModule.allowPartStack;
        KISAddonPointer.maxDist = pickupModule.maxDistance;
        if (item != null) {
          KISAddonPointer.StartPointer(
              null /* rootPart */, item,
              OnPointerAction, OnPointerState, pickupModule.transform);
        } else {
          KISAddonPointer.StartPointer(
              fromPart, null /* item */,
              OnPointerAction, OnPointerState, pickupModule.transform);
        }

        pointerMode = pickupMode == PickupMode.Undock
            ? PointerMode.ReDock
            : PointerMode.Drop;
      }
    }
    KISAddonCursor.StopPartDetection();
  }

  bool hoverInventoryGui() {
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

  void OnGUI() {
    if (draggedPart) {
      var mousePosition = Input.mousePosition;
      mousePosition.y = Screen.height - mousePosition.y;
      GUI.depth = 0;
      GUI.DrawTexture(new Rect(mousePosition.x - (draggedIconSize / 2),
                               mousePosition.y - (draggedIconSize / 2),
                               draggedIconSize,
                               draggedIconSize),
                      icon.texture, ScaleMode.ScaleToFit);
    }
  }

  IEnumerator WaitAndStopDrag() {
    yield return new WaitForFixedUpdate();
    DisableIcon();
    draggedPart = null;
  }

  /// <summary>Creates an icon for the currently dragging part or item.</summary>
  /// <seealso cref="draggedPart"/>
  /// <seealso cref="draggedItem"/>
  void EnableIcon() {
    DisableIcon();
    if (draggedPart != null) {
      if (draggedItem != null) {
        icon = new KIS_IconViewer(
            draggedPart.partInfo, draggedIconResolution,
            VariantsUtils.GetCurrentPartVariant(draggedPart.partInfo, draggedItem.partNode));
      } else {
        icon = new KIS_IconViewer(draggedPart, draggedIconResolution);
      }
    }
  }

  /// <summary>Clears icon, ensuring it is Disposed</summary>
  void DisableIcon() {
    if (icon != null) {
      icon.Dispose();
      icon = null;
    }
  }

  void OnPointerState(KISAddonPointer.PointerTarget pTarget,
                      KISAddonPointer.PointerState pState,
                      Part hoverPart, AttachNode hoverNode) {
    if (pState == KISAddonPointer.PointerState.OnPointerStopped) {
      pointerMode = PointerMode.Nothing;
    }
    if (pState == KISAddonPointer.PointerState.OnMouseEnterNode) {
      if (pTarget == KISAddonPointer.PointerTarget.PartMount) {
        KISAddonCursor.CursorEnable(
            MountIcon, MountActionStatusTooltipTxt,
            new List<string>() {
              PartCancelModeKeyTooltipTxt
            });
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

  void OnPointerAction(KISAddonPointer.PointerTarget pointerTarget, Vector3 pos,
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
        pMount.OnPartMounted();
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
        UISounds.PlayToolAttach();
      }
    }
    draggedItem = null;
    draggedPart = null;
    movingPart = null;
    KISAddonCursor.CursorDefault();
  }

  void MoveDrop(Part tgtPart, Vector3 pos, Quaternion rot) {
    DebugEx.Info("Move part");
    var modulePickup =
        GetActivePickupNearest(movingPart, probePosition: pos, probeRotation: rot);
    if (modulePickup != null) {
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
    var refVessel = tgtPart != null
        ? tgtPart.vessel
        : modulePickup != null ? modulePickup.vessel : null;
    KIS_Shared.PlaceVessel(movingPart.vessel, pos, rot, refVessel);
    KIS_Shared.SendKISMessage(movingPart, KIS_Shared.MessageAction.DropEnd,
                              KISAddonPointer.currentAttachNode, tgtPart);
    KISAddonPointer.StopPointer();
    movingPart = null;
  }

  Part CreateDrop(Part tgtPart, Vector3 pos, Quaternion rot) {
    DebugEx.Info("Create & drop part");
    var refPart = tgtPart ?? draggedItem.availablePart.partPrefab;
    var modulePickup =
        GetActivePickupNearest(refPart, probePosition: pos, probeRotation: rot);
    draggedItem.StackRemove(1);
    var refVessel = tgtPart != null
        ? tgtPart.vessel
        : modulePickup != null ? modulePickup.vessel : null;
    Part newPart = KIS_Shared.CreatePart(
        draggedItem.partNode, pos, rot, draggedItem.inventory.part,
        onPartReady: p => KIS_Shared.PlaceVessel(
            p.vessel, p.vessel.vesselTransform.position, p.vessel.vesselTransform.rotation,
            refVessel));
    KIS_Shared.SendKISMessage(newPart, KIS_Shared.MessageAction.DropEnd,
                              KISAddonPointer.currentAttachNode, tgtPart);
    KISAddonPointer.StopPointer();
    draggedItem = null;
    draggedPart = null;
    if (modulePickup != null) {
      AudioSource.PlayClipAtPoint(
          GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
    }
    return newPart;
  }

  void MoveAttach(Part tgtPart, Vector3 pos, Quaternion rot, string srcAttachNodeID = null,
                  AttachNode tgtAttachNode = null) {
    DebugEx.Info("Move part & attach: tgtPart={0}", tgtPart);
    KIS_Shared.MoveAssembly(movingPart, srcAttachNodeID, tgtPart, tgtAttachNode, pos, rot);
    KISAddonPointer.StopPointer();
    movingPart = null;
    draggedItem = null;
    draggedPart = null;
  }

  Part CreateAttach(Part tgtPart, Vector3 pos, Quaternion rot,
                    string srcAttachNodeID = null, AttachNode tgtAttachNode = null) {
    DebugEx.Info("Create part & attach: tgtPart={0}", tgtPart);
    Part newPart;
    draggedItem.StackRemove(1);
    if (tgtPart) {
      newPart = KIS_Shared.CreatePart(
          draggedItem.partNode, pos, rot, draggedItem.inventory.part,
          coupleToPart: tgtPart,
          srcAttachNodeId: srcAttachNodeID,
          tgtAttachNode: tgtAttachNode,
          onPartReady: createdPart => KIS_Shared.SendKISMessage(
              createdPart, KIS_Shared.MessageAction.AttachEnd,
              KISAddonPointer.currentAttachNode, tgtPart, tgtAttachNode));
    } else {
      newPart = KIS_Shared.CreatePart(
          draggedItem.partNode, pos, rot, draggedItem.inventory.part,
          onPartReady: createdPart => KIS_Shared.SendKISMessage(
              createdPart, KIS_Shared.MessageAction.AttachEnd,
              KISAddonPointer.currentAttachNode, tgtPart, tgtAttachNode));
    }
    KISAddonPointer.StopPointer();
    movingPart = null;
    draggedItem = null;
    draggedPart = null;
    return newPart;
  }

  /// <summary>Enables mode that allows re-docking a vessel attached to a station.</summary>
  void EnableRedockingMode() {
    if (KISAddonPointer.isRunning || cursorMode != CursorMode.Nothing) {
      return;
    }
    var pickupModules =
        FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>();
    if (pickupModules.Count > 0) {
      DebugEx.Info("Enable re-dock mode");
      KISAddonCursor.StartPartDetection(OnMouseRedockPartClick, OnMouseRedockEnterPart,
                                        null, OnMouseRedockExitPart);
      cursorMode = CursorMode.ReDock;
    }
  }

  /// <summary>Disables re-docking mode.</summary>
  void DisableRedockingMode() {
    if (cursorMode == CursorMode.ReDock) {
      DebugEx.Info("Disable re-dock mode");
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
  void OnMouseRedockEnterPart(Part part) {
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
          ForbiddenIcon, ReDockNotOkStatusTooltipTxt, ReDockIsNotPossibleTooltipTxt);
      return;
    }
    KIS_Shared.SetHierarchySelection(redockTarget, true /* isSelected */);

    if (!CheckCanGrabRealPart(redockTarget) || !CheckCanDetach(redockTarget) ||
        !CheckIsAttachable(redockTarget, redockTarget,
                           p => KIS_Shared.GetAvailableAttachNodes(p).Any())) {
      return;
    }

    // Re-docking is allowed.
    KISAddonCursor.CursorEnable(GrabOkIcon, ReDockOkStatusTooltipTxt,
                                ReDockStatusTooltipTxt.Format(redockVesselName, grabbedMass));
    redockOk = true;
  }

  /// <summary>Grabs re-docking vessel and starts movement.</summary>
  /// <param name="part">Not used.</param>
  void OnMouseRedockPartClick(Part part) {
    if (redockOk) {
      Pickup(redockTarget);
    }
  }
      
  /// <summary>Erases re-docking vessel selection.</summary>
  void OnMouseRedockExitPart(Part unusedPart) {
    if (cursorMode != CursorMode.ReDock) {
      return;
    }
    redockOk = false;
    redockVesselName = null;
    if (redockTarget) {
      KIS_Shared.SetHierarchySelection(redockTarget, false /* isSelected */);
      redockTarget = null;
    }
    KISAddonCursor.CursorEnable(GrabIcon, ReDockOkStatusTooltipTxt, ReDockSelectVesselText);
  }
  
  /// <summary>Checks if the part and its children can be grabbed and reports the errors.</summary>
  /// <remarks>It must be an existing part, no prefab allowed.
  /// <para>Also, collects <seealso cref="grabbedMass"/> and
  /// <seealso cref="grabbedPartsCount"/> of the attempted hierarchy.</para>
  /// </remarks>
  /// <param name="part">A hierarchy root being grabbed.</param>
  /// <returns><c>true</c> when the hierarchy can be grabbed.</returns>
  bool CheckCanGrabRealPart(Part part) {
    // Don't grab kerbals. It's weird, and they don't have the attachment nodes anyways.
    if (part.vessel.isEVA) {
      ReportCheckError(GrabNotOkStatusTooltipTxt, CannotMoveKerbonautTooltipTxt);
      return false;
    }
    // Don't grab stock game's ground experiments.
    if (part.Modules.OfType<ModuleGroundPart>().Any()) {
      ReportCheckError(GrabNotOkStatusTooltipTxt, CannotPickupGroundExperimentTooltipTxt);
      return false;
    }
    // Check if there are kerbals in range.
    if (!HasActivePickupInRange(part)) {
      ReportCheckError(TooFarStatusTooltipTxt, TooFarTooltipTxt, cursorIcon: TooFarIcon);
      return false;
    }
    // Check if attached part has acceptable mass and can be detached.
    return CheckMass(part, out grabbedPartsCount) && CheckCanDetach(part);
  }

  /// <summary>Calculates grabbed part/assembly mass and reports if it's too heavy.</summary>
  /// <param name="srcPart">The part or assembly root to check the mass for.</param>
  /// <param name="grabbedPartsCount">
  /// The return parameter to store the number of the parts in the assembly.
  /// </param>
  /// <param name="reportToConsole">If <c>true</c>, then the error is only reported on the screen
  /// (it's the game's "console"). Otherwise, the cursor status wil change to
  /// <seealso cref="TooHeavyIcon"/>.
  /// </param>
  /// <returns><c>true</c> if total mass is within the limits.</returns>
  bool CheckMass(Part srcPart, out int grabbedPartsCount, bool reportToConsole = false) {
    grabbedMass = KIS_Shared.GetAssemblyMass(srcPart, out grabbedPartsCount);
    var pickupMaxMass = GetAllPickupMaxMassInRange(srcPart);
    if (grabbedMass > pickupMaxMass) {
      ReportCheckError(TooHeavyStatusTooltipTxt,
                       TooHeavyTooltipTxt.Format(grabbedMass, pickupMaxMass),
                       cursorIcon: TooHeavyIcon,
                       reportToConsole: reportToConsole);
      return false;
    }
    return true;
  }

  /// <summary>Calculates the grabbed part/assembly mass and reports if it's too heavy.</summary>
  /// <param name="item">The inventory item to check mass for.</param>
  /// <param name="reportToConsole">
  /// If <c>true</c>, then error is only reported on the screen. Otherwise, the cursor icon changes
  /// to <seealso cref="TooHeavyIcon"/>.
  /// </param>
  /// <returns><c>true</c> if total mass is within the limits.</returns>
  bool CheckItemMass(KIS_Item item, bool reportToConsole = false) {
    grabbedMass = item.totalSlotMass;
    var refPart = item.inventory.vessel.isEVA
        ? item.inventory.vessel.rootPart  // Inventories on EVA kerbal may not have colliders.
        : item.inventory.part;
    var pickupMaxMass = GetAllPickupMaxMassInRange(refPart);
    if (grabbedMass > pickupMaxMass) {
      ReportCheckError(TooHeavyStatusTooltipTxt,
                       TooHeavyTooltipTxt.Format(grabbedMass, pickupMaxMass),
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
  bool CheckCanDetach(Part part) {
    // If part is attached then check if the right tool is equipped to detach it.
    if (!part.parent) {
      return true;  // No parent, no detach needed.
    }
          
    var item = part.GetComponent<ModuleKISItem>();
    string rejectText = null;  // If null then detach is allowed.
    if (item != null) {
      // Handle KIS items.
      if (part.parent.GetComponent<ModuleKISPartMount>() != null) {
        // Check if part is a ground base.
        if (item.staticAttached
            && item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool
            && !HasActivePickupInRange(part, canStaticAttachOnly: true)) {
          rejectText = NeedToolToStaticDetachTooltipTxt;
        }
      } else {
        // Check specific KIS items
        if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.Disabled) {
          // Part restricts attachments and detachments.
          //TODO(ihsoft): Findout what parts cannot be detached. And why.
          DebugEx.Error("Unknown item being detached: {0}", item);
          ReportCheckError(DetachNotOkStatusTootltipTxt, NotSupportedTooltipTxt);
          return false;
        }
        if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
          // Part requires a tool to be detached.
          if (!HasActivePickupInRange(part, canPartAttachOnly: true)) {
            rejectText = NeedToolToDetachTooltipTxt;
          }
        }
      }
    } else {
      // Handle regular game parts.
      if (!HasActivePickupInRange(part, canPartAttachOnly: true)) {
        rejectText = NeedToolToDetachTooltipTxt;
      }
    }
    if (rejectText != null) {
      ReportCheckError(NeedToolStatusTooltipTxt, rejectText, cursorIcon: NeedToolIcon);
      return false;
    }
          
    return true;
  }

  /// <summary>Checks if part can be attached.</summary>
  /// <param name="checkPart">The part to check. Can be a prefab.</param>
  /// <param name="refPart">
  /// The part to use as reference in the pickups search. Must be a real part for the scene.
  /// </param>
  /// <param name="checkNodesFn">
  /// The function that verifies if the part has at least one free attach node. The input argument
  /// is the part or assembly to check.
  /// </param>
  /// <param name="reportToConsole">
  /// Tells if any found error should be reported to the debug console. Set it to <c>true</c> when
  /// negative response from the method is not exactly expected.
  /// </param>
  bool CheckIsAttachable(Part checkPart, Part refPart, Func<Part, bool> checkNodesFn,
                         bool reportToConsole = false) {
    var item = checkPart.GetComponent<ModuleKISItem>();

    // Check if part has at least one free node.
    if (!checkNodesFn(checkPart)) {
      // Check if it's a static attachable item. Those are not required to have nodes
      // since they attach to the ground.
      if (item == null || item.allowStaticAttach == ModuleKISItem.ItemAttachMode.Disabled) {
        ReportCheckError(AttachNotOkStatusTooltipTxt, CannotAttachTooltipTxt, reportToConsole);
        return false;
      }
    }

    // Check if KISItem part is allowed for attach without a tool.
    if (item != null && (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedAlways
                         || item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways)) {
      return true;
    }

    // Check if there is a kerbonaut with a tool to handle the task.
    if (!HasActivePickupInRange(refPart, canPartAttachOnly: true)) {
      // Check if it's EVA engineer or a KAS item.
      if (FlightGlobals.ActiveVessel.isEVA) {
        ReportCheckError(
            NeedToolStatusTooltipTxt, NeedToolToAttachTooltipTxt, reportToConsole, NeedToolIcon);
      } else {
        ReportCheckError(NotSupportedStatusTooltipTxt, NotSupportedTooltipTxt, reportToConsole);
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
  static HashSet<Part> GetAllowedDockPorts() {
    var redockNode = redockTarget.GetComponent<ModuleDockingNode>();
    var result = new HashSet<Part>(redockTarget.vessel.parts
        .Select(p => p.FindModuleImplementing<ModuleDockingNode>())
        .Where(dp => dp != null
             && !redockTarget.hasIndirectChild(dp.part)
             && !KIS_Shared.IsNodeDocked(dp) && !KIS_Shared.IsNodeCoupled(dp)
             && KIS_Shared.CheckNodesCompatible(redockNode, dp))
        .Select(dp => dp.part));
    DebugEx.Info("Found {0} allowed docking ports", result.Count());
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
  void ReportCheckError(string error, string reason,
                        bool reportToConsole = false,
                        string cursorIcon = ForbiddenIcon) {
    if (reportToConsole) {
      ScreenMessaging.ShowInfoScreenMessage("{0}: {1}", error, reason);
      UISounds.PlayBipWrong();
    } else {
      KISAddonCursor.CursorEnable(cursorIcon, error, reason);
    }
  }
}

// Create an instance for managing inventory in the editor.
[KSPAddon(KSPAddon.Startup.EditorAny, false /*once*/)]
sealed class KISAddonPickupInEditor : MonoBehaviour {
  void Awake() {
    gameObject.AddComponent<KISAddonPickup>();
  }
}

// Create an instance for accessing inventory in EVA.
[KSPAddon(KSPAddon.Startup.Flight, false /*once*/)]
sealed class KISAddonPickupInFlight : MonoBehaviour {
  void Awake() {
    gameObject.AddComponent<KISAddonPickup>();
  }
}

}  // namespace
