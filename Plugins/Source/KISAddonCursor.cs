using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS {

[KSPAddon(KSPAddon.Startup.EveryScene, false)]
sealed class KISAddonCursor : MonoBehaviour {
  static bool cursorShow = false;
  static bool partDetectionActive = false;
      
  static Texture2D cursorTexture = null;
  static string cursorText;
  static List<string> cursorAdditionalTexts;
  public static Part hoveredPart = null;
  public static int partClickedFrame = -1;
  static OnMousePartAction delegateOnMousePartClick;
  static OnMousePartAction delegateOnMouseEnterPart;
  static OnMousePartAction delegateOnMouseHoverPart;
  static OnMousePartAction delegateOnMouseExitPart;
  public delegate void OnMousePartAction(Part part);

  // Cursor hint text settings.
  const int ActionIconSize = 24;
  // It's quare.
  const int HintFontSize = 10;
  const int HintTextLeftMargin = 4;
  // A gap between action icon and the text.
  static Color hintBackground = new Color(0.0f, 0.0f, 0.0f, 0.5f);
  static GUIStyle hintWindowStyle = new GUIStyle {
    normal = {
      background = CreateTextureFromColour(hintBackground),
      textColor = Color.white
    },
    padding = new RectOffset(3, 3, 3, 3),
    fontSize = HintFontSize
  };

  public static void StartPartDetection() {
    StartPartDetection(null, null, null, null);
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

  public void Update() {
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
        Part part = Mouse.HoveredPart;
        if (part && delegateOnMousePartClick != null) {
          delegateOnMousePartClick(part);
        }
      }
    }
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

  void OnGUI() {
    if (cursorShow) {
      // Display action icon.
      GUI.DrawTexture(
          new Rect(Event.current.mousePosition.x - ActionIconSize / 2,
                  Event.current.mousePosition.y - ActionIconSize / 2,
                  ActionIconSize, ActionIconSize),
          cursorTexture, ScaleMode.ScaleToFit);
              
      // Compile the whole hint text.
      var allLines = new List<String>{ cursorText };
      if (cursorAdditionalTexts != null && cursorAdditionalTexts.Any()) {
        allLines.Add("");  // A linefeed between status and hint text. 
        allLines.AddRange(cursorAdditionalTexts);
      }
      var hintText = String.Join("\n", allLines.ToArray());
      // Calculate the label region.
      Vector2 textSize = hintWindowStyle.CalcSize(new GUIContent(hintText));
      var hintLabelRect = new Rect(
          Event.current.mousePosition.x + ActionIconSize / 2 + HintTextLeftMargin,
          Event.current.mousePosition.y - ActionIconSize / 2,
          textSize.x, textSize.y);

      GUI.Label(hintLabelRect, hintText, hintWindowStyle);
    }
  }
}

}  // namespace
