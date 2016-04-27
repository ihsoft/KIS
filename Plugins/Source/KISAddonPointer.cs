using KSPDev.LogUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace KIS {

[KSPAddon(KSPAddon.Startup.Flight, false)]
public class KISAddonPointer : MonoBehaviour {
  public GameObject audioGo = new GameObject();
  public AudioSource audioBipWrong = new AudioSource();
  public static GameObject soundGo;

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
  private static HashSet<Part> _allowedAttachmentParts;

  private static bool _allowMount = false;
  public static bool allowMount {
    get {
      return _allowMount;
    }
    set {
      ResetMouseOver();
      _allowMount = value;
    }
  }

  private static bool _allowStack = false;
  public static bool allowStack {
    get {
      return _allowStack;
    }
    set {
      ResetMouseOver();
      _allowStack = value;
    }
  }

  public static Part partToAttach;
  public static float scale = 1;
  public static float maxDist = 2f;
  public static bool useAttachRules = false;
  private static Transform sourceTransform;
  private static RaycastHit hit;

  public static bool allowOffset = false;
  public static string offsetUpKey = "b";
  public static string offsetDownKey = "n";
  public static float maxOffsetDist = 0.5f;
  public static float aboveOffsetStep = 0.05f;

  private static bool running = false;
  private static Part hoveredPart = null;
  public static AttachNode hoveredNode = null;
  private static GameObject pointer;
  private static List<MeshRenderer> allModelMr;
  private static Vector3 customRot = new Vector3(0f, 0f, 0f);
  private static float aboveDistance = 0;
  private static Transform pointerNodeTransform;
  private static List<AttachNode> attachNodes = new List<AttachNode>();
  private static int attachNodeIndex;

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
    OnChangeAttachNode
  }
  private static OnPointerState SendPointerState;

  public delegate void OnPointerState(PointerTarget pTarget, PointerState pState,
                                      Part hoverPart, AttachNode hoverNode);

  public static bool isRunning {
    get { return running; }
  }

  void Awake() {
    audioBipWrong = audioGo.AddComponent<AudioSource>();
    audioBipWrong.volume = GameSettings.UI_VOLUME;
    audioBipWrong.spatialBlend = 0;  //set as 2D audiosource

    if (GameDatabase.Instance.ExistsAudioClip(KIS_Shared.bipWrongSndPath)) {
      audioBipWrong.clip = GameDatabase.Instance.GetAudioClip(KIS_Shared.bipWrongSndPath);
    } else {
      Logger.logError("Awake(AttachPointer) Bip wrong sound not found in the game database !");
    }
  }

  public static void StartPointer(Part partToMoveAndAttach, OnPointerClick pClick,
                                  OnPointerState pState, Transform from = null) {
    if (!running) {
      Logger.logInfo("StartPointer()");
      customRot = Vector3.zero;
      aboveDistance = 0;
      partToAttach = partToMoveAndAttach;
      sourceTransform = from;
      running = true;
      SendPointerClick = pClick;
      SendPointerState = pState;

      MakePointer();
             
      InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "KISpointer");
      allowedAttachmentParts = allowedAttachmentParts; // Apply selection.
    }
  }

  public static void StopPointer() {
    Logger.logInfo("StopPointer()");
    running = false;
    ResetMouseOver();
    InputLockManager.RemoveControlLock("KISpointer");
    DestroyPointer();
    allowedAttachmentParts = allowedAttachmentParts; // Clear selection.
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
      var colliderHit =
          Physics.Raycast(ray, out hit, maxDistance: 500, layerMask: (int)KspLayers.COMMON);
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
    if (hoverPart == partToAttach)
      return;
    if (allowMount) {
      ModuleKISPartMount pMount = hoverPart.GetComponent<ModuleKISPartMount>();
      if (pMount) {
        // Set current attach node 
        AttachNode an = attachNodes.Find(f => f.id == pMount.mountedPartNode);
        if (an != null) {
          attachNodeIndex = attachNodes.FindIndex(f => f.id == pMount.mountedPartNode);
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
    if (allowStack && GetCurrentAttachNode().nodeType != AttachNode.NodeType.Surface) {
      foreach (var an in KIS_Shared.GetAvailableAttachNodes(hoverPart, needSrf:false)) {
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
        Destroy(an.icon);
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
    if (hoveredPart) {
      OnMouseExitPart(hoveredPart);
      hoveredPart = null;
    }
    if (hoveredNode != null) {
      OnMouseExitNode(hoveredNode);
      hoveredNode = null;
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
      pointer.transform.rotation *= Quaternion.Euler(hoveredNode.orientation);
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
        if (Input.GetKeyDown(offsetUpKey) && aboveDistance < maxOffsetDist) {
          aboveDistance += aboveOffsetStep;
        }
        if (Input.GetKeyDown(offsetDownKey) && aboveDistance > -maxOffsetDist) {
          aboveDistance -= aboveOffsetStep;
        }
        if (GameSettings.Editor_resetRotation.GetKeyDown()) {
          aboveDistance = 0;
        }
        pointer.transform.position =
            pointer.transform.position + (hit.normal.normalized * aboveDistance);
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
    bool itselfIsInvalid =
      !allowPartItself && KIS_Shared.IsSameHierarchyChild(partToAttach, hoveredPart);
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
              invalidCurrentNode = GetCurrentAttachNode().nodeType != AttachNode.NodeType.Surface;
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
          notAllowedOnMount = !allowedPartNames.Contains(partToAttach.partInfo.name);
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
    foreach (MeshRenderer mr in allModelMr) {
      mr.material.color = color;
    }

    //On click.
    if (Input.GetMouseButtonDown(0)) {
      if (invalidTarget) {
        KIS_Shared.ShowRightScreenMessage("Target object is not allowed !");
        audioBipWrong.Play();
      } else if (itselfIsInvalid) {
        KIS_Shared.ShowRightScreenMessage("Cannot attach on itself !");
        audioBipWrong.Play();
      } else if (notAllowedOnMount) {
        KIS_Shared.ShowRightScreenMessage("This part is not allowed on the mount !");
        audioBipWrong.Play();
      } else if (cannotSurfaceAttach) {
        KIS_Shared.ShowRightScreenMessage("Target part do not allow surface attach !");
        audioBipWrong.Play();
      } else if (invalidCurrentNode) {
        KIS_Shared.ShowRightScreenMessage(
          "This node cannot be used for surface attach !");
        audioBipWrong.Play();
      } else if (sourceDist > maxDist) {
        KIS_Shared.ShowRightScreenMessage("Too far from source: {0:F3}m > {1:F3}m",
          sourceDist, maxDist);
        audioBipWrong.Play();
      } else if (targetDist > maxDist) {
        KIS_Shared.ShowRightScreenMessage("Too far from target: {0:F3}m > {1:F3}m",
          targetDist, maxDist);
        audioBipWrong.Play();
      } else if (restrictedPart) {
        KIS_Shared.ShowRightScreenMessage("Cannot attach to part: {0}", hoveredPart);
        audioBipWrong.Play();
      } else {
        SendPointerClick(pointerTarget, pointer.transform.position, pointer.transform.rotation,
                         hoveredPart, GetCurrentAttachNode().id, hoveredNode);
      }
    }
  }

  /// <summary>Handles keyboard input.</summary>
  private void UpdateKey() {
    if (isRunning) {
      if (Input.GetKeyDown(KeyCode.Escape)
          || Input.GetKeyDown(KeyCode.Return)) {
        Logger.logInfo("Cancel key pressed, stop eva attach mode");
        StopPointer();
        SendPointerClick(PointerTarget.Nothing, Vector3.zero, Quaternion.identity, null, null);
      }
      if (GameSettings.Editor_toggleSymMethod.GetKeyDown()) {  // "R" by default.
        if (pointerTarget != PointerTarget.PartMount && attachNodes.Count() > 1) {
          attachNodeIndex++;
          if (attachNodeIndex > (attachNodes.Count - 1)) {
            attachNodeIndex = 0;
          }
          Logger.logInfo("Attach node index changed to: {0}", attachNodeIndex);
          UpdatePointerAttachNode();
          ResetMouseOver();
          SendPointerState(pointerTarget, PointerState.OnChangeAttachNode, null, null);
        } else {
          KIS_Shared.ShowRightScreenMessage("This part has only one attach node!");
          audioBipWrong.Play();
        }
      }
    }
  }

  public static AttachNode GetCurrentAttachNode() {
    return attachNodes[attachNodeIndex];
  }

  /// <summary>Sets current pointer visible state.</summary>
  /// <remarks>
  /// Method expects all or none of the objects in the pointer to be visible: pointer
  /// visiblity state is determined by checking the first <c>MeshRenderer</c> only.
  /// </remarks>
  /// <param name="isVisible">New state.</param>
  /// <exception cref="InvalidOperationException">If pointer doesn't exist.</exception>
  private static void SetPointerVisible(bool isVisible) {
    if (!pointer) {
      throw new InvalidOperationException("Pointer doesn't exist");
    }
    foreach (var mr in pointer.GetComponentsInChildren<MeshRenderer>()) {
      if (mr.enabled == isVisible
          && mr.material.renderQueue == KIS_Shared.HighlighedPartRenderQueue) {
        return;  // Abort if current state is already up to date.
      }
      mr.enabled = isVisible;
      mr.material.renderQueue = KIS_Shared.HighlighedPartRenderQueue;
    }
    Logger.logInfo("Pointer state set to: visibility={0}", isVisible);
  }

  /// <summary>Makes a game object to represent currently dragging assembly.</summary>
  /// <remarks>It's a very expensive operation.</remarks>
  private static void MakePointer() {
    DestroyPointer();
    MakePointerAttachNodes();
          
    var combines = new List<CombineInstance>();
    if (!partToAttach.GetComponentInChildren<MeshFilter>()) {
      CollectMeshesFromPrefab(partToAttach, combines);
    } else {
      CollectMeshesFromAssembly(
          partToAttach, partToAttach.transform.localToWorldMatrix.inverse, combines);
    }

    pointer = new GameObject("KISPointer");

    // Create one filter per mesh in the hierarhcy. Simple combining all meshes into one
    // larger mesh may have weird representation artifacts on different video cards.
    foreach (var combine in combines) {
      var mesh = new Mesh();
      mesh.CombineMeshes(new CombineInstance[] { combine });
      var childObj = new GameObject("KISPointerChildMesh");

      var meshRenderer = childObj.AddComponent<MeshRenderer>();
      meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
      meshRenderer.receiveShadows = false;

      var filter = childObj.AddComponent<MeshFilter>();
      filter.sharedMesh = mesh;
              
      childObj.transform.parent = pointer.transform;
    }

    allModelMr = new List<MeshRenderer>(pointer.GetComponentsInChildren<MeshRenderer>());
    foreach (var mr in allModelMr) {
      mr.material = new Material(Shader.Find("Transparent/Diffuse"));
    }

    pointerNodeTransform.parent = pointer.transform;
    Logger.logInfo("Pointer created");
  }

  /// <summary>Sets possible attach nodes in <c>attachNodes</c>.</summary>
  /// <exception cref="InvalidOperationException">
  /// If part has no valid attachment nodes.
  /// </exception>
  private static void MakePointerAttachNodes() {
    // Make node transformations.
    if (pointerNodeTransform) { 
      Destroy(pointerNodeTransform);
    }
    pointerNodeTransform = new GameObject("KASPointerPartNode").transform;

    // Deatch will decouple from the parent so, ask to ignore it when looking for the nodes.
    attachNodes =
        KIS_Shared.GetAvailableAttachNodes(partToAttach, ignoreAttachedPart: partToAttach.parent);
    if (!attachNodes.Any()) {
      //TODO: When there are no nodes try finding ones in the parent or in the children.
      // Ideally, the caller should have checked if this part has free nodes. Now the only
      // way is to pick *any* node. The surface one always exists so, it's a good
      // candidate. Though, for many details it may result in a weird representation.
      Logger.logError("Part {0} has no free nodes, use {1}",
                      partToAttach, partToAttach.srfAttachNode);
      attachNodes.Add(partToAttach.srfAttachNode);
    }

    // Find the best default node.
    attachNodeIndex = -1;
    if (attachNodes[0].nodeType == AttachNode.NodeType.Surface) {
      // Surface is always the best one. May be not for docking ports, though.
      Logger.logInfo("Surface node set to default");
      attachNodeIndex = 0;
    } else {
      // In VAB "bottom" is a usual node so, prefer it when available.
      attachNodeIndex = attachNodes.FindIndex(an => an.id.Equals("bottom"));
      if (attachNodeIndex != -1) {
        Logger.logInfo("Bottom node set to default");
      } else {
        // Fallback if no default node is found.
        attachNodeIndex = 0;
        Logger.logInfo("Fallback: '{0}' node set to default", attachNodes[attachNodeIndex].id);
      }
    }

    UpdatePointerAttachNode();
  }

  /// <summary>Sets pointer origin to the current attachment node</summary>
  private static void UpdatePointerAttachNode() {
    pointerNodeTransform.localPosition = GetCurrentAttachNode().position;
    pointerNodeTransform.localRotation = KIS_Shared.GetNodeRotation(GetCurrentAttachNode());
  }
      
  /// <summary>Destroyes object(s) allocated to represent a pointer.</summary>
  /// <remarks>
  /// When making pointer for a complex hierarchy a lot of different resources may be
  /// allocated/dropped. Destroying each one of them can be too slow so, cleanup is done in
  /// one call to <c>UnloadUnusedAssets()</c>.
  /// </remarks>
  private static void DestroyPointer() {
    if (!pointer) {
      return;  // Nothing to do.
    }
    Destroy(pointer);
    pointer = null;
    Destroy(pointerNodeTransform);
    pointerNodeTransform = null;
    allModelMr.Clear();

    // On large assemblies memory consumption can be significant. Reclaim it.
    Resources.UnloadUnusedAssets();
    Logger.logInfo("Pointer destroyed");
  }

  /// <summary>Goes thru part assembly and collects all meshes in the hierarchy.</summary>
  /// <remarks>
  /// Returns shared meshes with the right transformations. No new objects are created.
  /// </remarks>
  /// <param name="assembly">Assembly to collect meshes from.</param>
  /// <param name="worldTransform">A world transformation matrix to apply to every mesh after
  ///     it's translated into world's coordinates.</param>
  /// <param name="meshCombines">[out] Collected meshes.</param>
  private static void CollectMeshesFromAssembly(Part assembly,
    Matrix4x4 worldTransform,
    List<CombineInstance> meshCombines) {
    // This gives part's mesh(es) and all surface attached children part meshes.
    MeshFilter[] meshFilters = assembly.GetComponentsInChildren<MeshFilter>();
    Logger.logInfo("Found {0} children meshes in: {1}", meshFilters.Count(), assembly);
    foreach (var meshFilter in meshFilters) {
      var combine = new CombineInstance();
      combine.mesh = meshFilter.sharedMesh;
      combine.transform = worldTransform * meshFilter.transform.localToWorldMatrix;
      meshCombines.Add(combine);
    }

    // Go thru the stacked children parts. They don't have local transformation.
    foreach (Part child in assembly.children) {
      if (child.transform.position.Equals(child.transform.localPosition)) {
        CollectMeshesFromAssembly(child, worldTransform, meshCombines);
      }
    }
  }

  /// <summary>Creates and returns meshes from a prefab.</summary>
  /// <param name="prefabPart">A part to make meshes for.</param>
  /// <param name="meshCombines">[out] Collected meshes.</param>
  private static void CollectMeshesFromPrefab(Part prefabPart, List<CombineInstance> meshCombines) {
    var model = prefabPart.FindModelTransform("model").gameObject;
    var meshModel = Instantiate(model, Vector3.zero, Quaternion.identity) as GameObject;
    var meshFilters = meshModel.GetComponentsInChildren<MeshFilter>();
    Logger.logInfo("Created {0} meshes from prefab: {1}", meshFilters.Count(), prefabPart);
    foreach (var meshFilter in meshFilters) {
      var combine = new CombineInstance();
      combine.mesh = meshFilter.mesh;  // Get a copy. 
      combine.transform = meshFilter.transform.localToWorldMatrix;
      meshCombines.Add(combine);
    }
    DestroyImmediate(meshModel);  // Don't allow it showing in the scene.
  }
}

}  // namespace
