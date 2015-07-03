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
        public bool canAttach = false;
        [KSPField]
        public float attachMaxMass = Mathf.Infinity;
        [KSPField]
        public float maxDistance = 2;
        [KSPField]
        public float grabMaxMass = 1;
        [KSPField]
        public string dropSndPath = "KIS/Sounds/drop";
        [KSPField]
        public string attachSndPath = "KIS/Sounds/attach";
        [KSPField]
        public string detachSndPath = "KIS/Sounds/detach";
        public FXGroup sndFx;
    }


}