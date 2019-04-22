// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KISAPIv1;
using KIS.GUIUtils;
using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using KSPDev.PartUtils;
using KSPDev.ProcessingUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
  public KIS_IconViewer icon;
  public bool stackable;
  public string equipSlot;
  public bool usableFromEva;
  public bool usableFromContainer;
  public bool usableFromPod;
  public bool usableFromEditor;
  public bool carriable;
  public bool equipable;

  #region Persisted properties
  /// <summary>The number of items in the inventory slot.</summary>
  /// <value>The number of items.</value>
  public int quantity {
    get { return _quantity; }
    private set { _quantity = value; }
  }
  [PersistentField("quantity", group = StdPersistentGroups.PartPersistant)]
  int _quantity;

  /// <summary>Tells if the item is equipped on the kerbal.</summary>
  /// <value><c>true</c> if equipped.</value>
  public bool equipped {
    get { return _equipped; }
    private set { _equipped = value; }
  }
  [PersistentField("equipped", group = StdPersistentGroups.PartPersistant)]
  bool _equipped;

  /// <summary>
  /// The item's part mass without the resources and content (if it's a KIS container).
  /// </summary>
  /// <value>The mass in tons.</value>
  public float itemDryMass { get { return _itemDryMass; } }
  [PersistentField("dryMass", group = StdPersistentGroups.PartPersistant)]
  float _itemDryMass;

  /// <summary>
  /// The item's part cost without the resources and content (if it's a KIS container).
  /// </summary>
  /// <value>The cost in the game currency units.</value>
  public float itemDryCost { get { return _itemDryCost; } }
  [PersistentField("dryCost", group = StdPersistentGroups.PartPersistant)]
  float _itemDryCost;

  /// <summary>The item's resource mass, if any.</summary>
  /// <value>The mass in tons.</value>
  public float itemResourceMass { get { return _resourceMass; } }
  [PersistentField("resourceMass", group = StdPersistentGroups.PartPersistant)]
  float _resourceMass;

  /// <summary>The item's resource cost, if any.</summary>
  /// <value>The cost in the game currency units.</value>
  public float itemResourceCost { get { return _resourceCost; } }
  [PersistentField("resourceCost", group = StdPersistentGroups.PartPersistant)]
  float _resourceCost;

  /// <summary>The item's content mass, if it's a KIS inventory.</summary>
  /// <value>The mass in tons.</value>
  public float itemContentMass { get { return _contentMass; } }
  [PersistentField("contentMass", group = StdPersistentGroups.PartPersistant)]
  float _contentMass;

  /// <summary>The item's content cost, if any.</summary>
  /// <value>The cost in the game currency units.</value>
  public float itemContentCost { get { return _contentCost; } }
  [PersistentField("contentCost", group = StdPersistentGroups.PartPersistant)]
  float _contentCost;
  #endregion

  #region API properties
  /// <summary>Total voulme of the item's meshes.</summary>
  /// <value>The volume in liters.</value>
  public float itemVolume { get; private set; }

  /// <summary>Total cost of the part and its content and resources.</summary>
  /// <value>The cost in the game currency untis.</value>
  public float fullItemCost { get { return itemDryCost + itemResourceCost + itemContentCost; } }

  /// <summary>Total mass of the part and its content and resources.</summary>
  /// <value>The mass in tons.</value>
  public float fullItemMass { get { return itemDryMass + itemResourceMass + itemContentMass; } }
  #endregion

  #region Slot properties
  public int slot { get { return inventory.items.FirstOrDefault(x => x.Value == this).Key; } }
  public float stackVolume { get { return itemVolume * quantity; } }
  public float totalSlotCost { get { return fullItemCost * quantity; } }
  public float totalSlotMass { get { return fullItemMass * quantity; } }
  #endregion

  /// <summary>Inventory that owns this item.</summary>
  public ModuleKISInventory inventory;

  /// <summary>Part module that implements this item.</summary>
  public ModuleKISItem prefabModule { get; private set; }

  GameObject equippedGameObj;
  public Part equippedPart { get; private set; }
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

  /// <summary>Creates an item, restored form the save file.</summary>
  /// <param name="itemNode">The item config node to load teh data from.</param>
  /// <param name="inventory">The owner inventory of the item.</param>
  /// <returns>The item instance.</returns>
  public static KIS_Item RestoreItemFromNode(ConfigNode itemNode, ModuleKISInventory inventory) {
    var qty = ConfigAccessor.GetValueByPath<int>(itemNode, "quantity") ?? 0;
    var partName = itemNode.GetValue("partName");
    AvailablePart avPart = null;
    if (partName != null) {
      avPart = PartLoader.getPartInfoByName(partName);
    }
    if (qty == 0 || partName == null || avPart == null) {
      DebugEx.Error("Bad item config:\n{0}", itemNode);
      throw new ArgumentException("Bad item config node", "itemNode");
    }
    return new KIS_Item(avPart, itemNode, inventory, qty);
  }

  /// <summary>Creates an item from a part in the scene.</summary>
  /// <param name="part">The part to clone. It must be fully initialized.</param>
  /// <param name="inventory">The owner inventory of the item.</param>
  /// <param name="quantity">The number of items in the slot.</param>
  /// <returns>The item instance.</returns>
  public static KIS_Item CreateItemFromScenePart(Part part, ModuleKISInventory inventory,
                                                 int quantity = 1) {
    return new KIS_Item(part, inventory, quantity);
  }

  /// <summary>Creates a new item, given a saved state.</summary>
  /// <remarks>
  /// It's intentionally private. The items must be restored thru the factory methods.
  /// </remarks>
  /// <seealso cref="RestoreItemFromNode"/>
  KIS_Item(AvailablePart availablePart, ConfigNode itemNode,
           ModuleKISInventory inventory, int quantity) {
    this.availablePart = availablePart;
    this.inventory = inventory;
    this.quantity = quantity;
    SetPrefabModule();
    this.stackable = CheckItemStackable(availablePart);
    this.partNode = new ConfigNode();
    itemNode.GetNode("PART").CopyTo(partNode);
    ConfigAccessor.ReadFieldsFromNode(
        itemNode, GetType(), this, group: StdPersistentGroups.PartPersistant);
    this.itemVolume = KISAPI.PartUtils.GetPartVolume(availablePart, partNode: partNode);

    // COMPATIBILITY: Set/restore the dry cost and mass. 
    // TODO(ihsoft): This code is only needed for the pre-1.17 KIS version saves. Drop it one day.  
    if (this.itemDryMass < float.Epsilon || this.itemDryCost < float.Epsilon) {
      this._itemDryMass = KISAPI.PartUtils.GetPartDryMass(availablePart, partNode: partNode);
      this._itemDryCost = KISAPI.PartUtils.GetPartDryCost(availablePart, partNode: partNode);
      DebugEx.Warning("Calculated values for a pre 1.17 version save: dryMass={0}, dryCost={1}",
                      this.itemDryMass, this.itemDryCost);
    }

    // COMPATIBILITY: Set/restore the resources cost and mass.
    // TODO(ihsoft): This code is only needed for the pre-1.17 KIS version saves. Drop it one day.  
    var resourceNodes = PartNodeUtils.GetModuleNodes(partNode, "RESOURCE");
    if (resourceNodes.Any()
        && (this.itemResourceMass < float.Epsilon || this.itemResourceCost < float.Epsilon)) {
      var oldResourceMass = this.itemResourceMass;
      foreach (var resourceNode in resourceNodes) {
        var resource = new ProtoPartResourceSnapshot(resourceNode);
        this._resourceMass += (float)resource.amount * resource.definition.density;
        this._resourceCost += (float)resource.amount * resource.definition.unitCost;
      }
      DebugEx.Warning("Calculated values for a pre 1.17 version save:"
                      + " oldResourceMass={0}, newResourceMass={1}, resourceCost={2}",
                      oldResourceMass, this.itemResourceMass, this.itemResourceCost);
    }
  }

  /// <summary>Creates a new part from scene.</summary>
  /// <remarks>
  /// It's intentionally private. The items must be created thru the factory methods.
  /// </remarks>
  /// <seealso cref="CreateItemFromScenePart"/>
  KIS_Item(Part part, ModuleKISInventory inventory, int quantity) {
    this.availablePart = part.partInfo;
    this.inventory = inventory;
    this.quantity = quantity;
    SetPrefabModule();
    this.stackable = CheckItemStackable(availablePart);
    this.partNode = KISAPI.PartNodeUtils.PartSnapshot(part);

    this.itemVolume = KISAPI.PartUtils.GetPartVolume(part.partInfo, partNode: partNode);
    // Don't trust the part's mass. It can be affected by too many factors.
    this._itemDryMass = 
        part.partInfo.partPrefab.mass + part.GetModuleMass(part.partInfo.partPrefab.mass);
    this._itemDryCost = part.partInfo.cost + part.GetModuleCosts(part.partInfo.cost);
    foreach (var resource in part.Resources) {
      this._resourceMass += (float)resource.amount * resource.info.density;
      this._resourceCost += (float)resource.amount * resource.info.unitCost;
    }

    // Handle the case when the item is a container.
    var itemInventories = part.Modules.OfType<ModuleKISInventory>();
    foreach (var itemInventory in itemInventories) {
      this._contentMass += itemInventory.contentsMass;
      this._contentCost += itemInventory.contentsCost;
    }
    // Fix dry mass/cost since it's reported by the container modules.
    this._itemDryMass -= this._contentMass;
    this._itemDryCost -= this._contentCost;
  }

  /// <summary>Sets the item's equipped state.</summary>
  /// <remarks>
  /// Changing of the state does <i>not</i> equip or unequip the item. It only changes teh state.
  /// </remarks>
  /// <param name="state">The state.</param>
  /// <seealso cref="equipped"/>
  public void SetEquipedState(bool state) {
    equipped = state;
  }

  /// <summary>Tells if the part can be stacked in the inventory.</summary>
  /// <param name="avPart">The part proto to check.</param>
  /// <returns><c>true</c> if it can stack.</returns>
  public static bool CheckItemStackable(AvailablePart avPart) {
    var module = avPart.partPrefab.GetComponent<ModuleKISItem>();
    var allModulesCompatible = avPart.partPrefab.Modules.Cast<PartModule>()
        .All(m => KISAddonConfig.stackableModules.Contains(m.moduleName));
    var hasNoResources = KISAPI.PartNodeUtils.GetResources(avPart.partConfig).Length == 0;
    return module != null && module.stackable
        || KISAddonConfig.stackableList.Contains(avPart.name.Replace('.', '_'))
        || allModulesCompatible && hasNoResources;
  }

  public void OnSave(ConfigNode node) {
    node.AddValue("partName", availablePart.name);
    node.AddValue("slot", slot);
    ConfigAccessor.WriteFieldsIntoNode(
        node, GetType(), this, group: StdPersistentGroups.PartPersistant);
    // Items in pod and container may have equipped status True but they are not actually equipped,
    // so there is no equipped part.
    if (equipped && equippedPart != null
        && (equipMode == EquipMode.Part || equipMode == EquipMode.Physic)) {
      HostedDebugLog.Info(
          inventory, "Update config node of equipped part: {0}", availablePart.name);
      partNode = KISAPI.PartNodeUtils.PartSnapshot(equippedPart);
    }
    partNode.CopyTo(node.AddNode("PART"));
  }

  /// <summary>Updates the item's resource.</summary>
  /// <param name="name">The new of the resource.</param>
  /// <param name="amount">The new amount or delta.</param>
  /// <param name="isAmountRelative">
  /// Tells if the amount must be added to the current item's amount instead of simply replacing it.
  /// </param>
  /// <returns>
  /// The new resource amount or <c>null</c> of resource not found. Note, that the resource amount
  /// can be less than zero.
  /// </returns>
  public double? UpdateResource(string name, double amount, bool isAmountRelative = false) {
    var res = KISAPI.PartNodeUtils.UpdateResource(partNode, name, amount,
                                                  isAmountRelative: isAmountRelative);
    if (res.HasValue) {
      HostedDebugLog.Fine(
          inventory, "Updated item resource: name={0}, newAmount={1}", name, res);
      inventory.RefreshContents();
    } else {
      HostedDebugLog.Error(
          inventory, "Cannot find resource {0} in item for {1}", name, availablePart.name);
    }
    return res;
  }

  public void EnableIcon(int resolution) {
    DisableIcon();
    icon = new KIS_IconViewer(
        availablePart, resolution,
        VariantsUtils.GetCurrentPartVariant(availablePart, partNode));
  }

  public void DisableIcon() {
    if (icon != null) {
      icon.Dispose();
      icon = null;
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
    float newVolume = inventory.totalContentsVolume + (itemVolume * qty);
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
      inventory.RefreshContents();
      return true;
    }
    return false;
  }

  /// <summary>Removes items form the stack.</summary>
  /// <remarks>
  /// This method does its best. If it cannot remove the desired number of items, then it removes as
  /// many is there are avaiable in the slot.
  /// </remarks>
  /// <param name="qty">The number of items to remove.</param>
  /// <returns>The actual number of items removed.</returns>
  public int StackRemove(int qty) {
    if (qty <= 0) {
      return 0;
    }
    int removeQty;
    if (quantity - qty < 0) {
      removeQty = quantity;
      HostedDebugLog.Fine(inventory, "Exhausted item quantity: name={0}, removeExactly={1}",
                          availablePart.name, removeQty);
    } else {
      removeQty = qty;
    }
    quantity -= removeQty;
    inventory.RefreshContents();
    if (quantity == 0) {
      Delete();
    }
    return removeQty;
  }

  public void Delete() {
    if (inventory.showGui) {
      DisableIcon();
    }
    if (equipped) {
      Unequip();
    }
    inventory.items.Remove(slot);
    inventory.RefreshContents();
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
    inventory.StartCoroutine(AlignEquippedPart());
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
      partNode = KISAPI.PartNodeUtils.PartSnapshot(equippedPart);
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

  #region Local utility methods
  /// <summary>Coroutine to align the equipped atems to the kerbal model.</summary>
  IEnumerator AlignEquippedPart() {
    while (equippedGameObj != null && evaTransform != null) {
      equippedGameObj.transform.rotation =
          evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
      equippedGameObj.transform.position = evaTransform.TransformPoint(prefabModule.equipPos);
      yield return new WaitForEndOfFrame();
    }
  }

  /// <summary>Extracts the KIS module item from the part.</summary>
  void SetPrefabModule() {
    prefabModule = availablePart.partPrefab.GetComponent<ModuleKISItem>();
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
  }
  #endregion
}

}  // namespace
