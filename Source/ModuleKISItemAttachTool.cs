// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KSPDev.GUIUtils;
using KSPDev.KSPInterfaces;
using System;
using System.Linq;

namespace KIS {

// Next localization ID: #kisLOC_04003.
public class ModuleKISItemAttachTool : ModuleKISItem,
    // KSP interfaces.
    IModuleInfo,
    // KSPDev sugar interfaces.
    IKSPDevModuleInfo {

  #region Localizable GUI strings.
  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_04000",
      defaultTemplate: "KIS Attach Tool",
      description: "");

  static readonly Message AllowNodeAttachModeInfo = new Message(
      "#kisLOC_04001",
      defaultTemplate: "<color=#00FFFF>Can attach to the stack nodes</color>",
      description: "");

  static readonly Message OnlySurfaceAttachModeInfo = new Message(
      "#kisLOC_04002",
      defaultTemplate: "<color=#FFA500>Can only attach to the part's surface</color>",
      description: "");
  #endregion

  [KSPField]
  public bool toolPartAttach = true;
  [KSPField]
  public bool toolStaticAttach;
  [KSPField]
  public bool toolPartStack;
  [KSPField]
  public string attachPartSndPath = "KIS/Sounds/attachPart";
  [KSPField]
  public string detachPartSndPath = "KIS/Sounds/detachPart";
  [KSPField]
  public string attachStaticSndPath = "KIS/Sounds/attachStatic";
  [KSPField]
  public string detachStaticSndPath = "KIS/Sounds/detachStatic";

  string orgAttachPartSndPath;
  string orgDetachPartSndPath;
  string orgAttachStaticSndPath;
  string orgDetachStaticSndPath;
  bool orgToolPartAttach;
  bool orgToolStaticAttach;
  bool orgToolPartStack;

  #region IPartInfo interface
  /// <inheritdoc/>
  public string GetModuleTitle() {
    return ModuleTitleInfo;
  }

  /// <inheritdoc/>
  public Callback<UnityEngine.Rect> GetDrawModulePanelCallback() {
    return null;
  }

  /// <inheritdoc/>
  public string GetPrimaryField() {
    return !toolPartStack ? OnlySurfaceAttachModeInfo.Format() : null;
  }

  /// <inheritdoc/>
  public override string GetInfo() {
    return toolPartStack ? AllowNodeAttachModeInfo : OnlySurfaceAttachModeInfo;
  }
  #endregion

  #region ModuleKISItem overrides
  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    // Check if grab key is pressed
    if (useFrom == KIS_Item.UseFrom.KeyDown) {
      KISAddonPickup.instance.EnableAttachMode();
    }
    if (useFrom == KIS_Item.UseFrom.KeyUp) {
      KISAddonPickup.instance.DisableAttachMode();
    }     
  }
      
  public override void OnEquip(KIS_Item item) {
    ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
    if (pickupModule) {
      orgToolPartAttach = pickupModule.allowPartAttach;
      orgToolStaticAttach = pickupModule.allowStaticAttach;
      orgToolPartStack = pickupModule.allowPartStack;
      pickupModule.allowPartAttach = toolPartAttach;
      pickupModule.allowStaticAttach = toolStaticAttach;
      pickupModule.allowPartStack = toolPartStack;

      orgAttachPartSndPath = pickupModule.attachPartSndPath;
      pickupModule.attachPartSndPath = attachPartSndPath;
      orgDetachPartSndPath = pickupModule.detachPartSndPath;
      pickupModule.detachPartSndPath = detachPartSndPath;

      orgAttachStaticSndPath = pickupModule.attachStaticSndPath;
      pickupModule.attachStaticSndPath = attachStaticSndPath;
      orgDetachStaticSndPath = pickupModule.detachStaticSndPath;
      pickupModule.detachStaticSndPath = detachStaticSndPath;
    }
  }

  public override void OnUnEquip(KIS_Item item) {
    ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
    if (pickupModule) {
      pickupModule.allowPartAttach = orgToolPartAttach;
      pickupModule.allowStaticAttach = orgToolStaticAttach;
      pickupModule.allowPartStack = orgToolPartStack;

      pickupModule.attachPartSndPath = orgAttachPartSndPath;
      pickupModule.detachPartSndPath = orgDetachPartSndPath;

      pickupModule.attachStaticSndPath = orgAttachStaticSndPath;
      pickupModule.detachStaticSndPath = orgDetachStaticSndPath;
    }
  }
  #endregion
}
  
}  // namespace
