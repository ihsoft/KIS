using System;
using System.Linq;

namespace KIS {

public class ModuleKISPickup : PartModule {
  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Allow attaching to part")]
  public bool allowPartAttach = true;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Allow attaching to surface")]
  public bool allowStaticAttach;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Allow attaching to stack node")]
  public bool allowPartStack;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Max distance")]
  public float maxDistance = 2;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Max mass")]
  public float grabMaxMass = 1;

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Drop")]
  public string dropSndPath = "KIS/Sounds/drop";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Attach part")]
  public string attachPartSndPath = "KIS/Sounds/attachPart";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Detach part")]
  public string detachPartSndPath = "KIS/Sounds/detachPart";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Attach to surface")]
  public string attachStaticSndPath = "KIS/Sounds/attachStatic";

  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Sound: Detach from surface")]
  public string detachStaticSndPath = "KIS/Sounds/detachStatic";

  //TODO(ihsoft): Figure out why it's here. I recall it's somehow needed.
  public FXGroup sndFx;
}

}  // namespace
