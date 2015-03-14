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
        public string attachSndPath = "KIS/Sounds/attach";
        [KSPField]
        public string detachSndPath = "KIS/Sounds/detach";
        [KSPField]
        public string changeModeSndPath = "KIS/Sounds/click";

        private string orgAttachSndPath, orgDetachSndPath;
        private float orgAttachMaxMass;

        public override string GetInfo()
        {
            return String.Format("<b>Maximum mass</b>: {0:F0}", attachMaxMass);
        }

        public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
        {
            // Check if grab key is pressed
            if (useFrom == KIS_Item.UseFrom.KeyDown)
            {
                if (!KISAddonPickup.draggedPart && !KISAddonPickup.instance.grabActive && !KISAddonPointer.isRunning)
                {
                    item.PlaySound(KIS_Shared.bipWrongSndPath);
                    ScreenMessages.PostScreenMessage("Use this tool while in drop mode to attach / Use grab key to detach", 5, ScreenMessageStyle.UPPER_CENTER);
                }
                if (KISAddonPointer.isRunning)
                {
                    float attachPartMass = KISAddonPointer.partToAttach.mass + KISAddonPointer.partToAttach.GetResourceMass();
                    if (attachPartMass < attachMaxMass)
                    {
                        KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Attach;
                        item.PlaySound(changeModeSndPath);
                    }
                    else
                    {
                        item.PlaySound(KIS_Shared.bipWrongSndPath);
                        ScreenMessages.PostScreenMessage("This part is too heavy for this tool", 5, ScreenMessageStyle.UPPER_CENTER);
                    }
                }

            }
            if (useFrom == KIS_Item.UseFrom.KeyUp)
            {
                if (KISAddonPointer.isRunning && KISAddonPickup.instance.pointerMode == KISAddonPickup.PointerMode.Attach)
                {
                    KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Drop;
                    item.PlaySound(changeModeSndPath);
                }
            }
                            
        }

        public override void OnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.canDetach = true;
                orgAttachMaxMass = pickupModule.detachMaxMass;
                pickupModule.detachMaxMass = attachMaxMass;
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
                pickupModule.canDetach = false;
                pickupModule.detachMaxMass = orgAttachMaxMass;
                pickupModule.attachSndPath = orgAttachSndPath;
                pickupModule.detachSndPath = orgDetachSndPath;
            }
        }

    }
}