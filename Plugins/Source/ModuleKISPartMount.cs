using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{
    public class ModuleKISPartMount : PartModule
    {
        [KSPField]
        public string sndStorePath = "KIS/Sounds/containerMount";
        [KSPField]
        public bool allowRelease = true;
        public FXGroup sndFxStore;

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
        }

        public bool PartIsMounted(Part mountedPart)
        {
            foreach (KeyValuePair<AttachNode, List<string>> mount in GetMounts())
            {
                if (mount.Key.attachedPart)
                {
                    if (mount.Key.attachedPart == mountedPart)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Dictionary<AttachNode, List<string>> GetMounts()
        {
            Dictionary<AttachNode, List<string>> mounts = new Dictionary<AttachNode, List<string>>();
            ConfigNode node = KIS_Shared.GetBaseConfigNode(this);
            foreach (ConfigNode mountNode in node.GetNodes("MOUNT"))
            {
                if (mountNode.HasValue("attachNode") && mountNode.HasValue("allowedPartName"))
                {
                    string attachNodeName = mountNode.GetValue("attachNode");
                    AttachNode an = this.part.findAttachNode(attachNodeName);
                    if (an == null)
                    {
                        KIS_Shared.DebugError("GetMountNodes - Node : " + attachNodeName + " not found !");
                        continue;
                    }

                    List<string> allowedPartNames = new List<string>();
                    foreach(string partName in mountNode.GetValues("allowedPartName"))
                    {
                        allowedPartNames.Add(partName.Replace('_', '.'));
                    }
                    mounts.Add(an, allowedPartNames);
                }
            }
            return mounts;
        }

        [KSPEvent(name = "ContextMenuRelease", active = true, guiActive = true, guiActiveUnfocused = true, guiName = "Release")]
        public void ContextMenuRelease()
        {
            foreach (KeyValuePair<AttachNode, List<string>> mount in GetMounts())
            {
                if (mount.Key.attachedPart)
                {
                    mount.Key.attachedPart.decouple();
                }
            }
        }

        [KSPAction("Release")]
        public void ActionGroupRelease(KSPActionParam param)
        {
            if (!this.part.packed)
            {
                ContextMenuRelease();
            }
        }
    }
}