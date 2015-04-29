using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISItemStatic : ModuleKISItem
    {
        [KSPField(isPersistant = true)]
        public bool groundAttached = false;
        [KSPField]
        public bool useJoint = false;
        [KSPField]
        public float jointBreakForce = 10;

        private FixedJoint fixedJoint;
        private GameObject connectedGameObject;

        public void OnAttachStatic()
        {
            if (useJoint)
            {
                KIS_Shared.DebugLog("Create kinematic rigidbody");
                if (connectedGameObject) Destroy(connectedGameObject);
                GameObject obj = new GameObject("KISBody");
                obj.AddComponent<Rigidbody>();
                obj.rigidbody.isKinematic = true;
                obj.transform.position = this.part.transform.position;
                obj.transform.rotation = this.part.transform.rotation;
                connectedGameObject = obj;

                KIS_Shared.DebugLog("Create fixed joint on the kinematic rigidbody");
                if (fixedJoint) Destroy(fixedJoint);
                FixedJoint CurJoint = this.part.gameObject.AddComponent<FixedJoint>();
                CurJoint.breakForce = jointBreakForce;
                CurJoint.breakTorque = jointBreakForce;
                CurJoint.connectedBody = obj.rigidbody;
                fixedJoint = CurJoint;
            }
            else
            {
                KIS_Shared.DebugLog("Set part as kinematic");
                this.part.rigidbody.isKinematic = true;
            }
            groundAttached = true;
        }

        public virtual void OnPartUnpack()
        {
            if (groundAttached)
            {
                KIS_Shared.DebugLog("Re-attach static object (OnPartUnpack)");
                OnAttachStatic();
            }
        }

        public void OnDecoupleFromAll()
        {
            if (useJoint)
            {
                KIS_Shared.DebugLog("Removing static rigidbody and fixed joint on " + this.part.partInfo.title);
                if (fixedJoint) Destroy(fixedJoint);
                if (connectedGameObject) Destroy(connectedGameObject);
                fixedJoint = null;
                connectedGameObject = null;
            }
            else
            {
                KIS_Shared.DebugLog("Unset part as kinematic");
                this.part.rigidbody.isKinematic = false;
            }
            groundAttached = false;
        }

    }
}