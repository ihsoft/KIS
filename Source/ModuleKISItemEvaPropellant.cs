// Kerbal Attachment System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.GUIUtils;
using System;
using System.Linq;

namespace KIS {

public class ModuleKISItemEvaPropellant : ModuleKISItem {
  #region Localizable GUI strings
  protected readonly Message NoNeedToRefillMsg = "EVA tank is full. No need to refill";
  protected readonly Message CanisterIsEmptyMsg = "The tank is empty! Cannot refuel EVA pack";
  protected readonly Message CanisterRefilledMsg = "Fuel tank refilled";
  protected readonly Message NotEnoughPropellantMsg =
      "Not enough propellant in the tank. EVA pack partially refueled";
  protected readonly Message EvaPackRefueledMsg = "EVA pack fully refueled";
  #endregion

  /// <summary>Sound to play when refuel operation succeeded.</summary>
  [KSPField]
  public string refuelSndPath = "KIS/Sounds/refuelEva";

  /// <summary>Name of the propellant resource in the canister part.</summary>
  /// <remarks>It dopesn't need to match EVA pack propellant name.</remarks>
  public const string EvaPropellantResourceName = "EVA Propellant";

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

  /// <summary>Fills up canister to the maximum capacity.</summary>
  /// <param name="item">Item to refill.</param>
  protected virtual void RefillCanister(KIS_Item item) {
    item.SetResource(EvaPropellantResourceName, GetCanisterFuelResource(item).maxAmount);
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
        item.SetResource(EvaPropellantResourceName, canisterFuelResource.amount - canRefuel);
        evaFuelResource.amount += canRefuel;
        if (canRefuel < needsFuel) {
          ScreenMessaging.ShowPriorityScreenMessage(
            NotEnoughPropellantMsg);
        } else {
          ScreenMessaging.ShowPriorityScreenMessage(EvaPackRefueledMsg);
        }
        UISoundPlayer.instance.Play(refuelSndPath);
      }
    }
  }

  /// <summary>Returns KIS resource dexcription for the propellant in the part.</summary>
  /// <param name="item">Item to get resource for.</param>
  /// <returns>Resource description.</returns>
  protected static KIS_Item.ResourceInfo GetCanisterFuelResource(KIS_Item item) {
    return item.GetResources().First(x => x.resourceName == EvaPropellantResourceName);
  }
}

}  // namespace
