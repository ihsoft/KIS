using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS {

/// <summary>A class to handle editor KIS part's category.</summary>
[KSPAddon(KSPAddon.Startup.MainMenu, true)]
sealed class KISAddonEditorFilter : MonoBehaviour {
  static List<AvailablePart> avPartItems = new List<AvailablePart>();
  const string category = "Filter by Function";
  const string subCategoryTitle = "EVA Items";
  const string iconName = "R&D_node_icon_evatech";

  void Awake() {
    GameEvents.onGUIEditorToolbarReady.Add(SubCategories);

    avPartItems.Clear();
    foreach (AvailablePart avPart in PartLoader.LoadedPartsList) {
      if (avPart.name == "kerbalEVA" || avPart.name == "kerbalEVA_RD" || !avPart.partPrefab) {
        continue;
      }
      ModuleKISItem moduleItem = avPart.partPrefab.GetComponent<ModuleKISItem>();
      if (moduleItem && moduleItem.editorItemsCategory) {
        avPartItems.Add(avPart);
      }
    }
  }

  bool EditorItemsFilter(AvailablePart avPart) {
    return avPartItems.Contains(avPart);
  }

  void SubCategories() {
    RUI.Icons.Selectable.Icon icon = PartCategorizer.Instance.iconLoader.GetIcon(iconName);
    PartCategorizer.Category Filter =
        PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
    PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, icon, EditorItemsFilter);
  }
}

}  // namespace
