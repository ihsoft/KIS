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
        public bool allowStack = false;
        [KSPField]
        public string attachSndPath = "KIS/Sounds/attach";
        [KSPField]
        public string detachSndPath = "KIS/Sounds/detach";
        [KSPField]
        public string changeModeSndPath = "KIS/Sounds/click";

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
                if (!KISAddonPickup.draggedPart && !KISAddonPickup.instance.grabActive && !KISAddonPointer.isRunning)
                {
                    item.PlaySound(KIS_Shared.bipWrongSndPath);
                    ScreenMessages.PostScreenMessage("Use this tool while in drop mode to attach / Use grab key to detach", 5, ScreenMessageStyle.UPPER_CENTER);
                }
                if (KISAddonPointer.isRunning && KISAddonPointer.pointerTarget != KISAddonPointer.PointerTarget.PartMount)
                {
                    float attachPartMass = KISAddonPointer.partToAttach.mass + KISAddonPointer.partToAttach.GetResourceMass();
                    if (attachPartMass < attachMaxMass)
                    {
                        KISAddonPickup.instance.pointerMode = KISAddonPickup.PointerMode.Attach;
                        KISAddonPointer.allowStack = allowStack;
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
                    KISAddonPointer.allowStack = false;
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
            }
        }

        public override void OnUnEquip(KIS_Item item)
        {
            ModuleKISPickup pickupModule = item.inventory.part.GetComponent<ModuleKISPickup>();
            if (pickupModule)
            {
                pickupModule.canDetach = false;
            }
        }

        // Check if the max mass is ok
        public virtual bool OnCheckDetach(Part partToDetach, ref String[] errorMsg)
        {
            if (partToDetach == null) return true;
            float pMass = (part.mass + part.GetResourceMass());
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
                Debug.Log("Play attach: " + attachSndPath);
                AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachSndPath), srcPart.transform.position);
            }
            else if (moveType == KISAttachType.DETACH && srcPart)
            {
                Debug.Log("Play Detach " + detachSndPath);
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
            float pMass = (srcPart.mass + part.GetResourceMass());
            if (pMass > attachMaxMass)
            {
                toolInvalidMsg = "Too heavy, (Use a better tool for this [" + pMass + " > " + attachMaxMass + ")";

                return false;
            }
            return true;
        }
    }

}