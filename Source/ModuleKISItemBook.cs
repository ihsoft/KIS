// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using KSPDev.LogUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_07007.
public sealed class ModuleKISItemBook: ModuleKISItem,
    // KSPDEV sugar interfaces.
    IPartModule {

  #region Localizable GUI strings.
  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_07000",
      defaultTemplate: "KIS Guide",
      description: "The title of the module to present in the editor details window.");
  
  static readonly Message PrimaryBookField = new Message(
      "#kisLOC_07001",
      defaultTemplate: "The last resort manual",
      description: "The info message to present in the editor's details window to designate the"
      + " fact that this item is for the learning purposes only.");

  static readonly Message ReaderWindowTitle = new Message(
      "#kisLOC_07002",
      defaultTemplate: "Reader",
      description: "The title for the window that shows the guide pages.");

  static readonly Message PreviousPageBtn = new Message(
      "#kisLOC_07003",
      defaultTemplate: "Previous page",
      description: "The caption on the button that navigates to the previous page.");

  static readonly Message NextPageBtn = new Message(
      "#kisLOC_07004",
      defaultTemplate: "Next page",
      description: "The caption on the button that navigates to the next page.");

  static readonly Message CloseBtn = new Message(
      "#kisLOC_07005",
      defaultTemplate: "Close",
      description: "The caption on the button that closes the guide window.");

  static readonly Message<int, int> CurrentPageTxt = new Message<int, int>(
      "#kisLOC_07006",
      defaultTemplate: "Page <<1>> / <<2>>",
      description: "The string in the reader window that displays the current page number."
      + "\nArgument <<1>> is the number of the current page."
      + "\nArgument <<2>> is the total number of the pages.");
  #endregion

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

  #region PartModule overrides
  /// <inheritdoc/>
  public override string GetModuleDisplayName() {
    return ModuleTitleInfo;
  }
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
      DebugEx.Info("The book has no pages configured");
    }      
  }

  /// <inheritdoc/>
  public override void OnItemGUI(KIS_Item item) {
    if (showPage) {
      GUI.skin = HighLogic.Skin;
      currentItem = item;
      guiWindowPos = GUILayout.Window(GetInstanceID(), guiWindowPos, GuiReader, ReaderWindowTitle);
    }
  }
  #endregion
  
  #region Inheritable & customization methods
  /// <inheritdoc/>
  protected override IEnumerable<string> GetPropInfo() {
    return base.GetPropInfo().Concat(new[] {
        PrimaryBookField.Format(),
    });
  }
  #endregion

  #region Local utility methods
  void GuiReader(int windowID) {
    GUILayout.Box("", GUILayout.Width(pageWidth), GUILayout.Height(pageHeight));
    Rect textureRect = GUILayoutUtility.GetLastRect();
    GUI.DrawTexture(textureRect, pageTexture, ScaleMode.ScaleToFit);
          
    GUILayout.BeginHorizontal();
    if (GUILayout.Button(PreviousPageBtn)) {
      if ((pageIndex - 1) >= 0) {
        pageIndex = pageIndex - 1;
        pageTexture = GameDatabase.Instance.GetTexture(pageList[pageIndex], false);
        UISoundPlayer.instance.Play(bookPageSndPath);
      }
    }
    GUILayout.Label(CurrentPageTxt.Format(pageIndex + 1, pageTotal));
    if (GUILayout.Button(NextPageBtn)) {
      if ((pageIndex + 1) < pageList.Count) {
        pageIndex = pageIndex + 1;
        pageTexture = GameDatabase.Instance.GetTexture(pageList[pageIndex], false);
        UISoundPlayer.instance.Play(bookPageSndPath);
      }
    }
    GUILayout.EndHorizontal();

    if (GUILayout.Button(CloseBtn)) {
      showPage = false;
      UISoundPlayer.instance.Play(bookCloseSndPath);
    }
    GUI.DragWindow();
  }
  #endregion
}

}  // namespace
