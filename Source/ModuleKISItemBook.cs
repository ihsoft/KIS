using KSPDev.GUIUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

public sealed class ModuleKISItemBook: ModuleKISItem {
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

  private int pageIndex = 0;
  private int pageTotal = 0;
  private List<string> pageList = new List<string>();
  private bool showPage = false;
  private Texture2D pageTexture;
  public Rect guiWindowPos;
  private KIS_Item currentItem;

  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    pageList.Clear();
    ConfigNode node = KIS_Shared.GetBaseConfigNode(this);
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

  public override void OnItemGUI(KIS_Item item) {
    if (showPage) {
      GUI.skin = HighLogic.Skin;
      currentItem = item;
      guiWindowPos = GUILayout.Window(GetInstanceID(), guiWindowPos, GuiReader, "Reader");
    }
  }

  private void GuiReader(int windowID) {
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
}

}  // namespace
