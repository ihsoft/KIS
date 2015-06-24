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
        [KSPField] //with this commit, it's a shortcut to part.hasModule<ModuleKISItemAttachTool>()
        public bool canDetach = false;
        [KSPField] //with this commit, it sould be deleted (as the compute is done in the ModuleAttachTool)
        public float detachMaxMass = Mathf.Infinity;
        [KSPField]
        public float maxDistance = 2;
        [KSPField]
        public float maxMass = 1;
        [KSPField] //should be in ModuleKISItemAttachTool.onMove(Drop_xx)
        public string dropSndPath = "KIS/Sounds/drop";
        [KSPField] //should be in ModuleKISItemAttachTool.onMove(attach_new)
        public string attachSndPath = "KIS/Sounds/attach";
        [KSPField] //should be in ModuleKISItemAttachTool.onMove(xx_move)
        public string detachSndPath = "KIS/Sounds/detach";
        public FXGroup sndFx;
    }


}