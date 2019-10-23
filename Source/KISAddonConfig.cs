// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
using KSPDev.ModelUtils;
using KSPDev.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.Instantly, false)]
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
sealed class KISAddonConfig : MonoBehaviour {
  [PersistentField("StackableItemOverride/partName", isCollection = true)]
  public readonly static List<string> stackableList = new List<string>();

  [PersistentField("StackableModule/moduleName", isCollection = true)]
  public readonly static List<string> stackableModules = new List<string>();

  [PersistentField("EquipAliases/alias", isCollection = true)]
  public readonly static List<string> equipAliases = new List<string>();

  [PersistentField("Global/showHintText")]
  public static bool showHintText = true;

  [PersistentField("Global/hideHintKey")]
  public static KeyCode hideHintKey = KeyCode.None;

  [PersistentField("EvaInventory")]
  readonly static PersistentConfigNode evaInventory = new PersistentConfigNode();

  [PersistentField("EvaPickup")]
  readonly static PersistentConfigNode evaPickup = new PersistentConfigNode();

  /// <summary>Instantly loads the KIS global settings.</summary>
  class KISConfigLoader: LoadingSystem {
    public override bool IsReady() {
      return true;
    }

    public override void StartLoad() {
      DebugEx.Info("Loading KIS global settings...");
      ConfigAccessor.ReadFieldsInType(typeof(KISAddonConfig), instance: null);
      ConfigAccessor.ReadFieldsInType(typeof(ModuleKISInventory), instance: null);
    }
  }

  /// <summary>Adds EVA inventories to every pod.</summary>
  class KISPodInventoryLoader: LoadingSystem {
    public override bool IsReady() {
      return true;
    }

    public override void StartLoad() {
      // Set inventory module for every pod with crew capacity.
      DebugEx.Info("Adding KIS modules to the parts...");
      for (var i = 0; i < PartLoader.LoadedPartsList.Count; i++) {
        var avPart = PartLoader.LoadedPartsList[i];
        var hasEvaModules = avPart.partPrefab.Modules.OfType<KerbalEVA>().Any();
        if (hasEvaModules) {
          var invModule = AddModule<ModuleKISInventory>(avPart.partPrefab, evaInventory);
          invModule.invType = ModuleKISInventory.InventoryType.Eva;
          AddModule<ModuleKISPickup>(avPart.partPrefab, evaPickup);
        } else if (avPart.partPrefab.CrewCapacity > 0) {
          AddPodInventories(avPart.partPrefab);
        }
      }
      for (var i = 0; i < QueuedPodInventoryParts.Count; i++) {
        AddPodInventories(QueuedPodInventoryParts[i]);
      }
    }

    /// <summary>Adds a custom part module and loads its fields from the config.</summary>
    T AddModule<T>(Part prefab, ConfigNode node) where T : PartModule {
      var module = prefab.AddModule(typeof(T).Name, forceAwake: true) as T;
      HostedDebugLog.Fine(module, "Add module and load config: type={0}", typeof(T));
      module.Fields.Load(node);
      return module;
    }
  }

  public void Awake() {
    List<LoadingSystem> list = LoadingScreen.Instance.loaders;
    if (list != null) {
      for (var i = 0; i < list.Count; i++) {
        if (list[i] is PartLoader) {
          var go = new GameObject();

          var invLoader = go.AddComponent<KISPodInventoryLoader>();
          // Cause the pod inventory loader to run AFTER the part loader.
          list.Insert(i + 1, invLoader);

          var cfgLoader = go.AddComponent<KISConfigLoader>();
          // Cause the config loader to run BEFORE the part loader this ensures
          // that the KIS configs are loaded after Module Manager has run but
          // before any parts are loaded so KIS aware part modules can add
          // pod inventories as necessary.
          list.Insert(i, cfgLoader);
          break;
        }
      }
    }
  }

  static List<Part> QueuedPodInventoryParts = new List<Part> ();
  /// <summary>Queues parts to have their pod inventories initialized</summary>
  /// <remarks>
  /// This is neccessary only when the part's initial CrewCapacity is 0 but has
  /// pod inventories that need to be initialized. Calling this for parts that
  /// have non-zero CrewCapacity is effectively a nop, and calling for parts
  /// that have no pod inventories should be harmless but best avoided.
  /// <remarks>
  /// <param name="part">The part to be queued.</param>
  public static void QueuePodInventoryPart (Part part)
  {
    if (part.CrewCapacity > 0) {
      // the part will be picked up automatically, no need to queue
      return;
    }
    QueuedPodInventoryParts.Add (part);
  }

  /// <summary>Adds seat inventories to cover the maximum pod occupancy.</summary>
  /// <remarks>
  /// If the part already has seat inventories, they will be adjusted to have the unique seat
  /// indexes. This is usefull if the part's config provides the needed number of modules. If number
  /// of the existing modules is not enough to cover <c>CrewCapacity</c>, extra modules are added.
  /// </remarks>
  /// <param name="part">The part to add seat inventorties for.</param>
  public static void AddPodInventories(Part part) {
    // Check the fields that once had unexpected values.
    if (part.partInfo == null) {
      HostedDebugLog.Error(part, "Unexpected part configuration: partInfo=<NULL>");
      return;
    }
    if (part.partInfo.partConfig == null) {
      HostedDebugLog.Error(part, "Unexpected part configuration: partConfig=<NULL>");
      return;
    }

    var checkInventories = part.Modules.OfType<ModuleKISInventory>()
        .Where(m => m.invType == ModuleKISInventory.InventoryType.Pod)
        .ToArray();
    var seatIndex = 0;
    foreach (var inventory in checkInventories) {
      HostedDebugLog.Info(
          inventory, "Assing seat to a pre-configured pod inventory: {0}", seatIndex);
      evaInventory.TryGetValue ("slotsX", ref inventory.slotsX);
      evaInventory.TryGetValue ("slotsY", ref inventory.slotsY);
      evaInventory.TryGetValue ("maxVolume", ref inventory.maxVolume);
      inventory.podSeat = seatIndex++;
    }
    while (seatIndex < part.CrewCapacity) {
      var moduleNode = new ConfigNode("MODULE", "Dynamically created by KIS.");
      evaInventory.CopyTo(moduleNode);
      moduleNode.SetValue("name", typeof(ModuleKISInventory).Name, createIfNotFound: true);
      moduleNode.SetValue(
          "invType", ModuleKISInventory.InventoryType.Pod.ToString(), createIfNotFound: true);
      moduleNode.SetValue("podSeat", seatIndex, createIfNotFound: true);
      part.partInfo.partConfig.AddNode(moduleNode);
      var inventory = part.AddModule(moduleNode, forceAwake: true);
      HostedDebugLog.Info(inventory, "Dynamically create pod inventory at seat: {0}", seatIndex);
      seatIndex++;
    }
  }

  /// <summary>Load config of EVA modules for the requested part name.</summary>
  static void UpdateEvaPrefab(string partName) {
    var partInfo = PartLoader.getPartInfoByName(partName);
    if (partInfo != null ){
      var prefab = partInfo.partPrefab;
      if (LoadModuleConfig(prefab, typeof(ModuleKISInventory), evaInventory)) {
        prefab.GetComponent<ModuleKISInventory>().invType = ModuleKISInventory.InventoryType.Eva;
      }
      LoadModuleConfig(prefab, typeof(ModuleKISPickup), evaPickup);
    } else {
      DebugEx.Info("Skipping EVA model: {0}. Expansion is not installed.", partName);
    }
  }

  /// <summary>Loads config values for the part's module from the provided config node.</summary>
  /// <returns><c>true</c> if loaded successfully.</returns>
  static bool LoadModuleConfig(Part p, Type moduleType, ConfigNode node) {
    var module = p.GetComponent(moduleType);
    if (module == null) {
      DebugEx.Warning(
          "Config node for module {0} in part {1} is NULL. Nothing to load!", moduleType, p);
      return false;
    }
    if (node == null) {
      DebugEx.Warning("Cannot find module {0} on part {1}. Config not loaded!", moduleType, p);
      return false;
    }
    var baseFields = new BaseFieldList(module);
    baseFields.Load(node);
    DebugEx.Info("Loaded config for {0} on part {1}", moduleType, p);
    return true;
  }

  /// <summary>Finds a bone to attach the equippable item to.</summary>
  /// <param name="root">The transform to start searching from.</param>
  /// <param name="bonePath">The hierarchy search pattern or a KIS alias.</param>
  /// <returns>The transform or <c>null</c> if nothing found.</returns>
  /// <seealso cref="KISAddonConfig.equipAliases"/>
  public static Transform FindEquipBone(Transform root, string bonePath) {
    Transform res;
    if (bonePath.StartsWith("alias", StringComparison.Ordinal)) {
      res = KISAddonConfig.equipAliases
          .Select(a => a.Split(new[] {','}, 2))
          .Where(pair => pair.Length == 2 && pair[0] == bonePath)
          .Select(pair => Hierarchy.FindTransformByPath(root, pair[1]))
          .FirstOrDefault(t => t != null);
      DebugEx.Fine("For alias '{0}' found transform: {1}", bonePath, res);
    } else {
      res = Hierarchy.FindTransformByPath(root, bonePath);
      DebugEx.Fine("For bone path '{0}' found transform: {1}", bonePath, res);
    }
    if (res == null) {
      DebugEx.Error("Cannot find object for EVA item: {0}", bonePath);
    }
    return res;
  }
}

}  // namespace
