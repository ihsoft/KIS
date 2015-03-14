using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISItem : PartModule
    {
        [KSPField]
        public string moveSndPath = "KIS/Sounds/itemMove";
        [KSPField]
        public string shortcutKeyAction = "drop";
        [KSPField]
        public bool usableFromEva = false;
        [KSPField]
        public bool usableFromContainer = false;
        [KSPField]
        public bool usableFromPod = false;
        [KSPField]
        public bool usableFromEditor = false;
        [KSPField]
        public string useName = "use";
        [KSPField]
        public bool stackable = false;
        [KSPField]
        public bool equipable = false;
        [KSPField]
        public string equipMode = "model";
        [KSPField]
        public string equipSlot = null;
        [KSPField]
        public string equipTrait = null;
        [KSPField]
        public bool equipRemoveHelmet = false;
        [KSPField]
        public string equipMeshName = "helmet";
        [KSPField]
        public string equipBoneName = "bn_helmet01";
        [KSPField]
        public Vector3 equipPos = new Vector3(0f, 0f, 0f);
        [KSPField]
        public Vector3 equipDir = new Vector3(0f, 0f, 0f);
        [KSPField]
        public float volumeOverride = 0;
        [KSPField]
        public bool editorItemsCategory = true;


        public virtual void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom)
        {

        }

        public virtual void OnItemUpdate(KIS_Item item)
        {

        }

        public virtual void OnItemGUI(KIS_Item item)
        {

        }

        public virtual void OnDragToPart(KIS_Item item, Part destPart)
        {

        }

        public virtual void OnDragToInventory(KIS_Item item, ModuleKISInventory destInventory, int destSlot)
        {

        }

        public virtual void OnEquip(KIS_Item item)
        {

        }

        public virtual void OnUnEquip(KIS_Item item)
        {

        }
    }
}