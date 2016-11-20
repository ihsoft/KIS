using System;
using System.Linq;
using UnityEngine;

namespace KIS {

public sealed class ModuleKISItemFood : ModuleKISItem {
  [KSPField]
  public string eatSndPath = "KIS/Sounds/foodEat";
  [KSPField]
  public string burpSndPath = "KIS/Sounds/foodBurp";
  private static int eatCount = 0;

  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    if (useFrom != KIS_Item.UseFrom.KeyUp) {
      item.StackRemove();
      eatCount++;

      if (eatCount > 3) {
        Debug.Log("Burp incoming...");
        System.Random rnd = new System.Random();
        int delay = rnd.Next(1, 5);
        item.inventory.DelayedAction(Burp, item, delay);
        eatCount = 0;
      }
      item.inventory.PlaySound(eatSndPath, false, false);
    }
  }

  private void Burp(KIS_Item item) {
    item.inventory.PlaySound(burpSndPath, false, false);
  }
}

}  // namespace
