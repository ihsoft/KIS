// Kerbal Attachment System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.GUIUtils;
using KSPDev.ResourceUtils;
using System;
using System.Linq;

namespace KIS {

// Next localization ID: kisLOC_08005.
public class ModuleKISItemEvaPropellant : ModuleKISItem {
  #region Localizable GUI strings
  static readonly Message NoNeedToRefillMsg = new Message(
      "#kisLOC_08000",
      defaultTemplate: "The jetpack is full. No need to refill",
      description: "The message to present when the jetpack is attempted to be refilled, but its'"
      + " already full.");

  static readonly Message CanisterIsEmptyMsg = new Message(
      "#kisLOC_08001",
      defaultTemplate: "The canister is empty! Cannot refuel the jetpack",
      description: "The message to present when the EVA kerbals has attempted to refill the"
      + " jetpack, but the tank is empty.");

  static readonly Message CanisterRefilledMsg = new Message(
      "#kisLOC_08002",
      defaultTemplate: "Fuel canister refilled",
      description: "The message to present when a non-full tank has successfully refilled from the"
      + " pod's resource.");

  static readonly Message NotEnoughPropellantMsg = new Message(
      "#kisLOC_08003",
      defaultTemplate: "Not enough fuel in the canister. The jetpack is partially refueled",
      description: "The message to present when the EVA kerbals has attempted to refill the"
      + " jetpack, but the tank didn't have enough fuel to fill the jetpack to full.");

  static readonly Message JetpackRefueledMsg = new Message(
      "#kisLOC_08004",
      defaultTemplate: "Jetpack fully refueled",
      description: "The message to present when the EVA kerbals has attempted to refill the"
      + " jetpack, and the jetpack has successfully refilled to full.");
  #endregion

  #region Part's config fields
  /// <summary>Sound to play when refuel operation succeeded.</summary>
  [KSPField]
  public string refuelSndPath = "KIS/Sounds/refuelEva";
  #endregion

  #region ModuleKISItem overrides
  /// <inheritdoc/>
  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    if (useFrom != KIS_Item.UseFrom.KeyUp) {
      if (item.inventory.invType == ModuleKISInventory.InventoryType.Pod) {
        RefillCanister(item);  // Refuel canister item.
      } else  if (item.inventory.invType == ModuleKISInventory.InventoryType.Eva) {
        RefillEVAPack(item);  // Refuel EVA pack from canister.
      }
    }
  }
  #endregion

  #region Inheritable & customization methods
  /// <summary>Fills up canister to the maximum capacity.</summary>
  /// <param name="item">Item to refill.</param>
  protected virtual void RefillCanister(KIS_Item item) {
    item.SetResource(StockResourceNames.EvaPropellant, GetCanisterFuelResource(item).maxAmount);
    ScreenMessaging.ShowPriorityScreenMessage(CanisterRefilledMsg);
    UISoundPlayer.instance.Play(refuelSndPath);
  }

  /// <summary>
  /// Refuels kerbal's EVA pack up to the maximum, and decreases canister reserve.
  /// </summary>
  /// <param name="item">Item to get fuel from.</param>
  protected virtual void RefillEVAPack(KIS_Item item) {
    var canisterFuelResource = GetCanisterFuelResource(item);
    var evaFuelResource = item.inventory.part.Resources.Get(
        item.inventory.part.GetComponent<KerbalEVA>().propellantResourceName);
    var needsFuel = evaFuelResource.maxAmount - evaFuelResource.amount;
    if (needsFuel < double.Epsilon) {
      ScreenMessaging.ShowPriorityScreenMessage(NoNeedToRefillMsg);
    } else {
      if (canisterFuelResource.amount < double.Epsilon) {
        ScreenMessaging.ShowPriorityScreenMessage(CanisterIsEmptyMsg);
        UISounds.PlayBipWrong();
      } else {
        var canRefuel = Math.Min(needsFuel, canisterFuelResource.amount);
        item.SetResource(StockResourceNames.EvaPropellant, canisterFuelResource.amount - canRefuel);
        evaFuelResource.amount += canRefuel;
        if (canRefuel < needsFuel) {
          ScreenMessaging.ShowPriorityScreenMessage(NotEnoughPropellantMsg);
        } else {
          ScreenMessaging.ShowPriorityScreenMessage(JetpackRefueledMsg);
        }
        UISoundPlayer.instance.Play(refuelSndPath);
      }
    }
  }

  /// <summary>Returns KIS resource dexcription for the propellant in the part.</summary>
  /// <param name="item">Item to get resource for.</param>
  /// <returns>Resource description.</returns>
  protected static KIS_Item.ResourceInfo GetCanisterFuelResource(KIS_Item item) {
    return item.GetResources().First(x => x.resourceName == StockResourceNames.EvaPropellant);
  }
  #endregion
}

}  // namespace
