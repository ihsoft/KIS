using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_11007.
public class ModuleKISPickup : PartModule, IModuleInfo {

  #region Localizable GUI strings.
  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_11000",
      defaultTemplate: "KIS Manipulator",
      description: "The title of the module to present in the editor details window.");

  static readonly Message<bool> AllowPartAttachPartInfo = new Message<bool>(
      "#kisLOC_11001",
      defaultTemplate: "Can surface attach: <<1>>",
      description: "The info string in the editor for part attach rule."
      + "\nArgument <<1>> is a boolean.");

  static readonly Message<bool> AllowStaticAttachPartInfo = new Message<bool>(
      "#kisLOC_11002",
      defaultTemplate: "Can attach to ground: <<1>>",
      description: "The info string in the editor for static attach rule."
      + "\nArgument <<1>> is a boolean.");

  static readonly Message<bool> AllowPartStackPartInfo = new Message<bool>(
      "#kisLOC_11003",
      defaultTemplate: "Can attach to stack nodes: <<1>>",
      description: "The info string in the editor for static attach rule."
      + "\nArgument <<1>> is a boolean.");

  static readonly Message<float> MaxDistancePartInfo = new Message<float>(
      "#kisLOC_11004",
      defaultTemplate: "Maximum range: <<1>>m",
      description: "The info string in the editor for manipulator range."
      + "\nArgument <<1>> is the maximum range.");

  static readonly Message<float> GrabMaxMassPartInfo = new Message<float>(
      "#kisLOC_11005",
      defaultTemplate: "Maximum mass: <<1>>t",
      description: "The info string in the editor for the maximum mass limit."
      + "\nArgument <<1>> is the maximum mass.");

  static readonly Message<string> RequiredSkillPartInfo = new Message<string>(
      "#kisLOC_11006",
      defaultTemplate: "Required skill: <<1>>",
      description: "The info string in the editor for a required skill."
      + "\nArgument <<1>> is the name of the required skill.");
  #endregion

  #region IModuleInfo implementation
  /// <inheritdoc/>
  public virtual string GetModuleTitle() { 
    return ModuleTitleInfo;
  }

  /// <inheritdoc/>
  public override string GetInfo() {
    var sb = new StringBuilder();
    sb.AppendLine(MaxDistancePartInfo.Format(maxDistance));
    sb.AppendLine(GrabMaxMassPartInfo.Format(grabMaxMass));
    sb.AppendLine(AllowPartAttachPartInfo.Format(allowPartAttach));
    sb.AppendLine(AllowPartStackPartInfo.Format(allowPartStack));
    //sb.AppendLine(AllowStaticAttachPartInfo.Format(allowStaticAttach));
    if (!string.IsNullOrEmpty(requiredSkill))
      sb.AppendLine(RequiredSkillPartInfo.Format(requiredSkill));
    return sb.ToString();
  }

  /// <inheritdoc/>
  public virtual Callback<Rect> GetDrawModulePanelCallback() {
    return null;
  }

  /// <inheritdoc/>
  public virtual string GetPrimaryField() {
    return null;
  }
  #endregion

  [KSPField]
  public bool allowPartAttach = true;
  [KSPField]
  public bool allowStaticAttach = false;
  [KSPField]
  public bool allowPartStack = false;
  [KSPField]
  public float maxDistance = 2;
  [KSPField]
  public float grabMaxMass = 1;
  [KSPField]
  public string requiredSkill = "";
  [KSPField]
  public string dropSndPath = "KIS/Sounds/drop";
  [KSPField]
  public string attachPartSndPath = "KIS/Sounds/attachPart";
  [KSPField]
  public string detachPartSndPath = "KIS/Sounds/detachPart";
  [KSPField]
  public string attachStaticSndPath = "KIS/Sounds/attachStatic";
  [KSPField]
  public string detachStaticSndPath = "KIS/Sounds/detachStatic";
  public FXGroup sndFx;

  public bool IsActive() {
    return vessel.IsControllable && CheckSkill();
  }

  private bool CheckSkill() {
    if (string.IsNullOrEmpty(requiredSkill))
      return true;

    var crew = part.CrewCapacity > 0
                ? part.protoModuleCrew
                : vessel.GetVesselCrew();

    if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX
        && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX) {
      return crew.Any();
    }

    return crew.Any(c => c.HasEffect(requiredSkill));
  }

  [PersistentField("EvaPickup/externalCommandSeatRangeModifier")]
  public static float externalCommandSeatRangeModifier = 10;

  [PersistentField("EvaPickup/externalCommandSeatStrengthModifier")]
  public static float externalCommandSeatStrengthModifier = 2;

  public float AdjustedMaxDist { 
    get {
      if (!FlightGlobals.ActiveVessel.isEVA && part.name.StartsWith("kerbalEVA"))
        return maxDistance * externalCommandSeatRangeModifier;

      return maxDistance;
    }
  }
    
  public float AdjustedGrabMaxMass { 
    get {
      if (!FlightGlobals.ActiveVessel.isEVA && part.name.StartsWith("kerbalEVA"))
        return grabMaxMass * externalCommandSeatStrengthModifier;

      return grabMaxMass;
    }
  }

  public float Distance(Part part) {
    return Distance(part.transform.position);
  }

  public float Distance(Vector3 position) {
    return Vector3.Distance(part.transform.position, position);
  }

  public bool IsInRange(Part part) { 
    return IsInRange(part.transform.position);
  }

  public bool IsInRange(Vector3 position) {
    return Distance(position) <= AdjustedMaxDist;
  }
}

}  // namespace
