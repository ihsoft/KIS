// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.PartUtils;
using KSPDev.SoundsUtils;
using KSPDev.KSPInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_05011.
public sealed class ModuleKISItemBomb : ModuleKISItem,
    // KSPDEV sugar interfaces.
    IHasGUI, IPartModule {

  #region Localizable GUI strings.
  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_05000",
      defaultTemplate: "KIS Bomb",
      description: "The title of the module to present in the editor details window.");

  static readonly Message<DistanceType> ExplosionRadiusInfo = new Message<DistanceType>(
      "#kisLOC_05001",
      defaultTemplate: "Max explosion radius: <<1>>",
      description: "The info message to present in the editor's details window for the maximum"
      + " radius of explosion of the bomb."
      + "\nArgument <<1>> is the radius. Format: DistanceType.");

  static readonly Message SetupWindowTitle = new Message(
      "#kisLOC_05002",
      defaultTemplate: "Explosive - Setup",
      description: "The title of the GUI window to setup the bomb.");

  static readonly Message TimerSettingsSectionTxt = new Message(
      "#kisLOC_05003",
      defaultTemplate: "Timer",
      description: "The GUI section title for settig up the explosion timer.");

  Message<int> TimerDelayInSecondsTxt = new Message<int>(
      "#kisLOC_05004",
      defaultTemplate: "<<1>> s",
      description: "The string that displays number of seconds till the bomb trigger.");

  static readonly Message RadiusSettingsSectionTxt = new Message(
      "#kisLOC_05005",
      defaultTemplate: "Explosion radius",
      description: "The GUI section title for settig up the explosion area.");

  static readonly Message<DistanceType, DistanceType> ExplosionRadiusTxt =
      new Message<DistanceType, DistanceType>(
      "#kisLOC_05006",
      defaultTemplate: "<<1>> / <<2>>",
      description: "The string that displays current setting of the explosion radius."
      + "\nArgument <<1>> is the current radius. Format: DistanceType."
      + "\nArgument <<2>> is the maximum allowed radius for the part. Format: DistanceType.");

  static readonly Message ActivateExplosionDialogTxt = new Message(
      "#kisLOC_05007",
      defaultTemplate: "ACTIVATE (cannot be undone)",
      description: "The caption on the button that starts the timer. It cannot be stopped!");

  static readonly Message CloseSetupDialogTxt = new Message(
      "#kisLOC_05008",
      defaultTemplate: "Close",
      description: "The caption on the button that closes the setup menu without starting the"
      + " timer");
  #endregion

  #region Part's config fields
  [KSPField]
  public float delay = 5f;
  [KSPField]
  public float maxRadius = 10f;
  [KSPField]
  public string timeStartSndPath = "KIS/Sounds/timeBombStart";
  [KSPField]
  public string timeLoopSndPath = "KIS/Sounds/timeBombLoop";
  [KSPField]
  public string timeEndSndPath = "KIS/Sounds/timeBombEnd";
  #endregion

  AudioSource sndTimeStart;
  AudioSource sndTimeLoop;
  AudioSource sndTimeEnd;
  float radius = 10f;
  bool activated;
  bool showSetup;
  Rect guiWindowPos;

  #region PartModule overrides
  /// <inheritdoc/>
  public override string GetModuleDisplayName() {
    return ModuleTitleInfo;
  }

  /// <inheritdoc/>
  public override void OnStart(StartState state) {
    base.OnStart(state);
    if (state == StartState.Editor || state == StartState.None) {
      return;
    }
    sndTimeStart = SpatialSounds.Create3dSound(gameObject, timeStartSndPath);
    sndTimeEnd = SpatialSounds.Create3dSound(gameObject, timeEndSndPath);
    sndTimeLoop = SpatialSounds.Create3dSound(gameObject, timeLoopSndPath, loop: true);
  }

  /// <inheritdoc/>
  public override void OnUpdate() {
    base.OnUpdate();
    if (showSetup) {
      var distToPart = Vector3.Distance(
          FlightGlobals.ActiveVessel.transform.position, part.transform.position);
      var setupEvent = PartModuleUtils.GetEvent(this, SetupEvent);
      if (setupEvent == null || distToPart > setupEvent.unfocusedRange) {
        showSetup = false;
      }
    }
    if (activated) {
      delay -= TimeWarp.deltaTime;
      if (delay < 1 && !sndTimeEnd.isPlaying) {
        sndTimeEnd.Play();
      }
      if (delay < 0) {
        sndTimeStart.Stop();
        sndTimeLoop.Stop();
        Explode(part.transform.position, radius);
      }
    }
  }
  #endregion

  public void Explode(Vector3 pos, float radius) {
    var nearestColliders = new List<Collider>(Physics.OverlapSphere(pos, radius, 557059));
    foreach (var col in nearestColliders) {
      // Check if if the collider have a rigidbody
      if (!col.attachedRigidbody) {
        continue;
      }
      // Check if it's a part
      Part p = col.attachedRigidbody.GetComponent<Part>();
      if (!p) {
        continue;
      }
      p.explosionPotential = radius;
      p.explode();
      p.Die();
    }
  }

  #region IHasGUI implementation
  /// <inheritdoc/>
  public void OnGUI() {
    if (showSetup && !activated) {
      GUI.skin = HighLogic.Skin;
      guiWindowPos = GUILayout.Window(GetInstanceID(), guiWindowPos, GuiSetup, SetupWindowTitle);
    }
  }
  #endregion

  #region Local utility methods
  void GuiSetup(int windowID) {
    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
    centeredStyle.alignment = TextAnchor.MiddleCenter;
    centeredStyle.wordWrap = false;
    // TIMER
    GUILayout.Label(TimerSettingsSectionTxt, centeredStyle);
    using (new GUILayout.HorizontalScope()) {
      if (GUILayout.Button(" -- ", GUILayout.Width(30))) {
        if (delay > 10) {
          delay = delay - 10;
        }
      }
      if (GUILayout.Button(" - ", GUILayout.Width(30))) {
        if (delay > 0) {
          delay--;
        }
      }
      GUILayout.Label(
          TimerDelayInSecondsTxt.Format((int)delay), centeredStyle, GUILayout.ExpandWidth(true));
      if (GUILayout.Button(" + ", GUILayout.Width(30))) {
        delay++;
      }
      if (GUILayout.Button(" ++ ", GUILayout.Width(30))) {
        delay = delay + 10;
      }
    }

    GUILayout.Space(5);

    // Explosion radius
    GUILayout.Label(RadiusSettingsSectionTxt, centeredStyle);
    using (new GUILayout.HorizontalScope()) {
      if (GUILayout.Button(" -- ", GUILayout.Width(30))) {
        if (radius > 1f) {
          radius = radius - 1f;
        }
      }
      if (GUILayout.Button(" - ", GUILayout.Width(30))) {
        if (radius > 0.5f) {
          radius = radius - 0.5f;
        }
      }
      GUILayout.Label(ExplosionRadiusTxt.Format(radius, maxRadius), centeredStyle);
      if (GUILayout.Button(" + ", GUILayout.Width(30))) {
        if ((radius + 0.5f) <= maxRadius) {
          radius = radius + 0.5f;
        }
      }
      if (GUILayout.Button(" ++ ", GUILayout.Width(30))) {
        if ((radius + 1f) <= maxRadius) {
          radius = radius + 1f;
        }
      }
    }

    if (GUILayout.Button(ActivateExplosionDialogTxt)) {
      ActivateEvent();
    }
    if (GUILayout.Button(CloseSetupDialogTxt)) {
      showSetup = false;
    }
    GUI.DragWindow();
  }
  #endregion

  #region KSP events and actions
  [KSPEvent(guiActiveUnfocused = true)]
  [LocalizableItem(
      tag = "#kisLOC_05009",
      defaultTemplate = "Activate",
      description = "The name of the context menu item to activate the bomb.")]
  public void ActivateEvent() {
    if (!activated) {
      activated = true;
      sndTimeStart.Play();
      sndTimeLoop.Play();
      PartModuleUtils.SetupEvent(this, ActivateEvent, x => x.active = false);
      PartModuleUtils.SetupEvent(this, SetupEvent, x => x.active = false);
    }
  }

  [KSPEvent(guiActiveUnfocused = true)]
  [LocalizableItem(
      tag = "#kisLOC_05010",
      defaultTemplate = "Setup",
      description = "The name of the context menu item to open the bomb GUI setup window.")]
  public void SetupEvent() {
    guiWindowPos =
        new Rect(Input.mousePosition.x, (Screen.height - Input.mousePosition.y), 0, 0);
    showSetup = !showSetup;
  }
  #endregion

  #region Inheritable & customization methods
  /// <inheritdoc/>
  protected override IEnumerable<string> GetParamInfo() {
    return base.GetParamInfo().Concat(new[] {
        ExplosionRadiusInfo.Format(maxRadius),
    });
  }
  #endregion
}

}  // namespace
