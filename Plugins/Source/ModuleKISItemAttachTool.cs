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

        //
        // Summary:
        //     Called when a part is Detach (from an other part).
        //     srcPart it's the part being dropped (it's attach to its parent)
        //     errorMsg is the string array needed when it's not possible to detach
        //      (first is the texture, then some messages)
        // default : check maxmass
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


        //private static float GetAllPickupMaxMassInRange(Part p)
        //{
        //	float maxMass = 0;
        //	ModuleKISPickup[] allPickupModules = FindObjectsOfType(typeof(ModuleKISPickup)) as ModuleKISPickup[];
        //	foreach (ModuleKISPickup pickupModule in allPickupModules)
        //	{
        //		float partDist = Vector3.Distance(pickupModule.part.transform.position, p.transform.position);
        //		if (partDist <= pickupModule.maxDistance)
        //		{
        //			maxMass += pickupModule.maxMass;
        //		}
        //	}
        //	return maxMass;
        //}

        //
        // Summary:
        //     Called when a part is dropped.
        //     srcPart it's the part being dropped
        //     tgtPart is a part src part will be going into
        public virtual void OnItemMove(Part srcPart, Part tgtPart, KISMoveType moveType, KISAddonPointer.PointerTarget pointerTarget)
        {
            if (moveType == (KISMoveType.ATTACH_MOVE | KISMoveType.ATTACH_NEW) && srcPart)
            {
                AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachSndPath), srcPart.transform.position);
            }
            else
            {
                if (tgtPart != null)
                {
                    AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSndPath), srcPart.transform.position);
                }
            }
        }

        //
        // Summary:
        //     Called when a part need to known if it can be surface-attach with this tool
        //     srcPart it's the part we want to attach
        //     tgtPart is where we want to attach srcPart
        //     toolInvalidMsg is the message used when this method return false.
        //     surfaceMountPosition is the surface mount position on tgtPart (in scene origin)
        public virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, Vector3 surfaceMountPosition)
        {
            return this.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg);
        }

        //
        // Summary:
        //     Called when a part need to known if it can be attachon a node with this tool
        //     srcPart it's the part we want to attach
        //     tgtPart is where we want to attach srcPart
        //     toolInvalidMsg is the message used when this method return false.
        //     surfaceMountPosition is the surface mount position on tgtPart (in scene origin)
        public virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, AttachNode tgtNode)
        {
            return this.OnCheckAttach(srcPart, tgtPart, ref toolInvalidMsg);
        }

        protected virtual bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg)
        {
            float pMass = (part.mass + part.GetResourceMass());
            if (pMass > attachMaxMass)
            {
                toolInvalidMsg = "Too heavy, (Use a better tool for this [" + pMass + " > " + attachMaxMass + ")";

                return false;
            }
            return true;
        }
    }

    // TODO: RETURN_INVENTORY option with the associated hook
    public enum KISMoveType { DROP_NEW, DROP_MOVE, ATTACH_NEW, ATTACH_MOVE }

}