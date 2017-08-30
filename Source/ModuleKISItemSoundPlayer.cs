// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.PartUtils;
using KSPDev.SoundsUtils;
using System;
using System.Collections;
using UnityEngine;

namespace KIS {

public sealed class ModuleKISItemSoundPlayer : ModuleKISItem,
    // KSPDEV interfaces.
    IHasContextMenu {
  #region Part's config fields
  [KSPField]
  public string sndPath = "KIS/Sounds/guitar";
  [KSPField]
  public float sndMaxDistance = 10;
  [KSPField]
  public bool loop;
  #endregion

  public AudioSource sndMainTune;

  #region IHasContextMenu implementation
  public void UpdateContextMenu() {
  }
  #endregion

  #region PartModule overrides
  /// <inheritdoc/>
  public override void OnStart(StartState state) {
    base.OnStart(state);
    UpdateContextMenu();
  }
  #endregion

  #region ModuleKISItem overrides
  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    if (useFrom != KIS_Item.UseFrom.KeyUp) {
      TogglePlayStateEvent();
    }
  }
  #endregion
  
  #region KSP events and actions
  [KSPEvent(guiActive = true, guiActiveUnfocused = true)]
  public void TogglePlayStateEvent() {
    if (sndMainTune == null) {
      sndMainTune = SpatialSounds.Create3dSound(
          gameObject, sndPath, loop: loop, maxDistance: sndMaxDistance);
    }
    if (sndMainTune.isPlaying) {
      sndMainTune.Stop();
    } else {
      sndMainTune.Play();
      if (!loop) {
        StartCoroutine(DetectEndOfClip());
      }
    }
    UpdateContextMenu();
  }
  #endregion

  IEnumerator DetectEndOfClip() {
    yield return new WaitWhile(() => sndMainTune != null && sndMainTune.isPlaying);
    UpdateContextMenu();
  }
}

}