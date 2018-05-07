using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
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

  [PersistentField("Global/breathableAtmoPressure")]
  public static float breathableAtmoPressure = 0.5f;

  [PersistentField("EvaInventory")]
  readonly static PersistentConfigNode evaInventory = new PersistentConfigNode();
  
  [PersistentField("EvaPickup")]
  readonly static PersistentConfigNode evaPickup = new PersistentConfigNode();

  const string MaleKerbalEva = "kerbalEVA";
  const string FemaleKerbalEva = "kerbalEVAfemale";
  const string MaleKerbalEvaVintage = "kerbalEVAVintage";
  const string FemaleKerbalEvaVintage = "kerbalEVAfemaleVintage";
  const string RdKerbalEva = "kerbalEVA_RD";

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
      // Kerbal parts.
      UpdateEvaPrefab(MaleKerbalEva);
      UpdateEvaPrefab(MaleKerbalEvaVintage);
      UpdateEvaPrefab(FemaleKerbalEva);
      UpdateEvaPrefab(FemaleKerbalEvaVintage);

      // Set inventory module for every pod with crew capacity.
      DebugEx.Info("Loading pod inventories...");
      for (var i = 0; i < PartLoader.LoadedPartsList.Count; i++) {
        var avPart = PartLoader.LoadedPartsList[i];
        if (!(avPart.name == MaleKerbalEva || avPart.name == FemaleKerbalEva
              || avPart.name == MaleKerbalEvaVintage || avPart.name == FemaleKerbalEvaVintage
              || avPart.name == RdKerbalEva
              || !avPart.partPrefab || avPart.partPrefab.CrewCapacity < 1)) {
          DebugEx.Fine("Found part with crew: {0}, CrewCapacity={1}",
                       avPart.name, avPart.partPrefab.CrewCapacity);
          AddPodInventories(avPart.partPrefab, avPart.partPrefab.CrewCapacity);
        }
      }
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

  public static void AddPodInventories(Part part, int crewCapacity) {
    for (var i = 0; i < crewCapacity; i++) {
      var moduleInventory =
          part.AddModule(typeof(ModuleKISInventory).Name) as ModuleKISInventory;
      KIS_Shared.AwakePartModule(moduleInventory);
      moduleInventory.invType = ModuleKISInventory.InventoryType.Pod;
      DebugEx.Fine("{0}: Add pod inventory to match the capacity", part);
    }
    var podInventories = part.Modules.OfType<ModuleKISInventory>()
        .Where(m => m.invType == ModuleKISInventory.InventoryType.Pod)
        .ToArray();
    for (var i = 0; i < podInventories.Length; i++) {
      try {
        var baseFields = new BaseFieldList(podInventories[i]);
        baseFields.Load(evaInventory);
        podInventories[i].podSeat = i;
        DebugEx.Fine("{0}: Pod inventory for seat {1} loaded successfully", part, i);
      } catch {
        DebugEx.Error("{0}: Pod inventory module for seat {1} can't be loaded!", part, i);
      }
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

  /// <summary>Loads config values for the part's module fro the provided config node.</summary>
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
}

}  // namespace
