using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;


using System.Text;

namespace KIS {

[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
[PersistentFieldsFile("KIS/settings.cfg", "KISConfig")]
class KISAddonConfig : MonoBehaviour {
  [PersistentField("StackableItemOverride/partName", isCollection = true)]
  public static List<string> stackableList = new List<string>();

  [PersistentField("StackableModule/moduleName", isCollection = true)]
  public static List<string> stackableModules = new List<string>();

  [PersistentField("Global/breathableAtmoPressure")]
  public static float breathableAtmoPressure = 0.5f;

  const string MaleKerbalEva = "kerbalEVA";
  const string FemaleKerbalEva = "kerbalEVAfemale";
  const string RdKerbalEva = "kerbalEVA_RD";
  
  public void Awake() {
    ConfigAccessor.ReadFieldsInType(GetType(), this);
    ConfigAccessor.ReadFieldsInType(typeof(ModuleKISInventory), instance: null);

    // Set inventory module for every eva kerbal
    Logger.logInfo("Set KIS config...");
    ConfigNode nodeSettings = GameDatabase.Instance.GetConfigNode("KIS/settings/KISConfig");
    if (nodeSettings == null) {
      Logger.logError("KIS settings.cfg not found or invalid !");
      return;
    }

    // Male Kerbal.
    UpdateEvaPrefab(PartLoader.getPartInfoByName(MaleKerbalEva), nodeSettings);
    // Female Kerbal.
    UpdateEvaPrefab(PartLoader.getPartInfoByName(FemaleKerbalEva), nodeSettings);
    
    // Set inventory module for every pod with crew capacity.
    Logger.logInfo("Loading pod inventories...");
    foreach (AvailablePart avPart in PartLoader.LoadedPartsList) {
      if (avPart.name == MaleKerbalEva || avPart.name == FemaleKerbalEva
          || avPart.name == RdKerbalEva
          || !avPart.partPrefab || avPart.partPrefab.CrewCapacity < 1) {
        continue;
      }
      Logger.logInfo("Found part with CrewCapacity: {0}", avPart.name);
      for (int i = 0; i < avPart.partPrefab.CrewCapacity; i++) {
        try {
          var moduleInventory =
              avPart.partPrefab.AddModule("ModuleKISInventory") as ModuleKISInventory;
          SetInventoryConfig(moduleInventory, nodeSettings);
          moduleInventory.podSeat = i;
          moduleInventory.invType = ModuleKISInventory.InventoryType.Pod;
          Logger.logInfo("Pod inventory module(s) for seat {0} loaded successfully", i);
        } catch {
          Logger.logError("Pod inventory module(s) for seat {0} can't be loaded!", i);
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

  void UpdateEvaPrefab(AvailablePart avPart, ConfigNode nodeSettings) {
    var prefab = avPart.partPrefab;
    // Adding module to EVA may cause an NPE excetption but work module update will still work.
    try {
      prefab.AddModule(typeof(ModuleKISInventory).Name);
    } catch (Exception ex) {
      Logger.logInfo("Ignoring error adding ModuleKISInventory to {0}: {1}", prefab, ex);
    }
    try {
      prefab.AddModule(typeof(ModuleKISPickup).Name);
    } catch (Exception ex) {
      Logger.logInfo("Ignoring error adding ModuleKISPickup to {0}: {1}", prefab, ex);
    }

    // Setup inventory module for eva.
    var evaInventory = prefab.GetComponent<ModuleKISInventory>();
    if (evaInventory) {
      SetInventoryConfig(evaInventory, nodeSettings);
      evaInventory.invType = ModuleKISInventory.InventoryType.Eva;
      Logger.logInfo("Eva inventory module loaded successfully");
    }

    // Load KSP fields for ModuleKISPickup module.
    var nodeEvaPickup = nodeSettings.GetNode("EvaPickup");
    var evaPickup = prefab.GetComponent<ModuleKISPickup>();
    if (evaPickup && nodeEvaPickup != null) {
      var fields = new BaseFieldList(evaPickup);
      fields.Load(nodeEvaPickup);
      Logger.logInfo("Eva pickup module loaded successfully");
    }
  }
}

}  // namespace
