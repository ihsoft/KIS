// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSP.UI.Screens;
using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using KSPDev.InputUtils;
using KSPDev.LogUtils;
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
      + " action key while pointing on an child part, and the action is allowed.");

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
      description: "The tooltip help string to display when the player attempts to perfrom an"
      + " attach action without the proper tool equipped.");

  static readonly Message NeedToolToDetachTooltipTxt = new Message(
      "#kisLOC_01025",
      defaultTemplate: "This part can't be detached without a tool",
      description: "The tooltip help string to display when the player attempts to perfrom a"
      + " detach action without the proper tool equipped.");

  static readonly Message NeedToolToStaticDetachTooltipTxt = new Message(
      "#kisLOC_01026",
      defaultTemplate: "This part can't be detached from the ground without a tool",
      description: "The tooltip help string to display when the player attempts to perfrom a"
      + " detach action on a ground attched part without the proper tool equipped.");

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
      + "\nAgrument <<1>> is a name of the target part.");

  static readonly Message<string, int> AssemblyTargetTooltipTxt = new Message<string, int>(
      "#kisLOC_01030",
      defaultTemplate: "<<1>>\nAttached parts: <<2>>",
      description: "The tooltip help string to display when multiple parts was targeted for an"
      + " action."
      + "\nAgrument <<1>> is a name of the target part."
      + "\nAgrument <<2>> is the number of the children parts attached to the target.");

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

  static readonly Message SourceInventoryTooFar = new Message(
      "#kisLOC_01040",
      defaultTemplate: "Storage is too far.",
      description: "The error text when an item is dragged out to the world from an inventory that is too far.");
  #endregion

  /// <summary>A helper class to handle mouse clicks in the editor.</summary>
  class EditorClickListener : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    EditorPartIcon partIcon;
    bool dragStarted;
    const PointerEventData.InputButton PartDragButton = PointerEventData.InputButton.Left;

    public virtual void OnBeginDrag(PointerEventData eventData) {
      // Start dargging for KIS or delegate event to the editor.
      if (eventData.button == PartDragButton
          && EventChecker.IsModifierCombinationPressed(editorGrabPartModifiers)) {
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
      } else if (eventData.button == PartDragButton) {
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

  [PersistentField("Editor/partGrabModifiers")]
  static KeyModifiers editorGrabPartModifiers = KeyModifiers.None;

  [PersistentField("EvaPickup/grabKey")]
  public static string grabKey = "y";

  [PersistentField("EvaPickup/draggedIconResolution")]
  public static int draggedIconResolution = 64;

  public static KIS_IconViewer icon = null;
  public static Part draggedPart;
  public static KIS_Item draggedItem;
  public static int draggedIconSize = 50;
  public static Part movingPart;
  public static KISAddonPickup instance;
  public bool grabActive = false;
  public bool detachActive = false;

  private bool grabOk = false;
  private bool detachOk = false;
  private bool jetpackLock = false;
  private bool delayedButtonUp = false;

  /// <summary>A number of parts in the currently grabbed assembly.</summary>
  public static int grabbedPartsCount;
  /// <summary>The total mass of the grabbed assembly. Tons.</summary>
  public static float grabbedMass;
  /// <summary>A root part of the currently grabbed assembly.</summary>
  public static Part grabbedPart;

  private static Part redockTarget;
  private static string redockVesselName;
  private static bool redockOk;

  public enum PointerMode {
    Nothing,
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
        texts.Add(PartAttachKeyTooltipTxt.Format(grabKey.ToUpper()));
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
            if (HasActivePickupNearby(attachPart, canStaticAttachOnly: true)) {
              KISAddonPointer.allowStatic = true;
            }
          }

          if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedAlways) {
            KISAddonPointer.allowPart = true;
          } else if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
            if (HasActivePickupNearby(attachPart, canPartAttachOnly: true)) {
              KISAddonPointer.allowPart = true;
            }
          }
        } else {
          if (HasActivePickupNearby(attachPart, canPartAttachOnly: true)) {
            KISAddonPointer.allowPart = true;
          }
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
    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel.IsControllable) {
      if (KIS_Shared.IsKeyDown(grabKey)) {
        if (KISAddonPointer.isRunning) {
          switch (pointerMode) {
            case PointerMode.Nothing:
              throw new InvalidOperationException();
            case PointerMode.Drop:
               EnableAttachMode();
               return;
            case PointerMode.Attach:
               DisableAttachMode();
               return;
            case PointerMode.ReDock:
               return;
            default:
              throw new ArgumentOutOfRangeException("pointerMode");
          }
        }

        if (draggedPart) {
          return;
        }

        if (!KISAddonCursor.isRunning) {
          InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "KISPickup");
          EnableGrabMode();
          return;
        }

        switch (cursorMode) {
          case CursorMode.Nothing:
            throw new InvalidOperationException();
          case CursorMode.Detach:
            DisableAttachMode();
            EnableRedockingMode();
            return;
          case CursorMode.Grab:
            DisableGrabMode();
            EnableAttachMode();
            return;
          case CursorMode.ReDock:
            DisableRedockingMode();
            InputLockManager.RemoveControlLock("KISPickup");
            return;

          default:
            throw new ArgumentOutOfRangeException("pointerMode");
        }
      }

      if (KIS_Shared.IsKeyUp(KeyCode.Escape)) {
        DisableGrabMode();
        DisableAttachMode();
        DisableRedockingMode();
        InputLockManager.RemoveControlLock("KISPickup");
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
    if (!GetActivePickupAll().Any()) {
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
    if (cursorMode != CursorMode.Nothing || !GetActivePickupAll().Any()) {
      return;
    }
    // Entering "detach parts" mode.
    if (!KISAddonPointer.isRunning && !draggedPart) {
      KISAddonCursor.StartPartDetection(OnMouseDetachPartClick, OnMouseDetachEnterPart,
                                        null, OnMouseDetachExitPart);
      KISAddonCursor.CursorEnable(DetachIcon, DetachOkStatusTooltipTxt);
      detachActive = true;
      cursorMode = CursorMode.Detach;
    }
    // Entering "attach moving part" mode.
    if (KISAddonPointer.isRunning
        && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount
        && pointerMode == KISAddonPickup.PointerMode.Drop) {
      if (CheckIsAttachable(grabbedPart, reportToConsole: true)) {
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
      if (HighLogic.LoadedSceneIsFlight) {
        InputLockManager.RemoveControlLock("KISPickup");
      }
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

    ModuleKISItem item = part.GetComponent<ModuleKISItem>();
    ModuleKISPartMount parentMount = null;
    if (part.parent) {
      parentMount = part.parent.GetComponent<ModuleKISPartMount>();
    }

    // Check part distance
    if (!HasActivePickupInRange(part)) {
      KISAddonCursor.CursorEnable(TooFarIcon, TooFarStatusTooltipTxt, TooFarTooltipTxt);
      return;
    }
          
    // Check if part is static attached
    if (item) {
      if (item.staticAttached) {
        if ((item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways)
            || (HasActivePickupNearby(part, canStaticAttachOnly: true)
                && item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool)) {
          part.SetHighlightColor(XKCDColors.Periwinkle);
          part.SetHighlight(true, false);
          KISAddonCursor.CursorEnable(DetachOkIcon, DetachStaticOkStatusTooltipTxt,
                                      LonePartTargetTooltipTxt.Format(part.partInfo.title));
          detachOk = true;
        } else {
          KISAddonCursor.CursorEnable(
              NeedToolIcon, NeedToolStatusTooltipTxt,
              NeedToolToStaticDetachTooltipTxt);
        }
      }
    }

    // Check if part can be detached
    if (!parentMount) {
      if (part.children.Count > 0 || part.parent) {
        //Part with a child or a parent
        if (item) {
          if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.Disabled) {
            KISAddonCursor.CursorEnable(
                ForbiddenIcon, DetachNotOkStatusTootltipTxt, NotSupportedTooltipTxt);
          } else if (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool) {
            if (!HasActivePickupNearby(part, canPartAttachOnly: true)) {
              KISAddonCursor.CursorEnable(
                  NeedToolIcon, NeedToolStatusTooltipTxt, NeedToolToDetachTooltipTxt);
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
      KISAddonCursor.CursorEnable(ForbiddenIcon, RootPartStatusTooltipTxt, NotSupportedTooltipTxt);
      return;
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
    cursorMode = CursorMode.Nothing;
    KISAddonCursor.StopPartDetection();
    KISAddonCursor.CursorDefault();

    // Detach part and play a detach sound if one available.
    ModuleKISItem item = part.GetComponent<ModuleKISItem>();
    if (item && item.staticAttached) {
      item.GroundDetach();  // Parts attached to the ground need special attention.
    } else {
      KIS_Shared.DecoupleAssembly(part);
    }

    var pickupModule = GetActivePickupNearest(part);
    KIS_Shared.PlaySoundAtPoint(
        pickupModule.detachStaticSndPath, pickupModule.part.transform.position);

    InputLockManager.RemoveControlLock("KISPickup");
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

  public bool HasActivePickupInRange(Part p, bool canPartAttachOnly = false,
                                     bool canStaticAttachOnly = false) {
    return HasActivePickupInRange(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
  }

  public bool HasActivePickupInRange(Vector3 position, bool canPartAttachOnly = false,
                                     bool canStaticAttachOnly = false) {
    return GetActivePickupAll(canPartAttachOnly, canStaticAttachOnly)
                .Any(pickupModule => pickupModule.IsInRange(position));
  }

  public bool HasActivePickupNearby(Part p, bool canPartAttachOnly = false,
                                  bool canStaticAttachOnly = false) {
    return HasActivePickupNearby(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
  }

  public bool HasActivePickupNearby(Vector3 position, bool canPartAttachOnly = false,
                                  bool canStaticAttachOnly = false) {
    return GetActivePickupAll(canPartAttachOnly, canStaticAttachOnly)
                .Any();
  }

  public ModuleKISPickup GetActivePickupNearest(Part p, bool canPartAttachOnly = false,
                                                bool canStaticAttachOnly = false) {
    return GetActivePickupNearest(p.transform.position, canPartAttachOnly, canStaticAttachOnly);
  }

  public ModuleKISPickup GetActivePickupNearest(Vector3 position, bool canPartAttachOnly = false,
                                                bool canStaticAttachOnly = false) {
    return GetActivePickupAll(canPartAttachOnly, canStaticAttachOnly)
                .OrderBy(pickupModule => pickupModule.Distance(position))
                .FirstOrDefault();
  }

  private List<ModuleKISPickup> GetActivePickupAll(
      bool canPartAttachOnly = false, bool canStaticAttachOnly = false) {
    return FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleKISPickup>()
                .Where(pickupModule => pickupModule.IsActive())
                .Where(pickupModule =>
                    (!canPartAttachOnly && !canStaticAttachOnly)
                    || (canPartAttachOnly && pickupModule.allowPartAttach)
                    || (canStaticAttachOnly && pickupModule.allowStaticAttach))
        .ToList();
  }

  private float GetAllPickupMaxMassInRange(Vector3 grabPosition) {
    return FindObjectsOfType(typeof(ModuleKISPickup))
                .Cast<ModuleKISPickup>()
                .Where(pickupModule => pickupModule.IsActive())
                .Where(pickupModule => pickupModule.IsInRange(grabPosition))
                .Sum(pickupModule => pickupModule.AdjustedGrabMaxMass);
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

  private void HandlePickup(PickupMode newPickupMode) {
    DebugEx.Info("Start pickup in mode {0} from part: {1}", newPickupMode, draggedPart);
    grabbedPart = null;
    pickupMode = newPickupMode;
    cursorMode = CursorMode.Nothing;
    EnableIcon(draggedPart, draggedIconResolution);
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
    draggedItem = item;
    Drop(item.inventory.part, item.availablePart.partPrefab, item: item);
  }

  /// <summary>Handles part drop action.</summary>
  /// <param name="fromPart">A part that was the source of the draggign action. If a world's part is
  /// grabbed than it will be that part. If part is being dragged from inventory then this parameter
  /// is an inventory reference.</param>
  /// <param name="part">
  /// Part being grabbed. It's always a real part. Mutial exclusive with <paramref name="item"/>.
  /// </param>
  /// <param name="item">Item being dragged. Mutial exclusive with <paramref name="part"/>.</param>
  void Drop(Part fromPart, Part part, KIS_Item item = null) {
    grabbedPart = part;
    DebugEx.Info("End pickup of {0} from part: {1}", part, fromPart);
    if (!KISAddonPointer.isRunning) {
      var pickupModule = GetActivePickupNearest(fromPart);
      if (!pickupModule.IsInRange(fromPart)) { 
        ReportCheckError(SourceInventoryTooFar,
                       TooFarTooltipTxt,
                       cursorIcon: TooFarIcon,
                       reportToConsole: true);
        return;
      }
      var grabPosition = fromPart.transform.position;
      int unusedPartsCount;
      if (pickupModule
          && (item == null && CheckMass(grabPosition, part, out unusedPartsCount,
                                        reportToConsole: true)
              || item != null && CheckItemMass(grabPosition, item, reportToConsole: true))) {
        KISAddonPointer.allowPart = true;
        KISAddonPointer.allowEva = true;
        KISAddonPointer.allowMount = true;
        KISAddonPointer.allowStatic = true;
        KISAddonPointer.pickupModule = pickupModule;
        KISAddonPointer.scale = draggedItem != null
            ? KIS_Shared.GetPartExternalScaleModifier(draggedItem.partNode)
            : 1;
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
    DisableIcon();
    draggedPart = null;
  }

  // Sets icon, ensuring any old icon is Disposed
  private void EnableIcon(Part part, int resolution) {
    DisableIcon();
    icon = new KIS_IconViewer(part, resolution);
  }

  // Clears icon, ensuring it is Disposed
  private void DisableIcon() {
    if (icon != null) {
      icon.Dispose();
      icon = null;
    }
  }

  private void OnPointerState(KISAddonPointer.PointerTarget pTarget,
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
    InputLockManager.RemoveControlLock("KISPickup");
  }

  void MoveDrop(Part tgtPart, Vector3 pos, Quaternion rot) {
    DebugEx.Info("Move part");
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
    ModuleKISPickup modulePickup = GetActivePickupNearest(pos);
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
    if (modulePickup) {
      AudioSource.PlayClipAtPoint(
          GameDatabase.Instance.GetAudioClip(modulePickup.dropSndPath), pos);
    }
    return newPart;
  }

  void MoveAttach(Part tgtPart, Vector3 pos, Quaternion rot, string srcAttachNodeID = null,
                  AttachNode tgtAttachNode = null) {
    DebugEx.Info("Move part & attach");
    KIS_Shared.MoveAssembly(movingPart, srcAttachNodeID, tgtPart, tgtAttachNode, pos, rot);
    KISAddonPointer.StopPointer();
    movingPart = null;
    draggedItem = null;
    draggedPart = null;
  }

  Part CreateAttach(Part tgtPart, Vector3 pos, Quaternion rot,
                    string srcAttachNodeID = null, AttachNode tgtAttachNode = null) {
    DebugEx.Info("Create part & attach");
    Part newPart;
    draggedItem.StackRemove(1);
    bool useExternalPartAttach = false;
    if (draggedItem.prefabModule && draggedItem.prefabModule.useExternalPartAttach) {
      useExternalPartAttach = true;
    }
    if (tgtPart && !useExternalPartAttach) {
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
    if (GetActivePickupAll().Any()) {
      DebugEx.Info("Enable re-dock mode");
      KISAddonCursor.StartPartDetection(OnMouseRedockPartClick, OnMouseRedockEnterPart,
                                        null, OnMouseRedockExitPart);
      KISAddonCursor.CursorEnable(GrabIcon, ReDockOkStatusTooltipTxt, ReDockSelectVesselText);
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
        !CheckIsAttachable(redockTarget)) {
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
    // Check if there are kerbals in range.
    if (!HasActivePickupInRange(part)) {
      ReportCheckError(TooFarStatusTooltipTxt, TooFarTooltipTxt, cursorIcon: TooFarIcon);
      return false;
    }
    // Check if attached part has acceptable mass and can be detached.
    return CheckMass(part.transform.position, part, out grabbedPartsCount) && CheckCanDetach(part);
  }

  /// <summary>Calculates grabbed part/assembly mass and reports if it's too heavy.</summary>
  /// <param name="grabPosition">Position to search pick up modules around.</param>
  /// <param name="part">A part or assembly root to check mass for.</param>
  /// <param name="grabbedPartsCount">A return parameter to give number of parts in the assembly.
  /// </param>
  /// <param name="reportToConsole">If <c>true</c> then error is only reported on the screen (it's a
  /// game's "console"). Otherwise, excess of mass only results in changing cursor icon to
  /// <seealso cref="TooHeavyIcon"/>.</param>
  /// <returns><c>true</c> if total mass is within the limits.</returns>
  bool CheckMass(Vector3 grabPosition, Part part, out int grabbedPartsCount,
                 bool reportToConsole = false) {
    grabbedMass = KIS_Shared.GetAssemblyMass(part, out grabbedPartsCount);
    float pickupMaxMass = GetAllPickupMaxMassInRange(grabPosition);
    if (grabbedMass > pickupMaxMass) {
      ReportCheckError(TooHeavyStatusTooltipTxt,
                       TooHeavyTooltipTxt.Format(grabbedMass, pickupMaxMass),
                       cursorIcon: TooHeavyIcon,
                       reportToConsole: reportToConsole);
      return false;
    }
    return true;
  }

  /// <summary>Calculates grabbed part/assembly mass and reports if it's too heavy.</summary>
  /// <param name="grabPosition">Position to search pick up modules around.</param>
  /// <param name="item">Inventory item to check mass for.</param>
  /// <param name="reportToConsole">
  /// If <c>true</c> then error is only reported on the screen (it's a game's "console"). Otherwise,
  /// excess of mass only results in changing cursor icon to <seealso cref="TooHeavyIcon"/>.
  /// </param>
  /// <returns><c>true</c> if total mass is within the limits.</returns>
  bool CheckItemMass(Vector3 grabPosition, KIS_Item item, bool reportToConsole = false) {
    grabbedMass = item.totalMass;
    float pickupMaxMass = GetAllPickupMaxMassInRange(grabPosition);
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
    if (item) {
      // Handle KIS items.
      if (part.parent.GetComponent<ModuleKISPartMount>() != null) {
        // Check if part is a ground base.
        if (item.staticAttached
            && item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedWithKisTool
            && !HasActivePickupNearby(part, canStaticAttachOnly: true)) {
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
          if (!HasActivePickupNearby(part, canPartAttachOnly: true)) {
            rejectText = NeedToolToDetachTooltipTxt;
          }
        }
      }
    } else {
      // Handle regular game parts.
      if (!GetActivePickupNearest(part, canPartAttachOnly: true)) {
        rejectText = NeedToolToDetachTooltipTxt;
      }
    }
    if (rejectText != null) {
      ReportCheckError(NeedToolStatusTooltipTxt, rejectText, cursorIcon: NeedToolIcon);
      return false;
    }
          
    return true;
  }

  /// <summary>Checks if part can be attached. At least in theory.</summary>
  /// <remarks>This method doesn't say if part *will* be attached if such attempt is made.</remarks>   
  bool CheckIsAttachable(Part part, bool reportToConsole = false) {
    var item = part.GetComponent<ModuleKISItem>();

    // Check if part has at least one free node.
    var nodes = KIS_Shared.GetAvailableAttachNodes(part, part.parent);
    if (!nodes.Any()) {
      // Check if it's a static attachable item. Those are not required to have nodes
      // since they attach to the ground.
      if (!item || item.allowStaticAttach == ModuleKISItem.ItemAttachMode.Disabled) {
        ReportCheckError(AttachNotOkStatusTooltipTxt, CannotAttachTooltipTxt, reportToConsole);
        return false;
      }
    }

    // Check if KISItem part is allowed for attach without a tool.
    if (item && (item.allowPartAttach == ModuleKISItem.ItemAttachMode.AllowedAlways
                 || item.allowStaticAttach == ModuleKISItem.ItemAttachMode.AllowedAlways)) {
      return true;
    }

    // Check if there is a KISPickup module to handle the task.
    if (!HasActivePickupNearby(part, canPartAttachOnly: true)) {
      ReportCheckError(
          NeedToolStatusTooltipTxt, NeedToolToAttachTooltipTxt, reportToConsole, NeedToolIcon);
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
