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
        public bool allowStack = false;
        [KSPField]
        public string attachPartSndPath = "KIS/Sounds/attachPart";
        [KSPField]
        public string detachPartSndPath = "KIS/Sounds/detachPart";
        [KSPField]
        public string attachStaticSndPath = "KIS/Sounds/attachStatic";
        [KSPField]
        public string detachStaticSndPath = "KIS/Sounds/detachStatic";

        private string orgAttachPartSndPath, orgDetachPartSndPath, orgAttachStaticSndPath, orgDetachStaticSndPath;

        public override string GetInfo()
        {
            var sb = new StringBuilder();
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
                pickupModule.allowStack = allowStack;

                orgAttachPartSndPath = pickupModule.attachPartSndPath;
                pickupModule.attachPartSndPath = attachPartSndPath;
                orgDetachPartSndPath = pickupModule.detachPartSndPath;
                pickupModule.detachPartSndPath = detachPartSndPath;

                orgAttachStaticSndPath = pickupModule.attachStaticSndPath;
                pickupModule.attachStaticSndPath = attachStaticSndPath;
                orgDetachStaticSndPath = pickupModule.detachStaticSndPath;
                pickupModule.detachStaticSndPath = detachStaticSndPath;
            }
        }

        public override void OnUnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.canAttach = false;
                pickupModule.allowStack = false;

                pickupModule.attachPartSndPath = orgAttachPartSndPath;
                pickupModule.detachPartSndPath = orgDetachPartSndPath;

                pickupModule.attachStaticSndPath = orgAttachStaticSndPath;
                pickupModule.detachStaticSndPath = orgDetachStaticSndPath;
            }
        }

    }
}