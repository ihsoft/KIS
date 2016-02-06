using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISItemAttachTool : ModuleKISItem, KISIAttachTool
    {

        //============= check mass => deactivated ==========
        //[KSPField]
        //public float attachMaxMass = 0.5f;
        [KSPField]
        public bool toolPartAttach = true;
        [KSPField]
        public bool toolStaticAttach = false;
        [KSPField]
        public bool toolPartStack = false;
        [KSPField]
        public string attachPartSndPath = "KIS/Sounds/attachPart";
        [KSPField]
        public string detachPartSndPath = "KIS/Sounds/detachPart";
        [KSPField]
        public string attachStaticSndPath = "KIS/Sounds/attachStatic";
        [KSPField]
        public string detachStaticSndPath = "KIS/Sounds/detachStatic";

        private bool orgToolPartAttach, orgToolStaticAttach, orgToolPartStack;

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            //============= check mass => deactivated ==========
            //sb.AppendFormat("<b>Maximum mass</b>: {0:F0}", attachMaxMass); sb.AppendLine();
            if (toolPartStack)
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
                //============= check mass => deactivated ==========
                //if (!KISAddonPickup.draggedPart && !KISAddonPickup.instance.grabActive && !KISAddonPointer.isRunning)
                //{
                //    item.PlaySound(KIS_Shared.bipWrongSndPath);
                //    ScreenMessages.PostScreenMessage("Use this tool while in drop mode to attach / Use grab key to detach", 5, ScreenMessageStyle.UPPER_CENTER);
                //}
                //if (KISAddonPointer.isRunning && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount)
                //{
                //    float attachPartMass = KISAddonPointer.partToAttach.mass + KISAddonPointer.partToAttach.GetResourceMass();
                //    if (attachPartMass < attachMaxMass)
                //    {
                //        KISAddonPickup.instance.EnableAttachMode();
                //    }
                //    else
                //    {
                //        item.PlaySound(KIS_Shared.bipWrongSndPath);
                //        ScreenMessages.PostScreenMessage("This part is too heavy for this tool", 5, ScreenMessageStyle.UPPER_CENTER);
                //    }
                //}
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
                orgToolPartAttach = pickupModule.allowPartAttach;
                orgToolStaticAttach = pickupModule.allowStaticAttach;
                orgToolPartStack = pickupModule.allowPartStack;
                pickupModule.allowPartAttach = toolPartAttach;
                pickupModule.allowStaticAttach = toolStaticAttach;
                pickupModule.allowPartStack = toolPartStack;
            }
        }

        public override void OnUnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.allowPartAttach = orgToolPartAttach;
                pickupModule.allowStaticAttach = orgToolStaticAttach;
                pickupModule.allowPartStack = orgToolPartStack;
            }
        }

        public virtual bool OnCheckDetach(Part partToDetach, ref String[] errorMsg)
        {
            //KIS_Shared.DebugLog("[KIS] OnCheckDetach " + partToDetach);
            //sanity check
            if (partToDetach == null) return true;
            //if can't node-attach and this part is attached via a node
            if (!toolPartStack && partToDetach.srfAttachNode == null)
            {
                errorMsg = new string[]{
					"KIS/Textures/forbidden", 
					"Wrong tool",
					"This tool can't stack-detach a part (via a node)."
				};
                return false;
            }
            //============= check mass => deactivated ==========
            // Check if the max mass is ok
            //float pMass = (partToDetach.mass + partToDetach.GetResourceMass());
            //if (pMass > attachMaxMass)
            //{
            //    errorMsg = new string[]{
            //        "KIS/Textures/tooHeavy", 
            //        "Too heavy",
            //        "(Use a better tool for this [" + pMass + " > " + attachMaxMass + ")"
            //    };
            //    return false;
            //}
            //all green
            return true;
        }
        // redirect to protected bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        public virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, Vector3 surfaceMountPosition)
        {
            //KIS_Shared.DebugLog("[KIS] OnCheckAttach (srf) " + srcPart.partName + " " + tgtPart.partName);
            return this.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg);
        }

        // redirect to protected bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        public virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, AttachNode tgtNode)
        {
            //KIS_Shared.DebugLog("[KIS] OnCheckAttach (node) " + srcPart.partName + " " + tgtPart.partName);
            if (toolPartStack)
                return this.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg);
            else
            {
                toolInvalidMsg = "This tool can't stack-attach a part (via a node).";
                return false;
            }
        }

        // Check if the max mass is ok
        protected virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        {
            //============= check mass => deactivated ==========
            //float pMass = (srcPart.mass + srcPart.GetResourceMass());
            //if (pMass > attachMaxMass)
            //{
            //    toolInvalidMsg = "Too heavy, (Use a better tool for this [" + pMass + " > " + attachMaxMass + ")";
            //    return false;
            //}
            return true;
        }


        // Play sound on attach & detach
        public virtual void OnAttachToolUsed(Part srcPart, Part oldParent, KISAttachType moveType, KISAddonPointer.PointerTarget pointerTarget)
        {
            //    KIS_Shared.DebugLog("OnAttachToolUsed " + (srcPart == null ? "null" : srcPart.partName)
            //+ " " + (oldParent == null ? "null" : oldParent.partName) + " " + moveType);

            //sound at part instead at scrPart (sound at tool instead of attached/detached part
            ModuleKISItem item = part.GetComponent<ModuleKISItem>();
            if (item && item.staticAttached)
            {
                if ((moveType == KISAttachType.DETACH_AND_ATTACH || moveType == KISAttachType.ATTACH) && srcPart)
                {
                    AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachStaticSndPath), part.transform.position);
                }
                else if (moveType == KISAttachType.DETACH && srcPart)
                {
                    AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachStaticSndPath), part.transform.position);
                }
            }
            else
            {
                if ((moveType == KISAttachType.DETACH_AND_ATTACH || moveType == KISAttachType.ATTACH) && srcPart)
                {
                    AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachPartSndPath), part.transform.position);
                }
                else if (moveType == KISAttachType.DETACH && srcPart)
                {
                    AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachPartSndPath), part.transform.position);
                }
            }
        }

    }

}