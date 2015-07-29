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
        public string equipSlot = "";
        [KSPField]
        public string equipSkill = "";
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
        public bool carriable = false;
        [KSPField]
        public bool editorItemsCategory = true;
        [KSPField]
        public int allowPartAttach = 2;     // 0:false / 1:true / 2:Attach tool needed
        [KSPField]
        public int allowStaticAttach = 0;   // 0:false / 1:true / 2:Attach tool needed
        [KSPField]
        public bool useExternalPartAttach = false; // For KAS
        [KSPField]
        public bool useExternalStaticAttach = false; // For KAS
        [KSPField]
        public float staticAttachBreakForce = 10;
        [KSPField(isPersistant = true)]
        public bool staticAttached = false;

        private FixedJoint fixedJoint;
        private GameObject connectedGameObject;

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

        public virtual void OnPartUnpack()
        {
            if (allowStaticAttach == 0) return;
            if (useExternalStaticAttach) return;
            if (staticAttached)
            {
                KIS_Shared.DebugLog("Re-attach static object (OnPartUnpack)");
                GroundAttach();
            }
        }

        public void OnKISAction(BaseEventData baseEventData)
        {
            if (allowStaticAttach == 0) return;
            if (useExternalStaticAttach) return;
            string action = baseEventData.GetString("action");
            Part tgtPart = (Part)baseEventData.Get("targetPart");
            if (action == KIS_Shared.MessageAction.Store.ToString() || action == KIS_Shared.MessageAction.DropEnd.ToString() || action == KIS_Shared.MessageAction.AttachStart.ToString())
            {
                GroundDetach();
            }
            if (action == KIS_Shared.MessageAction.AttachEnd.ToString() && tgtPart == null)
            {
                GroundAttach();
            }
        }

        public void GroundAttach()
        {
            KIS_Shared.DebugLog("Create kinematic rigidbody");
            if (connectedGameObject) Destroy(connectedGameObject);
            GameObject obj = new GameObject("KISBody");
            obj.AddComponent<Rigidbody>();
            obj.rigidbody.mass = 100;
            obj.rigidbody.isKinematic = true;
            obj.transform.position = this.part.transform.position;
            obj.transform.rotation = this.part.transform.rotation;
            connectedGameObject = obj;

            KIS_Shared.DebugLog("Create fixed joint on the kinematic rigidbody");
            if (fixedJoint) Destroy(fixedJoint);
            FixedJoint CurJoint = this.part.gameObject.AddComponent<FixedJoint>();
            CurJoint.breakForce = staticAttachBreakForce;
            CurJoint.breakTorque = staticAttachBreakForce;
            CurJoint.connectedBody = obj.rigidbody;
            fixedJoint = CurJoint;
            this.part.vessel.Landed = true;
            staticAttached = true;
            ModuleKISPickup modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
            if (modulePickup) KIS_Shared.PlaySoundAtPoint(modulePickup.attachStaticSndPath, this.transform.position);
        }

        public void GroundDetach()
        {
            if (staticAttached)
            {
                KIS_Shared.DebugLog("Removing static rigidbody and fixed joint on " + this.part.partInfo.title);
                if (fixedJoint) Destroy(fixedJoint);
                if (connectedGameObject) Destroy(connectedGameObject);
                fixedJoint = null;
                connectedGameObject = null;
                staticAttached = false;
                ModuleKISPickup modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
                if (modulePickup) KIS_Shared.PlaySoundAtPoint(modulePickup.detachStaticSndPath, this.transform.position);
            }
        }


    }
}