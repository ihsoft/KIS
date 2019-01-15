// Kerbal Inventory System
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using UnityEngine;
using System.Linq;

namespace KIS.Debug {

/// <summary>Class that shows a debug dialog to swpan an arbitrary item in the inventory.</summary>
class SpawnItemDialog : MonoBehaviour, IHasGUI {

  #region Local fields and proeprties
  /// <summary>Maximum number of matches to present when doing the search.</summary>
  const int MaxFoundItems = 10;

  static GameObject dialog;
  ModuleKISInventory tgtInventory;
  string searchText;
  string createQuantity;
  Rect guiMainWindowPos;
  AvailablePart[] foundMatches = {};
  GuiActionsList guiActionList = new GuiActionsList();
  #endregion

  /// <summary>Presents the span item dialog.</summary>
  /// <remarks>
  /// There can be only one dialog active. If a dialog for a different inventory is requested, then
  /// it substitutes any dialog that was presented before.
  /// </remarks>
  /// <param name="inventory">The inventory to bound the dialog to.</param>
  public static void ShowDialog(ModuleKISInventory inventory) {
    if (dialog != null) {
      Object.Destroy(dialog);
    }
    dialog = new GameObject("KisDebug-ItemSpawnDialog");
    var dlg = dialog.AddComponent<SpawnItemDialog>();
    dlg.tgtInventory = inventory;
    DebugEx.Info("Spawn item dialog created for inventory: {0}", inventory);
  }

  #region IHasGUI implementation
  /// <inheritdoc/>
  public void OnGUI() {
    guiMainWindowPos = GUILayout.Window(
        GetInstanceID(), guiMainWindowPos, GuiMain, "KIS spawn item dialog", GUILayout.Height(0));
  }
  #endregion

  #region Local utility methods
  /// <summary>Main GUI method.</summary>
  void GuiMain(int windowID) {
    guiActionList.ExecutePendingGuiActions();

    GUILayout.Label("Inventory: " + DebugEx.ObjectToString(tgtInventory), GUI.skin.box);
    GUILayout.Label("NOTE: The item's volume and mass are NOT checked!");
    using (new GUILayout.HorizontalScope(GUI.skin.box)) {
      GUILayout.Label("Search:");
      GUI.changed = false;
      searchText = GUILayout.TextField(searchText, GUILayout.Width(300));
      if (GUI.changed) {
        guiActionList.Add(() => GuiUpdateSearch(searchText));
      }
      GUILayout.Label("Quantity:");
      var oldQuantity = createQuantity;
      createQuantity = GUILayout.TextField(createQuantity, GUILayout.Width(50));
      int newQuantity;
      if (!int.TryParse(createQuantity, out newQuantity)) {
        createQuantity = oldQuantity;
      }
    }
    
    if (searchText.Length < 3) {
      GUILayout.Label("...give at least 3 characters...", GUI.skin.box);
    } else if (foundMatches.Length == 0) {
      GUILayout.Label("...nothing is found for the pattern...", GUI.skin.box);
    } else {
      //foreach (var p in foundMatches) {
      for (var i = 0; i < MaxFoundItems && i < foundMatches.Length; i++) {
        var p = foundMatches[i];
        using (new GUILayout.HorizontalScope(GUI.skin.box)) {
          GUILayout.Label(p.title + " (" + p.name + ")");
          GUILayout.FlexibleSpace();
          if (GUILayout.Button("Add", GUILayout.ExpandHeight(true))) {
            guiActionList.Add(() => GuiSpawnItems(p));
          }
        }
      }
      if (foundMatches.Length > MaxFoundItems) {
        GUILayout.Label("...there are more, but they are not shown...");
      }
    }
    
    if (GUILayout.Button("Close")) {
      Object.Destroy(gameObject);
    }
    GUI.DragWindow(new Rect(0, 0, 10000, 30));
  }

  /// <summary>Refreshes the list of found candidates.</summary>
  void GuiUpdateSearch(string pattern) {
    if (pattern.Length < 3) {
      foundMatches = new AvailablePart[0];
      return;
    }
    pattern = pattern.ToLower();
    foundMatches = PartLoader.Instance.loadedParts
        .Where(p => p.name.ToLower().Contains(pattern) || p.title.Contains(pattern))
        .OrderBy(p => p.name)
        .Take(MaxFoundItems + 1)
        .ToArray();
  }

  /// <summary>Spawns the item in the inventory.</summary>
  void GuiSpawnItems(AvailablePart p) {
    var node = new ConfigNode("PART");
    node.AddNode(p.partConfig.CreateCopy());
    tgtInventory.AddItem(p, node, qty: int.Parse(createQuantity));
  }

  /// <summary>Initializes the dialog.</summary>
  void Awake() {
    searchText = "";
    createQuantity = "1";
    GuiUpdateSearch(searchText);
    guiMainWindowPos = new Rect(100, 100, 500, 0);
  }
  #endregion
}

}  // namespace
