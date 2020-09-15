// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KISAPIv1;
using KSPDev.GUIUtils;
using KSPDev.GUIUtils.TypeFormatters;
using KSPDev.ProcessingUtils;
using KSPDev.LogUtils;
using KSPDev.PartUtils;
using KSPDev.ModelUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace KIS {

// Next localization ID: #kisLOC_03009.
[KSPAddon(KSPAddon.Startup.Flight, false)]
sealed class KISAddonPointer : MonoBehaviour {
  #region Localizable GUI strings.
  static readonly Message TargetObjectNotAllowedMsg = new Message(
      "#kisLOC_03000",
      defaultTemplate: "Target object is not allowed!",
      description: "The message to present when the selected action cannot be completed given the"
      + " currently grabbed part/assembly and the target at which the mouse cursor is pointing.");

  static readonly Message CannotAttachOnItselfMsg = new Message(
      "#kisLOC_03001",
      defaultTemplate: "Cannot attach on itself!",
      description: "The message to present when the selected action cannot be completed due to"
      + " the source and the target are the same objects.");

  static readonly Message NotAllowedOnTheMountMsg = new Message(
      "#kisLOC_03002",
      defaultTemplate: "This part is not allowed on the mount!",
      description: "The message to present when a non-mountable object is attempted to be"
      + " mounted.");

  static readonly Message TargetDoesntAllowSurfaceAttachMsg = new Message(
      "#kisLOC_03003",
      defaultTemplate: "Target part doesn't allow surface attach!",
      description: "The message to present when the source object is attempted to be attached to"
      + " the target's surface of a part which doesn't allow this mode.");

  static readonly Message NodeNotForSurfaceAttachMsg = new Message(
      "#kisLOC_03004",
      defaultTemplate: "This node cannot be used for surface attach!",
      description: "The message to present when the source object is attempted to be attached to"
      + " the target's surface, but the selected node on the source is not 'surface'.");

  static readonly Message<DistanceType, DistanceType> TooFarFromSourceMsg =
      new Message<DistanceType, DistanceType>(
          "#kisLOC_03005",
          defaultTemplate:
          "Too far from source: <<1>> > <<2>>",
          description: "The message to present when the acting kerbal is too far from the part"
          + " he's trying to act on (source part)."
          + "\nArgument <<1>> is the actual distance between the kerbal and the source part."
          + " Format: DistanceType."
          + "\nArgument <<2>> is the maximum allowed distance. Format: DistanceType.");

  static readonly Message<DistanceType, DistanceType> TooFarFromTargetMsg =
      new Message<DistanceType, DistanceType>(
          "#kisLOC_03006",
          defaultTemplate: "Too far from target: <<1>> > <<2>>",
          description: "The message to present when the acting kerbal is too far from the point"
          + " of the actual action (drop or attach)."
          + "\nArgument <<1>> is the actual distance between the kerbal and the target part."
          + " Format: DistanceType."
          + "\nArgument <<2>> is the maximum allowed distance. Format: DistanceType.");

  static readonly Message<string> CannotAttachToPartMsg = new Message<string>(
      "#kisLOC_03007",
      defaultTemplate: "Cannot attach to part: <<1>>",
      description: "The message to present when a source object is attempted to be attached to a"
      + " target part which is not allowed for this. This message is shown when the source object"
      + " can only attach to a very specific set of the vessel's part (e.g. during the re-docking)."
      + "\nArgument <<1>> is the name of the target part.");

  static readonly Message OnlyOneAttachNodeMsg = new Message(
      "#kisLOC_03008",
      defaultTemplate: "This part has only one attach node!",
      description: "The message to present when a 'change attach node' action is requested, but"
      + " the source part has only one node");
  #endregion

  // Pointer parameters
  public static bool allowPart = false;
  public static bool allowEva = false;
  public static bool allowPartItself = false;
  public static bool allowStatic = false;

  public static Color colorNok = Color.red;
  public static Color colorOk = Color.green;
  public static Color colorDistNok = Color.yellow;
  public static Color colorStack = XKCDColors.Teal;
  public static Color colorMountOk = XKCDColors.SeaGreen;
  public static Color colorMountNok = XKCDColors.LightOrange;
  public static Color colorWrong = XKCDColors.Teal;
      
  /// <summary>
  /// Defines parts that are allowed to be a target for "attach" action. Attaching to a part
  /// that is not in the set will be forbidden. When set to <c>null</c> any part can be a
  /// target.
  /// </summary>
  /// <remarks>
  /// <para>Assigning a value to the property does immediate highlighting of the allowed parts
  /// with <seealso cref="colorMountOk"/> color. Highlighting is only shown when pointer is
  /// visible, and pointer module takes care to correctly show/hide the selection when pointer
  /// is created/destroyed.</para>
  /// <para>Pointer visibility state doesn't affect parts selection.</para>
  /// </remarks>
  public static HashSet<Part> allowedAttachmentParts {
    get {
      return _allowedAttachmentParts;
    }
    set {
      // Erase old selection if pointer stopped or new selection set.
      if (_allowedAttachmentParts != null && (!running || _allowedAttachmentParts != value)) {
        foreach (var p in _allowedAttachmentParts) {
          p.SetHighlightDefault();
        }
      }
      _allowedAttachmentParts = value;
      // Highglight allowed parts if pointer is active.
      if (running && _allowedAttachmentParts != null) {
        foreach (var p in _allowedAttachmentParts) {
          p.SetHighlightColor(colorMountOk);
          p.SetHighlight(true, false);
          p.SetHighlightType(Part.HighlightType.AlwaysOn);
        }
      }
    }
  }
  static HashSet<Part> _allowedAttachmentParts;

  public static bool allowMount {
    get {
      return _allowMount;
    }
    set {
      ResetMouseOver();
      _allowMount = value;
    }
  }
  static bool _allowMount;

  public static bool allowStack {
    get {
      return _allowStack;
    }
    set {
      ResetMouseOver();
      _allowStack = value;
    }
  }
  static bool _allowStack;

  public static Part partToAttach;
  public static float maxDist = 2f;
  public static bool useAttachRules;
  static Transform sourceTransform;
  static RaycastHit hit;

  public static bool allowOffset = false;
  public static string offsetUpKey = "b";
  public static string offsetDownKey = "n";
  public static float maxOffsetDist = 0.5f;
  public static float aboveOffsetStep = 0.05f;

  static bool running = false;
  static Part hoveredPart = null;
  public static AttachNode hoveredNode = null;
  static GameObject pointer;
  static readonly List<Renderer> allModelRenderers = new List<Renderer>();
  static Vector3 customRot = new Vector3(0f, 0f, 0f);
  static float aboveDistance = 0;
  static float aboveAutoOffset = 0;
  static Transform pointerNodeTransform;
  static List<AttachNode> attachNodes = new List<AttachNode>();
  static float []autoOffsets;

  /// <summary>Index of the current node on the picked up part to attach with.</summary>
  /// <remarks>
  /// It's an index in the <see cref="attachNodes"/> colelction. Don't get over the maximum number
  /// of the items there.
  /// </remarks>
  /// <seealso cref="currentAttachNode"/>
  static int attachNodeIndex {
    get { return _attachNodeIndex; }
    set {
      if (value > attachNodes.Count - 1) {
        DebugEx.Error(
            "Cannot set node index to {0}! The max value is {1}", value, attachNodes.Count - 1);
        _attachNodeIndex = attachNodes.Count - 1;
      } else {
        _attachNodeIndex = value;
      }
      currentAttachNode = attachNodes[value];
      aboveAutoOffset = 0;
      if (autoOffsets != null) {
        aboveAutoOffset = autoOffsets[value];
      }
    }
  }
  static int _attachNodeIndex;

  /// <summary>Current node on the picked up part to attach with.</summary>
  /// <value>The attach node to couple with on the picked-up part.</value>
  public static AttachNode currentAttachNode {
    get { return _currentAttachNode; }
    private set {
      _currentAttachNode = value;
      pointerNodeTransform.localPosition = value.position;
      pointerNodeTransform.localRotation = KIS_Shared.GetNodeRotation(value);
    }
  }
  static AttachNode _currentAttachNode;

  public static PointerTarget pointerTarget = PointerTarget.Nothing;
  public enum PointerTarget {
    Nothing,
    Static,
    StaticRb,
    Part,
    PartNode,
    PartMount,
    KerbalEva
  }
  private static OnPointerClick SendPointerClick;
  
  public delegate void OnPointerClick(PointerTarget pTarget, Vector3 pos, Quaternion rot,
                                      Part pointerPart, string SrcAttachNodeID = null,
                                      AttachNode tgtAttachNode = null);

  public enum PointerState {
    OnMouseEnterPart,
    OnMouseExitPart,
    OnMouseEnterNode,
    OnMouseExitNode,
    OnChangeAttachNode,
    OnPointerStarted,
    OnPointerStopped,
  }
  private static OnPointerState SendPointerState;

  public delegate void OnPointerState(PointerTarget pTarget, PointerState pState,
                                      Part hoverPart, AttachNode hoverNode);

  public static bool isRunning {
    get { return running; }
  }

  public static void StartPointer(Part rootPart, KIS_Item item,
                                  OnPointerClick pClick, OnPointerState pState,
                                  Transform from = null) {
    if (!running) {
      DebugEx.Fine("StartPointer()");
      customRot = Vector3.zero;
      aboveDistance = 0;
      partToAttach = rootPart;
      sourceTransform = from;
      running = true;
      SendPointerClick = pClick;
      SendPointerState = pState;

      if (rootPart) {
        MakePointer(rootPart);
      } else {
        VariantsUtils.ExecuteAtPartVariant(
            item.availablePart,
            VariantsUtils.GetCurrentPartVariant(item.availablePart, item.partNode),
            MakePointer);
        pointer.transform.localScale *=
            (float) KISAPI.PartNodeUtils.GetTweakScaleSizeModifier(item.partNode);
      }
             
      LockUI();
      allowedAttachmentParts = allowedAttachmentParts;  // Apply selection.
      SendPointerState(PointerTarget.Nothing, PointerState.OnPointerStarted, null, null);
    }
  }

  /// <summary>Cancels active pointing mode and returns pointer to normal view.</summary>
  /// <param name="unlockUI">
  /// If <c>false</c> then input lock will not be released. Caller is responsible to release it,
  /// otherwise game's input will be blocked.
  /// </param>
  /// <seealso cref="UnlockUI"/>
  public static void StopPointer(bool unlockUI = true) {
    DebugEx.Fine("StopPointer()");
    running = false;
    ResetMouseOver();
    SendPointerState(PointerTarget.Nothing, PointerState.OnPointerStopped, null, null);
    if (unlockUI) {
      UnlockUI();
    }
    DestroyPointer();
    allowedAttachmentParts = allowedAttachmentParts; // Clear selection.
  }

  /// <summary>Acquires KIS input lock on UI interactions.</summary>
  public static void LockUI() {
    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "KISpointer");
    DebugEx.Info("KIS UI lock acquired");
  }

  /// <summary>Releases KIS input lock on UI interactions.</summary>
  public static void UnlockUI() {
    InputLockManager.RemoveControlLock("KISpointer");
    DebugEx.Info("KIS UI lock released");
  }

  public void Update() {
    UpdateHoverDetect();
    UpdatePointer();
    UpdateKey();
  }

  /// <summary>Handles everything realted to the pointer.</summary>
  public static void UpdateHoverDetect() {
    if (isRunning) {
      //Cast ray
      Ray ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
      var colliderHit = Physics.Raycast(
          ray, out hit, maxDistance: 500,
          layerMask: (int)(KspLayerMask.Part | KspLayerMask.Kerbal | KspLayerMask.SurfaceCollider),
          queryTriggerInteraction: QueryTriggerInteraction.Ignore);
      if (!colliderHit) {
        pointerTarget = PointerTarget.Nothing;
        ResetMouseOver();
        return;
      }

      // Check target type
      var tgtPart = Mouse.HoveredPart;
      KerbalEVA tgtKerbalEva = null;
      AttachNode tgtAttachNode = null;

      if (!tgtPart) {
        // check linked part
        KIS_LinkedPart linkedObject = hit.collider.gameObject.GetComponent<KIS_LinkedPart>();
        if (linkedObject) {
          tgtPart = linkedObject.part;
        }
      }
      if (tgtPart) {
        tgtKerbalEva = tgtPart.GetComponent<KerbalEVA>();
      }

      // If rigidbody
      if (hit.rigidbody && !tgtPart && !tgtKerbalEva) {
        pointerTarget = PointerTarget.StaticRb;
      }

      // If kerbal
      if (tgtKerbalEva) {
        pointerTarget = PointerTarget.KerbalEva;
      }

      // If part
      if (tgtPart && !tgtKerbalEva) {
        float currentDist = Mathf.Infinity;
        foreach (AttachNode an in tgtPart.attachNodes) {
          if (an.icon) {
            float dist;
            var cameraToMouseRay =
                FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
            var iconRenderer = an.icon.GetComponent<Renderer>();
            if (iconRenderer.bounds.IntersectRay(cameraToMouseRay, out dist)) {
              if (dist < currentDist) {
                tgtAttachNode = an;
                currentDist = dist;
              }
            }
          }
        }
        if (tgtAttachNode != null) {
          if (tgtAttachNode.icon.name == "KISMount") {
            pointerTarget = PointerTarget.PartMount;
          } else {
            pointerTarget = PointerTarget.PartNode;
          }
        } else {
          pointerTarget = PointerTarget.Part;
        }
      }

      //if nothing
      if (!hit.rigidbody && !tgtPart && !tgtKerbalEva) {
        pointerTarget = PointerTarget.Static;
      }

      if (tgtPart) {
        if (tgtAttachNode != null) {
          // OnMouseEnter node
          if (tgtAttachNode != hoveredNode) {
            if (hoveredNode != null) {
              OnMouseExitNode(hoveredNode);
            }
            OnMouseEnterNode(tgtAttachNode);
            hoveredNode = tgtAttachNode;
          }
        } else {
          // OnMouseExit node
          if (tgtAttachNode != hoveredNode) {
            OnMouseExitNode(hoveredNode);
            hoveredNode = null;
          }
        }

        // OnMouseEnter part
        if (tgtPart != hoveredPart) {
          if (hoveredPart) {
            OnMouseExitPart(hoveredPart);
          }
          OnMouseEnterPart(tgtPart);
          hoveredPart = tgtPart;
        }
      } else {
        // OnMouseExit part
        if (tgtPart != hoveredPart) {
          OnMouseExitPart(hoveredPart);
          hoveredPart = null;
        }
      }
    }
  }

  static void OnMouseEnterPart(Part hoverPart) {
    if (hoverPart == partToAttach) {
      return;
    }

    if (allowMount) {
      ModuleKISPartMount pMount = hoverPart.GetComponent<ModuleKISPartMount>();
      if (pMount) {
        // Set current attach node 
        AttachNode an = attachNodes.Find(f => f.id == pMount.mountedPartNode);
        if (an != null) {
          attachNodeIndex = attachNodes.IndexOf(an);
          SetPointerVisible(false);
        } else {
          SetPointerVisible(true);
        }
        // Init attach node
        foreach (KeyValuePair<AttachNode, List<string>> mount in pMount.GetMounts()) {
          if (!mount.Key.attachedPart) {
            KIS_Shared.AssignAttachIcon(hoverPart, mount.Key, colorMountOk, "KISMount");
          }
        }
      }
    }
    if (allowStack && currentAttachNode.nodeType != AttachNode.NodeType.Surface) {
      var variant = VariantsUtils.GetCurrentPartVariant(hoverPart);
      if (variant != null) {
        VariantsUtils.ApplyVariantOnAttachNodes(hoverPart, variant);
      }
      foreach (var an in KIS_Shared.GetAvailableAttachNodes(hoverPart, needSrf: false)) {
        KIS_Shared.AssignAttachIcon(hoverPart, an, colorStack);
      }
    }
    SendPointerState(pointerTarget, PointerState.OnMouseEnterPart, hoverPart, null);
  }

  static void OnMouseExitPart(Part hoverPart) {
    if (hoverPart == partToAttach) {
      return;
    }
    foreach (AttachNode an in hoverPart.attachNodes) {
      if (an.icon) {
        an.icon.DestroyGameObject();
      }
    }
    SendPointerState(pointerTarget, PointerState.OnMouseExitPart, hoverPart, null);
  }

  static void OnMouseEnterNode(AttachNode hoverNode) {
    SendPointerState(pointerTarget, PointerState.OnMouseEnterNode, hoverNode.owner, hoverNode);
  }

  static void OnMouseExitNode(AttachNode hoverNode) {
    SendPointerState(pointerTarget, PointerState.OnMouseExitNode, hoverNode.owner, hoverNode);
  }

  static void ResetMouseOver() {
    if (hoveredNode != null) {
      OnMouseExitNode(hoveredNode);
      hoveredNode = null;
    }
    if (hoveredPart) {
      OnMouseExitPart(hoveredPart);
      hoveredPart = null;
    }
  }

  public void UpdatePointer() {
    // Stop pointer on map
    if (running && MapView.MapIsEnabled) {
      StopPointer();
      return;
    }

    // Remove pointer if not running.
    if (!running) {
      DestroyPointer();
      return;
    }

    // Hide pointer if the raycast do not hit anything.
    if (pointerTarget == PointerTarget.Nothing) {
      SetPointerVisible(false);
      return;
    }

    SetPointerVisible(true);
          
    // Custom rotation
    float rotDegree = 15;
    if (Input.GetKey(KeyCode.LeftShift)) {
      rotDegree = 1;
    }
    if (GameSettings.Editor_rollLeft.GetKeyDown()) {
      customRot -= new Vector3(0, -1, 0) * rotDegree;
    }
    if (GameSettings.Editor_rollRight.GetKeyDown()) {
      customRot += new Vector3(0, -1, 0) * rotDegree;
    }
    if (GameSettings.Editor_pitchDown.GetKeyDown()) {
      customRot -= new Vector3(1, 0, 0) * rotDegree;
    }
    if (GameSettings.Editor_pitchUp.GetKeyDown()) {
      customRot += new Vector3(1, 0, 0) * rotDegree;
    }
    if (GameSettings.Editor_yawLeft.GetKeyDown()) {
      customRot -= new Vector3(0, 0, 1) * rotDegree;
    }
    if (GameSettings.Editor_yawRight.GetKeyDown()) {
      customRot += new Vector3(0, 0, 1) * rotDegree;
    }
    if (GameSettings.Editor_resetRotation.GetKeyDown()) {
      customRot = new Vector3(0, 0, 0);
    }
    Quaternion rotAdjust =
        Quaternion.Euler(0, 0, customRot.z) * Quaternion.Euler(customRot.x, customRot.y, 0);

    // Move to position
    if (pointerTarget == PointerTarget.PartMount) {
      //Mount snap
      KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hoveredNode.nodeTransform);
    } else if (pointerTarget == PointerTarget.PartNode) {
      //Part node snap
      KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform,
                           hoveredNode.nodeTransform, rotAdjust);
    } else {
      KIS_Shared.MoveAlign(pointer.transform, pointerNodeTransform, hit, rotAdjust);
    }

    // Move above
    if (allowOffset) {
      if (pointerTarget != PointerTarget.PartMount) {
        if (KIS_Shared.IsKeyDown(offsetUpKey) && aboveDistance < maxOffsetDist) {
          aboveDistance += aboveOffsetStep;
        }
        if (KIS_Shared.IsKeyDown(offsetDownKey) && aboveDistance > -maxOffsetDist) {
          aboveDistance -= aboveOffsetStep;
        }
        if (GameSettings.Editor_resetRotation.GetKeyDown()) {
          aboveDistance = 0;
        }
        pointer.transform.position =
            pointer.transform.position + (hit.normal.normalized * (aboveDistance + aboveAutoOffset));
      }
    }

    //Check distance
    float sourceDist = 0;
    if (sourceTransform) {
      sourceDist =
          Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, sourceTransform.position);
    }
    float targetDist = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, hit.point);

    //Set color
    Color color = colorOk;
    bool invalidTarget = false;
    bool notAllowedOnMount = false;
    bool cannotSurfaceAttach = false;
    bool invalidCurrentNode = false;
    bool itselfIsInvalid = !allowPartItself
        && partToAttach != null && partToAttach.hasIndirectChild(hoveredPart);
    bool restrictedPart =
      allowedAttachmentParts != null && !allowedAttachmentParts.Contains(hoveredPart);
    switch (pointerTarget) {
      case PointerTarget.Static:
      case PointerTarget.StaticRb:
        invalidTarget = !allowStatic;
        break;
      case PointerTarget.KerbalEva:
        invalidTarget = !allowEva;
        break;
      case PointerTarget.Part:
        if (allowPart) {
          if (useAttachRules) {
            if (hoveredPart.attachRules.allowSrfAttach) {
              invalidCurrentNode = currentAttachNode.nodeType != AttachNode.NodeType.Surface;
            } else {
              cannotSurfaceAttach = true;
            }
          }
        } else {
          invalidTarget = true;
        }
        break;
      case PointerTarget.PartMount:
        if (allowMount) {
          ModuleKISPartMount pMount = hoveredPart.GetComponent<ModuleKISPartMount>();
          var allowedPartNames = new List<string>();
          pMount.GetMounts().TryGetValue(hoveredNode, out allowedPartNames);
          notAllowedOnMount =
              partToAttach != null && !allowedPartNames.Contains(partToAttach.partInfo.name);
          color = colorMountOk;
        }
        break;
      case PointerTarget.PartNode:
        invalidTarget = !allowStack;
        color = colorStack;
        break;
    }
          
    // Handle generic "not OK" color. 
    if (sourceDist > maxDist || targetDist > maxDist) {
      color = colorDistNok;
    } else if (invalidTarget || cannotSurfaceAttach || invalidCurrentNode
               || itselfIsInvalid || restrictedPart) {
      color = colorNok;
    }
          
    color.a = 0.5f;
    foreach (var mr in allModelRenderers) {
      mr.material.color = color;
    }

    //On click.
    if (Input.GetMouseButtonDown(0)) {
      if (invalidTarget) {
        ScreenMessaging.ShowInfoScreenMessage(TargetObjectNotAllowedMsg);
        UISounds.PlayBipWrong();
      } else if (itselfIsInvalid) {
        ScreenMessaging.ShowInfoScreenMessage(CannotAttachOnItselfMsg);
        UISounds.PlayBipWrong();
      } else if (notAllowedOnMount) {
        ScreenMessaging.ShowInfoScreenMessage(NotAllowedOnTheMountMsg);
        UISounds.PlayBipWrong();
      } else if (cannotSurfaceAttach) {
        ScreenMessaging.ShowInfoScreenMessage(TargetDoesntAllowSurfaceAttachMsg);
        UISounds.PlayBipWrong();
      } else if (invalidCurrentNode) {
        ScreenMessaging.ShowInfoScreenMessage(NodeNotForSurfaceAttachMsg);
        UISounds.PlayBipWrong();
      } else if (sourceDist > maxDist) {
        ScreenMessaging.ShowInfoScreenMessage(TooFarFromSourceMsg.Format(sourceDist, maxDist));
        UISounds.PlayBipWrong();
      } else if (targetDist > maxDist) {
        ScreenMessaging.ShowInfoScreenMessage(TooFarFromTargetMsg.Format(targetDist, maxDist));
        UISounds.PlayBipWrong();
      } else if (restrictedPart) {
        ScreenMessaging.ShowInfoScreenMessage(
            CannotAttachToPartMsg.Format(hoveredPart.partInfo.title));
        UISounds.PlayBipWrong();
      } else {
        SendPointerClick(pointerTarget, pointer.transform.position, pointer.transform.rotation,
                         hoveredPart, currentAttachNode.id, hoveredNode);
      }
    }
  }

  /// <summary>Handles keyboard input.</summary>
  private void UpdateKey() {
    if (isRunning) {
      if (KIS_Shared.IsKeyUp(KeyCode.Escape) || KIS_Shared.IsKeyDown(KeyCode.Return)) {
        DebugEx.Fine("Cancel key pressed, stop eva attach mode");
        StopPointer(unlockUI: false);
        SendPointerClick(PointerTarget.Nothing, Vector3.zero, Quaternion.identity, null, null);
        // Delay unlocking to not let ESC be handled by the game.
        AsyncCall.CallOnEndOfFrame(this, UnlockUI);
      }
      if (GameSettings.Editor_toggleSymMethod.GetKeyDown()) {  // "R" by default.
        if (pointerTarget != PointerTarget.PartMount && attachNodes.Count() > 1) {
          if (attachNodeIndex < attachNodes.Count - 1) {
            attachNodeIndex++;
          } else {
            attachNodeIndex = 0;
          }
          DebugEx.Fine("Attach node index changed to: {0}", attachNodeIndex);
          ResetMouseOver();
          SendPointerState(pointerTarget, PointerState.OnChangeAttachNode, null, null);
        } else {
          ScreenMessaging.ShowInfoScreenMessage(OnlyOneAttachNodeMsg);
          UISounds.PlayBipWrong();
        }
      }
    }
  }

  /// <summary>Sets current pointer visible state.</summary>
  /// <remarks>
  /// Method expects all or none of the objects in the pointer to be visible: pointer
  /// visiblity state is determined by checking the first <c>MeshRenderer</c> only.
  /// </remarks>
  /// <param name="isVisible">New state.</param>
  /// <exception cref="InvalidOperationException">If pointer doesn't exist.</exception>
  private static void SetPointerVisible(bool isVisible) {
    foreach (var mr in allModelRenderers) {
      if (mr.enabled == isVisible
          && mr.material.renderQueue == KIS_Shared.HighlighedPartRenderQueue) {
        return;  // Abort if current state is already up to date.
      }
      mr.enabled = isVisible;
      mr.material.renderQueue = KIS_Shared.HighlighedPartRenderQueue;
    }
    DebugEx.Fine("Pointer state set to: visibility={0}", isVisible);
  }

  static int CompareDistance(RaycastHit a, RaycastHit b)
  {
    return a.distance.CompareTo(b.distance);
  }

  static void CreateAutoOffsets(Part part, List<Collider> colliders) {
    float distance = 1;
    int layerMask = (int)KspLayerMask.Part;
    int index = 0;
    var triggers = QueryTriggerInteraction.Ignore;

    autoOffsets = new float[attachNodes.Count];
    foreach (var node in attachNodes) {
      autoOffsets[index] = 0;
      Vector3 pos = part.transform.TransformPoint(node.position);
      Vector3 dir = part.transform.TransformDirection(node.orientation);

      var ray = new Ray(pos + distance * dir, -dir);
      var hits = Physics.RaycastAll(ray, distance, layerMask, triggers);
      Array.Sort(hits, CompareDistance);
      foreach (var hit in hits) {
        if (colliders.Contains (hit.collider)) {
          autoOffsets[index] = distance - hit.distance;
          break;
        }
      }
      index++;
    }
  }

  /// <summary>Makes a game object to represent currently dragging assembly.</summary>
  /// <remarks>It's a very expensive operation.</remarks>
  static void MakePointer(Part rootPart) {
    DestroyPointer();

    // Make pointer node transformations.
    if (pointerNodeTransform) {
      pointerNodeTransform.gameObject.DestroyGameObject();
    }
    pointerNodeTransform = new GameObject("KISPointerPartNode").transform;

    // Deatch will decouple from the parent so, ask to ignore it when looking for the nodes.
    attachNodes =
        KIS_Shared.GetAvailableAttachNodes(rootPart, ignoreAttachedPart: rootPart.parent)
        .Select(AttachNode.Clone)
        .ToList();
    if (!attachNodes.Any()) {
      //TODO: When there are no nodes try finding ones in the parent or in the children.
      // Ideally, the caller should have checked if this part has free nodes. Now the only
      // way is to pick *any* node. The surface one always exists so, it's a good
      // candidate. However, for many details it may result in a weird representation.
      DebugEx.Error(
          "Part {0} has no free nodes, use {1}", rootPart, rootPart.srfAttachNode);
      attachNodes.Add(AttachNode.Clone(rootPart.srfAttachNode));
    }
    attachNodeIndex = 0;  // Expect that first node is the best default.

    // Collect models from all the part in the assembly.
    pointer = new GameObject("KISPointer");
    var model = KISAPI.PartUtils.GetSceneAssemblyModel(rootPart, keepColliders: true);
    var colliders = model.GetComponentsInChildren<Collider>().ToList();
    CreateAutoOffsets (rootPart, colliders);
    aboveAutoOffset = autoOffsets[attachNodeIndex];
    foreach (var collider in colliders) {
      UnityEngine.Object.DestroyImmediate (collider);
    }
    model.transform.parent = pointer.transform;
    model.transform.position = Vector3.zero;
    model.transform.rotation = Quaternion.identity;

    allModelRenderers.Clear();
    allModelRenderers.AddRange(pointer.GetComponentsInChildren<Renderer>());
    foreach (var renderer in allModelRenderers) {
      renderer.material = new Material(Shader.Find("Transparent/Diffuse"));
      renderer.shadowCastingMode = ShadowCastingMode.Off;
      renderer.receiveShadows = false;
    }
    pointerNodeTransform.parent = pointer.transform;

    DebugEx.Fine("New pointer created");
  }

  /// <summary>Destroyes object(s) allocated to represent a pointer.</summary>
  /// <remarks>When making pointer for a complex hierarchy a lot of different resources may be
  /// allocated/dropped. Destroying each one of them can be too slow so, cleanup is done in
  /// one call to <c>UnloadUnusedAssets()</c>.
  /// <para>This method also destroys <see cref="pointerNodeTransform"/>.</para>
  /// </remarks>
  private static void DestroyPointer() {
    if (!pointer) {
      return;  // Nothing to do.
    }
    pointer.DestroyGameObject();
    pointer = null;
    pointerNodeTransform.gameObject.DestroyGameObject();
    pointerNodeTransform = null;
    allModelRenderers.Clear();

    // On large assemblies memory consumption can be significant. Reclaim it.
    Resources.UnloadUnusedAssets();
    DebugEx.Fine("Pointer destroyed");
  }
}

}  // namespace
