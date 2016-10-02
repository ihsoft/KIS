using KSPDev.GUIUtils;
using System;
using System.Linq;

namespace KIS {

public class ModuleKISItemEvaPropellant : ModuleKISItem {
  [KSPField]
  public string refuelSndPath = "KIS/Sounds/refuelEva";

  public const string EvaPropellantResource = "EVA Propellant";

  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    if (useFrom != KIS_Item.UseFrom.KeyUp) {
      if (item.inventory.invType == ModuleKISInventory.InventoryType.Pod) {
        // Refuel item
        ScreenMessaging.ShowPriorityScreenMessage("Fuel tank refueled");
        foreach (KIS_Item.ResourceInfo itemRessource in item.GetResources()) {
          if (itemRessource.resourceName == EvaPropellantResource) {
            item.SetResource(EvaPropellantResource, itemRessource.maxAmount);
            item.inventory.PlaySound(refuelSndPath, false, false);
          }
        }
      }
      if (item.inventory.invType == ModuleKISInventory.InventoryType.Eva) {
        // Refuel eva
        foreach (KIS_Item.ResourceInfo itemRessource in item.GetResources()) {
          if (itemRessource.resourceName == EvaPropellantResource) {
            PartResource evaRessource = item.inventory.part.GetComponent<PartResource>();
            if (evaRessource != null) {
              double amountToFill = evaRessource.maxAmount - evaRessource.amount;
              if (itemRessource.amount > amountToFill) {
                ScreenMessaging.ShowPriorityScreenMessage("EVA pack refueled");
                evaRessource.amount = evaRessource.maxAmount;
                item.SetResource(EvaPropellantResource, (itemRessource.amount - amountToFill));
                if (item.equippedPart) {  
                  PartResource equippedTankRessource = item.equippedPart.Resources.Get(EvaPropellantResource);
                  if (equippedTankRessource != null) {
                    equippedTankRessource.amount = (itemRessource.amount - amountToFill);
                  }
                }
                item.inventory.PlaySound(refuelSndPath, false, false);
              } else {
                if (itemRessource.amount == 0) {
                  ScreenMessaging.ShowPriorityScreenMessage(
                      "Fuel tank is empty ! Cannot refuel EVA pack");
                } else {
                  ScreenMessaging.ShowPriorityScreenMessage(
                      "Available propellant is not enough to refuel, EVA pack partially refueled");
                }
                evaRessource.amount += itemRessource.amount;
                item.SetResource("EVA Propellant", 0);
                item.inventory.PlaySound(refuelSndPath, false, false);
              }
            }
          }
        }
      }
    }
  }
}

}  // namespace
