// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.SoundsUtils;
using System;
using System.Linq;
using UnityEngine;

namespace KIS {

public sealed class ModuleKISItemSoundPlayer : ModuleKISItem {
  #region Part's config fields
  [KSPField]
  public string sndPath = "KIS/Sounds/guitar";
  [KSPField]
  public float sndMaxDistance = 10;
  [KSPField]
  public bool loop;
  #endregion

  public AudioSource sndMainTune;

  public override void OnStart(StartState state) {
    base.OnStart(state);
    if (state != StartState.None && HighLogic.LoadedSceneIsFlight) {
      sndMainTune = SpatialSounds.Create3dSound(
          gameObject, sndPath, loop: loop, maxDistance: sndMaxDistance);
    }
  }

  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    if (useFrom != KIS_Item.UseFrom.KeyUp) {
      if (sndMainTune.isPlaying) {
        sndMainTune.Stop();
      } else {
        sndMainTune.Play();
      }
    }
  }

  [KSPEvent(name = "ContextMenuPlay", guiActiveEditor = false, active = true, guiActive = true,
            guiActiveUnfocused = true, guiName = "Play")]
  public void Play() {
    if (sndMainTune.isPlaying) {
      sndMainTune.Stop();
    } else {
      sndMainTune.Play();
    }
  }
}

}