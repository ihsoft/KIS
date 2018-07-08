using System.Linq;
using UnityEngine;

namespace KIS {

public class ModuleKISPickup : PartModule {
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
    return Distance(position) <= maxDistance;
  }
}

}  // namespace
