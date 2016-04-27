using System;
using System.Linq;

namespace KIS {

public class ModuleKISPartDrag : PartModule {
  public string dragIconPath = "KIS/Textures/unknow";
  public string dragText = "Unknow action";
  public string dragText2 = "Bla bla";

  public virtual void OnItemDragged(KIS_Item draggedItem) {
  }

  public virtual void OnPartDragged(Part draggedPart) {
  }
}

}  // namespace
