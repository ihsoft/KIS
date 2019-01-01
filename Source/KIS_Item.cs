// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KIS.GUIUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using KSPDev.ProcessingUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_02005.
public sealed class KIS_Item {

  #region Localizable GUI strings.
  static readonly Message CannotEquipItemStackedMsg = new Message(
      "#kisLOC_02000",
      defaultTemplate: "Cannot equip stacked items",
      description: "The screen message to present when the item was attempted to equip, but the"
      + " relevant inventory slot has more than one item (stacked).");

  static readonly Message<string, string> CannotEquipAlreadyCarryingMsg =
      new Message<string, string>(
          "#kisLOC_02001",
          defaultTemplate: "Cannot equip item, slot [<<1>>] already used for carrying <<2>>",
          description: "The screen message to present when the item was attempted to equip, but its"
          + " equip slot is already taken by a carriable item."
          + "\nArgument <<1>> is the name of the equip slot of the item."
          + "\nArgument <<2>> is the name of the item being carried.");

  static readonly Message<string> CannotEquipRestrictedToSkillMsg = new Message<string>(
      "#kisLOC_02002",
      defaultTemplate: "This item can only be used by a kerbal with the skill: <<1>>",
      description: "The screen message to present when the item was attempted to equip, but a"
      + " specific kerbal trait (skill) is required to handle the item."
      + "\nArgument <<1>> is the name of the required trait.");

  static readonly Message<VolumeLType> CannotStackMaxVolumeReachedMsg = new Message<VolumeLType>(
      "#kisLOC_02003",
      defaultTemplate: "Max destination volume reached (+<<1>>)",
      description: "The screen message to present when the item was attempted to be added to an"
      + " existing slot stack, but the resulted inventory volume would exceed the maximum allowed"
      + " volume."
      + "\nArgument <<1>> is the excessive volume. Format: VolumeLType.");

  static readonly Message CannotStackItemEquippedMsg = new Message(
      "#kisLOC_02004",
      defaultTemplate: "Cannot stack with equipped item",
      description: "The screen message to present when the item was attempted to be added to an"
      + " existing slot stack, but the item being added is currently equipped.");
  #endregion

  /// <summary>Name for the helmet slot.</summary>
  /// <remarks>
  /// While the other slots are simply arbitrary strings, the helmet slot is special. When an item
  /// equips to it, the stock helmet becomes hidden, and it becomes visible gains on unequip. 
  /// </remarks>
  public const string HelmetSlotName = "helmet";

  public ConfigNode partNode;
  public AvailablePart availablePart;
  public int quantity;
  public KIS_IconViewer icon = null;
  public bool stackable = false;
  public string equipSlot;
  public bool usableFromEva = false;
  public bool usableFromContainer = false;
  public bool usableFromPod = false;
  public bool usableFromEditor = false;
  public bool carriable = false;
  public float volume;
  public float cost;
  public bool equipable = false;
  public bool equipped = false;
  public ModuleKISInventory inventory;
  public ModuleKISItem prefabModule;
  GameObject equippedGameObj;
  public Part equippedPart;
  Transform evaTransform;
  public enum ActionType {
    Drop,
    Equip,
    Custom
  }
  public enum UseFrom {
    KeyDown,
    KeyUp,
    InventoryShortcut,
    ContextMenu
  }
  public enum EquipMode {
    Model,
    Part,
    Physic
  }

  /// <summary>Specifies source of an action.</summary>
  public enum ActorType {
    /// <summary>Action is triggered as a result of some code logic.</summary>
    API,
    /// <summary>Player has triggered action via GUI.</summary>
    Player,
    /// <summary>Physics effect have trigegred the action.</summary>
    Physics,
  }

  public float resourceMass = 0;
  public float contentMass = 0;
  public float contentCost = 0;
  public string inventoryName = "";

  public struct ResourceInfo {
    public string resourceName;
    public double amount;
    public double maxAmount;
  }

  public EquipMode equipMode {
    get {
      EquipMode mode = EquipMode.Model;
      if (prefabModule) {
        if (prefabModule.equipMode == "physic") {
          mode = EquipMode.Physic;
        }
        if (prefabModule.equipMode == "part") {
          mode = EquipMode.Part;
        }
      }
      return mode;
    }
  }

  public ActionType shortcutAction {
    get {
      ActionType mode = ActionType.Drop;
      if (prefabModule) {
        if (prefabModule.shortcutKeyAction == "equip") {
          mode = ActionType.Equip;
        }
        if (prefabModule.shortcutKeyAction == "custom") {
          mode = ActionType.Custom;
        }
      }
      return mode;
    }
  }

  public bool carried {
    get {
      if (carriable && inventory.invType == ModuleKISInventory.InventoryType.Eva
          && HighLogic.LoadedSceneIsFlight) {
        return true;
      }
      return false;
    }
  }

  public int slot { get { return inventory.items.FirstOrDefault(x => x.Value == this).Key; } }
  public float stackVolume { get { return volume * quantity; } }
  public float dryMass { get { return availablePart.partPrefab.mass; } }
  public float stackDryMass { get { return dryMass * quantity; } }
  public float totalCost { get { return (cost + contentCost) * quantity; } }
  public float totalMass { get { return stackDryMass + resourceMass + contentMass; } }

  /// <summary>A breaking force of a joint that attaches equipped item to the kerbal.</summary>
  /// TODO: Read it from the items's config. See #128.
  const float EqippedPartJointBreakForce = 50.0f;

  /// <summary>Creates a new part from save.</summary>
  public KIS_Item(AvailablePart availablePart, ConfigNode itemNode, ModuleKISInventory inventory,
                  int quantity = 1) {
    // Get part node
    this.availablePart = availablePart;
    partNode = new ConfigNode();
    itemNode.GetNode("PART").CopyTo(partNode);
    // init config
    this.InitConfig(availablePart, inventory, quantity);
    // Get mass
    if (itemNode.HasValue("resourceMass")) {
      resourceMass = float.Parse(itemNode.GetValue("resourceMass"));
    } else {
      resourceMass = availablePart.partPrefab.GetResourceMass();
    }
    if (itemNode.HasValue("contentMass")) {
      contentMass = float.Parse(itemNode.GetValue("contentMass"));
    }
    if (itemNode.HasValue("contentCost")) {
      contentCost = float.Parse(itemNode.GetValue("contentCost"));
    }
    if (itemNode.HasValue("inventoryName")) {
      inventoryName = itemNode.GetValue("inventoryName");
    }
  }

  /// <summary>Creates a new part from scene.</summary>
  public KIS_Item(Part part, ModuleKISInventory inventory, int quantity = 1) {
    // Get part node
    this.availablePart = PartLoader.getPartInfoByName(part.partInfo.name);
    this.partNode = new ConfigNode();
    KIS_Shared.PartSnapshot(part).CopyTo(this.partNode);
    // init config
    this.InitConfig(availablePart, inventory, quantity);
    // Get mass
    this.resourceMass = part.GetResourceMass();
    ModuleKISInventory itemInventory = part.GetComponent<ModuleKISInventory>();
    if (itemInventory) {
      this.contentMass = itemInventory.GetContentMass();
      this.contentCost = itemInventory.GetContentCost();
      if (itemInventory.invName != "") {
        this.inventoryName = itemInventory.invName;
      }
    }
  }

  void InitConfig(AvailablePart availablePart, ModuleKISInventory inventory, int quantity) {
    this.inventory = inventory;
    this.quantity = quantity;
    prefabModule = availablePart.partPrefab.GetComponent<ModuleKISItem>();
    volume = KIS_Shared.GetPartVolume(availablePart);
    cost = GetCost();

    // Set launchID
    if (partNode.HasValue("launchID")) {
      if (int.Parse(this.partNode.GetValue("launchID")) == 0) {
        partNode.SetValue("launchID", this.inventory.part.launchID.ToString(), true);
      }
    } else {
      partNode.SetValue("launchID", this.inventory.part.launchID.ToString(), true);
    }

    if (prefabModule) {
      equipable = prefabModule.equipable;
      stackable = prefabModule.stackable;
      equipSlot = prefabModule.equipSlot;
      usableFromEva = prefabModule.usableFromEva;
      usableFromContainer = prefabModule.usableFromContainer;
      usableFromPod = prefabModule.usableFromPod;
      usableFromEditor = prefabModule.usableFromEditor;
      carriable = prefabModule.carriable;
    }
    int nonStackableModule = 0;
    foreach (PartModule pModule in availablePart.partPrefab.Modules) {
      if (!KISAddonConfig.stackableModules.Contains(pModule.moduleName)) {
        nonStackableModule++;
      }
    }
    if (nonStackableModule == 0 && GetResources().Count == 0) {
      DebugEx.Info(
          "No non-stackable module or a resource found on the part, set the item as stackable");
      stackable = true;
    }
    if (KISAddonConfig.stackableList.Contains(availablePart.name)
        || availablePart.name.IndexOf('.') != -1
        && KISAddonConfig.stackableList.Contains(availablePart.name.Replace('.', '_'))) {
      DebugEx.Info("Part name present in settings.cfg (node StackableItemOverride),"
                   + " force item as stackable");
      stackable = true;
    }
  }

  public void OnSave(ConfigNode node) {
    node.AddValue("partName", this.availablePart.name);
    node.AddValue("slot", slot);
    node.AddValue("quantity", quantity);
    node.AddValue("equipped", equipped);
    node.AddValue("resourceMass", resourceMass);
    node.AddValue("contentMass", contentMass);
    node.AddValue("contentCost", contentCost);
    if (inventoryName != "") {
      node.AddValue("inventoryName", inventoryName);
    }
    // Items in pod and container may have equipped status True but they are not actually equipped,
    // so there is no equipped part.
    if (equipped && equippedPart != null
        && (equipMode == EquipMode.Part || equipMode == EquipMode.Physic)) {
      DebugEx.Info("Update config node of equipped part: {0}", availablePart.title);
      partNode.ClearData();
      KIS_Shared.PartSnapshot(equippedPart).CopyTo(partNode);
    }
    partNode.CopyTo(node.AddNode("PART"));
  }

  public List<ResourceInfo> GetResources() {
    var resources = new List<ResourceInfo>();
    foreach (ConfigNode node in this.partNode.GetNodes("RESOURCE")) {
      if (node.HasValue("name") && node.HasValue("amount") && node.HasValue("maxAmount")) {
        var rInfo = new ResourceInfo();
        rInfo.resourceName = node.GetValue("name");
        rInfo.amount = double.Parse(node.GetValue("amount"));
        rInfo.maxAmount = double.Parse(node.GetValue("maxAmount"));
        resources.Add(rInfo);
      }
    }
    return resources;
  }

  public List<ScienceData> GetSciences() {
    var sciences = new List<ScienceData>();
    foreach (ConfigNode module in this.partNode.GetNodes("MODULE")) {
      foreach (ConfigNode experiment in module.GetNodes("ScienceData")) {
        var scienceData = new ScienceData(experiment);
        sciences.Add(scienceData);
      }
    }
    return sciences;
  }

  // TODO(ihsoft): Move to KIS_Shared.
  public float GetCost() {
    // TweakScale compatibility
    foreach (ConfigNode node in this.partNode.GetNodes("MODULE")) {
      if (node.HasValue("name") && node.GetValue("name") == "TweakScale"
          && node.HasValue("DryCost")) {
        double ressourcesCost = 0;
        foreach (ResourceInfo resource in GetResources()) {
          var pRessourceDef = PartResourceLibrary.Instance.GetDefinition(resource.resourceName);
          ressourcesCost += resource.amount * pRessourceDef.unitCost;
        }
        return float.Parse(node.GetValue("DryCost")) + (float)ressourcesCost;
      }
    }
    return availablePart.cost;
  }

  public void SetResource(string name, double amount) {
    foreach (ConfigNode node in this.partNode.GetNodes("RESOURCE")) {
      if (node.HasValue("name") && node.GetValue("name") == name
          && node.HasValue("amount") && node.HasValue("maxAmount")) {
        node.SetValue("amount", amount.ToString());
        return;
      }
    }
  }

  public void EnableIcon(int resolution) {
    DisableIcon();
    icon = new KIS_IconViewer(availablePart.partPrefab, resolution);
  }

  public void DisableIcon() {
    if (icon != null) {
      icon.Dispose();
      icon = null;
    }
  }

  //TODO(ihsoft): It's too expensive to call it for every item.
  public void Update() {
    if (equippedGameObj != null) {
      equippedGameObj.transform.rotation =
          evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
      equippedGameObj.transform.position = evaTransform.TransformPoint(prefabModule.equipPos);
    }
    if (prefabModule != null) {
      prefabModule.OnItemUpdate(this);
    }
  }

  public void GUIUpdate() {
    if (prefabModule) {
      prefabModule.OnItemGUI(this);
    }
  }

  public bool CanStackAdd(float qty, bool checkVolume = true) {
    if (qty <= 0) {
      return false;
    }
    if (equipped) {
      ScreenMessaging.ShowPriorityScreenMessage(CannotStackItemEquippedMsg);
      UISounds.PlayBipWrong();
      return false;
    }
    float newVolume = inventory.totalVolume + (volume * qty);
    if (checkVolume && newVolume > inventory.maxVolume) {
      ScreenMessaging.ShowPriorityScreenMessage(
          CannotStackMaxVolumeReachedMsg.Format(newVolume - inventory.maxVolume));
      return false;
    }
    return true;
  }

  public bool StackAdd(int qty, bool checkVolume = true) {
    if (CanStackAdd(qty, checkVolume)) {
      quantity += qty;
      inventory.RefreshMassAndVolume();
      return true;
    }
    return false;
  }

  public bool StackRemove(int qty = 1) {
    if (qty <= 0) {
      return false;
    }
    if (quantity - qty <= 0) {
      Delete();
      return false;
    }
    quantity -= qty;
    inventory.RefreshMassAndVolume();
    return true;
  }

  public void Delete() {
    if (inventory.showGui) {
      DisableIcon();
    }
    if (equipped) {
      Unequip();
    }
    inventory.items.Remove(slot);
    inventory.RefreshMassAndVolume();
  }

  public void ShortcutKeyPress() {
    if (shortcutAction == ActionType.Drop) {
      KISAddonPickup.instance.Drop(this);
    }
    if (shortcutAction == ActionType.Equip) {
      if (equipped) {
        Unequip(ActorType.Player);
      } else {
        Equip(ActorType.Player);
      }
    }
    if (shortcutAction == ActionType.Custom && prefabModule) {
      prefabModule.OnItemUse(this, KIS_Item.UseFrom.InventoryShortcut);
    }
  }

  public void Use(UseFrom useFrom) {
    if (prefabModule) {
      prefabModule.OnItemUse(this, useFrom);
    }
  }

  public void Equip(ActorType actorType = ActorType.API) {
    // Only equip EVA kerbals.
    if (!prefabModule || inventory.invType != ModuleKISInventory.InventoryType.Eva) {
      DebugEx.Warning("Cannot equip item from inventory type: {0}", inventory.invType);
      return;
    }
    if (quantity > 1) {
      ScreenMessaging.ShowPriorityScreenMessage(CannotEquipItemStackedMsg);
      UISounds.PlayBipWrong();
      return;
    }
    DebugEx.Info("Equip item {0} in mode {1}", availablePart.title, equipMode);

    // Check if the skill is needed. Skip the check in the sandbox modes.
    if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX
        && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX
        && !String.IsNullOrEmpty(prefabModule.equipSkill)) {
      bool skillFound = false;
      List<ProtoCrewMember> protoCrewMembers = inventory.vessel.GetVesselCrew();
      foreach (var expEffect in protoCrewMembers[0].experienceTrait.Effects) {
        if (expEffect.ToString().Replace("Experience.Effects.", "") == prefabModule.equipSkill) {
          skillFound = true;
          break;
        }
      }
      if (!skillFound) {
        if (actorType == ActorType.Player) {
          ScreenMessaging.ShowPriorityScreenMessage(
              CannotEquipRestrictedToSkillMsg.Format(prefabModule.equipSkill));
          UISounds.PlayBipWrong();
        }
        return;
      }
    }

    // Check if slot is already occupied.
    if (equipSlot != null) {
      KIS_Item equippedItem = inventory.GetEquipedItem(equipSlot);
      if (equippedItem != null) {
        if (equippedItem.carriable && actorType == ActorType.Player) {
          ScreenMessaging.ShowPriorityScreenMessage(
              CannotEquipAlreadyCarryingMsg.Format(equipSlot, equippedItem.availablePart.title));
          UISounds.PlayBipWrong();
          return;
        }
        equippedItem.Unequip();
      }
    }

    // Find the bone for this item to follow.
    evaTransform =
        KISAddonConfig.FindEquipBone(inventory.part.transform, prefabModule.equipBoneName);
    if (evaTransform == null) {
      return;  // Cannot equip!
    }
    if (equipMode == EquipMode.Model) {
      var modelGo = availablePart.partPrefab.FindModelTransform("model").gameObject;
      equippedGameObj = UnityEngine.Object.Instantiate(modelGo);
      equippedGameObj.transform.parent = inventory.part.transform;
      foreach (Collider col in equippedGameObj.GetComponentsInChildren<Collider>()) {
        UnityEngine.Object.DestroyImmediate(col);
      }
    } else {
      var alreadyEquippedPart = inventory.part.FindChildPart(availablePart.name);
      if (alreadyEquippedPart) {
        DebugEx.Info("Part {0} already found on eva, use it as the item", availablePart.name);
        equippedPart = alreadyEquippedPart;
        // This magic is copied from the KervalEVA.OnVesselGoOffRails() method.
        // There must be at least 3 fixed frames delay before updating the colliders.
        AsyncCall.WaitForPhysics(
            equippedPart, 3, () => false,
            failure: () => OnEquippedPartReady(equippedPart));
        if (equipMode == EquipMode.Part) {
          // Ensure the part doesn't have rigidbody and is not affected by physics.
          // The part may not like it.
          equippedPart.PhysicsSignificance = 1;  // Disable physics on the part.
          UnityEngine.Object.Destroy(equippedPart.rb);
        }
      } else {
        Vector3 equipPos = evaTransform.TransformPoint(prefabModule.equipPos);
        Quaternion equipRot = evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
        equippedPart = KIS_Shared.CreatePart(
            partNode, equipPos, equipRot, inventory.part,
            coupleToPart: inventory.part,
            srcAttachNodeId: "srfAttach",
            onPartReady: OnEquippedPartReady,
            createPhysicsless: equipMode != EquipMode.Physic);
      }
      if (equipMode == EquipMode.Part) {
        equippedGameObj = equippedPart.gameObject;
      }
    }

    // Hide the stock meshes if the custom helmet is equipped.
    if (equipSlot == HelmetSlotName) {
      var kerbalModule = inventory.part.FindModuleImplementing<KerbalEVA>();
      if (kerbalModule.helmetTransform != null) {
        for (var i = 0; i < kerbalModule.helmetTransform.childCount; i++) {
          kerbalModule.helmetTransform.GetChild(i).gameObject.SetActive(false);
        }
        if (equippedGameObj != null) {
          equippedGameObj.transform.parent = kerbalModule.helmetTransform;
        }
      } else {
        DebugEx.Warning("Kerbal model doesn't have helmet transform: {0}", inventory);
      }
    }

    if (actorType == ActorType.Player) {
      UISoundPlayer.instance.Play(prefabModule.moveSndPath);
    }
    equipped = true;
    prefabModule.OnEquip(this);
  }

  public void Unequip(ActorType actorType = ActorType.API) {
    if (!prefabModule) {
      return;
    }
    // This must be the first thing to happen to prevent the other handlers to trigger. 
    equipped = false;
    if (equipMode == EquipMode.Model) {
      UnityEngine.Object.Destroy(equippedGameObj);
    }
    if (equipMode == EquipMode.Part || equipMode == EquipMode.Physic) {
      DebugEx.Info("Update config node of equipped part: {0}", availablePart.title);
      partNode.ClearData();
      KIS_Shared.PartSnapshot(equippedPart).CopyTo(partNode);
      equippedPart.Die();
    }
    evaTransform = null;
    equippedPart = null;
    equippedGameObj = null;
    if (actorType == ActorType.Player) {
      UISoundPlayer.instance.Play(prefabModule.moveSndPath);
    }
    prefabModule.OnUnEquip(this);

    // Return back the stock meshes if the custom helmet is unequipped.
    if (equipSlot == HelmetSlotName) {
      var kerbalModule = inventory.part.FindModuleImplementing<KerbalEVA>();
      if (kerbalModule.helmetTransform != null) {
        for (var i = 0; i < kerbalModule.helmetTransform.childCount; i++) {
          kerbalModule.helmetTransform.GetChild(i).gameObject.SetActive(true);
        }
      } else {
        DebugEx.Warning("Kerbal model doesn't have helmet transform: {0}", inventory.part);
      }
    }
  }

  public void OnEquippedPartReady(Part createdPart) {
    if (equipMode == EquipMode.Part) {
      // Disable colliders since kerbal rotation doesn't respect physics. Equipped part collider
      // may give an insane momentum to the collided objects.  
      foreach (var col in equippedPart.gameObject.GetComponentsInChildren<Collider>()) {
        col.isTrigger = true;
      }
    }
  }

  public void Drop(Part fromPart = null) {
    DebugEx.Info("Drop item");
    if (fromPart == null) {
      fromPart = inventory.part;
    }
    Quaternion rot;
    Vector3 pos;
    if (prefabModule) {
      rot = evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
      pos = evaTransform.TransformPoint(prefabModule.equipPos);
    } else {
      rot = inventory.part.transform.rotation;
      pos = inventory.part.transform.position + new Vector3(0, 1, 0);
    }
    KIS_Shared.CreatePart(partNode, pos, rot, fromPart);
    StackRemove(1);
  }

  public void ReEquip() {
    if (equipped) {
      Unequip();
      Equip();
    }
  }

  public void OnMove(ModuleKISInventory srcInventory, ModuleKISInventory destInventory) {
    if (srcInventory != destInventory && equipped) {
      Unequip();
    }
    if (prefabModule) {
      UISoundPlayer.instance.Play(prefabModule.moveSndPath);
    } else {
      UISoundPlayer.instance.Play(inventory.defaultMoveSndPath);
    }
  }

  public void DragToPart(Part destPart) {
    if (prefabModule) {
      prefabModule.OnDragToPart(this, destPart);
    }
  }

  public void DragToInventory(ModuleKISInventory destInventory, int destSlot) {
    if (prefabModule) {
      prefabModule.OnDragToInventory(this, destInventory, destSlot);
    }
  }
}

}  // namespace
