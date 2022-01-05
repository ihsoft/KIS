// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KISAPIv1;
using KSPDev.GUIUtils;
using KSPDev.GUIUtils.TypeFormatters;
using KSPDev.ResourceUtils;
using System;
using System.Linq;
using System.Reflection;
using KSPDev.LogUtils;

namespace KIS {

// Next localization ID: kisLOC_08009.
// ReSharper disable once InconsistentNaming
public sealed class ModuleKISItemEvaPropellant : ModuleKISItem {
  #region Localizable GUI strings
  static readonly Message NoNeedToRefillJetpackMsg = new Message(
      "#kisLOC_08000",
      defaultTemplate: "The jetpack is full. No need to refill",
      description: "The message to present when the jetpack is attempted to be refilled, but its'"
      + " already full.");

  static readonly Message CanisterIsEmptyMsg = new Message(
      "#kisLOC_08001",
      defaultTemplate: "The canister is empty! Cannot refuel the jetpack",
      description: "The message to present when the EVA kerbals has attempted to refill the"
      + " jetpack, but the tank is empty.");

  static readonly Message CanisterFullyRefilledMsg = new Message(
      "#kisLOC_08002",
      defaultTemplate: "Fuel canister refilled",
      description: "The message to present when a non-full tank has successfully refilled to the"
      + " top.");

  static readonly Message JetpackFullyRefueledMsg = new Message(
      "#kisLOC_08004",
      defaultTemplate: "Jetpack fully refueled",
      description: "The message to present when the EVA kerbals has attempted to refill the"
      + " jetpack, and the jetpack has successfully refilled to full.");

  static readonly Message<SmallNumberType> JetpackPartiallyRefueledMsg =
      new Message<SmallNumberType>(
          "#kisLOC_08005",
          defaultTemplate: "Added <<1>> units of fuel to the jetpack",
          description: "The message to present when the EVA kerbals has attempted to refill the"
          + " jetpack, but there was not enough fuel in the canister to fill the jetpack up to the"
          + " top."
          + "\nArgument <<1>> is the amount of the resource added to the jetpack."
          + " Format: SmallNumberType.");

  static readonly Message NoNeedToRefillCanisterMsg = new Message(
      "#kisLOC_08006",
      defaultTemplate: "The canister is full. No need to refill",
      description: "The message to present when the canister is attempted to be refilled, but its'"
      + " already full.");

  static readonly Message<ResourceType, SmallNumberType> CanisterPartialRefilledMsg =
      new Message<ResourceType, SmallNumberType>(
          "#kisLOC_08007",
          defaultTemplate: "Consumed <<2>> units of <<1>> from the vessel",
          description: "The message to present when the canister have been refilled from the"
          + " vessel's reserve with a consumable fuel type."
          + "\nArgument <<1>> is the name of the resource. Format: ResourceType."
          + "\nArgument <<2>> is the amount of the resource taken from the vessel."
          + " Format: SmallNumberType.");

  static readonly Message<ResourceType> NoResourceInVesselMsg = new Message<ResourceType>(
      "#kisLOC_08008",
      defaultTemplate: "Vessel doesn't have a spare reserve of <<1>>",
      description: "The message to present when the canister have been attempted to refill from"
      + " the vessel's reserve with a consumable fuel type, and there were no resource."
      + "\nArgument <<1>> is the name of the resource. Format: ResourceType.");
  #endregion

  #region Part's config fields
  /// <summary>Sound to play when refuel operation succeeded.</summary>
  [KSPField]
  public string refuelSndPath = "KIS/Sounds/refuelEva";
  #endregion

  #region Local fields and properties
  /// <summary>Name of the resource that the canister holds.</summary>
  /// <remarks>
  /// Regardless to this name, in the jetpack the canister always refills the "EVA propellant" fuel.
  /// </remarks>
  string _mainResourceName;
  #endregion
  
  #region ModuleKISItem overrides
  /// <inheritdoc/>
  public override void OnLoad(ConfigNode node) {
    base.OnLoad(node);
    if (part.Resources.Count == 0) {
      HostedDebugLog.Error(this, "No resources on the canister! This won't work.");
      return;
    }
    if (part.Resources.Count > 1) {
      HostedDebugLog.Error(this, "Too many resources on the canister! The first one will be used.");
    }
    _mainResourceName = part.Resources[0].resourceName;
    if (_mainResourceName != StockResourceNames.EvaPropellant) {
      HostedDebugLog.Info(this, "Using a customized resource type: {0}", _mainResourceName);
    }
  }

  /// <inheritdoc/>
  public override void OnItemUse(KIS_Item item, KIS_Item.UseFrom useFrom) {
    if (useFrom != KIS_Item.UseFrom.KeyUp) {
      if (item.inventory.invType == ModuleKISInventory.InventoryType.Pod) {
        RefillCanister(item);  // Refuel canister item.
      } else  if (item.inventory.invType == ModuleKISInventory.InventoryType.Eva) {
        RefillEvaPack(item);  // Refuel EVA pack from canister.
      }
    }
  }
  #endregion

  #region Inheritable & customization methods
  /// <summary>Fills up canister to the maximum capacity.</summary>
  /// <param name="item">Item to refill.</param>
  void RefillCanister(KIS_Item item) {
    var canisterResource = GetCanisterFuelResource(item);
    var needResource = canisterResource.maxAmount - canisterResource.amount;
    if (needResource <= double.Epsilon) {
      ScreenMessaging.ShowPriorityScreenMessage(NoNeedToRefillCanisterMsg);
      return;
    }
    double newAmount;
    if (_mainResourceName != StockResourceNames.EvaPropellant) {
      var hasAmount = item.inventory.part.RequestResource(_mainResourceName, needResource);
      if (hasAmount <= double.Epsilon) {
        ScreenMessaging.ShowPriorityScreenMessage(
            NoResourceInVesselMsg.Format(canisterResource.resourceName));
        UISounds.PlayBipWrong();
        return;
      }
      newAmount = canisterResource.amount + hasAmount;
      ScreenMessaging.ShowPriorityScreenMessage(
          CanisterPartialRefilledMsg.Format(canisterResource.resourceName, hasAmount));
    } else {
      newAmount = canisterResource.maxAmount;
      ScreenMessaging.ShowPriorityScreenMessage(CanisterFullyRefilledMsg);
    }
    item.UpdateResource(_mainResourceName, newAmount);
    UISoundPlayer.instance.Play(refuelSndPath);
  }

  /// <summary>
  /// Refuels kerbal's EVA pack up to the maximum, and decreases canister reserve.
  /// </summary>
  /// <param name="item">Item to get fuel from.</param>
  void RefillEvaPack(KIS_Item item) {
    var p = item.inventory.part;
    if (!p.isVesselEVA) {
      HostedDebugLog.Error(this, "Cannot refill non-EVA kerbal");
      return;
    }
    var stockInventory = p.FindModuleImplementing<ModuleInventoryPart>();
    if (stockInventory == null) {
      HostedDebugLog.Error(this, "Cannot find stock inventory module");
      return;
    }
    var evaModule = p.FindModuleImplementing<KerbalEVA>();
    var propellantResourceField = evaModule.GetType()
        .GetField("propellantResource", BindingFlags.Instance | BindingFlags.NonPublic);
    if (propellantResourceField == null) {
      HostedDebugLog.Error(this, "Cannot access internal KerbalEVA logic: propellant field");
      return;
    }
    var propellantResource = propellantResourceField.GetValue(evaModule) as PartResource;
    if (propellantResource == null) {
      HostedDebugLog.Error(this, "Cannot access internal KerbalEVA logic: propellant field value");
      return;
    }

    var needAmount = propellantResource.maxAmount - propellantResource.amount;
    if (needAmount <= double.Epsilon) {
      ScreenMessaging.ShowPriorityScreenMessage(NoNeedToRefillJetpackMsg);
      return;
    }
    var canisterFuelResource = GetCanisterFuelResource(item);
    if (canisterFuelResource.amount < double.Epsilon) {
      ScreenMessaging.ShowPriorityScreenMessage(CanisterIsEmptyMsg);
      UISounds.PlayBipWrong();
      return;
    }
    var canProvide = Math.Min(needAmount, canisterFuelResource.amount);

    var storedResources = stockInventory.storedParts.Values.SelectMany(x => x.snapshot.resources)
        .Where(x => x.resourceName == StockResourceNames.EvaPropellant)
        .ToArray();
    if (storedResources.Length == 0) {
      UISounds.PlayBipWrong();
      DebugEx.Error("Unexpectedly no EVA resource parts found in: {0}", evaModule);
      return;
    }
    item.UpdateResource(_mainResourceName, -canProvide, isAmountRelative: true);
    p.TransferResource(propellantResource, canProvide, p);
    var distributeAmount = canProvide;
    for (var i = 0; i < storedResources.Length && canProvide > double.Epsilon; i++) {
      var resource = storedResources[i];
      var canAccept = resource.maxAmount - resource.amount;
      if (canAccept <= double.Epsilon) {
        continue;
      }
      var refillAmount = Math.Min(canAccept, distributeAmount);
      resource.amount += refillAmount;
      resource.UpdateConfigNodeAmounts();
      distributeAmount -= refillAmount;
    }
    if (canProvide < needAmount) {
      ScreenMessaging.ShowPriorityScreenMessage(JetpackPartiallyRefueledMsg.Format(canProvide));
    } else {
      ScreenMessaging.ShowPriorityScreenMessage(JetpackFullyRefueledMsg);
    }
    UISoundPlayer.instance.Play(refuelSndPath);
  }

  /// <summary>Returns KIS resource description for the propellant in the part.</summary>
  /// <param name="item">Item to get resource for.</param>
  /// <returns>Resource description.</returns>
  ProtoPartResourceSnapshot GetCanisterFuelResource(KIS_Item item) {
    var resources = KISAPI.PartNodeUtils.GetResources(item.partNode);
    if (resources.Length == 0) {
      throw new Exception("Bad save state: no resource on the part");
    }
    var itemResource = resources[0];  // Always use the first one.
    if (itemResource.resourceName == _mainResourceName) {
      return itemResource;
    }
    // A mod that changes the default resource has been installed or removed. Update the part state.
    DebugEx.Warning(
        "Fixing saved state of the resource: oldName={0}, newName={1}",
        itemResource.resourceName, _mainResourceName);
    KISAPI.PartNodeUtils.DeleteResource(item.partNode, itemResource.resourceName);
    itemResource = new ProtoPartResourceSnapshot(part.Resources[0]) {
        amount = itemResource.amount
    };
    KISAPI.PartNodeUtils.AddResource(item.partNode, itemResource);
    return itemResource;
  }
  #endregion
}

}  // namespace
