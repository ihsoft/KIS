// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

public sealed class ModuleKISItemBook: ModuleKISItem,
    // KSPDEV sugar interfaces.
    IPartModule {
  #region Part's config fields
  [KSPField]
  public int pageWidth = 800;
  [KSPField]
  public int pageHeight = 800;
  [KSPField]
  public string bookOpenSndPath = "KIS/Sounds/bookOpen";
  [KSPField]
  public string bookPageSndPath = "KIS/Sounds/bookPage";
  [KSPField]
  public string bookCloseSndPath = "KIS/Sounds/bookClose";
  #endregion

  #region Local fields
  int pageIndex = 0;
  int pageTotal = 0;
  List<string> pageList = new List<string>();
  bool showPage = false;
  Texture2D pageTexture;
  Rect guiWindowPos;
  KIS_Item currentItem;
  #endregion

  #region ModuleKISItem overrides
  /// <inheritdoc/>
  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    pageList.Clear();
    var node = KIS_Shared.GetBaseConfigNode(this);
    foreach (string page in node.GetValues("page")) {
      pageList.Add(page);
    }
    if (pageList.Count > 0) {
      pageIndex = 0;
      pageTotal = pageList.Count;
      pageTexture = GameDatabase.Instance.GetTexture(pageList[0], false);
      showPage = true;
      UISoundPlayer.instance.Play(bookOpenSndPath);
    } else {
      Debug.LogError("The book has no pages configured");
    }      
  }

  /// <inheritdoc/>
  public override void OnItemGUI(KIS_Item item) {
    if (showPage) {
      GUI.skin = HighLogic.Skin;
      currentItem = item;
      guiWindowPos = GUILayout.Window(GetInstanceID(), guiWindowPos, GuiReader, "Reader");
    }
  }
  #endregion

  #region Local utility methods
  void GuiReader(int windowID) {
    GUILayout.Box("", GUILayout.Width(pageWidth), GUILayout.Height(pageHeight));
    Rect textureRect = GUILayoutUtility.GetLastRect();
    GUI.DrawTexture(textureRect, pageTexture, ScaleMode.ScaleToFit);
          
    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Previous page")) {
      if ((pageIndex - 1) >= 0) {
        pageIndex = pageIndex - 1;
        pageTexture = GameDatabase.Instance.GetTexture(pageList[pageIndex], false);
        UISoundPlayer.instance.Play(bookPageSndPath);
      }
    }
    GUILayout.Label("Page " + (pageIndex + 1) + " / " + pageTotal);
    if (GUILayout.Button("Next page")) {
      if ((pageIndex + 1) < pageList.Count) {
        pageIndex = pageIndex + 1;
        pageTexture = GameDatabase.Instance.GetTexture(pageList[pageIndex], false);
        UISoundPlayer.instance.Play(bookPageSndPath);
      }
    }
    GUILayout.EndHorizontal();

    if (GUILayout.Button("Close")) {
      showPage = false;
      UISoundPlayer.instance.Play(bookCloseSndPath);
    }
    GUI.DragWindow();
  }
  #endregion
}

}  // namespace
