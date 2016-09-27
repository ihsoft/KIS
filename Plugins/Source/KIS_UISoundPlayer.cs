// Kerbal Ineventory System (KIS)
// Mod author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com 
// License: https://github.com/KospY/KIS/blob/master/LICENSE.md 

using KSPDev.LogUtils;
using System;
using System.Linq;
using UnityEngine;

using Logger = KSPDev.LogUtils.Logger;

[KSPAddon(KSPAddon.Startup.EveryScene, false /*once*/)]
sealed class KIS_UISoundPlayer : MonoBehaviour {
  public static KIS_UISoundPlayer instance;

  // TODO: Read these settings from a config.
  static readonly string bipWrongSndPath = "KIS/Sounds/bipwrong";
  static readonly string clickSndPath = "KIS/Sounds/click";
  static readonly string attachPartSndPath = "KIS/Sounds/attachScrewdriver";

  GameObject audioGo;
  AudioSource audioBipWrong;
  AudioSource audioClick;
  AudioSource audioAttach;

  /// <summary>Plays a sound indicating a wrong action that was blocked.</summary>
  public void PlayBipWrong() {
    audioBipWrong.Play();
  }

  /// <summary>Plays a sound indicating an action was accepted.</summary>
  public void PlayClick() {
    audioClick.Play();
  }

  /// <summary>Plays a sound indicating a part was attached using a tool.</summary>
  public void PlayToolAttach() {
    audioAttach.Play();
  }

  void Awake() {
    audioGo = new GameObject();
    Logger.logInfo("Loading UI sounds for KIS...");
    InitSound(bipWrongSndPath, out audioBipWrong);
    InitSound(clickSndPath, out audioClick);
    InitSound(attachPartSndPath, out audioAttach);
    instance = this;
  }

  void InitSound(string clipPath, out AudioSource source) {
    Logger.logInfo("Loading clip: {0}", clipPath);
    source = audioGo.AddComponent<AudioSource>();
    source.volume = GameSettings.UI_VOLUME;
    source.spatialBlend = 0;  //set as 2D audiosource

    if (GameDatabase.Instance.ExistsAudioClip(clipPath)) {
      source.clip = GameDatabase.Instance.GetAudioClip(clipPath);
    } else {
      Logger.logError("Cannot locate clip: {0}", clipPath);
    }
  }
}
