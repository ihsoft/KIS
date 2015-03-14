using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{
    public class ModuleKISPartDrag : PartModule
    {
        public string dragIconPath = "KIS/Textures/unknow";
        public string dragText = "Unknow action";
        public string dragText2 = "Bla bla";

        public virtual void OnItemDragged(KIS_Item draggedItem)
        {

        }

        public virtual void OnPartDragged(Part draggedPart)
        {

        }

    }
}