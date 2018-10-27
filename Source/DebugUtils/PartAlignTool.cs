// Kerbal Attachment System
// Mod idea: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using UnityEngine;

namespace KIS {

  /// <summary>
  /// 
  /// </summary>
[KSPAddon(KSPAddon.Startup.FlightAndEditor, false /*once*/)]
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
sealed class PartAlignTool : MonoBehaviour, IHasGUI {

  #region Configuration settings
  /// <summary>Keyboard key to trigger the GUI.</summary>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  [PersistentField("Debug/partAlignToolKey")]
  public string openGUIKey = "";
  #endregion

  #region Local fields
  const string DialogTitle = "Part alignment tool";
  
  /// <summary>Actual screen position of the console window.</summary>
  static Rect windowRect = new Rect(100, 100, 400, 1);

  /// <summary>A title bar location.</summary>
  static Rect titleBarRect = new Rect(0, 0, 10000, 20);

  /// <summary>A list of actions to apply at the end of the GUI frame.</summary>
  static readonly GuiActionsList guiActions = new GuiActionsList();

  /// <summary>Style to draw a control of the minimum size.</summary>
  static readonly GUILayoutOption MinSizeLayout = GUILayout.ExpandWidth(false);

  /// <summary>Keyboard event that opens/closes the remote GUI.</summary>
  static Event openGUIEvent;

  /// <summary>Tells if GUI is open.</summary>
  bool isGUIOpen;

  /// <summary>The part being adjusted.</summary>
  /// <remarks>
  /// If it's not a <see cref="ModuleKISItem"/> compatible, then nothing can be adjusted.
  /// </remarks>
  Part parentPart;

  /// <summary>Tells if the parent part capture mode is enabled.</summary>
  bool parentPartTracking;

  /// <summary>The item module to adjust. Can be <c>null</c>.</summary>
  ModuleKISItem itemModule;

  /// <summary>Ediable field for the equip item position.</summary>
  readonly GUILayoutTextField<Vector3> itemPosition = new GUILayoutTextField<Vector3>(
      v => string.Format("{0}, {1}, {2}", v.x, v.y, v.z),
      s => ConfigNode.ParseVector3(s),
      useOwnLayout: false);

  /// <summary>Ediable field for the equip item direction.</summary>
  readonly GUILayoutTextField<Vector3> itemDirection = new GUILayoutTextField<Vector3>(
      v => string.Format("{0}, {1}, {2}", v.x, v.y, v.z),
      s => ConfigNode.ParseVector3(s),
      useOwnLayout: false);
  #endregion

  #region GUI styles & contents
  GUIStyle guiNoWrapStyle;
  GUIStyle guiCaptionStyle;
  GUIStyle guiValueStyle;
  #endregion

  #region IHasGUI implementation
  /// <inheritdoc/>
  public void OnGUI() {
    //FIXME: check if debug events enabled!
    if (openGUIEvent != null && Event.current.Equals(openGUIEvent)) {
      Event.current.Use();
      isGUIOpen = !isGUIOpen;
    }
    if (isGUIOpen) {
      windowRect = GUILayout.Window(
          GetInstanceID(), windowRect, ConsoleWindowFunc, DialogTitle,
          GUILayout.MaxHeight(1), GUILayout.MinWidth(300));
    }
  }
  #endregion

  #region MonoBehavour methods
  void Awake() {
    ConfigAccessor.ReadFieldsInType(GetType(), instance: this);
    if (!string.IsNullOrEmpty(openGUIKey)) {
      DebugEx.Info("EqippedItemAlignTool controller created");
      openGUIEvent = Event.KeyboardEvent(openGUIKey);
    }
  }
  #endregion

  /// <summary>Shows a window that displays the winch controls.</summary>
  /// <param name="windowId">Window ID.</param>
  void ConsoleWindowFunc(int windowId) {
    MakeGuiStyles();

    if (guiActions.ExecutePendingGuiActions()) {
      if (parentPartTracking) {
        SetPart(Mouse.HoveredPart);
      }
      if (parentPartTracking && Input.GetMouseButtonDown(0)) {
        parentPartTracking = false;
      }
    }

    using (new GUILayout.VerticalScope(GUI.skin.box)) {
      using (new GuiEnabledStateScope(!parentPartTracking)) {
        if (GUILayout.Button("Set part")) {
          guiActions.Add(() => parentPartTracking = true);
        }
      }
      using (new GuiEnabledStateScope(parentPartTracking)) {
        if (GUILayout.Button("Cancel set mode...")) {
          guiActions.Add(() => parentPartTracking = false);
        }
      }
      var parentPartName =
          "Part: " + (parentPart != null ? DbgFormatter.PartId(parentPart) : "NONE");
      GUILayout.Label(parentPartName, guiNoWrapStyle);
    }

    if (parentPart != null && itemModule != null
        && (itemModule.equipable || itemModule.carriable)) {
      GUILayout.Label("KIS Item detected:");
      using (new GUILayout.VerticalScope(GUI.skin.box)) {
        using (new GUILayout.HorizontalScope(GUI.skin.box)) {
          GUILayout.Label("Equip pos (metres):", guiCaptionStyle);
          GUILayout.FlexibleSpace();
          itemModule.equipPos = itemPosition.UpdateFrame(
              itemModule.equipPos, guiValueStyle, new[] {GUILayout.Width(100)});
        }
        using (new GUILayout.HorizontalScope(GUI.skin.box)) {
          GUILayout.Label("Equip dir (euler degrees):", guiCaptionStyle);
          GUILayout.FlexibleSpace();
          itemModule.equipDir = itemDirection.UpdateFrame(
              itemModule.equipDir, guiValueStyle, new[] {GUILayout.Width(100)});
        }
      }
    }

    if (GUILayout.Button("Close", GUILayout.ExpandWidth(false))) {
      guiActions.Add(() => isGUIOpen = false);
    }

    // Allow the window to be dragged by its title bar.
    GuiWindow.DragWindow(ref windowRect, titleBarRect);
  }

  /// <summary>Sets the part to be adjusted.</summary>
  /// <param name="part">The part to set.</param>
  void SetPart(Part part) {
    parentPart = part;
    if (part != null) {
      itemModule = part.partInfo.partPrefab.GetComponent<ModuleKISItem>();
      if (itemModule != null) {
        itemPosition.Reset();
        itemDirection.Reset();
      }
    } else {
      itemModule = null;
    }
  }

  /// <summary>Creates the styles. Only does it once.</summary>
  void MakeGuiStyles() {
    if (guiNoWrapStyle == null) {
      guiNoWrapStyle = new GUIStyle(GUI.skin.box);
      guiNoWrapStyle.wordWrap = false;
      guiCaptionStyle = new GUIStyle(GUI.skin.label);
      guiCaptionStyle.wordWrap = false;
      guiCaptionStyle.alignment = TextAnchor.MiddleLeft;
      guiCaptionStyle.padding = GUI.skin.textField.padding;
      guiCaptionStyle.margin = GUI.skin.textField.margin;
      guiCaptionStyle.border = GUI.skin.textField.border;
      guiValueStyle = new GUIStyle(GUI.skin.label);
      guiValueStyle.padding = new RectOffset(0, 0, 0, 0);
      guiValueStyle.margin = new RectOffset(0, 0, 0, 0);
      guiValueStyle.border = new RectOffset(0, 0, 0, 0);
      guiValueStyle.alignment = TextAnchor.MiddleRight;
    }
  }
}

}  // namespace
