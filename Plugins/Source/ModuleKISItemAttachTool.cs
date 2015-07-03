using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISItemAttachTool : ModuleKISItem
    {
        [KSPField]
        public float attachMaxMass = 0.5f;
        [KSPField]
        public bool allowStack = false;
        [KSPField]
        public string attachSndPath = "KIS/Sounds/attach";
        [KSPField]
        public string detachSndPath = "KIS/Sounds/detach";
        [KSPField]
        public string changeModeSndPath = "KIS/Sounds/click";

        private string orgAttachSndPath, orgDetachSndPath;
        private float orgAttachMaxMass;

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<b>Maximum mass</b>: {0:F0}", attachMaxMass); sb.AppendLine();
            if (allowStack)
            {
                sb.AppendLine("Allow snap attach on stack node");
            }
            return sb.ToString();
        }

        public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
        {
            // Check if grab key is pressed
            if (useFrom == KIS_Item.UseFrom.KeyDown)
            {
                KISAddonPickup.instance.EnableAttachMode();
            }
            if (useFrom == KIS_Item.UseFrom.KeyUp)
            {
                KISAddonPickup.instance.DisableAttachMode();
            }     
        }

        public override void OnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.canAttach = true;
                KISAddonPointer.allowStack = allowStack;
                orgAttachMaxMass = pickupModule.attachMaxMass;
                pickupModule.attachMaxMass = attachMaxMass;
                orgAttachSndPath = pickupModule.attachSndPath;
                pickupModule.attachSndPath = attachSndPath;
                orgDetachSndPath = pickupModule.detachSndPath;
                pickupModule.detachSndPath = detachSndPath;
            }
        }

        public override void OnUnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.canAttach = false;
                KISAddonPointer.allowStack = false;
                pickupModule.attachMaxMass = orgAttachMaxMass;
                pickupModule.attachSndPath = orgAttachSndPath;
                pickupModule.detachSndPath = orgDetachSndPath;
            }
        }

    }
}