// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: https://github.com/KospY/KIS/blob/master/LICENSE.md

using KSPDev.LogUtils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

/// <summary>An EDITOR event listener to add default items to the pod's seats</summary>
/// <remarks>Items should only be added when a new pod is created in the editor. Any other cases of
/// pod creation must be ignored.</remarks>
[KSPAddon(KSPAddon.Startup.EditorAny, false /* once */)]
class EditorDefaultItemsAdder : MonoBehaviour {
  void Awake() {
    GameEvents.onEditorPartEvent.Add(OnEditPartCreate);
  }
  
  void OnDestroy() {
    GameEvents.onEditorPartEvent.Remove(OnEditPartCreate);
  }

  /// <summary>Adds default items to the pod's seats.</summary>
  /// <remarks>Items are only added to a part created in the editor. Thus, reacting on the editor
  /// event.</remarks>
  /// <param name="type">Unused.</param>
  /// <param name="p">A target part.</param>
  void OnEditPartCreate(ConstructionEventType type, Part p) {
    if (type != ConstructionEventType.PartCreated && type != ConstructionEventType.PartCopied) {
      return;
    }
    var inventories = p.GetComponents<ModuleKISInventory>();
    foreach (var inventory in inventories) {
      if (inventory.podSeat == 0 && ModuleKISInventory.defaultItemsForTheFirstSeat.Count > 0) {
        DebugEx.Info("Adding default item(s) into the first seat of part {0}: {1}",
                     p, DbgFormatter.C2S(ModuleKISInventory.defaultItemsForTheFirstSeat));
        AddItems(inventory, ModuleKISInventory.defaultItemsForTheFirstSeat);
      }
      if (inventory.podSeat != -1 && ModuleKISInventory.defaultItemsForAllSeats.Count > 0) {
        DebugEx.Info(
            "Adding default item(s) into seat's {0} inventory of part {1}: {2}",
            inventory.podSeat, p, DbgFormatter.C2S(ModuleKISInventory.defaultItemsForAllSeats));
        AddItems(inventory, ModuleKISInventory.defaultItemsForAllSeats);
      }
    }
  }

  /// <summary>Adds the specified items into the inventory.</summary>
  /// <param name="inventory">An inventory to add items into.</param>
  /// <param name="itemNames">A list of names of the parts to add.</param>
  void AddItems(ModuleKISInventory inventory, List<string> itemNames) {
    foreach (var defItemName in itemNames) {
      var defPart = PartLoader.getPartInfoByName(defItemName);
      if (defPart != null) {
        inventory.AddItem(defPart.partPrefab);
      } else {
        DebugEx.Info("Cannot make item {0} specified as a default for the pod seat", defItemName);
      }
    }
  }
}

}  // namespace
