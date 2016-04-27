using System;
using System.Linq;

namespace KIS {

public class ModuleKISItemEvaTweaker : ModuleKISItem {
  [KSPField]
  public float walkSpeed = -1;
  //Default : 0.8f
  [KSPField]
  public float runSpeed = -1;
  // Default : 2.2f
  [KSPField]
  public float ladderSpeed = -1;
  // Default : 0.6f
  [KSPField]
  public float swimSpeed = -1;
  // Default : 0.8f
  [KSPField]
  public float maxJumpForce = -1;
  // Default : ?

  private float orgWalkSpeed;
  private float orgRunSpeed;
  private float orgLadderSpeed;
  private float orgSwimSpeed;
  private float orgMaxJumpForce;

  public override void OnEquip(KIS_Item item) {
    KerbalEVA kerbalEva = item.inventory.part.GetComponent<KerbalEVA>();

    if (walkSpeed != -1) {
      orgWalkSpeed = kerbalEva.walkSpeed;
      kerbalEva.walkSpeed = this.walkSpeed;
    }
    if (runSpeed != -1) {
      orgRunSpeed = kerbalEva.runSpeed;
      kerbalEva.runSpeed = this.runSpeed;
    }
    if (ladderSpeed != -1) {
      orgLadderSpeed = kerbalEva.ladderClimbSpeed;
      kerbalEva.ladderClimbSpeed = this.ladderSpeed;
    }
    if (swimSpeed != -1) {
      orgSwimSpeed = kerbalEva.swimSpeed;
      kerbalEva.swimSpeed = this.swimSpeed;
    }
    if (maxJumpForce != -1) {
      orgMaxJumpForce = kerbalEva.maxJumpForce;
      kerbalEva.maxJumpForce = this.maxJumpForce;
    }
  }

  public override void OnUnEquip(KIS_Item item) {
    KerbalEVA kerbalEva = item.inventory.part.GetComponent<KerbalEVA>();
    if (walkSpeed != -1) {
      kerbalEva.walkSpeed = orgWalkSpeed;
    }
    if (runSpeed != -1) {
      kerbalEva.runSpeed = orgRunSpeed;
    }
    if (ladderSpeed != -1) {
      kerbalEva.ladderClimbSpeed = orgLadderSpeed;
    }
    if (swimSpeed != -1) {
      kerbalEva.swimSpeed = orgSwimSpeed;
    }
    if (maxJumpForce != -1) {
      kerbalEva.maxJumpForce = orgMaxJumpForce;
    }
  }
}

}  // namespace
