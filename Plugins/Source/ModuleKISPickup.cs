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
        [KSPField]
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
        [KSPField]
        public string attachPartSndPath = "KIS/Sounds/attachPart";
        [KSPField]
        public string detachPartSndPath = "KIS/Sounds/detachPart";
        [KSPField]
        public string attachStaticSndPath = "KIS/Sounds/attachStatic";
        [KSPField]
        public string detachStaticSndPath = "KIS/Sounds/detachStatic";
        public FXGroup sndFx;
    }


}