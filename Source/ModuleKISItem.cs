using KSPDev.ProcessingUtils;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using UnityEngine;

namespace KIS {

public class ModuleKISItem : PartModule {
  /// <summary>Specifies how item can be attached.</summary>
  public enum ItemAttachMode {
    /// <summary>Not initialized. Special value.</summary>
    Unknown = -1,
    /// <summary>The item cannot be attached.</summary>
    Disabled = 0,
    /// <summary>The item can be attached with bare hands.</summary>
    /// <remarks>EVA skill is not checked. Anyone can attach such items.</remarks>
    AllowedAlways = 1,
    /// <summary>The item can be attached only if a KIS attach tool is equipped.</summary>
    /// <remarks>The tool may apply extra limitations on the attach action. E.g. wrenches cannot
    /// attach to stack nodes.</remarks>
    AllowedWithKisTool = 2
  }
  
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
  public ItemAttachMode allowPartAttach = ItemAttachMode.AllowedWithKisTool;
  [KSPField]
  public ItemAttachMode allowStaticAttach = ItemAttachMode.Disabled;
  [KSPField]
  public bool useExternalPartAttach = false;
  // For KAS
  [KSPField]
  public bool useExternalStaticAttach = false;
  // For KAS
  [KSPField]
  public float staticAttachBreakForce = 10;
  [KSPField(isPersistant = true)]
  public bool staticAttached = false;

  FixedJoint staticAttachJoint;

  protected virtual void OnPartDie() {
    if (vessel.isEVA) {
      var inventory = vessel.rootPart.GetComponent<ModuleKISInventory>();
      var item = inventory.items.Values.FirstOrDefault(i => i.equipped && i.equippedPart == part);
      if (item != null) {
        Debug.LogFormat("Item {0} has been destroyed. Drop it from inventory of {1}",
                        item.availablePart.title, inventory.part.name);
        AsyncCall.CallOnEndOfFrame(inventory, () => inventory.DeleteItem(item.slot));
      }
    }
  }

  #region IInventoryItem candidate
  public virtual void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
  }

  // TODO(ihsoft): Deprecate it. Too expensive.
  public virtual void OnItemUpdate(KIS_Item item) {
  }

  // TODO(ihsoft): Deprecate it. Too expensive.
  public virtual void OnItemGUI(KIS_Item item) {
  }

  public virtual void OnDragToPart(KIS_Item item, Part destPart) {
  }

  public virtual void OnDragToInventory(KIS_Item item, ModuleKISInventory destInventory,
                                        int destSlot) {
  }

  public virtual void OnEquip(KIS_Item item) {
  }

  public virtual void OnUnEquip(KIS_Item item) {
  }
  #endregion

  public virtual void OnPartUnpack() {
    if (allowStaticAttach == ItemAttachMode.Disabled || useExternalStaticAttach) {
      return;
    }
    if (staticAttached) {
      Debug.Log("Re-attach static object (OnPartUnpack)");
      GroundAttach();
    }
  }

  public void OnKISAction(Dictionary<string, object> eventData) {
    if (allowStaticAttach == ItemAttachMode.Disabled || useExternalStaticAttach) {
      return;
    }
    var action = eventData["action"].ToString();
    var tgtPart = eventData["targetPart"] as Part;
    //FIXME: use enum values 
    if (action == KIS_Shared.MessageAction.Store.ToString()
        || action == KIS_Shared.MessageAction.DropEnd.ToString()
        || action == KIS_Shared.MessageAction.AttachStart.ToString()) {
      GroundDetach();
      var modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
      if (modulePickup) {
        KIS_Shared.PlaySoundAtPoint(modulePickup.detachStaticSndPath, this.transform.position);
      }
    }
    if (action == KIS_Shared.MessageAction.AttachEnd.ToString() && tgtPart == null) {
      GroundAttach();
      var modulePickup = KISAddonPickup.instance.GetActivePickupNearest(this.transform.position);
      if (modulePickup) {
        KIS_Shared.PlaySoundAtPoint(modulePickup.attachStaticSndPath, this.transform.position);
      }
    }
  }

  public void GroundAttach() {
    staticAttached = true;
    StartCoroutine(WaitAndStaticAttach());
  }

  IEnumerator WaitAndStaticAttach() {
    // Wait for part to become active in case of it came from inventory.
    while (!part.started && part.State != PartStates.DEAD) {
      yield return new WaitForFixedUpdate();
    }
    part.vessel.Landed = true;

    Debug.Log("Create fixed joint attached to the world");
    if (staticAttachJoint) {
      Destroy(staticAttachJoint);
    }
    staticAttachJoint = part.gameObject.AddComponent<FixedJoint>();
    staticAttachJoint.breakForce = staticAttachBreakForce;
    staticAttachJoint.breakTorque = staticAttachBreakForce;
  }

  // Resets item state when joint is broken.
  // A callback from MonoBehaviour.
  void OnJointBreak(float breakForce) {
    if (staticAttached) {
      Debug.LogWarningFormat("A static joint has just been broken! Force: {0}", breakForce);
    } else {
      Debug.LogWarningFormat("A fixed joint has just been broken! Force: {0}", breakForce);
    }
    GroundDetach();
  }

  public void GroundDetach() {
    if (staticAttached) {
      Debug.LogFormat(
          "Removing static rigidbody and fixed joint on: {0}", this.part.partInfo.title);
      if (staticAttachJoint) {
        Destroy(staticAttachJoint);
      }
      staticAttachJoint = null;
      staticAttached = false;
    }
  }
}

}  // namespace
