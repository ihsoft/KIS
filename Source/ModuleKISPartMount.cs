// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.SoundsUtils;
using KSPDev.PartUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

public class ModuleKISPartMount : PartModule,
    // KSPDev interfaces.
    IHasContextMenu {
  #region Part's config fields
  [KSPField]
  public string sndStorePath = "KIS/Sounds/containerMount";
  [KSPField]
  public string mountedPartNode = AttachNodeId.Bottom;
  [KSPField]
  public bool allowRelease = true;
  #endregion

  AudioSource sndAttach;

  #region KSP events and actions
  [KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "Release")]
  public void ReleaseEvent() {
    foreach (KeyValuePair<AttachNode, List<string>> mount in GetMounts()) {
      if (mount.Key.attachedPart) {
        mount.Key.attachedPart.decouple();
        break;
      }
    }
  }

  [KSPAction("Release")]
  public void ActionGroupRelease(KSPActionParam param) {
    if (!part.packed) {
      ReleaseEvent();
    }
  }
  #endregion

  #region IHasContextMenu implementation
  /// <inheritdoc/>
  public void UpdateContextMenu() {
    PartModuleUtils.SetupEvent(this, ReleaseEvent, x => x.active = allowRelease);
    PartModuleUtils.SetupAction(this, ActionGroupRelease, x => x.active = allowRelease);
  }
  #endregion

  #region PartModule overrides
  /// <inheritdoc/>
  public override void OnStart(StartState state) {
    base.OnStart(state);
    UpdateContextMenu();
    if (HighLogic.LoadedSceneIsFlight) {
      sndAttach = SpatialSounds.Create3dSound(gameObject, sndStorePath, maxDistance: 10);
    }
  }
  #endregion

  #region IPartMount interface candidates
  public bool PartIsMounted(Part mountedPart) {
    foreach (KeyValuePair<AttachNode, List<string>> mount in GetMounts()) {
      if (mount.Key.attachedPart) {
        if (mount.Key.attachedPart == mountedPart) {
          return true;
        }
      }
    }
    return false;
  }

  public Dictionary<AttachNode, List<string>> GetMounts() {
    var mounts = new Dictionary<AttachNode, List<string>>();
    ConfigNode node = KIS_Shared.GetBaseConfigNode(this);
    foreach (ConfigNode mountNode in node.GetNodes("MOUNT")) {
      if (mountNode.HasValue("attachNode") && mountNode.HasValue("allowedPartName")) {
        string attachNodeName = mountNode.GetValue("attachNode");
        AttachNode an = part.FindAttachNode(attachNodeName);
        if (an == null) {
          Debug.LogErrorFormat("GetMountNodes - Node : {0} not found !", attachNodeName);
          continue;
        }

        var allowedPartNames = new List<string>();
        foreach (string partName in mountNode.GetValues("allowedPartName")) {
          allowedPartNames.Add(partName.Replace('_', '.'));
        }
        mounts.Add(an, allowedPartNames);
      }
    }
    return mounts;
  }

  public void OnPartMounted() {
    sndAttach.Play();
  }
  #endregion
}

}  // namespace
