using KSPDev.ConfigUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
sealed class KISAddonConfig : MonoBehaviour {
  [PersistentField("StackableItemOverride/partName", isCollection = true)]
  public readonly static List<string> stackableList = new List<string>();

  [PersistentField("StackableModule/moduleName", isCollection = true)]
  public readonly static List<string> stackableModules = new List<string>();

  [PersistentField("Global/breathableAtmoPressure")]
  public readonly static float breathableAtmoPressure = 0.5f;

  const string MaleKerbalEva = "kerbalEVA";
  const string FemaleKerbalEva = "kerbalEVAfemale";
  const string RdKerbalEva = "kerbalEVA_RD";
  
  public void Awake() {
    ConfigAccessor.ReadFieldsInType(GetType(), this);
    ConfigAccessor.ReadFieldsInType(typeof(ModuleKISInventory), instance: null);

    // Set inventory module for every eva kerbal
    Debug.Log("Set KIS config...");
    ConfigNode nodeSettings = GameDatabase.Instance.GetConfigNode("KIS/settings/KISConfig");
    if (nodeSettings == null) {
      Debug.LogError("KIS settings.cfg not found or invalid !");
      return;
    }

    // Kerbal parts.
    UpdateEvaPrefab(MaleKerbalEva, nodeSettings);
    UpdateEvaPrefab(FemaleKerbalEva, nodeSettings);

    // Set inventory module for every pod with crew capacity.
    Debug.Log("Loading pod inventories...");
    foreach (AvailablePart avPart in PartLoader.LoadedPartsList) {
      if (avPart.name == MaleKerbalEva || avPart.name == FemaleKerbalEva
          || avPart.name == RdKerbalEva
          || !avPart.partPrefab || avPart.partPrefab.CrewCapacity < 1) {
        continue;
      }

      Debug.LogFormat("Found part with CrewCapacity: {0}", avPart.name);
      for (int i = 0; i < avPart.partPrefab.CrewCapacity; i++) {
        try {
          var moduleInventory =
            avPart.partPrefab.AddModule(typeof(ModuleKISInventory).Name) as ModuleKISInventory;
          KIS_Shared.AwakePartModule(moduleInventory);
          SetInventoryConfig(moduleInventory, nodeSettings);
          moduleInventory.podSeat = i;
          moduleInventory.invType = ModuleKISInventory.InventoryType.Pod;
          Debug.LogFormat("Pod inventory module(s) for seat {0} loaded successfully", i);
        } catch {
          Debug.LogErrorFormat("Pod inventory module(s) for seat {0} can't be loaded!", i);
        }
      }
    }
  }

  void SetInventoryConfig(Component moduleInventory, ConfigNode nodeSettings) {
    var nodeEvaInventory = nodeSettings.GetNode("EvaInventory");
    if (nodeEvaInventory != null) {
      var baseFields = new BaseFieldList(moduleInventory);
      baseFields.Load(nodeEvaInventory);
    }
  }

  /// <summary>Load config of EVA modules for the requested part name.</summary>
  void UpdateEvaPrefab(string partName, ConfigNode nodeSettings) {
    var prefab = PartLoader.getPartInfoByName(partName).partPrefab;
    if (LoadModuleConfig(prefab, typeof(ModuleKISInventory),
                         nodeSettings.GetNode("EvaInventory"))) {
      prefab.GetComponent<ModuleKISInventory>().invType = ModuleKISInventory.InventoryType.Eva;
    }
    LoadModuleConfig(prefab, typeof(ModuleKISPickup), nodeSettings.GetNode("EvaPickup"));
  }

  /// <summary>Loads config values for the part's module fro the provided config node.</summary>
  /// <returns><c>true</c> if loaded successfully.</returns>
  bool LoadModuleConfig(Part p, Type moduleType, ConfigNode node) {
    var module = p.GetComponent(moduleType);
    if (module == null) {
      Debug.LogWarningFormat("Config node for module {0} in part {1} is NULL. Nothing to load!",
                             moduleType, p.name);
      return false;
    }
    if (node == null) {
      Debug.LogWarningFormat("Cannot find module {0} on part {1}. Config not loaded!",
                             moduleType, p.name);
      return false;
    }
    var baseFields = new BaseFieldList(module);
    baseFields.Load(node);
    Debug.LogFormat("Loaded config for {0} on part {1}", moduleType, p.name);
    return true;
  }
}

}  // namespace
