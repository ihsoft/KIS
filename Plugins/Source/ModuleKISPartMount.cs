using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{
    public class ModuleKISPartMount : ModuleKISPartDrag
    {
        [KSPField]
        public string bayNodeName = "top";
        [KSPField]
        public string sndStorePath = "KIS/Sounds/containerMount";
        [KSPField]
        public bool allowRelease = true;
        public FXGroup sndFxStore;
        public List<string> allowedPartNames = new List<string>();

        public override string GetInfo()
        {
            return "Releasable : " + allowRelease;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state != StartState.None)
            {
                if (allowRelease)
                {
                    Actions["ActionGroupRelease"].active = true;
                    Events["ContextMenuRelease"].guiActive = true;
                    Events["ContextMenuRelease"].guiActiveUnfocused = true;
                }
                else
                {
                    Actions["ActionGroupRelease"].active = false;
                    Events["ContextMenuRelease"].guiActive = false;
                    Events["ContextMenuRelease"].guiActiveUnfocused = false;
                }
            }

            if (state == StartState.Editor || state == StartState.None) return;

            sndFxStore.audio = part.gameObject.AddComponent<AudioSource>();
            sndFxStore.audio.volume = GameSettings.SHIP_VOLUME;
            sndFxStore.audio.rolloffMode = AudioRolloffMode.Linear;
            sndFxStore.audio.dopplerLevel = 0f;
            sndFxStore.audio.panLevel = 1f;
            sndFxStore.audio.maxDistance = 10;
            sndFxStore.audio.loop = false;
            sndFxStore.audio.playOnAwake = false;
            sndFxStore.audio.clip = GameDatabase.Instance.GetAudioClip(sndStorePath);
            dragIconPath = "KIS/Textures/mount";
            dragText = "Mount";
            dragText2 = "Release mouse to mount";

            allowedPartNames.Clear();
            ConfigNode node = KIS_Shared.GetBaseConfigNode(this);
            foreach (string partName in node.GetValues("allowedPartName"))
            {
                allowedPartNames.Add(partName.Replace('_', '.'));
            }
        }

        [KSPEvent(name = "ContextMenuRelease", active = true, guiActive = true, guiActiveUnfocused = true, guiName = "Release")]
        public void ContextMenuRelease()
        {
            AttachNode bayNode = this.part.findAttachNode(bayNodeName);
            if (bayNode == null)
            {
                KIS_Shared.DebugError("(PartBay) Node : " + bayNodeName + " not found !");
                return;
            }
            Part tmpPart = bayNode.attachedPart;
            bayNode.attachedPart.decouple();
            //Physics.IgnoreCollision(tmpPart.collider, this.part.collider);
        }

        [KSPAction("Release")]
        public void ActionGroupRelease(KSPActionParam param)
        {
            if (!this.part.packed)
            {
                ContextMenuRelease();
            }
        }

        public Part GetPartMounted()
        {
            AttachNode bayNode = this.part.findAttachNode(bayNodeName);
            return bayNode.attachedPart;
        }

        public override void OnPartDragged(Part draggedPart)
        {
            AttachNode bayNode = this.part.findAttachNode(bayNodeName);
            if (bayNode == null)
            {
                KIS_Shared.DebugError("Node : " + bayNodeName + " not found !");
                return;
            }
            KIS_Shared.AddNodeTransform(this.part, bayNode);

            if (bayNode.attachedPart)
            {
                ScreenMessages.PostScreenMessage("A part is already stored", 2, ScreenMessageStyle.UPPER_CENTER);
                KIS_Shared.DebugWarning("(PartMount) This node are used");
                return;
            }

            if (!KISAddonPickup.instance.HasActivePickupInRange(bayNode.nodeTransform.position))
            {
                ScreenMessages.PostScreenMessage("Part is too far", 2, ScreenMessageStyle.UPPER_CENTER);
                KIS_Shared.DebugWarning("(PartMount) Too far");
                return;
            }

            // Check if part is allowed
            if (!allowedPartNames.Contains(draggedPart.partInfo.name))
            {
                ScreenMessages.PostScreenMessage("Dragged part is not allowed", 2, ScreenMessageStyle.UPPER_CENTER);
                KIS_Shared.DebugWarning("(PartMount) Dragged part is not allowed !");
                return;
            }

            AttachNode draggedPartAn = draggedPart.findAttachNode("bottom");
            if (draggedPartAn == null)
            {
                KIS_Shared.DebugError("(PartMount) Dragged part node not found !");
                return;
            }
            KIS_Shared.DebugLog("(PartMount) Decouple part if needed...");
            KIS_Shared.DecoupleFromAll(draggedPart);

            KIS_Shared.DebugLog("(PartMount) Add node transform if not exist...");
            KIS_Shared.AddNodeTransform(draggedPart, draggedPartAn);

            KIS_Shared.DebugLog("(PartMount) Move part...");
            KIS_Shared.MoveAlign(draggedPart.transform, draggedPartAn.nodeTransform, bayNode.nodeTransform);
            draggedPart.transform.rotation *= Quaternion.Euler(bayNode.orientation);

            //Couple part with bay
            KIS_Shared.DebugLog("(PartMount) Couple part with bay...");
            draggedPart.Couple(this.part);
            bayNode.attachedPart = draggedPart;

            sndFxStore.audio.Play();
        }



    }
}