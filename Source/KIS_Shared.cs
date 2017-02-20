using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KIS {

public class KIS_LinkedPart : MonoBehaviour {
  public Part part;
}

/// <summary>Constants for standard attach node ids.</summary>
public static class AttachNodeId {
  /// <summary>Stack node "bottom".</summary>
  public const string Bottom = "bottom";
  /// <summary>Stack node "top".</summary>
  public const string Top = "top";
}

public static class KIS_Shared {
  // TODO: Read it from the config.
  private const float DefaultMessageTimeout = 5f; // Seconds.

  /// <summary>Mesh render queue of the highlight part layer.</summary>
  /// <remarks>When other renderers need to be drawn on the part they should have queue set to this
  /// or higher value. Otherwise, the part's highliting will overwrite the output.</remarks>
  public const int HighlighedPartRenderQueue = 4000;  // As of KSP 1.1.1230

  public static string bipWrongSndPath = "KIS/Sounds/bipwrong";
  public delegate void OnPartReady(Part affectedPart);

  public enum MessageAction {
    DropEnd,
    AttachStart,
    AttachEnd,
    Store,
    Decouple
  }

  public static void SendKISMessage(Part destPart, MessageAction action, AttachNode srcNode = null,
                                    Part tgtPart = null, AttachNode tgtNode = null) {
    BaseEventData bEventData = new BaseEventData(BaseEventData.Sender.AUTO);
    bEventData.Set("action", action.ToString());
    bEventData.Set("sourceNode", srcNode);
    bEventData.Set("targetPart", tgtPart);
    bEventData.Set("targetNode", tgtNode);
    destPart.SendMessage("OnKISAction", bEventData, SendMessageOptions.DontRequireReceiver);
  }

  // TODO: Deprecate the method after June 2016.
  [ObsoleteAttribute("Use Mouse.HoveredPart instead", true)]
  public static Part GetPartUnderCursor() {
    return Mouse.HoveredPart;
  }

  public static void PlaySoundAtPoint(string soundPath, Vector3 position) {
    AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(soundPath), position);
  }

  public static bool createFXSound(Part part, FXGroup group, string sndPath, bool loop,
                                   float maxDistance = 30f) {
    group.audio = part.gameObject.AddComponent<AudioSource>();
    group.audio.volume = GameSettings.SHIP_VOLUME;
    group.audio.rolloffMode = AudioRolloffMode.Linear;
    group.audio.dopplerLevel = 0f;
    group.audio.spatialBlend = 1f;
    group.audio.maxDistance = maxDistance;
    group.audio.loop = loop;
    group.audio.playOnAwake = false;
    if (GameDatabase.Instance.ExistsAudioClip(sndPath)) {
      group.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
      return true;
    } else {
      Debug.LogError("Sound not found in the game database !");
      ScreenMessaging.ShowPriorityScreenMessageWithTimeout(
          10, "Sound file : {0} has not been found, please check your KIS installation !",sndPath);
      return false;
    }
  }

  /// <summary>
  /// Walks thru the hierarchy and calculates the total mass of the assembly.
  /// </summary>
  /// <param name="rootPart">A root part of the assembly.</param>
  /// <param name="childrenCount">[out] A total number of children in the assembly.</param>
  /// <returns>Full mass of the hierarchy.</returns>
  public static float GetAssemblyMass(Part rootPart, out int childrenCount) {
    childrenCount = 0;
    return Internal_GetAssemblyMass(rootPart, ref childrenCount);
  }

  /// <summary>Recursive implementation of <c>GetAssemblyMass</c>.</summary>
  static float Internal_GetAssemblyMass(Part rootPart, ref int childrenCount) {
    float totalMass = rootPart.mass + rootPart.GetResourceMass();
    ++childrenCount;
    foreach (Part child in rootPart.children) {
      totalMass += Internal_GetAssemblyMass(child, ref childrenCount);
    }
    return totalMass;
  }

  /// <summary>Fixes all structural links to another vessel(s).</summary>
  /// <remarks>
  /// Normally compound parts should handle decoupling themselves but sometimes they do it
  /// horribly wrong. For instance, stock strut connector tries to restore connection when
  /// part is re-attached to the former vessel which may produce a collision. This method
  /// deletes all compound parts with target pointing to a different vessel.
  /// </remarks>
  /// <param name="vessel">Vessel to fix links for.</param>
  // TODO: Break the link instead of destroying the part.
  // TODO: Handle KAS and other popular plugins connectors.         
  public static void CleanupExternalLinks(Vessel vessel) {
    var parts = vessel.parts.FindAll(p => p is CompoundPart);
    Debug.LogFormat("Check {0} compound part(s) in vessel: {1}", parts.Count(), vessel);
    foreach (var part in parts) {
      var compoundPart = part as CompoundPart;
      if (compoundPart.target && compoundPart.target.vessel != vessel) {
        Debug.LogFormat("Destroy compound part '{0}' which links '{1}' to '{2}'",
                        compoundPart, compoundPart.parent, compoundPart.target);
        compoundPart.Die();
      }
    }
  }

  /// <summary>Decouples <paramref name="assemblyRoot"/> from the vessel.</summary>
  /// <remarks>Also does external links cleanup on both vessels.</remarks>
  /// <param name="assemblyRoot">An assembly to decouple.</param>
  public static void DecoupleAssembly(Part assemblyRoot) {
    if (assemblyRoot.parent == null) {
      return;  // Nothing to decouple.
    }
    SendKISMessage(assemblyRoot, MessageAction.Decouple);
    Vessel oldVessel = assemblyRoot.vessel;
    var formerParent = assemblyRoot.parent;

    // Regular parts can be just decoupled but docking ports want to do it via their own methods.
    var srcDockingPort = assemblyRoot.GetComponent<ModuleDockingNode>();
    if (srcDockingPort != null && srcDockingPort.otherNode != null) {
      var tgtDockingPort = srcDockingPort.otherNode;
      Debug.LogFormat("Undock port {0} from {1}",
                      DbgFormatter.PartId(srcDockingPort.part),
                      DbgFormatter.PartId(tgtDockingPort.part));
      // Set ejection forces to zero to not have physics effect on KIS detach.
      var srcUndockForce = srcDockingPort.undockEjectionForce;
      srcDockingPort.undockEjectionForce = 0;
      var tgtUndockForce = tgtDockingPort.undockEjectionForce;
      tgtDockingPort.undockEjectionForce = 0;
      // Propery undock docked ports.
      srcDockingPort.Undock();
      // Restore undock forces on the parts.
      srcDockingPort.undockEjectionForce = srcUndockForce;
      tgtDockingPort.undockEjectionForce = tgtUndockForce;
    } else {
      Debug.LogFormat("Decouple part {0} from part {1}",
                      DbgFormatter.PartId(assemblyRoot), DbgFormatter.PartId(assemblyRoot.parent));
      assemblyRoot.decouple();
    }

    // HACK: As of KSP 1.0.5 some parts (e.g docking ports) can be attached by both a
    // surface node and by a stack node which looks like an editor bug in some corner case.
    // In this case decouple() will only clear the surface node leaving the stack one
    // refering the parent. This misconfiguration will badly affect all further KIS
    // operations on the part. Do a cleanup job here to workaround this bug.
    var orphanNode = assemblyRoot.FindAttachNodeByPart(formerParent);
    if (orphanNode != null) {
      Debug.LogWarningFormat("KSP BUG: Cleanup orphan node {0} in the assembly", orphanNode.id);
      orphanNode.attachedPart = null;
      // Also, check that parent is properly cleaned up.
      var parentOrphanNode = formerParent.FindAttachNodeByPart(assemblyRoot);
      if (parentOrphanNode != null) {
        Debug.LogWarningFormat(
            "KSP BUG: Cleanup orphan node {0} in the parent", parentOrphanNode.id);
        parentOrphanNode.attachedPart = null;
      }
    }
          
    CleanupExternalLinks(oldVessel);
    CleanupExternalLinks(assemblyRoot.vessel);
    RenameAssemblyVessel(assemblyRoot);
  }

  /// <summary>Gives a nicer name to a vessel created during KIS deatch operation.</summary>
  /// <remarks>When a part is pulled out of inventory or assembly deatched from a vessel it gets a
  /// standard name saying it's now "debris". When using KIS such parts are not actually debris.
  /// This method renames vessel depening on the case:
  /// <list type="">
  /// <item>Single part vessels are named after the part's title.</item>
  /// <item>Multiple parts vessels are named after the source vessel name.</item>
  /// </list>
  /// Also, vessel's type is reset to <c>VesselType.Unknown</c>.</remarks>
  /// <param name="part">A part of the vessel to get name and vessel from.</param>
  public static void RenameAssemblyVessel(Part part) {
    part.vessel.vesselType = VesselType.Unknown;
    part.vessel.vesselName = part.partInfo.title;
    ModuleKISInventory inv = part.GetComponent<ModuleKISInventory>();
    if (inv && inv.invName.Length > 0) {
      // Add inventory name suffix if any.
      part.vessel.vesselName += string.Format(" ({0})", inv.invName);
    }
    // For assemblies add number of parts.
    if (part.vessel.parts.Count > 1) {
      part.vessel.vesselName += string.Format(" with {0} parts", part.vessel.parts.Count - 1);
    }
  }

  public static ConfigNode PartSnapshot(Part part) {
    if (ReferenceEquals(part, part.partInfo.partPrefab)) {
      // HACK: Prefab may have fields initialized to "null". Such fields cannot be saved via
      //   BaseFieldList when making a snapshot. So, go thru the persistent fields of all prefab
      //   modules and replace nulls with a default value of the type. It's unlikely we break
      //   something since by design such fields are not assumed to be used until loaded, and it's
      //   impossible to have "null" value read from a config.
      CleanupModuleFieldsInPart(part);
    }

    var node = new ConfigNode("PART");
    var snapshot = new ProtoPartSnapshot(part, null);

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

    for (int i = 0; i < prefab_modules.Length && i < module_nodes.Length; i++) {
      var module = module_nodes[i];
      var name = module.GetValue("name") ?? "";

      node.AddNode(module);

      if (name == "KASModuleContainer") {
        // Containers get to keep their contents
        module.RemoveNodes("EVENTS");
      } else if (name.StartsWith("KASModule")) {
        // Prune the state of the KAS modules completely
        module.ClearData();
        module.AddValue("name", name);
        continue;
      }

      module.RemoveNodes("ACTIONS");
    }

    return node;
  }

  public static ConfigNode vesselSnapshot(Vessel vessel) {
    ProtoVessel snapshot = new ProtoVessel(vessel);
    ConfigNode node = new ConfigNode("VESSEL");
    snapshot.Save(node);
    return node;
  }

  public static Collider GetEvaCollider(Vessel evaVessel, string colliderName) {
    KerbalEVA kerbalEva = evaVessel.rootPart.gameObject.GetComponent<KerbalEVA>();
    Collider evaCollider = null;
    if (kerbalEva) {
      foreach (var col in kerbalEva.characterColliders) {
        if (col.name == colliderName) {
          evaCollider = col;
          break;
        }
      }
    }
    return evaCollider;
  }

  public static Part CreatePart(AvailablePart avPart, Vector3 position, Quaternion rotation,
                                Part fromPart) {
    var partNode = new ConfigNode();
    PartSnapshot(avPart.partPrefab).CopyTo(partNode);
    return CreatePart(partNode, position, rotation, fromPart);
  }

  /// <summary>Creates a new part from the config.</summary>
  /// <param name="partConfig">Config to read part from.</param>
  /// <param name="position">Initial position of the new part.</param>
  /// <param name="rotation">Initial rotation of the new part.</param>
  /// <param name="fromPart"></param>
  /// <param name="coupleToPart">Optional. Part to couple new part to.</param>
  /// <param name="srcAttachNodeId">
  /// Optional. Attach node ID on the new part to use for coupling. It's required if coupling to
  /// part is requested.
  /// </param>
  /// <param name="tgtAttachNode">
  /// Optional. Attach node on the target part to use for coupling. It's required if
  /// <paramref name="srcAttachNodeId"/> specifies a stack node.
  /// </param>
  /// <param name="onPartReady">
  /// Callback to call when new part is fully operational and its joint is created (if any). It's
  /// undetermined how long it may take before the callback is called. The calling code must expect
  /// that there will be several frame updates and at least one fixed frame update.
  /// </param>
  /// <param name="createPhysicsless">
  /// Tells if new part must be created without rigidbody and joint. It's only used to create
  /// equippable parts. Any other use-case is highly unlikely.
  /// </param>
  /// <returns></returns>
  public static Part CreatePart(
      ConfigNode partConfig, Vector3 position, Quaternion rotation, Part fromPart,
      Part coupleToPart = null,
      string srcAttachNodeId = null,
      AttachNode tgtAttachNode = null,
      OnPartReady onPartReady = null,
      bool createPhysicsless = false) {
    // Sanity checks for the parameters.
    if (coupleToPart != null) {
      if (srcAttachNodeId == null
          || srcAttachNodeId == "srfAttach" && tgtAttachNode != null
          || srcAttachNodeId != "srfAttach"
             && (tgtAttachNode == null || tgtAttachNode.id == "srfAttach")) {
        Debug.LogWarningFormat(
            "Wrong parts attach parameters: srcNodeId={0}, tgtNodeId={1}",
            srcAttachNodeId ?? "N/A",
            tgtAttachNode != null ? tgtAttachNode.id : "N/A");
        // Best we can do is falling back to surface attach.
        srcAttachNodeId = "srfAttach";
        tgtAttachNode = null;
      }
    }

    var refVessel = coupleToPart != null ? coupleToPart.vessel : fromPart.vessel;
    var partNodeCopy = new ConfigNode();
    partConfig.CopyTo(partNodeCopy);
    var snapshot =
        new ProtoPartSnapshot(partNodeCopy, refVessel.protoVessel, HighLogic.CurrentGame);
    if (HighLogic.CurrentGame.flightState.ContainsFlightID(snapshot.flightID)
        || snapshot.flightID == 0) {
      snapshot.flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
    }
    snapshot.parentIdx = coupleToPart != null ? refVessel.parts.IndexOf(coupleToPart) : 0;
    snapshot.position = position;
    snapshot.rotation = rotation;
    snapshot.stageIndex = 0;
    snapshot.defaultInverseStage = 0;
    snapshot.seqOverride = -1;
    snapshot.inStageIndex = -1;
    snapshot.attachMode = srcAttachNodeId == "srfAttach"
        ? (int)AttachModes.SRF_ATTACH
        : (int)AttachModes.STACK;
    snapshot.attached = true;
    snapshot.flagURL = fromPart.flagURL;

    var newPart = snapshot.Load(refVessel, false);
    refVessel.Parts.Add(newPart);
    newPart.transform.position = position;
    newPart.transform.rotation = rotation;
    newPart.missionID = fromPart.missionID;
    newPart.UpdateOrgPosAndRot(newPart.vessel.rootPart);

    if (coupleToPart != null) {
      // Wait for part to initialize and then fire ready event.
      newPart.StartCoroutine(
          WaitAndCouple(newPart, srcAttachNodeId, tgtAttachNode, onPartReady,
                        createPhysicsless: createPhysicsless));
    } else {
      // Create new part as a separate vessel.
      newPart.StartCoroutine(WaitAndMakeLonePart(newPart, onPartReady));
    }
    return newPart;
  }

  static IEnumerator WaitAndCouple(Part newPart, string srcAttachNodeId,
                                   AttachNode tgtAttachNode, OnPartReady onPartReady,
                                   bool createPhysicsless = false) {
    var tgtPart = newPart.parent;
    if (createPhysicsless) {
      newPart.PhysicsSignificance = 1;  // Disable physics on the part.
    }

    // Create proper attach nodes.
    Debug.LogFormat("Attach new part {0} to {1}: srcNodeId={2}, tgtNode={3}",
                    newPart.name, newPart.vessel,
                    srcAttachNodeId, tgtAttachNode != null ? tgtAttachNode.id : "N/A");
    var srcAttachNode = GetAttachNodeById(newPart, srcAttachNodeId);
    srcAttachNode.attachedPart = tgtPart;
    srcAttachNode.attachedPartId = tgtPart.flightID;
    if (tgtAttachNode != null) {
      tgtAttachNode.attachedPart = newPart;
      tgtAttachNode.attachedPartId = newPart.flightID;
    }

    // When target, source or both are docking ports force them into state PreAttached. It's the
    // most safe state that simulates behavior of parts attached in the editor.
    var srcDockingNode = GetDockingNode(newPart, attachNodeId: srcAttachNodeId);
    if (srcDockingNode != null) {
      // Source part is not yet started. It's functionality is very limited.
      srcDockingNode.state = "PreAttached";
      srcDockingNode.dockedPartUId = 0;
      srcDockingNode.dockingNodeModuleIndex = 0;
      Debug.LogFormat(
          "Force new node {0} to state {1}", DbgFormatter.PartId(newPart), srcDockingNode.state);
    }
    var tgtDockingNode = GetDockingNode(tgtPart, attachNode: tgtAttachNode);
    if (tgtDockingNode != null) {
      CoupleDockingPortWithPart(tgtDockingNode, newPart);
    }
    
    // Wait until part is started. Keep it in position till it happen.
    Debug.LogFormat("Wait for part {0} to get alive...", newPart.name);
    newPart.transform.parent = tgtPart.transform;
    var relPos = newPart.transform.localPosition;
    var relRot = newPart.transform.localRotation;
    if (newPart.PhysicsSignificance != 1) {
      // Mangling with colliders on physicsless parts may result in camera effects.
      var childColliders = newPart.GetComponentsInChildren<Collider>(includeInactive: false);
      CollisionManager.IgnoreCollidersOnVessel(tgtPart.vessel, childColliders);
    }
    while (!newPart.started && newPart.State != PartStates.DEAD) {
      yield return new WaitForFixedUpdate();
      if (newPart.rb != null) {
        newPart.rb.position = newPart.parent.transform.TransformPoint(relPos);
        newPart.rb.rotation = newPart.parent.transform.rotation * relRot;
        newPart.rb.velocity = newPart.parent.Rigidbody.velocity;
        newPart.rb.angularVelocity = newPart.parent.Rigidbody.angularVelocity;
      }
    }
    newPart.transform.parent = newPart.transform;
    Debug.LogFormat("Part {0} is in state {1}", newPart.name, newPart.State);
    if (newPart.State == PartStates.DEAD) {
      Debug.LogWarningFormat("Part {0} has died before fully instantiating", newPart.name);
      yield break;
    }

    // Complete part initialization.
    newPart.Unpack();
    newPart.InitializeModules();

    // Notify game about a new part that has just "coupled".
    GameEvents.onPartCouple.Fire(new GameEvents.FromToAction<Part, Part>(newPart, tgtPart));
    tgtPart.vessel.ClearStaging();
    GameEvents.onVesselPartCountChanged.Fire(tgtPart.vessel);
    newPart.vessel.checkLanded();
    newPart.vessel.currentStage = StageManager.RecalculateVesselStaging(tgtPart.vessel) + 1;
    GameEvents.onVesselWasModified.Fire(tgtPart.vessel);
    newPart.CheckBodyLiftAttachment();

    if (onPartReady != null) {
      onPartReady(newPart);
    }
  }

  /// <summary>Finds and returns attach node by name.</summary>
  /// <param name="p">Part to find node for.</param>
  /// <param name="id">Name of the node. Surface nodename is allowed as well (srfAttach).</param>
  /// <returns>
  /// Found node. If node with the exact name cannot be found then surface attach node is returned.
  /// </returns>
  public static AttachNode GetAttachNodeById(Part p, string id) {
    var node = id == "srfAttach" ? p.srfAttachNode : p.FindAttachNode(id);
    if (node == null) {
      Debug.LogWarningFormat(
          "Cannot find attach node {0} on part {1}. Using srfAttach", id, p.name);
      node = p.srfAttachNode;
    }
    return node;
  }

  /// <summary>Couples parts of different vessels together.</summary>
  /// <remarks>
  /// When parts are compatible docking ports thet are docked instead of coupling. Docking ports
  /// handle own logic on docking.
  /// <para>
  /// Parts will be coupled even if source and/or target attach node is incorrect. In such a case
  /// the parts will be logically and physically joint into one vessel but normal parts interaction
  /// logic may get broken (e.g. fuel flow).
  /// </para>
  /// </remarks>
  /// <param name="srcPart">Source part to couple.</param>
  /// <param name="tgtPart">New parent of the source part.</param>
  /// <param name="srcAttachNodeId">
  /// Attach node id on the source part. Can be <c>null</c> for the compatibility but it's an
  /// erroneous situation and it will be logged.
  /// </param>
  /// <param name="tgtAttachNode">
  /// Attach node on the parent to couple thru. Can be <c>null</c> for the compatibility but it's an
  /// erroneous situation and it will be logged.
  /// </param>
  public static void CouplePart(Part srcPart, Part tgtPart,
                                string srcAttachNodeId = null,
                                AttachNode tgtAttachNode = null) {
    // Node links.
    if (srcAttachNodeId != null) {
      if (srcAttachNodeId == "srfAttach") {
        Debug.LogFormat("Attach type: {0} | ID : {1}",
                        srcPart.srfAttachNode.nodeType, srcPart.srfAttachNode.id);
        srcPart.attachMode = AttachModes.SRF_ATTACH;
        srcPart.srfAttachNode.attachedPart = tgtPart;
        srcPart.srfAttachNode.attachedPartId = tgtPart.flightID;
      } else {
        AttachNode srcAttachNode = srcPart.FindAttachNode(srcAttachNodeId);
        if (srcAttachNode != null) {
          Debug.LogFormat("Attach type : {0} | ID : {1}",
                          srcPart.srfAttachNode.nodeType, srcAttachNode.id);
          srcPart.attachMode = AttachModes.STACK;
          srcAttachNode.attachedPart = tgtPart;
          srcAttachNode.attachedPartId = tgtPart.flightID;
          if (tgtAttachNode != null) {
            tgtAttachNode.attachedPart = srcPart;
          } else {
            Debug.LogWarning("Target node is null");
          }
        } else {
          Debug.LogErrorFormat("Source attach node not found: {0}", srcAttachNodeId);
        }
      }
    } else {
      Debug.LogWarning("Missing source attach node !");
    }

    srcPart.Couple(tgtPart);
  }

  public static void MoveAlign(Transform source, Transform childNode, RaycastHit hit,
                               Quaternion adjust) {
    Vector3 refDir = hit.transform.TransformDirection(Vector3.up);
    Quaternion rotation = Quaternion.LookRotation(hit.normal, refDir);
    MoveAlign(source, childNode, hit.point, rotation * adjust);
  }

  public static void MoveAlign(Transform source, Transform childNode, Transform target,
                               Quaternion adjust) {
    MoveAlign(source, childNode, target.position, target.rotation * adjust);
  }

  public static void MoveAlign(Transform source, Transform childNode, Transform target) {
    MoveAlign(source, childNode, target.position, target.rotation);
  }

  public static void MoveAlign(Transform source, Transform childNode, Vector3 targetPos,
                               Quaternion targetRot) {
    source.rotation = targetRot * childNode.localRotation;
    source.position = source.position - (childNode.position - targetPos);
  }

  public static void ResetCollisionEnhancer(Part p, bool create_new = true) {
    if (p.collisionEnhancer) {
      UnityEngine.Object.DestroyImmediate(p.collisionEnhancer);
    }

    if (create_new) {
      p.collisionEnhancer = p.gameObject.AddComponent<CollisionEnhancer>();
    }
  }

  /// <summary>Returns part's volume basing on its geometrics.</summary>
  /// <remarks>Geometry of a part depends on the state (e.g. solar panel can be deployed and take
  /// more space). It's not possible (nor practical) for KIS to figure out which state of the part
  /// is the most compact one. So, when calculating part's volume the initial state of the mesh
  /// renderers in the prefab is considered the right ones. If parts's initial state is deployed
  /// (e.g. Drill-O-Matic) then it will take more space than it could have.</remarks>
  /// <param name="partInfo">A part to get volume for.</param>
  /// <returns>Volume in liters.</returns>
  public static float GetPartVolume(AvailablePart partInfo) {
    var p = partInfo.partPrefab;
    float volume;

    // If there is a KIS item volume then use it but still apply scale tweaks. 
    var kisItem = p.GetComponent<ModuleKISItem>();
    if (kisItem && kisItem.volumeOverride > 0) {
      volume = kisItem.volumeOverride;
    } else {
      var boundsSize = PartGeometryUtil.MergeBounds(p.GetRendererBounds(), p.transform).size;
      volume = boundsSize.x * boundsSize.y * boundsSize.z * 1000f;
    }

    // Apply cube of the scale modifier since volume involves all 3 axis.
    return (float) (volume * Math.Pow(GetPartExternalScaleModifier(partInfo), 3));
  }

  /// <summary>Returns external part's scale for a default part configuration.</summary>
  /// <param name="avPart">A part info to check modules for.</param>
  /// <returns>Multiplier to a model's scale on one axis.</returns>
  public static float GetPartExternalScaleModifier(AvailablePart avPart) {
    return GetPartExternalScaleModifier(avPart.partConfig);
  }

  /// <summary>Returns external part's scale given a config.</summary>
  /// <remarks>This is a scale applied on the module by the other mods. I.e. it's a "runtime" scale,
  /// not the one specified in the common part's config.
  /// <para>The only mod supported till now is <c>TweakScale</c>.</para>
  /// </remarks>
  /// <param name="partNode">A config to get values from.</param>
  /// <returns>Multiplier to a model's scale on one axis.</returns>
  public static float GetPartExternalScaleModifier(ConfigNode partNode) {
    // TweakScale compatibility.
    foreach (var node in partNode.GetNodes("MODULE")) {
      if (node.GetValue("name") == "TweakScale") {
        double defaultScale = 1.0f;
        ConfigAccessor.GetValueByPath(node, "defaultScale", ref defaultScale);
        double currentScale = 1.0f;
        ConfigAccessor.GetValueByPath(node, "currentScale", ref currentScale);
        return (float) (currentScale / defaultScale);
      }
    }
    return 1.0f;
  }

  public static ConfigNode GetBaseConfigNode(PartModule partModule) {
    UrlDir.UrlConfig pConfig = null;
    foreach (UrlDir.UrlConfig uc in GameDatabase.Instance.GetConfigs("PART")) {
      if (uc.name.Replace('_', '.') == partModule.part.partInfo.name) {
        pConfig = uc;
        break;
      }
    }
    if (pConfig != null) {
      foreach (ConfigNode cn in pConfig.config.GetNodes("MODULE")) {
        if (cn.GetValue("name") == partModule.moduleName) {
          return cn;
        }
      }
    }
    return null;
  }

  /// <summary>Returns a rotation for the attach node.</summary>
  /// <param name="attachNode">A node to get orientation from.</param>
  /// <param name="mirrorZ">If <c>true</c> then Z axis in the node's orientation will be mirrored.
  /// E.g. <c>(1, 1, 1)</c> will be translated into <c>(1, 1, -1)</c>.</param>
  /// <returns>Rotation quaternion.</returns>
  public static Quaternion GetNodeRotation(AttachNode attachNode, bool mirrorZ = false) {
    var orientation = attachNode.orientation;
    if (mirrorZ) {
      orientation.z = -orientation.z;
    }
    return Quaternion.LookRotation(orientation);
  }

  public static void AssignAttachIcon(Part part, AttachNode node, Color iconColor,
                                      string name = null) {
    // Create NodeTransform if needed
    if (!node.nodeTransform) {
      node.nodeTransform = new GameObject("KISNodeTransf").transform;
      node.nodeTransform.parent = part.transform;
      node.nodeTransform.localPosition = node.position;
      node.nodeTransform.localRotation = KIS_Shared.GetNodeRotation(node);
    }

    if (!node.icon) {
      node.icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      if (node.icon.GetComponent<Collider>()) {
        UnityEngine.Object.DestroyImmediate(node.icon.GetComponent<Collider>());
      }
      var iconRenderer = node.icon.GetComponent<Renderer>();
      
      if (iconRenderer) {
        iconRenderer.material = new Material(Shader.Find("Transparent/Diffuse"));
        iconColor.a = 0.5f;
        iconRenderer.material.color = iconColor;
        iconRenderer.material.renderQueue = HighlighedPartRenderQueue;
      }
      node.icon.transform.parent = part.transform;
      if (name != null)
        node.icon.name = name;
      double num;
      if (node.size == 0) {
        num = (double)node.size + 0.5;
      } else {
        num = (double)node.size;
      }
      node.icon.transform.localScale = Vector3.one * node.radius * (float)num;
      node.icon.transform.parent = node.nodeTransform;
      node.icon.transform.localPosition = Vector3.zero;
      node.icon.transform.localRotation = Quaternion.identity;
    }
  }

  public static void EditField(string label, ref bool value, int maxLenght = 50) {
    value = GUILayout.Toggle(value, label);
  }

  public static Dictionary<string, string> editFields = new Dictionary<string, string>();

  public static bool EditField(string label, ref Vector3 value, int maxLenght = 50) {
    bool btnPress = false;
    if (!editFields.ContainsKey(label + "x")) {
      editFields.Add(label + "x", value.x.ToString());
    }
    if (!editFields.ContainsKey(label + "y")) {
      editFields.Add(label + "y", value.y.ToString());
    }
    if (!editFields.ContainsKey(label + "z")) {
      editFields.Add(label + "z", value.z.ToString());
    }
    GUILayout.BeginHorizontal();
    GUILayout.Label(label + " : " + value + "   ");
    editFields[label + "x"] = GUILayout.TextField(editFields[label + "x"], maxLenght);
    editFields[label + "y"] = GUILayout.TextField(editFields[label + "y"], maxLenght);
    editFields[label + "z"] = GUILayout.TextField(editFields[label + "z"], maxLenght);
    if (GUILayout.Button(new GUIContent("Set", "Set vector"), GUILayout.Width(60f))) {
      var tmpVector3 = new Vector3(float.Parse(editFields[label + "x"]),
                                   float.Parse(editFields[label + "y"]),
                                   float.Parse(editFields[label + "z"]));
      value = tmpVector3;
      btnPress = true;
    }
    GUILayout.EndHorizontal();
    return btnPress;
  }

  public static bool EditField(string label, ref string value, int maxLenght = 50) {
    bool btnPress = false;
    if (!editFields.ContainsKey(label)) {
      editFields.Add(label, value.ToString());
    }
    GUILayout.BeginHorizontal();
    GUILayout.Label(label + " : " + value + "   ");
    editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
    if (GUILayout.Button(new GUIContent("Set", "Set string"), GUILayout.Width(60f))) {
      value = editFields[label];
      btnPress = true;
    }
    GUILayout.EndHorizontal();
    return btnPress;
  }

  public static bool EditField(string label, ref int value, int maxLenght = 50) {
    bool btnPress = false;
    if (!editFields.ContainsKey(label)) {
      editFields.Add(label, value.ToString());
    }
    GUILayout.BeginHorizontal();
    GUILayout.Label(label + " : " + value + "   ");
    editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
    if (GUILayout.Button(new GUIContent("Set", "Set int"), GUILayout.Width(60f))) {
      value = int.Parse(editFields[label]);
      btnPress = true;
    }
    GUILayout.EndHorizontal();
    return btnPress;
  }

  public static bool EditField(string label, ref float value, int maxLenght = 50) {
    bool btnPress = false;
    if (!editFields.ContainsKey(label)) {
      editFields.Add(label, value.ToString());
    }
    GUILayout.BeginHorizontal();
    GUILayout.Label(label + " : " + value + "   ");
    editFields[label] = GUILayout.TextField(editFields[label], maxLenght);
    if (GUILayout.Button(new GUIContent("Set", "Set float"), GUILayout.Width(60f))) {
      value = float.Parse(editFields[label]);
      btnPress = true;
    }
    GUILayout.EndHorizontal();
    return btnPress;
  }

  /// <summary>
  /// Helper method to verify if part is an indirect children of another part.
  /// </summary>
  /// <param name="rootPart">A root part of the hierarchy.</param>
  /// <param name="child">A part being tested.</param>
  /// <returns></returns>
  public static bool IsSameHierarchyChild(object rootPart, Part child) {
    for (Part part = child; part; part = part.parent) {
      if (System.Object.ReferenceEquals(rootPart, part)) {
        return true;
      }
    }
    return false;
  }

  /// <summary>Sets highlight status of the entire heierarchy.</summary>
  /// <param name="hierarchyRoot">A root part of the hierarchy.</param>
  /// <param name="isSelected">The status.</param>
  public static void SetHierarchySelection(Part hierarchyRoot, bool isSelected) {
    if (isSelected) {
      hierarchyRoot.SetHighlight(true, true /* recursive */);
    } else {
      hierarchyRoot.SetHighlight(false, true /* recursive */);
      hierarchyRoot.RecurseHighlight = false;
      // Restore highlighting of the currently hovered part.
      if (Mouse.HoveredPart == hierarchyRoot) {
        hierarchyRoot.SetHighlight(true, false /* recursive */);
      }
    }
  }

  /// <summary>Returns nodes available for attaching.</summary>
  /// <remarks>
  /// When part has a surface attachment node it may (and usually does) point in the same
  /// direction as a stack node. In such situation two different nodes in fact become the same
  /// attachment point, and if one of them is occupied the other one should be considered
  /// "blocked", i.e. not available for attachment. This method detects such situations and
  /// doesn't return nodes that may result in collision.
  /// </remarks>
  /// <param name="p">A part to get nodes for.</param>
  /// <param name="ignoreAttachedPart">Don't consider attachment node occupied if it's
  /// attached to this part.</param>
  /// <param name="needSrf">If <c>true</c> then free surface node should be retruned as well.
  /// Otherwise, only the stack nodes are returned.</param>
  /// <returns>A list of nodes that are available for attaching. First nodes in the list are the
  /// most preferable for the part.</returns>
  public static List<AttachNode> GetAvailableAttachNodes(Part p,
                                                         Part ignoreAttachedPart = null,
                                                         bool needSrf = true) {
    var result = new List<AttachNode>();
    var srfNode = p.attachRules.srfAttach ? p.srfAttachNode : null;
    bool srfHasPart = (srfNode != null && srfNode.attachedPart != null
                       && srfNode.attachedPart != ignoreAttachedPart);
    foreach (var an in p.attachNodes) {
      // Skip occupied nodes.
      if (an.attachedPart && an.attachedPart != ignoreAttachedPart) {
        // Reset surface node if it points in the same direction as the occupied node. 
        if (srfNode != null && an.orientation == srfNode.orientation) {
          srfNode = null;
        }
        continue;
      }
      // Skip free nodes that point in the same direction as an occupied surface node.
      if (srfHasPart && an.orientation == srfNode.orientation) {
        continue;
      }
      // Put "bottom" and "top" nodes before anything else. If part is stackable then bottom node is
      // the most used node, and top one is the second most used.
      if (an.id == AttachNodeId.Bottom) {
        result.Insert(0, an);  // Always go first in the list.
      } else if (an.id == AttachNodeId.Top) {
        // Put "top" node after "bottom" but before anything else.
        if (result.Count > 0 && result[0].id == AttachNodeId.Bottom) {
          result.Insert(1, an);
        } else {
          result.Insert(0, an);
        }
      } else {
        result.Add(an);  // All other nodes are added at the end.
      }
    }
    // Add a surface node if it's free.
    // FIXME: Temporarily rollback to the old behavior. See #134.
    if (needSrf && srfNode != null && !srfHasPart) {
      result.Insert(0, srfNode);
    }
    return result;
  }

  /// <summary>Walks thru all modules in the part and fixes null persistent fields.</summary>
  /// <remarks>Used to prevent NREs in methods that persist KSP fields.
  /// <para>Bad modules that cannot be fixed will be dropped which may make the part to be not
  /// behaving as expected. It's guaranteed that <i>stock</i> modules that need fixing will be
  /// fixed successfully. So, failures are only expected on the modules from the third-parties mods.
  /// </para></remarks>
  /// <param name="part">Prefab to fix.</param>
  public static void CleanupModuleFieldsInPart(Part part) {
    var badModules = new List<PartModule>();
    foreach (var moduleObj in part.Modules) {
      var module = moduleObj as PartModule;
      try {
        CleanupFieldsInModule(module);
      } catch {
        badModules.Add(module);
      }
    }
    // Cleanup modules that block KIS. It's a bad thing to do but not working KIS is worse.
    foreach (var moduleToDrop in badModules) {
      Debug.LogErrorFormat(
          "Module on part prefab {0} is setup improperly: name={1}, type={2}. Drop it!",
          part, moduleToDrop.moduleName, moduleToDrop.GetType());
      part.RemoveModule(moduleToDrop);
    }
  }

  /// <summary>Fixes null persistent fields in the module.</summary>
  /// <remarks>Used to prevent NREs in methods that persist KSP fields.</remarks>
  /// <param name="module">Module to fix.</param>
  public static void CleanupFieldsInModule(PartModule module) {
    // HACK: Fix uninitialized fields in science lab module.
    var scienceModule = module as ModuleScienceLab;
    if (scienceModule != null) {
      scienceModule.ExperimentData = new List<string>();
      Debug.LogWarningFormat(
          "WORKAROUND. Fix null field in ModuleScienceLab module on part prefab {0}", module.part);
    }
    
    // Ensure the module is awaken. Otherwise, any access to base fields list will result in NRE.
    // HACK: Accessing Fields property of a non-awaken module triggers NRE. If it happens then do
    // explicit awakening of the *base* module class.
    try {
      var unused = module.Fields.GetEnumerator();
    } catch {
      Debug.LogWarningFormat(
          "WORKAROUND. Module {0} on part prefab {1} is not awaken. Call Awake on it",
          module.GetType(), module.part);
      AwakePartModule(module);
    }
    foreach (var field in module.Fields) {
      var baseField = field as BaseField;
      if (baseField.isPersistant && baseField.GetValue(module) == null) {
        var proto = new StandardOrdinaryTypesProto();
        var defValue = proto.ParseFromString("", baseField.FieldInfo.FieldType);
        Debug.LogWarningFormat(
            "WORKAROUND. Found null field {0} in module prefab {1},"
            + " fixing to default value of type {2}: {3}",
            baseField.name, module.moduleName, baseField.FieldInfo.FieldType, defValue);
        baseField.SetValue(defValue, module);
      }
    }
  }

  /// <summary>Makes a call to <c>Awake()</c> method of the part module.</summary>
  /// <remarks>Modules added to prefab via <c>AddModule()</c> call are not get activated as they
  /// would if activated by the Unity core. As a result some vital fields may be left uninitialized
  /// which may result in an NRE later when working with the prefab (e.g. making a part snapshot).
  /// This method finds and invokes method <c>Awake</c> via reflection which is normally done by
  /// Unity.
  /// <para><b>IMPORTANT!</b> This method cannot awake a module! To make the things right every
  /// class in the hierarchy should get its <c>Awake</c> called. This method only calls <c>Awake</c>
  /// method on <c>PartModule</c> parent class which is not enough to do a complete awakening.
  /// </para>
  /// <para>This is a HACK since <c>Awake()</c> method is not supposed to be called by anyone but
  /// Unity. For now it works fine but one day it may awake the kraken.</para>
  /// </remarks>
  /// <param name="module">Module instance to awake.</param>
  public static void AwakePartModule(PartModule module) {
    // Private method can only be accessed via reflection when requested on the class that declares
    // it. So, don't use type of the argument and specify it explicitly. 
    var moduleAwakeMethod = typeof(PartModule).GetMethod(
        "Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (moduleAwakeMethod != null) {
      moduleAwakeMethod.Invoke(module, new object[] {});
    } else {
      Debug.LogErrorFormat("Cannot find Awake() method on {0}. Skip awakening of component: {1}",
                           module.GetType(), module.GetType());
    }
  }

  /// <summary>
  /// Returns <c>true</c> if key was pressed during the current frame. Respects UI locks set by the
  /// game.
  /// </summary>
  public static bool IsKeyDown(string key) {
    return InputLockManager.IsUnlocked(ControlTypes.UI) && Input.GetKeyDown(key.ToLower());
  }

  /// <summary>
  /// Returns <c>true</c> if key was pressed during the current frame. Respects UI locks set by the
  /// game.
  /// </summary>
  public static bool IsKeyDown(KeyCode keyCode) {
    return InputLockManager.IsUnlocked(ControlTypes.UI) && Input.GetKeyDown(keyCode);
  }

  /// <summary>
  /// Returns <c>true</c> if key was release during the current frame. Respects UI locks set by the
  /// game.
  /// </summary>
  public static bool IsKeyUp(string key) {
    return InputLockManager.IsUnlocked(ControlTypes.UI) && Input.GetKeyUp(key.ToLower());
  }

  /// <summary>
  /// Returns <c>true</c> if key was release during the current frame. Respects UI locks set by the
  /// game.
  /// </summary>
  public static bool IsKeyUp(KeyCode keyCode) {
    return InputLockManager.IsUnlocked(ControlTypes.UI) && Input.GetKeyUp(keyCode);
  }

  /// <summary>Tells if two docking nodes can potentially dock.</summary>
  static bool CheckNodesCompatible(ModuleDockingNode srcNode, ModuleDockingNode tgtNode) {
    return
        srcNode.nodeTypes.Any(tgtNode.nodeTypes.Contains)
        && tgtNode.gendered == srcNode.gendered
        && (!srcNode.gendered || tgtNode.genderFemale != srcNode.genderFemale)
        && tgtNode.snapRotation == srcNode.snapRotation
        && (!srcNode.snapRotation || Mathf.Approximately(tgtNode.snapOffset, srcNode.snapOffset));
  }

  /// <summary>Tells if node is docked to another node.</summary>
  public static bool IsNodeDocked(ModuleDockingNode portNode) {
    return portNode.state == portNode.st_docked_docker.name
        || portNode.state == portNode.st_docked_dockee.name;
  }

  /// <summary>Tells if node is attached to another node.</summary>
  /// <remarks>
  /// When node's reference attach node is attached ot a non-node part it's always "attached",
  /// not "docked". Two docking nodes can be in atatch mode when connected from the editor.
  /// </remarks>
  public static bool IsNodeCoupled(ModuleDockingNode portNode) {
    return portNode.state == portNode.st_preattached.name;
  }

  /// <summary>Resets docking node to state "ready".</summary>
  /// <remarks>
  /// It may take several update cycles before node actually reaches the ready state. Moreover,
  /// due to internal logic of the node it may start interacting with another node, thus, turning
  /// into a different state.
  /// <para>
  /// This method tries to do the reset right by simulating moving node out of reach for the other
  /// node. Though, if this fails then a hard reset is done. It may leave this node or other nodes
  /// in inconsistent state.
  /// </para>
  /// </remarks>
  /// <param name="dockingNode">Node to reset.</param>
  public static void ResetDockingNode(ModuleDockingNode dockingNode) {
    if (dockingNode.state != dockingNode.st_ready.name) {
      dockingNode.otherNode = null;  // Normally node does it in FixedUpdate().
      if (dockingNode.fsm.CurrentState.IsValid(dockingNode.on_nodeDistance)) {
        // Reset state politely by simulating nodes distance increase.
        Debug.LogFormat("Soft reset node {0} from state '{1}' to '{2}'",
                        DbgFormatter.PartId(dockingNode.part),
                        dockingNode.state, dockingNode.st_ready.name);
        dockingNode.fsm.RunEvent(dockingNode.on_nodeDistance);
      } else {
        // Do it a hard way: force the ready state!
        Debug.LogWarningFormat("Hard reset node {0} to state '{1}' from state {2}",
                               DbgFormatter.PartId(dockingNode.part),
                               dockingNode.st_ready.name, dockingNode.fsm.currentStateName);
        dockingNode.dockedPartUId = 0;
        dockingNode.dockingNodeModuleIndex = 0;
        dockingNode.fsm.StartFSM(dockingNode.st_ready.name);
      }
    }
  }

  /// <summary>Couples docking port with another part.</summary>
  /// <remarks>Both parts must be already attached and the attach nodes correctly set.</remarks>
  /// <param name="dockingNode">Port to couple.</param>
  /// <param name="tgtPart">Part to couple with.</param>
  /// <returns><c>true</c> if coupling was successful.</returns>
  public static bool CoupleDockingPortWithPart(ModuleDockingNode dockingNode, Part tgtPart) {
    Debug.LogFormat(
        "Hard reset docking node {0} from state {1} to {2}",
        DbgFormatter.PartId(tgtPart), dockingNode.state, dockingNode.st_preattached.name);
    dockingNode.dockedPartUId = 0;
    dockingNode.dockingNodeModuleIndex = 0;
    // Target part lived in real world for some time, so its state may be anything. Do a hard reset.
    dockingNode.fsm.StartFSM(dockingNode.st_ready.name);
    var initState = dockingNode.lateFSMStart(PartModule.StartState.None);
    // Make sure part init catched the new state.
    while (initState.MoveNext()) {
      // Do nothing. Just wait.
    }
    if (dockingNode.fsm.currentStateName != dockingNode.st_preattached.name) {
      Debug.LogWarningFormat("Node on {0} is unexpected state '{1}'",
                             DbgFormatter.PartId(dockingNode.part),
                             dockingNode.fsm.currentStateName);
      return false;
    }
    return true;
  }

  /// <summary>Returns docking node that sits at the provided attach node.</summary>
  /// <param name="part">Part to get node for.</param>
  /// <param name="attachNodeId">
  /// Refrence attach node ID. If not set then <paramref name="attachNode"/> will be used.
  /// </param>
  /// <param name="attachNode">
  /// Reference attach node. If not set then method will return <c>null</c>.
  /// </param>
  /// <returns><c>null</c> if not node found.</returns>
  public static ModuleDockingNode GetDockingNode(Part part, string attachNodeId = null, AttachNode attachNode = null) {
    var nodeId = attachNodeId ?? (attachNode != null ? attachNode.id : null);
    return part.FindModulesImplementing<ModuleDockingNode>()
        .FirstOrDefault(x => x.referenceAttachNode == nodeId);
  }

  /// <summary>Helper method to properly separate docking nodes.</summary>
  /// <remarks>Docking nodes must be in state that allows undocking/decoupling.</remarks>
  /// <returns><c>true</c> if at least one node was undocked/decoupled.</returns>
  static bool SeparateDockingNodes(Part srcPart, Part tgtPart, bool doUndock = true) {
    var changedNodes = false;
    for (var i = 0; i < srcPart.Modules.Count; i++) {
      var node = srcPart.Modules[i] as ModuleDockingNode;
      if (node == null || node.otherNode == null || node.otherNode.part != tgtPart) {
        continue;
      }
      var oldUndockForce = node.undockEjectionForce;
      node.undockEjectionForce = 0;
      if (IsNodeDocked(node)) {
        if (!doUndock) {
          continue;
        }
        Debug.LogFormat("Undock docking module {0} on part {1} from part {2}",
                        i, DbgFormatter.PartId(node.part), DbgFormatter.PartId(node.part.parent));
        node.Undock();
        changedNodes = true;
      } else if (IsNodeCoupled(node)) {
        Debug.LogFormat("Decouple docking module {0} on part {1} from part {2}",
                        i, DbgFormatter.PartId(node.part), DbgFormatter.PartId(node.part.parent));
        node.Decouple();
        changedNodes = true;
      } else {
        Debug.LogWarningFormat("Unexpected docking node state in module {0} on part {1}: {2}",
                               i, DbgFormatter.PartId(node.part), node.state);
      }
      node.undockEjectionForce = oldUndockForce;
    }
    return changedNodes;
  }

  /// <summary>
  /// Turns part into physicsless. It's a counterpart to <see cref="Part.PromoteToPhysicalPart"/>.
  /// </summary>
  static void SetPartToPhysicsless(Part p) {
    Debug.LogFormat("Disable phiycs on physicsless part {0}", DbgFormatter.PartId(p));
    p.transform.parent = p.parent.transform;
    p.attachJoint.DestroyJoint();
    UnityEngine.Object.Destroy(p.rb);
    p.rb = null;
    p.physicalSignificance = Part.PhysicalSignificance.NONE;
    UnityEngine.Object.Destroy(p.collisionEnhancer);
    p.collisionEnhancer = null;
    UnityEngine.Object.Destroy(p.partBuoyancy);
    p.partBuoyancy = null;

    // Re-attach physical children to the new physical parent since former parent is now
    // physicsless (no RB means no joint).
    var physicalChildren = new List<Part>();
    p.FindNonPhysicslessChildren(physicalChildren);
    foreach (var physicalChild in physicalChildren) {
      Debug.LogFormat("Re-create joint for phisics child {0}", DbgFormatter.PartId(physicalChild));
      if (physicalChild.attachJoint) {
        physicalChild.attachJoint.DestroyJoint();
        physicalChild.attachJoint = null;
      }
      physicalChild.CreateAttachJoint(physicalChild.attachMode);
    }
  }

  /// <summary>Creates a new vessel from a single part.</summary>
  /// <remarks>Initially the part must belong to some vessel.</remarks>
  static IEnumerator WaitAndMakeLonePart(Part newPart, OnPartReady onPartReady) {
    Debug.LogFormat("Create lone part vessel for {0}", DbgFormatter.PartId(newPart));
    newPart.PromoteToPhysicalPart();
    newPart.disconnect(true);
    Vessel newVessel = newPart.gameObject.AddComponent<Vessel>();
    newVessel.id = Guid.NewGuid();
    if (newVessel.Initialize(false)) {
      newVessel.vesselName = newPart.partInfo.title;
      newVessel.IgnoreGForces(10);
      newVessel.currentStage = StageManager.RecalculateVesselStaging(newVessel);
      newPart.setParent(null);
    }
    yield return new WaitWhile(() => !newPart.started && newPart.State != PartStates.DEAD);
    Debug.LogFormat("Part {0} is in state {1}", newPart.name, newPart.State);
    if (newPart.State == PartStates.DEAD) {
      Debug.LogWarningFormat("Part {0} has died before fully instantiating", newPart.name);
      yield break;
    }
    newPart.Unpack();
    newPart.InitializeModules();

    if (onPartReady != null) {
      onPartReady(newPart);
    }
  }
}

}  // namespace
