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
        [KSPField] //it's a shortcut to 'part.hasModule<ModuleKISItemAttachTool>()'
        public bool canDetach = false;
        [KSPField]
        public float maxDistance = 2;
        [KSPField]
        public float maxMass = 1;
        [KSPField]
        public string dropSndPath = "KIS/Sounds/drop";
        public FXGroup sndFx;
    }


}