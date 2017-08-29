// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_05001.
public sealed class ModuleKISItemBomb : ModuleKISItem,
    // KSP interfaces.
    IModuleInfo,
    // KSPDEV sugar interfaces.
    IKSPDevModuleInfo, IHasGUI {

  #region Localizable GUI strings.
  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_05000",
      defaultTemplate: "KIS Bomb",
      description: "The title of the module to present in the editor details window.");
  #endregion

  [KSPField]
  public float delay = 5f;
  [KSPField]
  public float maxRadius = 10f;
  [KSPField]
  public string activateText = "Activate";
  [KSPField]
  public string timeStartSndPath = "KIS/Sounds/timeBombStart";
  [KSPField]
  public string timeLoopSndPath = "KIS/Sounds/timeBombLoop";
  [KSPField]
  public string timeEndSndPath = "KIS/Sounds/timeBombEnd";

  public FXGroup fxSndTimeStart;
  public FXGroup fxSndTimeLoop;
  public FXGroup fxSndTimeEnd;
  float radius = 10f;
  bool activated;
  bool showSetup;
  public Rect guiWindowPos;

  #region IModuleInfo implementation
  /// <inheritdoc/>
  public string GetModuleTitle() {
    return ModuleTitleInfo;
  }

  /// <inheritdoc/>
  public Callback<Rect> GetDrawModulePanelCallback() {
    return null;
  }

  /// <inheritdoc/>
  public string GetPrimaryField() {
    return ExplosionRadiusInfo.Format(maxRadius);
  }

  /// <inheritdoc/>
  public override string GetInfo() {
    return ExplosionRadiusInfo.Format(maxRadius);
  }
  #endregion

  public override void OnStart(StartState state) {
    base.OnStart(state);
    if (state == StartState.Editor || state == StartState.None) {
      return;
    }
    Events["Activate"].guiName = activateText;
    KIS_Shared.createFXSound(this.part, fxSndTimeStart, timeStartSndPath, false);
    KIS_Shared.createFXSound(this.part, fxSndTimeLoop, timeLoopSndPath, true);
    KIS_Shared.createFXSound(this.part, fxSndTimeEnd, timeEndSndPath, false);
  }

  public override void OnUpdate() {
    base.OnUpdate();
    if (showSetup) {
      float distToPart = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position,
                                          this.part.transform.position);
      if (distToPart > 2) {
        showSetup = false;
      }
    }
    if (activated) {
      delay += -TimeWarp.deltaTime;
      if (delay < 1 && !fxSndTimeEnd.audio.isPlaying) {
        fxSndTimeEnd.audio.Play();
      }
      if (delay < 0) {
        fxSndTimeStart.audio.Stop();
        fxSndTimeLoop.audio.Stop();
        Explode(this.part.transform.position, radius);
      }
    }
  }

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
    if (showSetup) {
      GUI.skin = HighLogic.Skin;
      guiWindowPos = GUILayout.Window(GetInstanceID(), guiWindowPos, GuiSetup, "Explosive - Setup");
    }
  }
  #endregion

  #region Local utility methods
  void GuiSetup(int windowID) {
    GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
    centeredStyle.alignment = TextAnchor.MiddleCenter;
    // TIMER
    GUILayout.Label("Timer", centeredStyle, GUILayout.Width(200));
    GUILayout.BeginHorizontal();
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
    GUILayout.Label(delay + " s", centeredStyle, GUILayout.Width(80));
    if (GUILayout.Button(" + ", GUILayout.Width(30))) {
      delay++;
    }
    if (GUILayout.Button(" ++ ", GUILayout.Width(30))) {
      delay = delay + 10;
    }
    GUILayout.EndHorizontal();

    GUILayout.Space(5);

    // Explosion radius
    GUILayout.Label("Explosion radius", centeredStyle, GUILayout.Width(200));
    GUILayout.BeginHorizontal();
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
    GUILayout.Label(radius + " / " + maxRadius + " m", centeredStyle, GUILayout.Width(80));
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
    GUILayout.EndHorizontal();

    if (GUILayout.Button(" ! Activate !")) {
      Activate();
    }
    if (GUILayout.Button("Close")) {
      showSetup = false;
    }
    GUI.DragWindow();
  }
  #endregion

  #region KSP events and actions
  [KSPEvent(name = "Activate", active = true, guiActive = false, guiActiveUnfocused = true,
            guiName = "Activate")]
  public void Activate() {
    if (!activated) {
      activated = true;
      fxSndTimeStart.audio.Play();
      fxSndTimeLoop.audio.Play();
      Events["Activate"].guiActiveUnfocused = false;
    }
  }

  [KSPEvent(name = "Setup", active = true, guiActive = false, guiActiveUnfocused = true,
            guiName = "Setup")]
  public void Setup() {
    guiWindowPos =
        new Rect(Input.mousePosition.x, (Screen.height - Input.mousePosition.y), 200, 100);
    showSetup = !showSetup;
  }
  #endregion
}

}  // namespace
