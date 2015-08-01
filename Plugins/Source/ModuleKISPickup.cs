using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISPickup : PartModule
    {
        [KSPField] //it's ~ a shortcut to 'rightHandEquipedItem'.Modules.Contains("KISIAttachTool")
        public bool allowPartAttach = true;
        [KSPField]
        public bool allowStaticAttach = false;
        [KSPField]
        public bool allowPartStack = false;
        [KSPField]
        public float maxDistance = 2;
        [KSPField]
        public float grabMaxMass = 1;
        [KSPField]
        public string dropSndPath = "KIS/Sounds/drop";
        public FXGroup sndFx;
    }


}