// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using KSPDev.ModelUtils;
using KSPDev.LogUtils;
using KSPDev.ProcessingUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.MainMenu, false)]
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
sealed class MainScreenTweaker : MonoBehaviour {

  public class ModelTweak {
    /// <summary>Just a name for the tweak. Used in logs.</summary>
    [PersistentField("tweakName")]
    public string tweakName = "";

    /// <summary>Specifies target model object's path pattern.</summary>
    /// <seealso cref="Hierarchy.FindTransformByPath"/>
    [PersistentField("modelNamePattern")]
    public string modelNamePattern = "";

    /// <summary>List of KIS item names to equip.</summary>
    [PersistentField("itemName", isCollection = true)]
    public List<string> itemNames = new List<string>();
  }

  /// <summary>Tells if tweaks should be applied.</summary>
  [PersistentField("MainScreenTweaker/enabled")]
  public bool twekerEnabled;
  
  /// <summary>Tells if all object paths in the scene needs to be logged.</summary>
  /// <remarks>Only enable it to get the full hierarchy dump.</remarks>
  [PersistentField("MainScreenTweaker/logAllObjects")]
  public bool logAllObjects;

  /// <summary>Full list of configured tweaks on the screan.</summary>
  [PersistentField("MainScreenTweaker/modelTweak", isCollection = true)]
  public readonly List<ModelTweak> modelTweaks = new List<ModelTweak>();

  /// <summary>All tweaks that are applied to the scene at the time.</summary>
  readonly List<TweakEquippableItem> sceneTweaks = new List<TweakEquippableItem>();

  void Awake() {
    ConfigAccessor.ReadFieldsInType(GetType(), this);
    if (twekerEnabled) {
      AsyncCall.CallOnEndOfFrame(this, WaitAndApplyTweaks);
    }
  }

  void Update() {
    foreach (var item in sceneTweaks) {
      item.OnUpdate();
    }
  }

  void WaitAndApplyTweaks() {
    var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
    foreach (var root in roots) {
      if (logAllObjects) {
        DebugEx.Fine("Objects at the root:\n{0}",
                     DbgFormatter.C2S(Hierarchy.ListHirerahcy(root.transform), separator: "\n"));
      }
      foreach (var tweak in modelTweaks) {
        var names = tweak.modelNamePattern.Split('/');
        var reducedNames = names.Skip(1).ToArray();
        // Try the first name part separately since the scene objects don't have a single root.
        if (names[0] == "**") {
          reducedNames = names;  // Try all children in the root. 
        } else if (!Hierarchy.PatternMatch(names[0], root.transform.name)) {
          continue;
        }
        var objTransform = Hierarchy.FindTransformByPath(root.transform, reducedNames);
        if (objTransform != null) {
          DebugEx.Info("Tweak '{0}' matched kerbal model: {1}", tweak.tweakName, objTransform);
          tweak.itemNames.ToList().ForEach(x => {
            var item = new TweakEquippableItem(x);
            item.ApplyTweak(objTransform.gameObject);
            sceneTweaks.Add(item);
          });
        }
      }
    }
  }
}

/// <summary>Service class that holds tweaks for an item.</summary>
sealed class TweakEquippableItem {
  GameObject equippedGameObj;
  Transform evaTransform;
  readonly AvailablePart avPart;
  readonly ModuleKISItem itemModule;

  public TweakEquippableItem(string partName) {
    avPart = PartLoader.getPartInfoByName(partName);
    if (avPart == null) {
      DebugEx.Error("Cannot find part {0} for main menu tweaker", partName);
    }
    itemModule = avPart.partPrefab.FindModuleImplementing<ModuleKISItem>();
    if (itemModule == null || !itemModule.equipable && !itemModule.carriable) {
      DebugEx.Warning("Part is not a KIS carriable/equippable item: {0}", avPart.name);
      avPart = null;
      return;
    }
  }
  
  public void ApplyTweak(GameObject modelObj) {
    if (avPart == null) {
      return;
    }
    evaTransform = KISAddonConfig.FindEquipBone(modelObj.transform, itemModule.equipBoneName);
    if (evaTransform != null) {
      var partModel = Hierarchy.GetPartModelTransform(avPart.partPrefab);
      equippedGameObj = UnityEngine.Object.Instantiate(partModel.gameObject);
      Hierarchy.MoveToParent(equippedGameObj.transform, evaTransform);
      DebugEx.Info("Equipped part on kerbal model in main screen: {0}", avPart.name);
    } else {
      DebugEx.Error("Failed finding model transforms for part {0}", avPart.name);
    }
  }

  /// <summary>Aligns item's position to the kerbal's model bone.</summary>
  public void OnUpdate() {
    if (equippedGameObj != null) {
      equippedGameObj.transform.rotation =
          evaTransform.rotation * Quaternion.Euler(itemModule.equipDir);
      equippedGameObj.transform.position = evaTransform.TransformPoint(itemModule.equipPos);
    }
  }
}

}  // namespace
