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
public sealed class ModuleKISItemBook : ModuleKISItem,
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
  [Debug.KISDebugAdjustableAttribute("Page width")]
  public int pageWidth = 800;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Page height")]
  public int pageHeight = 800;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Book open")]
  public string bookOpenSndPath = "KIS/Sounds/bookOpen";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Page selected")]
  public string bookPageSndPath = "KIS/Sounds/bookPage";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Book close")]
  public string bookCloseSndPath = "KIS/Sounds/bookClose";
  #endregion

  #region Helper classes
  /// <summary>Simple class to present a GUI dialog.</summary>
  class GuiDialog : MonoBehaviour, IHasGUI {
    /// <summary>Main window function to call from <c>OnGUI</c> method.</summary>
    public GUI.WindowFunction dialogFunction;

    /// <summary>Current dialog size and position.</summary>
    Rect guiMainWindowPos;

    #region IHasGUI implementation
    public void OnGUI() {
      GUIStyle currentStyle = GUI.skin.GetStyle("Window");
      currentStyle.fontSize = (int)Math.Round(11.0 * GameSettings.UI_SCALE * GameSettings.UI_SCALE_APPS);
      guiMainWindowPos = GUILayout.Window(
          GetInstanceID(), guiMainWindowPos, dialogFunction, ModuleTitleInfo,currentStyle);
    }
    #endregion
  }
  #endregion

  #region Local fields
  int pageIndex;
  int pageTotal;
  readonly List<string> pageList = new List<string>();
  Texture2D pageTexture;
  GameObject guiObj;
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
      guiObj = new GameObject("KISManualDialog-" + part.flightID);
      var dlg = guiObj.AddComponent<GuiDialog>();
      dlg.dialogFunction = GuiReader;
      
      pageTexture = GameDatabase.Instance.GetTexture(pageList[0], false);
      UISoundPlayer.instance.Play(bookOpenSndPath);
    } else {
      DebugEx.Info("The book has no pages configured");
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
    GUIStyle buttonStyle = GUI.skin.GetStyle("Button");
    buttonStyle.fontSize = (int)Math.Round(11.0 * GameSettings.UI_SCALE * GameSettings.UI_SCALE_APPS);
    if (GUILayout.Button(PreviousPageBtn, buttonStyle)) {
      if (pageIndex - 1 >= 0) {
        pageIndex = pageIndex - 1;
        pageTexture = GameDatabase.Instance.GetTexture(pageList[pageIndex], false);
        UISoundPlayer.instance.Play(bookPageSndPath);
      }
    }
    GUIStyle labelStyle = GUI.skin.GetStyle("Label");
    labelStyle.fontSize = (int)Math.Round(11.0 * GameSettings.UI_SCALE * GameSettings.UI_SCALE_APPS);
    GUILayout.Label(CurrentPageTxt.Format(pageIndex + 1, pageTotal),labelStyle);
    if (GUILayout.Button(NextPageBtn, buttonStyle)) {
      if (pageIndex + 1 < pageTotal) {
        pageIndex = pageIndex + 1;
        pageTexture = GameDatabase.Instance.GetTexture(pageList[pageIndex], false);
        UISoundPlayer.instance.Play(bookPageSndPath);
      }
    }
    GUILayout.EndHorizontal();

    if (GUILayout.Button(CloseBtn, buttonStyle)) {
      UISoundPlayer.instance.Play(bookCloseSndPath);
      Destroy(guiObj);
      guiObj = null;
    }
    GUI.DragWindow();
  }
  #endregion
}

}  // namespace
