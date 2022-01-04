using KSPDev.GUIUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using KSPDev.LogUtils;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.EveryScene, false)]
sealed class KISAddonCursor : MonoBehaviour {
  public static Part hoveredPart = null;
  public static int partClickedFrame = -1;
  public delegate void OnMousePartAction(Part part);

  static bool cursorShow = false;
  static bool partDetectionActive = false;
  static Texture2D cursorTexture = null;
  static string cursorText;
  static List<string> cursorAdditionalTexts;
  static OnMousePartAction delegateOnMousePartClick;
  static OnMousePartAction delegateOnMouseEnterPart;
  static OnMousePartAction delegateOnMouseHoverPart;
  static OnMousePartAction delegateOnMouseExitPart;

  // Cursor hint text settings.
  const int ActionIconSize = 24;
  const int HintFontSize = 10;
  HintOverlay hintOverlay;
  Vector2 _mousePosition;
  GuiScale _guiMainScale;

  public static void AbortPartDetection() {
    StartPartDetection(null, null, null, null);
    partDetectionActive = false;
  }

  public static void StartPartDetection(OnMousePartAction onMousePartClick,
                                        OnMousePartAction onMouseEnterPart,
                                        OnMousePartAction onMouseHoverPart,
                                        OnMousePartAction onMouseExitPart) {
    delegateOnMousePartClick = onMousePartClick;
    delegateOnMouseEnterPart = onMouseEnterPart;
    delegateOnMouseHoverPart = onMouseHoverPart;
    delegateOnMouseExitPart = onMouseExitPart;
    partDetectionActive = true;
  }

  public static void StopPartDetection() {
    partDetectionActive = false;
    if (hoveredPart && delegateOnMouseExitPart != null) {
      delegateOnMouseExitPart(hoveredPart);
    }
    if (hoveredPart != null) {
      KIS_Shared.SetHierarchySelection(hoveredPart, false /* isSelected */);
    }
    hoveredPart = null;
  }

  public static void CursorEnable(string texturePath, string text, string text2) {
    var texts = new List<String>();
    texts.Add(text2);
    CursorEnable(texturePath, text, texts);
  }

  public static void CursorEnable(string texturePath, string text,
                                  List<string> additionalTexts = null) {
    cursorShow = true;
    Cursor.visible = false;
    cursorTexture = GameDatabase.Instance.GetTexture(texturePath, false);
    cursorText = text;
    cursorAdditionalTexts = additionalTexts;
  }
      
  public static void CursorDefault() {
    cursorShow = false;
    Cursor.visible = true;
  }

  public static void CursorDisable() {
    cursorShow = false;
    Cursor.visible = false;
  }

  void Awake() {
    hintOverlay = new HintOverlay(
        () => HighLogic.Skin,
        () => new GUIStyle(GUI.skin.box) {
            padding = GUI.skin.button.padding,
            margin = GUI.skin.button.margin,
            alignment = TextAnchor.MiddleLeft,
            fontSize = HintFontSize,
        },
        adjustGuiScale: true);
    _guiMainScale = new GuiScale(getPivotFn: () => _mousePosition);
  }

  void Update() {
    if (partDetectionActive) {
      Part part = Mouse.HoveredPart;

      if (part != hoveredPart) {
        // OnMouseExit
        if (hoveredPart && delegateOnMouseExitPart != null) {
          delegateOnMouseExitPart(hoveredPart);
        }
        // OnMouseEnter
        if (part && delegateOnMouseEnterPart != null) {
          delegateOnMouseEnterPart(part);
        }
        hoveredPart = part;
      }

      if (hoveredPart) {
        // OnMouseDown
        if (Input.GetMouseButtonDown(0)) {
          partClickedFrame = Time.frameCount;  // Protection against false click trigger.
          if (delegateOnMousePartClick != null) {
            delegateOnMousePartClick(hoveredPart);
          }
        }
        // OnMouseOver
        if (delegateOnMouseHoverPart != null) {
          delegateOnMouseHoverPart(hoveredPart);
        }
      }
    }

    if (HighLogic.LoadedSceneIsEditor && Input.GetMouseButtonDown(0)) {
      if (InputLockManager.IsUnlocked(ControlTypes.EDITOR_PAD_PICK_PLACE)) {
        if (Mouse.HoveredPart != null && delegateOnMousePartClick != null) {
          delegateOnMousePartClick(Mouse.HoveredPart);
        }
      }
    }
  }

  void OnGUI() {
    if (cursorShow
        && (!KISAddonPointer.isRunning || !Input.GetKey(KISAddonConfig.hideHintKey))) {
      _mousePosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
      using (new GuiMatrixScope()) {
        _guiMainScale.UpdateMatrix();
        GUI.DrawTexture(
            new Rect(
                _mousePosition.x - ActionIconSize / 2, _mousePosition.y - ActionIconSize / 2, ActionIconSize,
                ActionIconSize), cursorTexture, ScaleMode.ScaleToFit);
      }

      if (KISAddonConfig.showHintText) {
        // Compile the whole hint text.
        var allLines = new List<String>{ cursorText };
        if (cursorAdditionalTexts != null && cursorAdditionalTexts.Any()) {
          allLines.Add("");  // A linefeed between status and hint text. 
          allLines.AddRange(cursorAdditionalTexts);
        }
        var hintText = String.Join("\n", allLines.ToArray());
        hintOverlay.text = hintText;
        hintOverlay.ShowAtCursor();
      }
    }
  }

  /// <summary>Makes a texture with the requested background color.</summary>
  /// <remarks>
  /// Borrowed from <see href="https://github.com/CYBUTEK/KerbalEngineer">KER Redux</see>
  /// </remarks>
  /// <param name="colour"></param>
  /// <returns></returns>
  static Texture2D CreateTextureFromColour(Color colour) {
    var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
    texture.SetPixel(1, 1, colour);
    texture.Apply();
    return texture;
  }
}

}  // namespace
