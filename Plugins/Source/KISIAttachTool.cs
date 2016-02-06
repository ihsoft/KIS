using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{
    //
    // Summary:
    //          Interface for item who can attach and detach parts from other parts.
    //          /!\ At this time, you MUST set item.inventory.part.GetComponent<ModuleKISPickup>().canDetach = true; 
    //              in onEquip to make it work (and to false value in onUnequip)
    //          Your item MUST be in the right hand, to make it work & be compliant with the doc
    public interface KISIAttachTool
    {

        //
        // Summary:
        //     Called when a part is Detach (from an other part).
        //     srcPart it's the part being dropped (it's attach to its parent)
        //     errorMsg is the string array needed when it's not possible to detach
        //      (first is the texture, then some messages)
        // default : check maxmass
        bool OnCheckDetach(Part partToDetach, ref String[] errorMsg);

        //
        // Summary:
        //     Called when a part is dropped.
        //     srcPart it's the part being dropped
        //     previousParent is the previous parent part of srcPart
        void OnAttachToolUsed(Part srcPart, Part previousParent, KISAttachType moveType, KISAddonPointer.PointerTarget pointerTarget);

        //
        // Summary:
        //     Called when a part need to known if it can be surface-attach with this tool
        //     srcPart it's the part we want to attach
        //     tgtPart is where we want to attach srcPart
        //     toolInvalidMsg is the message used when this method return false.
        //     surfaceMountPosition is the surface mount position on tgtPart (in scene origin)
        bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, Vector3 surfaceMountPosition);

        //
        // Summary:
        //     Called when a part need to known if it can be attach on a node with this tool
        //     srcPart it's the part we want to attach
        //     tgtPart is where we want to attach srcPart
        //     toolInvalidMsg is the message used when this method return false.
        //     surfaceMountPosition is the surface mount position on tgtPart (in scene origin)
        bool OnCheckAttach(Part srcPart, Part tgtPart, ref string toolInvalidMsg, AttachNode tgtNode);
    }
    
    public enum KISAttachType { DETACH, ATTACH, DETACH_AND_ATTACH }

}