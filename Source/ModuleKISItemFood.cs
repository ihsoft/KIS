using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using KSPDev.ProcessingUtils;
using System;
using System.Linq;

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
        DebugEx.Fine("Burp incoming...");
        Random rnd = new System.Random();
        int delay = rnd.Next(1, 5);
        AsyncCall.CallOnTimeout(item.inventory, delay, () => Burp(item));
        eatCount = 0;
      }
      UISoundPlayer.instance.Play(eatSndPath);
    }
  }

  private void Burp(KIS_Item item) {
    UISoundPlayer.instance.Play(burpSndPath);
  }
}

}  // namespace
