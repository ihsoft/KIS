using System;
using System.Linq;

namespace KIS
{

    public class ModuleKISItemEvaPropellant : ModuleKISItem
    {
        [KSPField]
        public string refuelSndPath = "KIS/Sounds/refuelEva";

        public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
        {
            if (useFrom != KIS_Item.UseFrom.KeyUp)
            {
                if (item.inventory.invType == ModuleKISInventory.InventoryType.Pod)
                {
                    // Refuel item
                    ScreenMessages.PostScreenMessage("Fuel tank refueled", 5, ScreenMessageStyle.UPPER_CENTER);
                    foreach (KIS_Item.ResourceInfo itemRessource in item.GetResources())
                    {
                        if (itemRessource.resourceName == "EVA Propellant")
                        {
                            item.SetResource("EVA Propellant", itemRessource.maxAmount);
                            item.inventory.PlaySound(refuelSndPath, false, false);
                        }
                    }
                }
                if (item.inventory.invType == ModuleKISInventory.InventoryType.Eva)
                {
                    // Refuel eva
                    foreach (KIS_Item.ResourceInfo itemRessource in item.GetResources())
                    {
                        if (itemRessource.resourceName == "EVA Propellant")
                        {
                            PartResource evaRessource = item.inventory.part.GetComponent<PartResource>();
                            if (evaRessource)
                            {
                                double amountToFill = evaRessource.maxAmount - evaRessource.amount;
                                if (itemRessource.amount > amountToFill)
                                {
                                    ScreenMessages.PostScreenMessage("EVA pack refueled", 5, ScreenMessageStyle.UPPER_CENTER);
                                    evaRessource.amount = evaRessource.maxAmount;
                                    item.SetResource("EVA Propellant", (itemRessource.amount - amountToFill));
                                    if (item.equippedPart)
                                    {  
                                        PartResource equippedTankRessource = item.equippedPart.Resources.list.Find(p => p.resourceName == "EVA Propellant");
                                        if (equippedTankRessource)
                                        {
                                            equippedTankRessource.amount = (itemRessource.amount - amountToFill);
                                        }
                                    }
                                    item.inventory.PlaySound(refuelSndPath, false, false);
                                }
                                else
                                {
                                    if (itemRessource.amount == 0)
                                    {
                                        ScreenMessages.PostScreenMessage("Fuel tank is empty ! Cannot refuel EVA pack", 5, ScreenMessageStyle.UPPER_CENTER);
                                    }
                                    else
                                    {
                                        ScreenMessages.PostScreenMessage("Available propellant is not enough to refuel, EVA pack partially refueled", 5, ScreenMessageStyle.UPPER_CENTER);
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
}