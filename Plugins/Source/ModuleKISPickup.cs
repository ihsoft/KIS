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
		[KSPField] // used at 1 line in KisAddonPoiner to show a good pointer, can be done inside KISItemAttachTool
		public bool allowPartStack = false;
        [KSPField]
        public float maxDistance = 2;
        [KSPField]
        public float grabMaxMass = 1;

		//sound for attach-detach without tools
		[KSPField]
		public string dropSndPath = "KIS/Sounds/drop";
		public FXGroup sndFx;
		[KSPField]
		public string attachPartSndPath = "KIS/Sounds/attachPart";
		[KSPField]
		public string detachPartSndPath = "KIS/Sounds/detachPart";
		[KSPField]
		public string attachStaticSndPath = "KIS/Sounds/attachStatic";
		[KSPField]
		public string detachStaticSndPath = "KIS/Sounds/detachStatic";
    }


}