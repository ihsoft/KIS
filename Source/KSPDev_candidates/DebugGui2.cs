// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using KSPDev.ModelUtils;
using KSPDev.LogUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KSPDev.DebugUtils {

/// <summary>Helper class to deal with the debug GUI functionality.</summary>
/// <seealso cref="DebugAdjustableAttribute"/>
public static class DebugGui2 {

  /// <summary>Game object to use to host the debug dialogs.</summary>
  /// <seealso cref="MakePartDebugDialog"/>
  static GameObject dialogsRoot {
    get {
      if (_dialogsRoot == null) {
        //FIXME: Use assembly version here.
        _dialogsRoot = new GameObject("dialogsRoot-DUMMY");
      }
      return _dialogsRoot;
    }
  }
  static GameObject _dialogsRoot;

  /// <summary>Dumps the objects hierarchy to the logs.</summary>
  /// <remarks>
  /// This method recirsively goes down to the all decendants, starting from
  /// <paramref name="child"/>. The path, however, is shown realtive to <paramref name="root"/>.
  /// </remarks>
  /// <param name="root">The root object to make the path from.</param>
  /// <param name="child">The descendant of <paramref name="root"/> to dump descendants for.</param>
  public static void DumpHierarchy(Transform  root, Transform child) {
    if (root != child) {
      DebugEx.Warning("{0} (localPos: {1}, localRot: {2}, localEuler: {3})",
                      Hierarchy.MakePath(Hierarchy.GetFullPath(child, parent: root)),
                      child.localPosition, child.localRotation, child.localRotation.eulerAngles);
    }
    for (var i = 0; i < child.childCount; i++) {
      DumpHierarchy(root, child.GetChild(i));
    }
  }

  /// <summary>Creates a debug dialog for the parts.</summary>
  /// <param name="title">The titile of the dialog.</param>
  /// <param name="dialogWidth">
  /// The width of the dialog. If omitted, then the code will decide.
  /// </param>
  /// <param name="valueColumnWidth">
  /// The width of the value changing controls. If omitted, then the code will decide.
  /// </param>
  /// <param name="group">
  /// The group of the controls to present. If empty, then all the controls are shown.
  /// </param>
  /// <param name="bindToPart">
  /// The fixed part to atatch the dialog to. The dialog won't allow changing the part.
  /// </param>
  /// <returns>The created dialog.</returns>
  /// <seealso cref="DestroyPartDebugDialog"/>
  /// <seealso cref="DebugAdjustableAttribute"/>
  public static PartDebugAdjustmentDialog2 MakePartDebugDialog(
      string title,
      float? dialogWidth = null, float? valueColumnWidth = null, string group = "",
      Part bindToPart = null) {
    if (bindToPart != null) {
      title += " : " + DbgFormatter.PartId(bindToPart);
    }
    var dlg = dialogsRoot.AddComponent<PartDebugAdjustmentDialog2>();
    dlg.dialogTitle = title;
    dlg.dialogWidth = dialogWidth ?? dlg.dialogWidth;
    dlg.dialogValueSize = valueColumnWidth ?? dlg.dialogValueSize;
    dlg.controlsGroup = group;
    if (bindToPart != null) {
      dlg.lockToPart = true;
      dlg.SetPart(bindToPart);
    }
    DebugEx.Info("Created debug dialog: {0}", title);
    return dlg;
  }

  /// <summary>Destroys the debug dialog.</summary>
  /// <param name="dlg">The dialog to destroy.</param>
  /// <seealso cref="MakePartDebugDialog"/>
  public static void DestroyPartDebugDialog(PartDebugAdjustmentDialog2 dlg) {
    UnityEngine.Object.Destroy(dlg);
  }
}

}  // namespace
