using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISPartStatic : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool groundAttached = false;
        [KSPField]
        public float breakForce = 10;

        private FixedJoint fixedJoint;
        private GameObject connectedGameObject;

        public virtual void OnPartUnpack()
        {
            if (groundAttached)
            {
                KIS_Shared.DebugLog("Re-attach static object (OnPartUnpack)");
                GroundAttach();
            }
        }

        public void OnKISAction(BaseEventData baseEventData)
        {
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
            CurJoint.breakForce = breakForce;
            CurJoint.breakTorque = breakForce;
            CurJoint.connectedBody = obj.rigidbody;
            fixedJoint = CurJoint;
            this.part.vessel.Landed = true;
            groundAttached = true;
        }

        public void GroundDetach()
        {
            KIS_Shared.DebugLog("Removing static rigidbody and fixed joint on " + this.part.partInfo.title);
            if (fixedJoint) Destroy(fixedJoint);
            if (connectedGameObject) Destroy(connectedGameObject);
            fixedJoint = null;
            connectedGameObject = null;
            groundAttached = false;
        }

    }
}