// Kerbal Attachment System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: https://github.com/KospY/KAS/blob/master/LICENSE.md

using KSPDev.GUIUtils;
using System;

namespace KIS {
  
static class UISounds {
  /// <summary>Sound to play when user chosen action cannot be acomplished.</summary>
  static string bipWrongSndPath = "KIS/Sounds/bipwrong";
  /// <summary>Sound to play when click action is successfully handled.</summary>
  static string clickSndPath = "KIS/Sounds/click";
  /// <summary>Sound to play when mechanican screedriver has done the job.</summary>
  static string attachPartSndPath = "KIS/Sounds/attachScrewdriver";

  /// <summary>Plays a sound to indicate the last user action hasn't succeeded.</summary>
  public static void PlayBipWrong() {
    UISoundPlayer.instance.Play(bipWrongSndPath);
  }

  /// <summary>Plays a sound to indicate the click event has been accepted.</summary>
  public static void PlayClick() {
    UISoundPlayer.instance.Play(clickSndPath);
  }

  /// <summary>Plays a sound to indicate the mechanical tool has worked.</summary>
  public static void PlayToolAttach() {
    UISoundPlayer.instance.Play(attachPartSndPath);
  }
}
  
}  // namespace
