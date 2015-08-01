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
        [KSPField]
        public float attachMaxMass = 0.5f;
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

        public override string GetInfo()
        {
            var sb = new StringBuilder();
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
                pickupModule.detachSndPath = toolPartAttach;
                pickupModule.allowStaticAttach = toolStaticAttach;
                pickupModule.allowPartStack = toolPartStack;
            }
        }

        public override void OnUnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.allowPartAttach = false;
                pickupModule.allowStaticAttach = false;
                pickupModule.allowPartStack = false;
            }
        }

        // Check if the max mass is ok
        public virtual bool OnCheckDetach(Part partToDetach, ref String[] errorMsg)
        {
            if (partToDetach == null) return true;
            float pMass = (partToDetach.mass + partToDetach.GetResourceMass());
            if (pMass > attachMaxMass)
            {
                errorMsg = new string[]{
					"KIS/Textures/tooHeavy", 
					"Too heavy",
					"(Use a better tool for this [" + pMass + " > " + attachMaxMass + ")"
				};
                return false;
            }
            return true;
        }

        // Play sound on attach & detach
        public virtual void OnAttachToolUsed(Part srcPart, Part oldParent, KISAttachType moveType, KISAddonPointer.PointerTarget pointerTarget)
        {
            if ( (moveType == KISAttachType.DETACH_AND_ATTACH || moveType == KISAttachType.ATTACH) && srcPart)
            {
                AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachSndPath), srcPart.transform.position);
            }
            else if (moveType == KISAttachType.DETACH && srcPart)
            {
                AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSndPath), srcPart.transform.position);
            }
        }

        // redirect to protected bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        public virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, Vector3 surfaceMountPosition)
        {
            return this.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg);
        }

        // redirect to protected bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        public virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, AttachNode tgtNode)
        {
            return this.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg);
        }

        // Check if the max mass is ok (but this is already done in OnItemUse)
        protected virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        {
            //float pMass = (srcPart.mass + srcPart.GetResourceMass());
            //if (pMass > attachMaxMass)
            //{
            //    toolInvalidMsg = "Too heavy, (Use a better tool for this [" + pMass + " > " + attachMaxMass + ")";
            //    return false;
            //}
            return true;
        }
    }

}