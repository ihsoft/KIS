// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using KSPDev.PartUtils;
using KSPDev.SoundsUtils;
using System;
using System.Collections;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_09002.
public sealed class ModuleKISItemSoundPlayer : ModuleKISItem,
    // KSPDEV interfaces.
    IHasContextMenu,
    // KSPDEV sugar interfaces.
    IPartModule {

  #region Localizable GUI strings.
  static readonly Message PlayMenuTxt = new Message(
      "#kisLOC_09000",
      defaultTemplate: "Play",
      description: "The name of the context menu item to start the playback.");

  static readonly Message StopMenuTxt = new Message(
      "#kisLOC_09001",
      defaultTemplate: "Stop",
      description: "The name of the context menu item to stop the playback.");
  #endregion

  #region Part's config fields
  [KSPField]
  public string sndPath = "KIS/Sounds/guitar";
  [KSPField]
  public float sndMaxDistance = 10;
  [KSPField]
  public bool loop;
  #endregion

  AudioSource sndMainTune;

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

  #region IHasContextMenu implementation
  public void UpdateContextMenu() {
    PartModuleUtils.SetupEvent(
        this, TogglePlayStateEvent,
        x => x.guiName =
            sndMainTune == null || !sndMainTune.isPlaying ? PlayMenuTxt : StopMenuTxt);
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
    if (useFrom != KIS_Item.UseFrom.KeyUp && item.equippedPart != null) {
      // Only play if the item is equipped, since we need a real part for this to work.
      // Multiple modules are not supported!
      var soundPlayerModule = item.equippedPart.GetComponent<ModuleKISItemSoundPlayer>();
      if (soundPlayerModule != null) {
        soundPlayerModule.TogglePlayStateEvent();
      }
    }
  }
  #endregion
  
  IEnumerator DetectEndOfClip() {
    yield return new WaitWhile(() => sndMainTune != null && sndMainTune.isPlaying);
    UpdateContextMenu();
  }
}

}