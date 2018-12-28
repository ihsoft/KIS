// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using KSPDev.DebugUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSPDev.DebugUtils {

/// <summary>Debug dialog for adjusting the part configuration.</summary>
/// <seealso cref="DebugAdjustableAttribute"/>
public sealed class PartDebugAdjustmentDialog2 : MonoBehaviour,
    // KSPDev interfaces
    IHasGUI {

  #region Local fields
  /// <summary>Actual screen position of the console window.</summary>
  Rect windowRect = new Rect(100, 100, 1, 1);

  /// <summary>A title bar location.</summary>
  Rect titleBarRect = new Rect(0, 0, 10000, 20);

  /// <summary>A list of actions to apply at the end of the GUI frame.</summary>
  readonly GuiActionsList guiActions = new GuiActionsList();

  /// <summary>The part being adjusted.</summary>
  Part parentPart;

  /// <summary>Tells if the parent part capture mode is enabled.</summary>
  bool parentPartTracking;

  /// <summary>Array of the modules, available for the debug dialog.</summary>
  /// <remarks>The key of the pair is the module name.</remarks>
  KeyValuePair<string, IRenderableGUIControl[]>[] adjustableModules;

  /// <summary>The index of the selected module in <see cref="adjustableModules"/>.</summary>
  int selectedModule = -1;

  /// <summary>Scroll view for the adjustable modules.</summary>
  readonly GUILayoutVerticalScrollView mainScrollView = new GUILayoutVerticalScrollView();
  #endregion

  #region Dialog configurable settings
  /// <summary>Dialog title.</summary>
  internal string dialogTitle = "";

  /// <summary>Dialog width.</summary>
  internal float dialogWidth = 500.0f;

  /// <summary>Size of the controls for the values.</summary>
  internal float dialogValueSize = 250.0f;

  /// <summary>Controls group to show.</summary>
  internal string controlsGroup = "";

  /// <summary>Tells if this dialog must be bound to one part only.</summary>
  /// <remarks>
  /// There will be no part selection UI offered, so the caller must set the part. If the parent
  /// part dies, then the dialog automatically closes. 
  /// </remarks>
  /// <seealso cref="SetPart"/>
  internal bool lockToPart;
  #endregion

  #region IHasGUI implementation
  /// <inheritdoc/>
  public void OnGUI() {
    if (lockToPart && parentPart == null) {
      DebugEx.Info("Part has died, destroying debug dialog: {0}", dialogTitle);
      Object.Destroy(this);
      return;
    }
    windowRect = GUILayout.Window(
        GetInstanceID(), windowRect, ConsoleWindowFunc, dialogTitle,
        GUILayout.MaxHeight(1), GUILayout.Width(dialogWidth));
  }
  #endregion

  #region Public interface methods
  /// <summary>Sets the part to be adjusted.</summary>
  /// <param name="part">The part to set.</param>
  public void SetPart(Part part) {
    parentPart = part;
    if (part != null) {
      var adjustables = new List<KeyValuePair<string, IRenderableGUIControl[]>>();
      foreach (var module in part.Modules.Cast<PartModule>()) {
        var moduleControls = new List<DebugGui.DebugMemberInfo>()
            .Concat(DebugGui.GetAdjustableFields(module, group: controlsGroup))
            .Concat(DebugGui.GetAdjustableProperties(module, group: controlsGroup))
            .Concat(DebugGui.GetAdjustableActions(module, group: controlsGroup))
            .Select(m => new StdTypesDebugGuiControl(
                m.attr.caption, module,
                fieldInfo: m.fieldInfo, propertyInfo: m.propertyInfo, methodInfo: m.methodInfo)
            )
            .ToArray();
        if (moduleControls.Length > 0) {
          adjustables.Add(new KeyValuePair<string, IRenderableGUIControl[]>(
              module.moduleName, moduleControls));
        }
      }
      adjustableModules = adjustables.ToArray();
    } else {
      adjustableModules = null;
    }
  }
  #endregion

  #region Local utility methods
  /// <summary>Shows a window that displays the winch controls.</summary>
  /// <param name="windowId">Window ID.</param>
  void ConsoleWindowFunc(int windowId) {
    if (guiActions.ExecutePendingGuiActions()) {
      if (parentPartTracking && Input.GetMouseButtonDown(0)
          && !windowRect.Contains(Mouse.screenPos)) {
        SetPart(Mouse.HoveredPart);
        parentPartTracking = false;
      }
    }

    string parentPartName = parentPart != null ? DbgFormatter.PartId(parentPart) : "NONE";
    if (!lockToPart) {
      if (GUILayout.Button(!parentPartTracking ? "Set part" : "Cancel set mode...")) {
        guiActions.Add(() => { parentPartTracking = !parentPartTracking; });
      }
      if (parentPartTracking && Mouse.HoveredPart != null) {
        parentPartName = "Select: " + DbgFormatter.PartId(Mouse.HoveredPart);
      }
      GUILayout.Label(parentPartName, new GUIStyle(GUI.skin.box) { wordWrap = true });
    }

    // Render the adjustable fields.
    if (parentPart != null && adjustableModules != null) {
      if (adjustableModules.Length > 0) {
        mainScrollView.BeginView(GUI.skin.box, Screen.height - 200);
        for (var i = 0; i < adjustableModules.Length; i++) {
          var isSelected = selectedModule == i;
          var module = adjustableModules[i];
          var toggleCaption = (isSelected ? "\u25b2 " : "\u25bc ") + "Module: " + module.Key;
          if (GUILayout.Button(toggleCaption)) {
            var selectedModuleSnapshot = selectedModule == i ? -1 : i;  // Make a copy for lambda!
            guiActions.Add(() => selectedModule = selectedModuleSnapshot);
          }
          if (isSelected) {
            foreach (var control in module.Value) {
              control.RenderControl(
                  guiActions, GUI.skin.label, new[] { GUILayout.Width(dialogValueSize) });
            }
          }
        }
        mainScrollView.EndView();
      } else {
        GUILayout.Box("No adjustable members found");
      }
    }

    if (GUILayout.Button("Close", GUILayout.ExpandWidth(false))) {
      guiActions.Add(() => DebugGui2.DestroyPartDebugDialog(this));
    }

    // Allow the window to be dragged by its title bar.
    GuiWindow.DragWindow(ref windowRect, titleBarRect);
  }
  #endregion
}

}  // namespace
