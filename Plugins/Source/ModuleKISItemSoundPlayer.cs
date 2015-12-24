using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISItemSoundPlayer : ModuleKISItem
    {
        [KSPField]
        public string sndPath = "KIS/Sounds/guitar";
        [KSPField]
        public float sndMaxDistance = 10;
        public bool loop = false;
        public FXGroup sndFx;


        public override void OnStart(StartState state)
        {
            KSP_Dev.LoggedCallWrapper.Action(Internal_OnStart, state);
        }

        private void Internal_OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.None) return;
            if (HighLogic.LoadedSceneIsFlight)
            {
                sndFx.audio = this.part.gameObject.AddComponent<AudioSource>();
                sndFx.audio.volume = GameSettings.SHIP_VOLUME;
                sndFx.audio.rolloffMode = AudioRolloffMode.Linear;
                sndFx.audio.dopplerLevel = 0f;
                sndFx.audio.panLevel = 1f;
                sndFx.audio.maxDistance = sndMaxDistance;
                sndFx.audio.loop = loop;
                sndFx.audio.playOnAwake = false;
                sndFx.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
            }
        }

        public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
        {
            if (useFrom != KIS_Item.UseFrom.KeyUp)
            {
                if (item.inventory.sndFx.audio.isPlaying)
                {
                    item.inventory.sndFx.audio.Stop();
                }
                else
                {
                    item.inventory.PlaySound(sndPath, false, false);
                }
            }
        }

        [KSPEvent(name = "ContextMenuPlay", guiActiveEditor = false, active = true, guiActive = true, guiActiveUnfocused = true, guiName = "Play")]
        public void Play()
        {
            if (sndFx.audio.isPlaying)
            {
                sndFx.audio.Stop();
            }
            else
            {
                sndFx.audio.Play();
            }
        }

    }
}