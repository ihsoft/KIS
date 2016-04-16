using System.Collections;
using KSPDev.LogUtils;
using System;
using System.Linq;
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
                Logger.logInfo("Re-attach static object (OnPartUnpack)");
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
                ModuleKISPickup modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
                if (modulePickup) KIS_Shared.PlaySoundAtPoint(modulePickup.detachStaticSndPath, this.transform.position);
            }
            if (action == KIS_Shared.MessageAction.AttachEnd.ToString() && tgtPart == null)
            {
                GroundAttach();
                ModuleKISPickup modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
                if (modulePickup) KIS_Shared.PlaySoundAtPoint(modulePickup.attachStaticSndPath, this.transform.position);
            }
        }

        public void GroundAttach()
        {
            staticAttached = true;
            StartCoroutine(WaitAndStaticAttach());
        }
        
        IEnumerator WaitAndStaticAttach() {
            // Wait for part to become active in case of it came from inventory.
            while (!part.started && part.State != PartStates.DEAD) {
                yield return new WaitForFixedUpdate();
            }

            part.vessel.Landed = true;
            yield return new WaitForFixedUpdate();

            // Stop physics on the part to prevent immediate breaking of the joint.
            // FIXME(ihsoft): Remove it once 1.1 issue with broken joints is fixed.
            Logger.logInfo("Enable kinematic rigindbody on part: {0}", part);
            part.Rigidbody.isKinematic = true;
  
            Logger.logInfo("Create kinematic rigidbody");
            if (connectedGameObject) Destroy(connectedGameObject);
            var obj = new GameObject("KISBody");
            var objRigidbody = obj.AddComponent<Rigidbody>();
            objRigidbody.mass = 100;
            objRigidbody.isKinematic = true;
            obj.transform.position = part.transform.position;
            obj.transform.rotation = part.transform.rotation;
            connectedGameObject = obj;

            Logger.logInfo("Create fixed joint on the kinematic rigidbody");
            if (fixedJoint) Destroy(fixedJoint);
            fixedJoint = part.gameObject.AddComponent<FixedJoint>();
            fixedJoint.breakForce = staticAttachBreakForce;
            fixedJoint.breakTorque = staticAttachBreakForce;
        }

        // Resets item state when joint is broken.
        // A callback from MonoBehaviour.
        void OnJointBreak(float breakForce) {
          Logger.logWarning("A ground joint has just been broken! Force: {0}", breakForce);
          GroundDetach();
        }

        public void GroundDetach()
        {
            if (staticAttached)
            {
                Logger.logInfo(
                    "Removing static rigidbody and fixed joint on: {0}", this.part.partInfo.title);
                if (fixedJoint) Destroy(fixedJoint);
                if (connectedGameObject) Destroy(connectedGameObject);
                fixedJoint = null;
                connectedGameObject = null;
                staticAttached = false;

                // Enable physics back.
                // FIXME(ihsoft): Remove it once 1.1 issue with broken joints is fixed.
                part.Rigidbody.isKinematic = false;
            }
        }


    }
}