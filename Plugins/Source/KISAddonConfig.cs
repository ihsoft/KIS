using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
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
            avPart.partPrefab.AddModule(typeof(ModuleKISInventory).Name) as ModuleKISInventory;
          CallAwakeMethod(moduleInventory);
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
    // Adding module to EVA may cause an NPE but module update will still work.
    try {
      prefab.AddModule(typeof(ModuleKISInventory).Name);
    } catch (Exception ex) {
      Logger.logInfo(
          "NOT A BUG! Ignoring error while adding ModuleKISInventory to {0}: {1}", prefab, ex);
    }
    try {
      prefab.AddModule(typeof(ModuleKISPickup).Name);
    } catch (Exception ex) {
      Logger.logInfo("NOT A BUG! Ignoring error adding ModuleKISPickup to {0}: {1}", prefab, ex);
    }

    // Setup inventory module for eva.
    var evaInventory = prefab.GetComponent<ModuleKISInventory>();
    CallAwakeMethod(evaInventory);
    if (evaInventory) {
      SetInventoryConfig(evaInventory, nodeSettings);
      evaInventory.invType = ModuleKISInventory.InventoryType.Eva;
      Logger.logInfo("Eva inventory module loaded successfully");
    }

    // Load KSP fields for ModuleKISPickup module.
    var nodeEvaPickup = nodeSettings.GetNode("EvaPickup");
    var evaPickup = prefab.GetComponent<ModuleKISPickup>();
    CallAwakeMethod(evaPickup);
    if (evaPickup && nodeEvaPickup != null) {
      var fields = new BaseFieldList(evaPickup);
      fields.Load(nodeEvaPickup);
      Logger.logInfo("Eva pickup module loaded successfully");
    }
  }

  /// <summary>Makes a call to <c>Awake()</c> method of the part module.</summary>
  /// <remarks>Modules added to prefab via <c>AddModule()</c> call are not get activated as they
  /// would if activated by the Unity core. As a result some vital fields may be left uninitialized
  /// which may result in an NRE later when working with the prefab (e.g. making a part snapshot).
  /// This method finds and invokes method Awakes() via reflect which is normally done by Unity.
  /// <para>This is a HACK since <c>Awake()</c> method is not supposed to be called by anone one but
  /// Unity. For now it works fine but one day it may awake the kraken.</para>
  /// <para>Private method can only be accessed via reflection when requested on the class that
  /// declares it. I.e. method info must be requested from <c>PartModule</c> and not from a
  /// descendant.</para>
  /// </remarks>
  /// <param name="instance">A module instance.</param>
  static void CallAwakeMethod(object instance) {
    var moduleAwakeMethod = typeof(PartModule).GetMethod(
        "Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (moduleAwakeMethod != null) {
      moduleAwakeMethod.Invoke(instance, new object[] {});
    } else {
      Logger.logError("Cannot found Awake() method on {0}. Skip awakening of the component: {1}",
                      instance.GetType(), instance);
    }
  }
}

}  // namespace
