// Kerbal Inventory System
// Mod's author: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module authors: KospY, igor.zavoychinskiy@gmail.com
// License: Restricted

using KIS.GUIUtils;
using KSPDev.ConfigUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using KSPDev.KSPInterfaces;
using KSPDev.PartUtils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS {

// Next localization ID: #kisLOC_00061.
[PersistentFieldsDatabase("KIS/settings/KISConfig")]
public class ModuleKISInventory : PartModule,
    // KSP interfaces.
    IPartCostModifier, IPartMassModifier, IModuleInfo,
    // KSPDev interfaces.
    IHasContextMenu,
    // KSPDev syntax sugar interfaces.
    IPartModule, IsDestroyable, IKSPDevModuleInfo {

  #region Localizable GUI strings.
  static readonly Message NoItemEquippedMsg = new Message(
      "#kisLOC_00000",
      defaultTemplate: "Cannot use equipped item because nothing is equipped",
      description: "The message to present when 'use' key is pressed, but no item is equipped in"
      + " the right hand of the EVA kerbal.");

  static readonly Message<string> CannotTransferInventoryMsg = new Message<string>(
      "#kisLOC_00001",
      defaultTemplate: "Pod <<1>> doesn't have personal inventory space",
      description: "The message to present when EVA kerbal enters a pod which doesn't have KIS"
      + " inventory.");

  static readonly Message<int> PartHasChildrenMsg = new Message<int>(
      "#kisLOC_00002",
      defaultTemplate: "Cannot put an assembly into inventory: <<1>> part(s) attached",
      description: "The message to present when EVA kerbal tries to put into inventory an assembly"
      + " of multiple parts."
      + "\nArgument <<1>> is the number of the children parts atatched to the part being dragged.");

  static readonly Message<VolumeLType, VolumeLType> MaxVolumeReachedMsg =
      new Message<VolumeLType, VolumeLType>(
          "#kisLOC_00003",
          defaultTemplate: "Max destination volume reached: <<1>> (+<<2>>)",
          description: "The message to present when an item being dragged into an inventory which"
          + "doesn't have enough free space."
          + "\nArgument <<1>> is a value of type VolumeLType which specifies the volume which is"
          + " attempted to be moved."
          + "\nArgument <<2>> is a value of type VolumeLType which specifies the exceeding volume"
          +" over the max inventory capacity.");

  static readonly Message NotAccessibleWhileCarriedMsg = new Message(
      "#kisLOC_00004",
      defaultTemplate: "This storage is not accessible while carried!",
      description: "The message to present when a storage, which is being carried on a back of an"
      + " EVA kerbal, is attempted to be accessed.");

  static readonly Message NotAccessibleFromOutsideMsg = new Message(
      "#kisLOC_00005",
      defaultTemplate: "This storage is not accessible from the outside!",
      description: "The message to present when an inventory which cannot be accessed from EVA is"
      + " attempted to be opened by an EVA kerbal.");

  static readonly Message NotAccessibleFromInsideMsg =new Message(
      "#kisLOC_00006",
      defaultTemplate: "This storage is not accessible from the inside!",
      description: "The message to present when an inventory which cannot be accessed from inside"
      + " the vessel is attempted to be accessed while the active vessel is no an EVA kerbal.");

  static readonly Message CannotRemoveHelmetNoOxygenMsg = new Message(
      "#kisLOC_00007",
      defaultTemplate: "Cannot remove helmet: atmosphere does not contain oxygen!",
      description: "The message to present when a remove helmet action is attempted in the"
      + " atmosphere which doesn't contain oxygen.");

  static readonly Message<PressureType, PressureType>
      CannotRemoveHelmetPressureTooLowMsg = new Message<PressureType, PressureType>(
          "#kisLOC_00008",
          defaultTemplate: "Cannot remove helmet: pressure too low (<<2>> < <<1>>)",
          description: "The message to present when a remove helmet action is attempted in the"
          + " atmosphere which is not dense enough."
          + "\nArgument <<1>> is a value of type PressureType which specifies a minimum allowed"
          + " pressure."
          + "\nArgument <<2>> is a value of type PressureType which specifies the actual pressure"
          + " outside.");

  static readonly Message InventoryFullCannotSplitMsg = new Message(
      "#kisLOC_00009",
      defaultTemplate: "Inventory is full, cannot split!",
      description: "The message to present when a split action is attempted on the the inventory,"
      + " but there are no empty slots available to fit the new pack.");

  static readonly Message CarriableItemsNotForSeatsInventoryMsg = new Message(
      "#kisLOC_00010",
      defaultTemplate: "Carriable items cannot be stored in the seat's inventory",
      description: "The message to present when an item, designed to be carried by an EVA kerbal,"
      + " is attempted to be put into a pod's seat inventory.");

  static readonly Message PartAlreadyCarriedMsg = new Message(
      "#kisLOC_00011",
      defaultTemplate: "Another part is already carried",
      description: "The message to present when an item is attempted to be placed on an EVA kerbal,"
      + " but there is another item already being carried.");

  static readonly Message MustBeCrewedAtLaunchMsg = new Message(
      "#kisLOC_00012",
      defaultTemplate: "The seat must be crewed at launch to acquire items",
      description: "The text to show in an inventory window in the editor to highlight the fact"
      + " that the items added there will only be availabe in the flight if the seat is occupied"
      + " at the launch.");

  static readonly Message RemoveHelmetMenuTxt = new Message(
      "#kisLOC_00013",
      defaultTemplate: "Remove Helmet",
      description: "The name of the context menu item that removes kerbal's helmet if the"
      + " environment conditions allow it.");

  static readonly Message PutOnHelmetMenuTxt =new Message(
      "#kisLOC_00014",
      defaultTemplate: "Put On Helmet",
      description: "The name of the context menu item that pust the kerbal's helmet back.");

  static readonly Message<string, int> PodInventoryWindowTitle =
      new Message<string, int>(
          "#kisLOC_00015",
          defaultTemplate: "<<1>> | Seat <<2>>",
          description: "The title of the window that represents an open pod's inventory in the"
          + " editor."
          + "\nArgument <<1>> is a name of the part that holds the inventory."
          + "\nArgument <<2>> is a number of the seat to which the inventory belongs.");

  static readonly Message<string, string> PersonalInventoryWindowTitle =
      new Message<string, string>(
          "#kisLOC_00016",
          defaultTemplate: "<<1>> | <<2>>",
          description: "The title of the window that represents an open kerbal's inventory."
          + "\nArgument <<1>> is a name of the part that holds the inventory."
          + "\nArgument <<2>> is a name of the kerbal.");

  static readonly Message<string, string> ContainerInventoryWindowTitle =
      new Message<string, string>(
          "#kisLOC_00017",
          defaultTemplate: "<<1>> | <<2>>",
          description: "The title of the window that represents an open parts's inventory in the"
          + " flight. This title is only used when the inventory has a custom name."
          + "\nArgument <<1>> is a name of the part that holds the inventory."
          + "\nArgument <<2>> is a custom name of the inventory.");

  static readonly Message<string, string> TooltipWindowTitle =
      new Message<string, string>(
          "#kisLOC_00018",
          defaultTemplate: "<<1>> | <<2>>",
          description: "The title of the window that shows a tooltip for the item being hovered"
          + " over in the open inventory."
          + "\nArgument <<1>> is a name of the part item."
          + "\nArgument <<2>> is a custom name of the inventory.");

  static readonly Message ItemActionMenuWindowTitle = new Message(
      "#kisLOC_00019",
      defaultTemplate: "Action",
      description: "The title of the window that represents a context menu for a specific item in"
      + " the inventory.");

  static readonly Message AcceptNameChangeBtn = new Message(
      "#kisLOC_00020",
      defaultTemplate: "OK",
      description: "The caption of the button that accepts the changed inventory name. This button"
      + " is vary narrow, so keep the text as short as possible.");

  static readonly Message SetInventoryNameBtn = new Message(
      "#kisLOC_00021",
      defaultTemplate: "Set name",
      description: "The caption of the button that shows an input field to enter a custom name for"
      + " an inventory.");

  static readonly Message CloseInventoryBtn = new Message(
      "#kisLOC_00022",
      defaultTemplate: "Close",
      description: "The caption of the button that closes the opened inventory dialog.");

  static readonly Message UnequipItemContextMenuBtn = new Message(
      "#kisLOC_00023",
      defaultTemplate: "Unequip",
      description: "The caption of the button that triggers the uneqip action on the item in the"
      + " inventory. The button is shown in a context menu of the selected item.");

  static readonly Message EquipItemContextBtn = new Message(
      "#kisLOC_00024",
      defaultTemplate: "Equip",
      description: "The caption of the button that triggers the eqip action on the item in the"
      + " inventory. The button is shown in a context menu of the selected item.");

  static readonly Message DropCarriedItemContextBtn = new Message(
      "#kisLOC_00025",
      defaultTemplate: "Drop",
      description: "The caption of the button that triggers the drop action on the item in the"
      + " inventory. The button is shown in a context menu of the selected item.");

  static readonly Message<int> SplitItemsContextBtn = new Message<int>(
      "#kisLOC_00026",
      defaultTemplate: "Split (<<1>>)",
      description: "The caption of the button that extracts the specified number of items from the"
      + " selected inventory slot, and moves them into a new slot."
      + "\nArgument <<1>> is the number of items to extract.");

  static readonly Message<int> ItemsQuantityItemContextMsg = new Message<int>(
      "#kisLOC_00027",
      defaultTemplate: "Quantity: <<1>>",
      description: "The text to show in the context menu of the selected inventory item that"
      + " tells how many items are in the slot."
      + "\nArgument <<1>> is the number of items in the slot.");

  static readonly Message NoActionItemContextMsg = new Message(
      "#kisLOC_00028",
      defaultTemplate: "No action",
      description: "The text to show in the context menu of the selected inventory item that"
      + " tells that no actions can be done on the item(s) in the slot.");

  static readonly Message<int> PodSeatInventoryMenuTxt = new Message<int>(
      "#kisLOC_00029",
      defaultTemplate: "Seat <<1>> inventory",
      description: "The name of the part's menu item that opens the inventory for a pod's seat."
      + "\nArgument <<1>> is the number of seat.");

  static readonly Message<string> PersonalInventoryMenuTxt = new Message<string>(
      "#kisLOC_00030",
      defaultTemplate: "<<1>>`s inventory",
      description: "The name of the part's menu item that opens the inventory of a specific kerbal."
      + "\nArgument <<1>> is the first name of the kerbal.");

  static readonly Message PartInventoryMenuTxt = new Message(
      "#kisLOC_00031",
      defaultTemplate: "Inventory",
      description: "The name of the part's menu item that opens the associated inventory. The"
      + " \"part\" can be a kerbal.");

  static readonly Message<string> PartInventoryWithNameMenuTxt = new Message<string>(
      "#kisLOC_00032",
      defaultTemplate: "Inventory | <<1>>",
      description: "The name of the part's menu item that opens the associated inventory with a"
      + " custom name. The \"part\" can be a kerbal."
      + "\nArgument <<1>> is a custom name of the inventory.");

  static readonly Message CarriedItemContextCaption = new Message(
      "#kisLOC_00033",
      defaultTemplate: "Carried",
      description: "The text to display in the inventory slot background to tell if the item is"
      + " being carried by the kerbal.");

  static readonly Message EquippedItemContextCaption = new Message(
      "#kisLOC_00034",
      defaultTemplate: "Equip.",
      description: "The text to display in the inventory slot background to tell if the item is"
      + " being equipped by the kerbal.");

  static readonly Message<int> SlotIdContextCaption = new Message<int>(
      "#kisLOC_00035",
      defaultTemplate: "<<1>>",
      description: "The text to display in the inventory slot background to identify it."
      + "\nArgument <<1>> is the number of the slot.");

  static readonly Message<int> MultipleItemsContextCaption = new Message<int>(
      "#kisLOC_00036",
      defaultTemplate: "x<<1>>",
      description: "The text to display in the inventory slot background to tell ho many items are"
      + " stacked."
      + "\nArgument <<1>> is the number of the items in the slot.");

  static readonly Message<VolumeLType, VolumeLType> InventoryVolumeInfo =
      new Message<VolumeLType, VolumeLType>(
          "#kisLOC_00037",
          defaultTemplate: "Volume: <<1>> / <<2>>",
          description: "The volume stat of the iventory in the main inventory window."
          + "\nArgument <<1>> is the occupied volume of the inventory of type VolumeLType."
          + "\nArgument <<2>> is the maximum volume of the inventory of type VolumeLType.");

  static readonly Message<MassType> InventoryMassInfo = new Message<MassType>(
      "#kisLOC_00038",
      defaultTemplate: "Mass: <<1>>",
      description: "The total part mass in the main inventory window. It includes the combined mass"
      + " of all the items in the inventory."
      + "\nArgument <<1>> is the total mass of type MassType.");

  static readonly Message<CostType> InventoryCostInfo = new Message<CostType>(
      "#kisLOC_00039",
      defaultTemplate: "Cost: <<1>>",
      description: "The total part cost in the main inventory window. It includes the combined cost"
      + " of all the items in the inventory."
      + "\nArgument <<1>> is the total cost of type CostType.");

  static readonly Message<VolumeLType> ItemVolumeTooltipInfo = new Message<VolumeLType>(
      "#kisLOC_00040",
      defaultTemplate: "Volume: <<1>>",
      description: "The volume of a single item in the inventory slot. It's presented in a tooltip"
      + " window."
      + "\nArgument <<1>> is the volume of type VolumeLType.");

  static readonly Message<MassType> ItemDryMassTooltipInfo = new Message<MassType>(
      "#kisLOC_00041",
      defaultTemplate: "Dry mass: <<1>>",
      description: "The mass of a single item in the inventory slot without the resources or the"
      + " contents. It's presented in a tooltip window."
      + "\nArgument <<1>> is the mass of type MassType.");

  static readonly Message<MassType> ItemResourceMassTooltipInfo = new Message<MassType>(
      "#kisLOC_00042",
      defaultTemplate: "Resource mass: <<1>>",
      description: "The mass of the resources in a single item in the inventory slot. It's"
      + " presented in a tooltip window."
      + "\nArgument <<1>> is the mass of type MassType.");

  static readonly Message<CostType> ItemCostTooltipInfo = new Message<CostType>(
      "#kisLOC_00043",
      defaultTemplate: "Cost: <<1>>",
      description: "The cost of a single item in the inventory slot including the cost of the"
      + " resources. It's presented in a tooltip window."
      + "\nArgument <<1>> is the cost of type CostType.");

  static readonly Message<CostType> ItemContentsCostTooltipInfo = new Message<CostType>(
      "#kisLOC_00044",
      defaultTemplate: "Contents cost: <<1>>",
      description: "The cost of the contents of a single item in the inventory slot. It's presented"
      + " in a tooltip window."
      + "\nArgument <<1>> is the cost of type CostType.");

  static readonly Message<MassType> ItemContentsMassTooltipInfo = new Message<MassType>(
      "#kisLOC_00045",
      defaultTemplate: "Contents mass: <<1>>",
      description: "The mass of the contents of a single item in the inventory slot. It's presented"
      + " in a tooltip window."
      + "\nArgument <<1>> is the mass of type MassType.");

  static readonly Message<CostType> TotalSlotCostTooltipInfo = new Message<CostType>(
      "#kisLOC_00046",
      defaultTemplate: "Total cost: <<1>>",
      description: "The total cost of the items in the inventory slot. It's presented in a tooltip"
      + " window."
      + "\nArgument <<1>> is the cost of type CostType.");

  static readonly Message<VolumeLType> TotalSlotVolumeTooltipInfo =
      new Message<VolumeLType>(
          "#kisLOC_00047",
          defaultTemplate: "Total volume: <<1>>",
          description: "The total volume of the items in the inventory slot. It's presented in a"
          + " tooltip window."
          + "\nArgument <<1>> is the volume of type VolumeLType.");

  static readonly Message<MassType> TotalSlotMassTooltipInfo = new Message<MassType>(
      "#kisLOC_00048",
      defaultTemplate: "Total mass: <<1>>",
      description: "The total mass of the items in the inventory slot. It's presented in a tooltip"
      + " window."
      + "\nArgument <<1>> is the mass of type MassType.");

  static readonly Message NoResourcesItemTooltipInfo = new Message(
      "#kisLOC_00049",
      defaultTemplate: "Part has no resources",
      description: "The message to present in the tooltip window when the item has no resources.");

  static readonly Message<string> EquipItemKeyTootltipInfo = new Message<string>(
      "#kisLOC_00051",
      defaultTemplate: "Press [<<1>>] to use (equipped)",
      description: "The information for the key that activates the equipped item. It's presented in"
      + " a tooltip window."
      + "\nArgument <<1>> is the string name of the key as set in the settings file.");

  static readonly Message<ResourceType, CompactNumberType, CompactNumberType>
      ItemResourceTootltipInfo = new Message<ResourceType, CompactNumberType, CompactNumberType>(
          "#kisLOC_00052",
          defaultTemplate: "<<1>>: <<2>> / <<3>>",
          description: "The template to present the resources reserve in the item. It's presented"
          + " in a tooltip window."
          + "\nArgument <<1>> is the resource name of type ResourceType."
          + "\nArgument <<2>> is the current reserve of the resource."
          + "\nArgument <<3>> is the maximum amount of the resource.");

  static readonly Message<string, CompactNumberType, CompactNumberType>
      ItemScienceDataTooltipInfo = new Message<string, CompactNumberType, CompactNumberType>(
          "#kisLOC_00053",
          defaultTemplate: "<<1>> (Data=<<2>>, Value=<<3>>)",
          description: "The template to present the science data stored in the item. It's presented"
          + " in a tooltip window."
          + "\nArgument <<1>> is the science title."
          + "\nArgument <<2>> is the science data amount."
          + "\nArgument <<3>> is the value of the science data.");

  static readonly Message ItemNoScienceDataTooltipInfo = new Message(
      "#kisLOC_00054",
      defaultTemplate: "Part has no science data",
      description: "The message to present in the tooltip window when the item has no science"
      + " data.");

  static readonly Message ModuleTitleInfo = new Message(
      "#kisLOC_00055",
      defaultTemplate: "KIS Inventory",
      description: "The title of the module to present in the editor details window.");

  static readonly Message<VolumeLType> MaxVolumePartInfo = new Message<VolumeLType>(
      "#kisLOC_00056",
      defaultTemplate: "Max Volume: <<1>>",
      description: "The info string in the editor for the maximum allowed volume of the"
      + " inventory."
      + "\nArgument <<1>> is the voulme of type VolumeLType");

  static readonly Message InternalAccessAllowedPartInfo = new Message(
      "#kisLOC_00057",
      defaultTemplate: "<color=#00FFFF>Can be accessed from inside</color>",
      description: "The info string in the editor to present if kerbals can access the items in the"
      + " inventory when staying inside the vessel.");

  static readonly Message InternalAccessNotAllowedPartInfo = new Message(
      "#kisLOC_00058",
      defaultTemplate: "<color=#FFA500>Cannot be accessed from inside</color>",
      description: "The info string in the editor to present if kerbals cannot access the items in"
      + " the inventory when staying inside the vessel.");

  static readonly Message ExternalAccessAllowedPartInfo = new Message(
      "#kisLOC_00059",
      defaultTemplate: "<color=#00FFFF>Can be accessed from EVA</color>",
      description: "The info string in the editor to present if kerbals can access the items in the"
      + " inventory when going EVA.");

  static readonly Message ExternalAccessNotAllowedPartInfo = new Message(
      "#kisLOC_00060",
      defaultTemplate: "<color=#FFA500>Cannot be accessed from EVA</color>",
      description: "The info string in the editor to present if kerbals cannot access the items in"
      + " the inventory when going EVA.");
  #endregion

  static readonly GUILayoutOption QuantityAdjustBtnLayout = GUILayout.Width(20);
  static GUIStyle noWrapLabelStyle;

  #region KSP Part's config fields
  // Inventory
  public Dictionary<int, KIS_Item> items = new Dictionary<int, KIS_Item>();
  [KSPField]
  public bool externalAccess = true;
  [KSPField]
  public bool internalAccess = true;
  [KSPField]
  public int slotsX = 6;
  [KSPField]
  public int slotsY = 4;
  [KSPField]
  public int slotSize = 50;
  [KSPField]
  public int itemIconResolution = 128;
  [KSPField]
  public int selfIconResolution = 128;
  [KSPField]
  public float maxVolume = 1;
  [KSPField]
  public string openSndPath = "KIS/Sounds/containerOpen";
  [KSPField]
  public string closeSndPath = "KIS/Sounds/containerClose";
  [KSPField]
  public string helmetOnSndPath = "KIS/Sounds/helmetOn";
  [KSPField]
  public string helmetOffSndPath = "KIS/Sounds/helmetOff";
  [KSPField]
  public string defaultMoveSndPath = "KIS/Sounds/itemMove";
  [KSPField(isPersistant = true)]
  public string invName = "";
  [KSPField(isPersistant = true)]
  public bool helmetEquipped = true;
  #endregion

  #region Global settings
  [PersistentField("EvaInventory/inventoryKey")]
  public static string evaInventoryKey = "tab";

  [PersistentField("EvaInventory/rightHandKey")]
  public static string evaRightHandKey = "x";

  [PersistentField("EvaInventory/helmetKey")]
  public static string evaHelmetKey = "j";
      
  // Inventory hotkeys control.
  [PersistentField("Global/slotHotkeysEnabled")]
  public static bool inventoryKeysEnabled = true;

  [PersistentField("Global/slotHotkey1")]
  public static KeyCode slotHotkey1 = KeyCode.Alpha1;

  [PersistentField("Global/slotHotkey2")]
  public static KeyCode slotHotkey2 = KeyCode.Alpha2;

  [PersistentField("Global/slotHotkey3")]
  public static KeyCode slotHotkey3 = KeyCode.Alpha3;

  [PersistentField("Global/slotHotkey4")]
  public static KeyCode slotHotkey4 = KeyCode.Alpha4;

  [PersistentField("Global/slotHotkey5")]
  public static KeyCode slotHotkey5 = KeyCode.Alpha5;

  [PersistentField("Global/slotHotkey6")]
  public static KeyCode slotHotkey6 = KeyCode.Alpha6;

  [PersistentField("Global/slotHotkey7")]
  public static KeyCode slotHotkey7 = KeyCode.Alpha7;

  [PersistentField("Global/slotHotkey8")]
  public static KeyCode slotHotkey8 = KeyCode.Alpha8;

  [PersistentField("Global/kerbalDefaultMass")]
  public static float kerbalDefaultMass = 0.094f;

  [PersistentField("Global/itemDebug")]
  public static bool debugContextMenu = false;

  [PersistentField("Editor/PodInventory/addToAllSeats", isCollection = true)]
  public static List<String> defaultItemsForAllSeats = new List<string>();

  [PersistentField("Editor/PodInventory/addToTheFirstSeatOnly", isCollection = true)]
  public static List<String> defaultItemsForTheFirstSeat = new List<string>();
  #endregion

  public string openGuiName;
  public float totalVolume = 0;
  public int podSeat = -1;
  public InventoryType invType = InventoryType.Container;
  public enum InventoryType {
    Container,
    Pod,
    Eva
  }
  float keyPressTime = 0f;
  public delegate void DelayedActionMethod(KIS_Item item);
  public string kerbalTrait;
  List<KIS_Item> startEquip = new List<KIS_Item>();

  // GUI
  public bool showGui = false;
  GUIStyle lowerRightStyle, upperLeftStyle, upperRightStyle, buttonStyle, boxStyle;
  public Rect guiMainWindowPos;
  Rect guiDebugWindowPos = new Rect(0, 50, 500, 300);
  KIS_IconViewer icon = null;
  Rect defaultEditorPos = new Rect(Screen.width / 3, 40, 10, 10);
  Rect defaultFlightPos = new Rect(0, 50, 10, 10);
  Vector2 scrollPositionDbg;
  int splitQty = 1;
  bool clickThroughLocked = false;
  bool guiSetName = false;

  //Tooltip
  private KIS_Item tooltipItem;

  // Context menu
  private KIS_Item contextItem;
  private bool contextClick = false;
  private int contextSlot;
  private Rect contextRect;

  // Animation (Not tested)
  [KSPField]
  string openAnimName = "Doors";
  [KSPField]
  float openAnimSpeed = 1f;
  Animation openAnim;

  // Sounds
  public FXGroup sndFx;

  // Debug
  private KIS_Item debugItem;

  #region IHasContextMenu implementation
  public void UpdateContextMenu() {
    var invEvent = PartModuleUtils.GetEvent(this, ToggleInventory);
    if (invType == InventoryType.Pod) {
      if (HighLogic.LoadedSceneIsEditor) {
        invEvent.guiActive = true;
        invEvent.guiActiveUnfocused = true;
        invEvent.guiName = PodSeatInventoryMenuTxt.Format(podSeat);
      } else {
        invEvent.guiActive = false;
        invEvent.guiActiveUnfocused = false;
        ProtoCrewMember crewAtPodSeat = part.protoModuleCrew.Find(x => x.seatIdx == podSeat);
        if (crewAtPodSeat != null) {
          string kerbalName = crewAtPodSeat.name.Split(' ').FirstOrDefault();
          invEvent.guiActive = true;
          invEvent.guiActiveUnfocused = true;
          invEvent.guiName = PersonalInventoryMenuTxt.Format(kerbalName);
        } else {
          if (showGui) {
            // In case of there was GUI active but the kerbal has left the seat.
            ToggleInventory(); 
          }
        }
      }
    } else {
      invEvent.guiActive = true;
      invEvent.guiActiveUnfocused = true;
      invEvent.guiName = invName != ""
          ? PartInventoryWithNameMenuTxt.Format(invName)
          : PartInventoryMenuTxt.Format();
    }
  }
  #endregion

  #region IModuleInfo implementation
  /// <inheritdoc/>
  public virtual string GetModuleTitle() {
    return ModuleTitleInfo;
  }

  /// <inheritdoc/>
  public override string GetInfo() {
    if (invType == InventoryType.Pod) {
      // Such inventories are added after the DB is loaded, so the info string is not visible.
      return "";
    }
    var sb = new StringBuilder();
    sb.AppendLine(MaxVolumePartInfo.Format(maxVolume));
    sb.AppendLine();
    sb.AppendLine(internalAccess
        ? InternalAccessAllowedPartInfo
        : InternalAccessNotAllowedPartInfo);
    sb.AppendLine(externalAccess
        ? ExternalAccessAllowedPartInfo
        : ExternalAccessNotAllowedPartInfo);
    sb.AppendLine();
    return sb.ToString();
  }

  /// <inheritdoc/>
  public virtual Callback<Rect> GetDrawModulePanelCallback() {
    return null;
  }

  /// <inheritdoc/>
  public virtual string GetPrimaryField() {
    return null;
  }
  #endregion

  #region IsDestroyable implementation
  /// <inheritdoc/>
  public virtual void OnDestroy() {
    GameEvents.onCrewTransferred.Remove(OnCrewTransferred);
    GameEvents.onCrewTransferSelected.Remove(OnCrewTransferSelected);
    GameEvents.onVesselChange.Remove(OnVesselChange);
  }
  #endregion

  #region PartModule overrides
  /// <inheritdoc/>
  public override void OnAwake() {
    if (HighLogic.LoadedSceneIsFlight) {
      GameEvents.onCrewTransferred.Add(OnCrewTransferred);
      GameEvents.onCrewTransferSelected.Add(OnCrewTransferSelected);
      GameEvents.onVesselChange.Add(OnVesselChange);
    }
  }

  /// <inheritdoc/>
  public override void OnStart(StartState state) {
    base.OnStart(state);
    if (state == StartState.None) {
      return;
    }
    if ((state & StartState.Editor) == 0) {
      // Clean pod's inventory if the seat is unoccupied.
      if (invType == InventoryType.Pod && podSeat != -1) {
        var crewAtPodSeat = part.protoModuleCrew.Find(x => x.seatIdx == podSeat);
        if (crewAtPodSeat == null && items.Count > 0) {
          HostedDebugLog.Info(
              this, "Clear unoccupied seat inventory: seat={0}, count={1}, mass={2}",
              podSeat, items.Count, GetContentMass());
          items.Clear();
          RefreshMassAndVolume();
        }
      }
    }

    guiMainWindowPos = defaultFlightPos;

    Animation[] anim = part.FindModelAnimators(openAnimName);
    if (anim.Length > 0) {
      openAnim = part.FindModelAnimators(openAnimName)[0];
    }

    if (invType == InventoryType.Eva) {
      var protoCrewMember = part.protoModuleCrew[0];
      kerbalTrait = protoCrewMember.experienceTrait.Title;
    }
    sndFx.audio = part.gameObject.AddComponent<AudioSource>();
    sndFx.audio.volume = GameSettings.SHIP_VOLUME;
    sndFx.audio.rolloffMode = AudioRolloffMode.Linear;
    sndFx.audio.dopplerLevel = 0f;
    sndFx.audio.spatialBlend = 1f;
    sndFx.audio.maxDistance = 10;
    sndFx.audio.loop = false;
    sndFx.audio.playOnAwake = false;
    foreach (var item in startEquip) {
      HostedDebugLog.Info(this, "equip {0}", item.availablePart.name);
      item.Equip();
    }
    RefreshMassAndVolume();

    if (!helmetEquipped) {
      SetHelmet(false, true);
    }
    UpdateContextMenu();
  }

  /// <inheritdoc/>
  public override void OnLoad(ConfigNode node) {
    base.OnLoad(node);
    foreach (ConfigNode itemNode in node.nodes) {
      if (itemNode.name == "ITEM") {
        if (itemNode.HasValue("partName") && itemNode.HasValue("slot")
            && itemNode.HasValue("quantity")) {
          string availablePartName = itemNode.GetValue("partName");
          AvailablePart availablePart = PartLoader.getPartInfoByName(availablePartName);
          if (availablePart != null) {
            int slot = int.Parse(itemNode.GetValue("slot"));
            int qty = int.Parse(itemNode.GetValue("quantity"));
            KIS_Item item = null;
            if (itemNode.HasNode("PART")) {
              item = AddItem(availablePart, itemNode, qty, slot);
            } else {
              HostedDebugLog.Warning(
                  this, "No part node found on item {0}, creating new one from prefab",
                  availablePartName);
              item = AddItem(availablePart.partPrefab, qty, slot);
            }
            if (item != null) {
              bool isEquipped = false;
              ConfigAccessor.GetValueByPath(itemNode, "equipped", ref isEquipped);
              if (isEquipped) {
                if (invType == InventoryType.Eva) {
                  startEquip.Add(item);
                } else {
                  item.equipped = true;
                }
              }
            }
          } else {
            HostedDebugLog.Error(this, "Unable to load {0} from inventory", availablePartName);
          }
        } else {
          HostedDebugLog.Error(this, "Unable to load an item from inventory");
        }
      }
    }
  }

  /// <inheritdoc/>
  public override void OnSave(ConfigNode node) {
    base.OnSave(node);
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      ConfigNode itemNode = node.AddNode("ITEM");
      item.Value.OnSave(itemNode);

      // Science recovery works by retrieving all MODULE/ScienceData 
      // subnodes from the part node, so copy all experiments from 
      // contained parts to where it expects to find them. 
      // This duplicates data but allows recovery to work properly. 
      foreach (ConfigNode module in item.Value.partNode.GetNodes("MODULE")) {
        foreach (ConfigNode experiment in module.GetNodes("ScienceData")) {
          experiment.CopyTo(node.AddNode("ScienceData"));
        }
      }
    }
  }
  #endregion

  /// <summary>Overridden from MonoBehaviour.</summary>
  void Update() {
    if (showGui && HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel.isEVA) {
      var distEvaToContainer = Vector3.Distance(
          FlightGlobals.ActiveVessel.transform.position, part.transform.position);
      var pickup = KISAddonPickup.instance.GetActivePickupNearest(part);
      if (!pickup || distEvaToContainer > pickup.maxDistance) {
        ToggleInventory();
      }
    }
    UpdateKey();
  }

  /// <summary>Overridden from MonoBehaviour.</summary>
  void LateUpdate() {
    //TODO(ihsoft): It's too BAD for performance.
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      item.Value.Update();
    }
  }

  void UpdateKey() {
    if (!HighLogic.LoadedSceneIsFlight
        || FlightGlobals.ActiveVessel != part.vessel
        || !FlightGlobals.ActiveVessel.isEVA) {
      return;
    }

    // Open inventory on keypress
    if (KIS_Shared.IsKeyDown(evaInventoryKey)) {
      ToggleInventory();
    }
    // Use slot when not in drag mode.
    if (!KISAddonPointer.isRunning) {
      slotKeyPress(slotHotkey1, 0, 1);
      slotKeyPress(slotHotkey2, 1, 1);
      slotKeyPress(slotHotkey3, 2, 1);
      slotKeyPress(slotHotkey4, 3, 1);
      slotKeyPress(slotHotkey5, 4, 1);
      slotKeyPress(slotHotkey6, 5, 1);
      slotKeyPress(slotHotkey7, 6, 1);
      slotKeyPress(slotHotkey8, 7, 1);
    }

    // Use right hand tool
    if (KIS_Shared.IsKeyDown(evaRightHandKey)) {
      KIS_Item rightHandItem = GetEquipedItem("rightHand");
      if (rightHandItem != null) {
        rightHandItem.Use(KIS_Item.UseFrom.KeyDown);
      } else {
        UISounds.PlayBipWrong();
        ScreenMessaging.ShowInfoScreenMessage(NoItemEquippedMsg);
      }
    }

    if (KIS_Shared.IsKeyUp(evaRightHandKey)) {
      KIS_Item rightHandItem = GetEquipedItem("rightHand");
      if (rightHandItem != null) {
        rightHandItem.Use(KIS_Item.UseFrom.KeyUp);
      }
    }

    // Put/remove helmet
    if (KIS_Shared.IsKeyDown(evaHelmetKey)) {
      if (helmetEquipped) {
        if (SetHelmet(false, true)) {
          UISoundPlayer.instance.Play(helmetOffSndPath);
        }
      } else {
        if (SetHelmet(true)) {
          UISoundPlayer.instance.Play(helmetOnSndPath);
        }
      }
    }
  }

  /// <summary>Helper method to get all inventories on the part with open UI.</summary>
  /// <returns>List of inventories.</returns>
  public static List<ModuleKISInventory> GetAllOpenInventories() {
    var openInventories = new List<ModuleKISInventory>();
    var allInventory = FindObjectsOfType(typeof(ModuleKISInventory)) as ModuleKISInventory[];
    foreach (var inventory in allInventory) {
      if (inventory.showGui) {
        openInventories.Add(inventory);
      }
    }
    return openInventories;
  }

  void OnVesselChange(Vessel vess) {
    if (showGui) {
      ToggleInventory();
    }

    // Update the menu unfocused range to the newly selected pick up module (if any).
    var pickup = FlightGlobals.ActiveVessel
        .FindPartModulesImplementing<ModuleKISPickup>()
        .OrderByDescending(x => x.maxDistance)
        .FirstOrDefault();
    var invEvent = PartModuleUtils.GetEvent(this, ToggleInventory);
    invEvent.unfocusedRange = pickup
        ? pickup.maxDistance :
        new KSPEvent().unfocusedRange;  // Reset to the game's default.
  }

  /// <summary>Checks if target part can accept non-empty inventories.</summary>
  void OnCrewTransferSelected(CrewTransfer.CrewTransferData transferData) {
    if (invType != InventoryType.Pod || transferData.sourcePart != part
        || transferData.crewMember.seatIdx != podSeat || items.Count == 0) {
      return;  // Not our problem.
    }
    var podInventories = transferData.destPart.FindModulesImplementing<ModuleKISInventory>()
        .Count(x => x.invType == InventoryType.Pod);
    if (transferData.destPart.CrewCapacity > podInventories) {
      ScreenMessaging.ShowErrorScreenMessage(
          CannotTransferInventoryMsg.Format(transferData.destPart.name));
      UISounds.PlayBipWrong();
      transferData.canTransfer = false;
    }
  }

  void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> fromToAction) {
    UpdateContextMenu();

    // Ensure target pod will accept the inventory.
    if (!fromToAction.to.vessel.isEVA) {
      // Assing pod seat if not yet done.
      if (fromToAction.host.seatIdx == -1) {  
        fromToAction.host.seatIdx = GetFirstFreeSeatIdx(fromToAction.to);
        HostedDebugLog.Info(
            this, "Assign {0} to seat {1} in {2}",
            fromToAction.host.name, fromToAction.host.seatIdx, fromToAction.to);
      }
    }

    if (fromToAction.from == part && invType == InventoryType.Pod) {
      if (fromToAction.to.vessel.isEVA) {
        // pod to eva
        ProtoCrewMember crewAtPodSeat =
            fromToAction.from.protoModuleCrew.Find(x => x.seatIdx == podSeat);
        if (items.Count > 0 && crewAtPodSeat == null) {
          ModuleKISInventory destInventory = fromToAction.to.GetComponent<ModuleKISInventory>();
          HostedDebugLog.Info(this, "Items transfer | source seat: {0}", podSeat);
          HostedDebugLog.Info(this, "Items transfer | destination: {0}", destInventory.part);
          MoveItems(items, destInventory);
          RefreshMassAndVolume();
          destInventory.RefreshMassAndVolume();

          // Re-equip items on the EVA kerbal.
          destInventory.startEquip.Clear();
          foreach (var item in destInventory.items.Values) {
            if (item.equipped) {
              HostedDebugLog.Info(
                  this, "Schedule re-equipping item: {0}", item.availablePart.title);
              item.equipped = false;
              destInventory.startEquip.Add(item);
            }
          }
          destInventory.helmetEquipped = helmetEquipped;  // Restore helmet state.
        }
      } else {
        // Pod-to-Pod
        // The target seat is always known from the crew proto, but the source seat has to be
        // deducted. To do so, an assumtion is made that the event listeners are called in the
        // exactly same order as they were registered, and the modules register the listeners in the
        // order of appearance in the part's config. Basing on this, if a POD inventory has some
        // items but the relevant seat ID is not occupied, then the kerbal has just left the pod
        // from this seat. If there are no items, then it doesn't matter anyways.
        ProtoCrewMember crewAtPodSeat =
            fromToAction.from.protoModuleCrew.Find(x => x.seatIdx == podSeat);
        if (items.Count > 0 && crewAtPodSeat == null) {
          HostedDebugLog.Info(this, "Items transfer | source seat: {0}", podSeat);
          // Find target seat and schedule a coroutine.
          var destInventory = fromToAction.to.GetComponents<ModuleKISInventory>().ToList()
              .Find(x => x.podSeat == fromToAction.host.seatIdx);
          destInventory.helmetEquipped = helmetEquipped;  // Move helmet state.
          StartCoroutine(destInventory.WaitAndTransferItems(items, fromToAction.host, this));
        }
      }
    }

    if (fromToAction.to == part && invType == InventoryType.Pod) {
      // eva to pod
      // Only process transfer for the inventory in the target seat. This event will trigger for
      // all inventory modules on the part.
      if (fromToAction.from.vessel.isEVA && fromToAction.host.seatIdx == podSeat) {
        if (fromToAction.host.seatIdx == podSeat) {
          var evaInventory = fromToAction.from.GetComponent<ModuleKISInventory>();
          HostedDebugLog.Info(this, "Item transfer | source {0}", fromToAction.host.name);
          var itemsToDrop = new List<KIS_Item>();
          foreach (var item in evaInventory.items) {
            if (item.Value.carriable) {
              itemsToDrop.Add(item.Value);
            } else if (item.Value.equipped) {
              item.Value.Unequip();
              item.Value.equipped = true;  // Mark state for the further re-equip.
            }
          }
          helmetEquipped = evaInventory.helmetEquipped;  // Save helmet state.
          foreach (var item in itemsToDrop) {
            item.Drop(part);
          }
          var transferedItems = new Dictionary<int, KIS_Item>(evaInventory.items);
          StartCoroutine(WaitAndTransferItems(transferedItems, fromToAction.host));
        }
      }
    }
  }

  int GetFirstFreeSeatIdx(Part p) {
    for (var i = 0; i < p.protoModuleCrew.Count; i++) {
      var pcm = p.protoModuleCrew.Find(x => x.seatIdx == i);
      if (pcm == null) {
        return i;
      }
    }
    HostedDebugLog.Error(this, "Cannot find a free seat in: {0}", p);
    return -1;
  }

  IEnumerator WaitAndTransferItems(Dictionary<int, KIS_Item> transferedItems,
                                   ProtoCrewMember protoCrew,
                                   ModuleKISInventory srcInventory = null) {
    yield return new WaitForFixedUpdate();
    var crewAtPodSeat = part.protoModuleCrew.Find(x => x.seatIdx == podSeat);
    if (crewAtPodSeat == protoCrew) {
      MoveItems(transferedItems, this);
      HostedDebugLog.Info(this, "Item transfer | destination seat: {0}", podSeat);
      RefreshMassAndVolume();
      if (srcInventory) {
        srcInventory.RefreshMassAndVolume();
      }
    }
  }

  /// <summary>Refreshes container mass, volume and cost.</summary>
  public void RefreshMassAndVolume() {
    // Update volume.
    totalVolume = GetContentVolume();
    // Update vessel cost in editor.
    if (HighLogic.LoadedSceneIsEditor) {
      GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
    }
  }

  void slotKeyPress(KeyCode kc, int slot, int delay = 1) {
    if (kc == KeyCode.None || !inventoryKeysEnabled) {
      return;
    }

    // TODO: Add a check for shift keys to not trigger use action on combinations with
    // Shift, Ctrl, and Alt.
    if (KIS_Shared.IsKeyDown(kc)) {
      keyPressTime = Time.time;
    }
    if (InputLockManager.IsUnlocked(ControlTypes.UI) && Input.GetKey(kc)) {
      if (Time.time - keyPressTime >= delay) {
        if (items.ContainsKey(slot)) {
          items[slot].Use(KIS_Item.UseFrom.InventoryShortcut);
        }
        keyPressTime = Mathf.Infinity;
      }
    }
    if (KIS_Shared.IsKeyUp(kc)) {
      if (!float.IsInfinity(keyPressTime) && items.ContainsKey(slot)) {
        items[slot].ShortcutKeyPress();
      }
      keyPressTime = 0;
    }
  }

  public KIS_Item AddItem(AvailablePart availablePart, ConfigNode partNode,
                          int qty = 1, int slot = -1) {
    KIS_Item item = null;
    if (items.ContainsKey(slot)) {
      slot = -1;
    }
    int maxSlot = (slotsX * slotsY) - 1;
    if (slot < 0 || slot > maxSlot) {
      slot = GetFreeSlot();
      if (slot == -1) {
        HostedDebugLog.Error(
            this, "AddItem error : No free slot available for {0}", availablePart.title);
        return null;
      }
    }
    item = new KIS_Item(availablePart, partNode, this, qty);
    items.Add(slot, item);
    if (showGui) {
      items[slot].EnableIcon(itemIconResolution);
    }
    RefreshMassAndVolume();
    return item;
  }

  public KIS_Item AddItem(Part p, int qty = 1, int slot = -1) {
    KIS_Item item = null;
    if (items.ContainsKey(slot)) {
      slot = -1;
    }
    int maxSlot = (slotsX * slotsY) - 1;
    if (slot < 0 || slot > maxSlot) {
      slot = GetFreeSlot();
      if (slot == -1) {
        HostedDebugLog.Error(
            this, "AddItem error : No free slot available for {0}", p.partInfo.title);
        return null;
      }
    }
    item = new KIS_Item(p, this, qty);
    items.Add(slot, item);
    if (showGui) {
      items[slot].EnableIcon(itemIconResolution);
    }
    RefreshMassAndVolume();
    return item;
  }

  public static void MoveItem(KIS_Item srcItem, ModuleKISInventory tgtInventory, int tgtSlot) {
    ModuleKISInventory srcInventory = srcItem.inventory;
    srcItem.OnMove(srcInventory, tgtInventory);
    int srcSlot = srcItem.slot;
    tgtInventory.items.Add(tgtSlot, srcItem);
    srcItem.inventory.items.Remove(srcSlot);
    srcItem.inventory = tgtInventory;
    srcInventory.RefreshMassAndVolume();
    tgtInventory.RefreshMassAndVolume();
  }

  public static void MoveItems(Dictionary<int, KIS_Item> srcItems, ModuleKISInventory destInventory) {
    destInventory.items.Clear();
    destInventory.items = new Dictionary<int, KIS_Item>(srcItems);
    foreach (KeyValuePair<int, KIS_Item> item in destInventory.items) {
      item.Value.inventory = destInventory;
    }
    srcItems.Clear();
  }

  public void DeleteItem(int slot) {
    if (items.ContainsKey(slot)) {
      items[slot].StackRemove();
    }
  }

  public bool isFull() {
    return GetFreeSlot() < 0;
  }

  public KIS_Item GetEquipedItem(string equipSlot) {
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      if (item.Value.equipped && item.Value.equipSlot == equipSlot) {
        return item.Value;
      }
    }
    return null;
  }

  public int GetFreeSlot() {
    int maxSlot = (slotsX * slotsY) - 1;
    for (int i = 0; i <= maxSlot; i++) {
      if (items.ContainsKey(i) == false) {
        return i;
      }
    }
    return -1;
  }

  public float GetContentMass() {
    float contentMass = 0;
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      contentMass += item.Value.totalMass;
    }
    return contentMass;
  }

  public float GetContentVolume() {
    float contentVolume = 0;
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      if (item.Value.carriable && invType == InventoryType.Eva) {
        contentVolume += 0;
      } else {
        contentVolume += item.Value.stackVolume;
      }
    }
    return contentVolume;
  }

  public float GetContentCost() {
    float contentCost = 0;
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      contentCost += item.Value.totalCost;
    }
    return contentCost;
  }

  /// <summary>Overridden from IPartCostModifier.</summary>
  public ModifierChangeWhen GetModuleCostChangeWhen() {
    // TODO(ihsoft): Figure out what value is right.
    return ModifierChangeWhen.FIXED;
  }

  /// <summary>Overridden from IPartCostModifier.</summary>
  public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) {
    return GetContentCost();
  }

  /// <summary>Overridden from IPartMassModifier.</summary>
  public ModifierChangeWhen GetModuleMassChangeWhen() {
    return ModifierChangeWhen.CONSTANTLY;
  }
      
  /// <summary>Overridden from IPartMassModifier.</summary>
  public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) {
    return GetContentMass();
  }

  /// <summary>Checks if part has a child, and reports the problem.</summary>
  /// <param name="p">A part to check.</param>
  /// <returns><c>true</c> if it's OK to put the part into the inventory.</returns>
  bool VerifyIsNotAssembly(Part p) {
    if (!HighLogic.LoadedSceneIsEditor && KISAddonPickup.grabbedPartsCount > 1) {
      ScreenMessaging.ShowPriorityScreenMessage(
          PartHasChildrenMsg.Format(KISAddonPickup.grabbedPartsCount - 1));
      return false;
    }
    return true;
  }

  bool VolumeAvailableFor(Part p) {
    float partVolume = KIS_Shared.GetPartVolume(p.partInfo);
    var newTotalVolume = GetContentVolume() + partVolume;
    if (newTotalVolume > maxVolume) {
      ScreenMessaging.ShowPriorityScreenMessage(
          MaxVolumeReachedMsg.Format(partVolume, newTotalVolume - maxVolume));
      return false;
    }
    return true;
  }

  bool VolumeAvailableFor(KIS_Item item) {
    RefreshMassAndVolume();
    if (KISAddonPickup.draggedItem.inventory == this) {
      return true;
    } else {
      float newTotalVolume = GetContentVolume() + item.stackVolume;
      if (newTotalVolume > maxVolume) {
        ScreenMessaging.ShowPriorityScreenMessage(
            MaxVolumeReachedMsg.Format(item.stackVolume, (newTotalVolume - maxVolume)));
        return false;
      } else {
        return true;
      }
    }
  }

  // TODO(ihsoft): Move out of base inventory module.
  public void DelayedAction(DelayedActionMethod actionMethod, KIS_Item item, float delay) {
    StartCoroutine(WaitAndDoAction(actionMethod, item, delay));
  }

  IEnumerator WaitAndDoAction(DelayedActionMethod actionMethod, KIS_Item item, float delay) {
    yield return new WaitForSeconds(delay);
    actionMethod(item);
  }

  [KSPEvent(guiActiveEditor = true, guiActive = true, guiActiveUnfocused = true)]
  [LocalizableItem(tag = null)]
  public void ToggleInventory() {
    if (showGui) {
      // Destroy icons viewer
      foreach (KeyValuePair<int, KIS_Item> item in items) {
        item.Value.DisableIcon();
      }
      if (openAnim) {
        openAnim[openAnimName].speed = -openAnimSpeed;
        openAnim.Play(openAnimName);
      }
      DisableIcon();
      showGui = false;
      if (HighLogic.LoadedSceneIsEditor) {
        UISoundPlayer.instance.Play(closeSndPath);
      } else {
        UISoundPlayer.instance.Play(closeSndPath);
      }
      clickThroughLocked = false;
      if (HighLogic.LoadedSceneIsFlight) {
        InputLockManager.RemoveControlLock("KISInventoryFlightLock");
      }
      if (HighLogic.LoadedSceneIsEditor) {
        InputLockManager.RemoveControlLock("KISInventoryEditorLock");
      }
    } else {
      // Check if inventory can be opened from interior/exterior
      if (HighLogic.LoadedSceneIsFlight) {
        // Don't allow access to the container being carried by a kerbal. Its state is
        // serialized in the kerbal's invenotry so, any changes will be reverted once
        // the container is dropped.
        // TODO: Find a way to update serialized state and remove this check (#89). 
        if (GetComponent<ModuleKISItemEvaTweaker>() && vessel.isEVA) {
          ScreenMessaging.ShowPriorityScreenMessage(NotAccessibleWhileCarriedMsg);
          return;
        }
        if (FlightGlobals.ActiveVessel.isEVA && !externalAccess) {
          ScreenMessaging.ShowPriorityScreenMessage(NotAccessibleFromOutsideMsg);
          return;
        }
        if (!FlightGlobals.ActiveVessel.isEVA && !internalAccess) {
          ScreenMessaging.ShowPriorityScreenMessage(NotAccessibleFromInsideMsg);
          return;
        }
      }

      // Create icons viewer
      foreach (KeyValuePair<int, KIS_Item> item in items) {
        item.Value.EnableIcon(itemIconResolution);
      }
      EnableIcon();

      // TODO(ihsoft): Don't limit to one open inventory. Add till bootom is reached.
      if (GetAllOpenInventories().Count == 1
          && Mathf.Approximately(guiMainWindowPos.x, defaultFlightPos.x)
          && Mathf.Approximately(guiMainWindowPos.y, defaultFlightPos.y)) {
        guiMainWindowPos.y += 250;
      }
      if (openAnim) {
        openAnim[openAnimName].speed = openAnimSpeed;
        openAnim.Play(openAnimName);
      }
      showGui = true;
      if (HighLogic.LoadedSceneIsEditor) {
        UISoundPlayer.instance.Play(openSndPath);
      } else {
        UISoundPlayer.instance.Play(openSndPath);
      }
    }
  }

  public bool SetHelmet(bool active, bool checkAtmo = false) {
    if (checkAtmo) {
      if (!part.vessel.mainBody.atmosphereContainsOxygen) {
        helmetEquipped = true;
        ScreenMessaging.ShowPriorityScreenMessage(CannotRemoveHelmetNoOxygenMsg);
        UISounds.PlayBipWrong();
        return false;
      }
      if (FlightGlobals.getStaticPressure() < KISAddonConfig.breathableAtmoPressure) {
        helmetEquipped = true;
        ScreenMessaging.ShowPriorityScreenMessage(
            CannotRemoveHelmetPressureTooLowMsg.Format(
                KISAddonConfig.breathableAtmoPressure, FlightGlobals.getStaticPressure()));
        UISounds.PlayBipWrong();
        return false;
      }
    }

    //Disable helmet and visor
    var skmrs =
        new List<SkinnedMeshRenderer>(part.GetComponentsInChildren<SkinnedMeshRenderer>());
    foreach (var skmr in skmrs) {
      if (skmr.name == "helmet" || skmr.name == "visor") {
        skmr.GetComponent<Renderer>().enabled = active;
        helmetEquipped = active;
      }
    }

    //Disable flares and light
    var lights = new List<Light>(part.GetComponentsInChildren<Light>(true) as Light[]);
    foreach (var light in lights) {
      if (light.name == "headlamp") {
        light.enabled = active;
        light.transform.Find("flare1").GetComponent<Renderer>().enabled = active;
        light.transform.Find("flare2").GetComponent<Renderer>().enabled = active;
      }
    }

    return true;
  }

  void GUIStyles() {
    GUI.skin = HighLogic.Skin;
    GUI.skin.button.alignment = TextAnchor.MiddleCenter;

    boxStyle = new GUIStyle(GUI.skin.box);
    boxStyle.fontSize = 11;
    boxStyle.padding.top = GUI.skin.box.padding.bottom = GUI.skin.box.padding.left = 5;
    boxStyle.alignment = TextAnchor.UpperLeft;

    upperRightStyle = new GUIStyle(GUI.skin.label);
    upperRightStyle.alignment = TextAnchor.UpperRight;
    upperRightStyle.fontSize = 9;
    upperRightStyle.padding = new RectOffset(4, 4, 4, 4);
    upperRightStyle.normal.textColor = Color.yellow;

    upperLeftStyle = new GUIStyle(GUI.skin.label);
    upperLeftStyle.alignment = TextAnchor.UpperLeft;
    upperLeftStyle.fontSize = 11;
    upperLeftStyle.padding = new RectOffset(4, 4, 4, 4);
    upperLeftStyle.normal.textColor = Color.green;

    lowerRightStyle = new GUIStyle(GUI.skin.label);
    lowerRightStyle.alignment = TextAnchor.LowerRight;
    lowerRightStyle.fontSize = 10;
    lowerRightStyle.padding = new RectOffset(4, 4, 4, 4);
    lowerRightStyle.normal.textColor = Color.white;

    noWrapLabelStyle = new GUIStyle(GUI.skin.label);
    noWrapLabelStyle.wordWrap = false;

    buttonStyle = new GUIStyle(GUI.skin.button);
    buttonStyle.padding = new RectOffset(4, 4, 4, 4);
    buttonStyle.alignment = TextAnchor.MiddleCenter;
  }

  void OnGUI() {
    if (!showGui) {
      return;
    }

    // Update GUI of items
    //TODO(ihsoft): Call in the per item display instead. 
    foreach (KeyValuePair<int, KIS_Item> item in items) {
      item.Value.GUIUpdate();
    }

    GUIStyles();

    // Set title
    string title = part.partInfo.title;
    if (invType == InventoryType.Pod) {
      title = PodInventoryWindowTitle.Format(part.partInfo.title, podSeat);
      if (!HighLogic.LoadedSceneIsEditor) {
        ProtoCrewMember crewAtPodSeat = part.protoModuleCrew.Find(x => x.seatIdx == podSeat);
        if (crewAtPodSeat != null) {
          title = crewAtPodSeat.name;
        }
      }
    }
    if (invType == InventoryType.Eva) {
      title = PersonalInventoryWindowTitle.Format(part.partInfo.title, kerbalTrait);
    }
    if (invType == InventoryType.Container && invName != "") {
      title = ContainerInventoryWindowTitle.Format(part.partInfo.title, invName);
    }

    guiMainWindowPos = GUILayout.Window(GetInstanceID(), guiMainWindowPos, GuiMain, title);

    if (tooltipItem != null) {
      if (contextItem == null) {
        var tooltipName = tooltipItem.inventoryName != ""
            ? TooltipWindowTitle.Format(tooltipItem.availablePart.title, tooltipItem.inventoryName)
            : tooltipItem.availablePart.title;
        GUILayout.Window(GetInstanceID() + 780,
                         new Rect(Event.current.mousePosition.x + 5,
                                  Event.current.mousePosition.y + 5, 400, 1),
                         GuiTooltip, tooltipName);
      }
    }
    if (contextItem != null) {
      var contextRelativeRect = new Rect(
          guiMainWindowPos.x + contextRect.x + (contextRect.width / 2),
          guiMainWindowPos.y + contextRect.y + (contextRect.height / 2),
          0, 0);
      GUILayout.Window(
          GetInstanceID() + 781, contextRelativeRect, GuiContextMenu, ItemActionMenuWindowTitle);
      if (contextClick) {
        contextClick = false;
        splitQty = 1;
      } else if (Event.current.type == EventType.MouseDown) {
        contextItem = null;
      }
    }

    if (debugItem != null) {
      guiDebugWindowPos =
          GUILayout.Window(GetInstanceID() + 782, guiDebugWindowPos, GuiDebugItem, "Debug item");
    }

    // Disable Click through
    if (HighLogic.LoadedSceneIsEditor) {
      if (guiMainWindowPos.Contains(Event.current.mousePosition) && !clickThroughLocked) {
        InputLockManager.SetControlLock(
            ControlTypes.EDITOR_PAD_PICK_PLACE, "KISInventoryEditorLock");
        clickThroughLocked = true;
      }
      if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked) {
        InputLockManager.RemoveControlLock("KISInventoryEditorLock");
        clickThroughLocked = false;
      }
    } else if (HighLogic.LoadedSceneIsFlight) {
      if (guiMainWindowPos.Contains(Event.current.mousePosition) && !clickThroughLocked) {
        InputLockManager.SetControlLock(
            ControlTypes.CAMERACONTROLS | ControlTypes.MAP, "KISInventoryFlightLock");
        clickThroughLocked = true;
      }
      if (!guiMainWindowPos.Contains(Event.current.mousePosition) && clickThroughLocked) {
        InputLockManager.RemoveControlLock("KISInventoryFlightLock");
        clickThroughLocked = false;
      }
    }
  }

  void GuiMain(int windowID) {
    // For pod inventory make a special note.
    if (HighLogic.LoadedSceneIsEditor && invType == InventoryType.Pod) {
      GUILayout.BeginVertical();
      var warningStyle = new GUIStyle();
      warningStyle.normal.textColor = Color.yellow;
      warningStyle.fontStyle = FontStyle.Bold;
      warningStyle.fontSize = 11;
      warningStyle.alignment = TextAnchor.MiddleCenter;
      warningStyle.wordWrap = true;
      GUILayout.Label(MustBeCrewedAtLaunchMsg, warningStyle);
      GUILayout.EndVertical();
    }

    GUILayout.BeginHorizontal();

    GUILayout.BeginVertical();
    const int Width = 160;
    GUILayout.Box("", GUILayout.Width(Width), GUILayout.Height(100));
    Rect textureRect = GUILayoutUtility.GetLastRect();
    GUI.DrawTexture(textureRect, icon.texture, ScaleMode.ScaleToFit);

    int extraSpace = 0;
    //Set inventory name
    if (invType == InventoryType.Container) {
      if (guiSetName) {
        GUILayout.BeginHorizontal();
        invName = GUILayout.TextField(invName, 14, GUILayout.Height(22));
        if (GUILayout.Button(AcceptNameChangeBtn, GUILayout.Width(30), GUILayout.Height(22))) {
          guiSetName = false;
        }
        GUILayout.EndHorizontal();
      } else {
        if (GUILayout.Button(SetInventoryNameBtn,
                             GUILayout.Width(Width), GUILayout.Height(22))) {
          guiSetName = true;
        }
      }
    } else if (invType == InventoryType.Eva) {
      if (helmetEquipped) {
        if (GUILayout.Button(RemoveHelmetMenuTxt, GUILayout.Width(Width), GUILayout.Height(22))) {
          if (SetHelmet(false, true)) {
            UISoundPlayer.instance.Play(helmetOffSndPath);
          }
        }
      } else {
        if (GUILayout.Button(PutOnHelmetMenuTxt, GUILayout.Width(Width), GUILayout.Height(22))) {
          if (SetHelmet(true)) {
            UISoundPlayer.instance.Play(helmetOnSndPath);
          }
        }
      }
    } else {
      extraSpace = 30;
    }

    if (slotsY == 5 && slotSize == 50) {
      extraSpace += 50;
    }

    var sb = new StringBuilder();
    sb.AppendLine(InventoryVolumeInfo.Format(totalVolume, maxVolume));
    sb.AppendLine(InventoryMassInfo.Format(part.mass));
    sb.AppendLine(InventoryCostInfo.Format(GetContentCost() + part.partInfo.cost));
    GUILayout.Box(sb.ToString(), boxStyle,
                  GUILayout.Width(Width), GUILayout.Height(45 + extraSpace));
    bool closeInv = false;

    if (GUILayout.Button(CloseInventoryBtn, GUILayout.Width(Width), GUILayout.Height(21))) {
      closeInv = true;
    }
    GUILayout.EndVertical();

    GUILayout.BeginVertical();
    GuiInventory();
    GUILayout.EndVertical();

    GUILayout.EndHorizontal();

    if (contextItem == null) {
      GUI.DragWindow(new Rect(0, 0, 10000, 30));
    }
    if (closeInv) {
      ToggleInventory();
    }
  }

  void GuiTooltip(int windowID) {
    if (tooltipItem == null) {
      return;
    }

    GUILayout.BeginHorizontal();

    GUILayout.BeginVertical();
    GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(100));
    Rect textureRect = GUILayoutUtility.GetLastRect();
    GUI.DrawTexture(textureRect, tooltipItem.icon.texture, ScaleMode.ScaleToFit);
    GUILayout.EndVertical();

    var sb = new StringBuilder();
    sb.AppendLine(ItemVolumeTooltipInfo.Format(tooltipItem.volume));
    sb.AppendLine(ItemDryMassTooltipInfo.Format(tooltipItem.dryMass));
    if (tooltipItem.availablePart.partPrefab.Resources.Count > 0) {
      sb.AppendLine(ItemResourceMassTooltipInfo.Format(tooltipItem.resourceMass));
    }
    sb.AppendLine(ItemCostTooltipInfo.Format(tooltipItem.cost));
    if (tooltipItem.contentCost > 0) {
      sb.AppendLine(ItemContentsCostTooltipInfo.Format(tooltipItem.contentCost));
    }
    if (tooltipItem.contentMass > 0) {
      sb.AppendLine(ItemContentsMassTooltipInfo.Format(tooltipItem.contentMass));
    }
    if (tooltipItem.equipSlot != null) {
      sb.AppendLine(tooltipItem.prefabModule.GetEqipSlotString());
      if (tooltipItem.equipSlot == "rightHand") {
        sb.AppendLine(EquipItemKeyTootltipInfo.Format(evaRightHandKey));
      }
    }
    GUILayout.BeginVertical();
    GUILayout.Box(sb.ToString(), boxStyle, GUILayout.Width(150), GUILayout.Height(100));
    GUILayout.EndVertical();

    GUILayout.BeginVertical();
    StringBuilder text2 = new StringBuilder();

    if (tooltipItem.quantity > 1) {
      // Show total if stacked
      GUI.Label(textureRect,
          MultipleItemsContextCaption.Format(tooltipItem.quantity),
          lowerRightStyle);
      text2.AppendLine(TotalSlotCostTooltipInfo.Format(tooltipItem.totalCost));
      text2.AppendLine(TotalSlotVolumeTooltipInfo.Format(tooltipItem.stackVolume));
      text2.AppendLine(TotalSlotMassTooltipInfo.Format(tooltipItem.totalMass));
    } else {
      // Show resource if not stacked
      List<KIS_Item.ResourceInfo> resources = tooltipItem.GetResources();
      if (resources.Count > 0) {
        foreach (KIS_Item.ResourceInfo resource in resources) {
          text2.AppendLine(ItemResourceTootltipInfo.Format(
              resource.resourceName, resource.amount, resource.maxAmount));
        }
      } else {
        text2.AppendLine(NoResourcesItemTooltipInfo);
      }
    }

    // Show science data
    var sciences = tooltipItem.GetSciences();
    if (sciences.Count > 0) {
      foreach (ScienceData scienceData in sciences) {
        text2.AppendLine(ItemScienceDataTooltipInfo.Format(
            scienceData.title,
            scienceData.dataAmount,
            scienceData.baseTransmitValue * scienceData.transmitBonus));
      }
    } else {
      text2.AppendLine(ItemNoScienceDataTooltipInfo);
    }

    GUILayout.Box(text2.ToString(), boxStyle, GUILayout.Width(200), GUILayout.Height(100));
    GUILayout.EndVertical();

    GUILayout.EndHorizontal();
    GUILayout.Space(10);
    GUILayout.Box(tooltipItem.availablePart.description, boxStyle,
                  GUILayout.Width(450), GUILayout.Height(100));
  }

  void GuiContextMenu(int windowID) {
    GUI.FocusWindow(windowID);
    GUI.BringWindowToFront(windowID);
    bool noAction = true;

    //Equip
    if (contextItem != null) {
      if (contextItem.equipable && contextItem.quantity == 1 && invType == InventoryType.Eva) {
        noAction = false;
        if (contextItem.equipped) {
          if (GUILayout.Button(UnequipItemContextMenuBtn)) {
            contextItem.Unequip(KIS_Item.ActorType.Player);
            contextItem = null;
          }
        } else {
          if (GUILayout.Button(EquipItemContextBtn)) {
            contextItem.Equip(KIS_Item.ActorType.Player);
            contextItem = null;
          }
        }
      }
    }

    //Carriable
    if (contextItem != null) {
      if (contextItem.carriable && invType == InventoryType.Eva) {
        noAction = false;
        if (GUILayout.Button(DropCarriedItemContextBtn)) {
          contextItem.Drop();
          contextItem = null;
        }
      }
    }

    //Set stack quantity (editor only)
    if (contextItem != null && HighLogic.LoadedSceneIsEditor) {
      if (contextItem.stackable) {
        noAction = false;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-", buttonStyle, QuantityAdjustBtnLayout)) {
          if (Input.GetKey(KeyCode.LeftShift)) {
            if (contextItem.quantity > 10) {
              if (contextItem.StackRemove(10) == false)
                contextItem = null;
            }
          } else if (Input.GetKey(KeyCode.LeftControl)) {
            if (contextItem.quantity > 100) {
              if (contextItem.StackRemove(100) == false)
                contextItem = null;
            }
          } else {
            if (contextItem.quantity > 1) {
              if (contextItem.StackRemove(1) == false)
                contextItem = null;
            }
          }
        }
        if (GUILayout.Button("+", buttonStyle, QuantityAdjustBtnLayout)) {
          if (contextItem.stackable) {
            if (Input.GetKey(KeyCode.LeftShift)) {
              contextItem.StackAdd(10);
            } else if (Input.GetKey(KeyCode.LeftControl)) {
              contextItem.StackAdd(100);
            } else {
              contextItem.StackAdd(1);
            }
          }
        }
        if (contextItem != null) {
          GUILayout.Label(
              ItemsQuantityItemContextMsg.Format(contextItem.quantity), noWrapLabelStyle);
        }
        GUILayout.EndHorizontal();
      }
    }

    //Split
    if (contextItem != null && !HighLogic.LoadedSceneIsEditor) {
      if (contextItem.quantity > 1) {
        noAction = false;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-", buttonStyle, QuantityAdjustBtnLayout)) {
          if (splitQty - 1 > 0)
            splitQty -= 1;
        }
        if (GUILayout.Button(SplitItemsContextBtn.Format(splitQty), buttonStyle)) {
          if (!isFull()) {
            contextItem.quantity -= splitQty;
            AddItem(contextItem.availablePart.partPrefab, splitQty);
          } else {
            ScreenMessaging.ShowPriorityScreenMessage(InventoryFullCannotSplitMsg);
          }
          contextItem = null;
        }
        if (GUILayout.Button("+", buttonStyle, QuantityAdjustBtnLayout)) {
          if (splitQty + 1 < contextItem.quantity)
            splitQty += 1;
        }
        GUILayout.EndHorizontal();
      }
    }

    // Use
    if (contextItem != null) {
      if ((HighLogic.LoadedSceneIsFlight && contextItem.usableFromEva
           && contextItem.inventory.invType == InventoryType.Eva)
          || (HighLogic.LoadedSceneIsFlight && contextItem.usableFromContainer
              && contextItem.inventory.invType == InventoryType.Container)
          || (HighLogic.LoadedSceneIsFlight && contextItem.usableFromPod
              && contextItem.inventory.invType == InventoryType.Pod)
          || (HighLogic.LoadedSceneIsEditor && contextItem.usableFromEditor)) {
        noAction = false;
        if (GUILayout.Button(contextItem.prefabModule.useName)) {
          contextItem.Use(KIS_Item.UseFrom.ContextMenu);
          contextItem = null;
        }
      }
    }

    //Debug
    if (debugContextMenu) {
      if (contextItem != null && !HighLogic.LoadedSceneIsEditor && invType == InventoryType.Eva) {
        if (contextItem.prefabModule != null) {
          noAction = false;
          if (GUILayout.Button("Debug")) {
            debugItem = contextItem;
            contextItem = null;
          }
        }
      }
    }
    if (noAction) {
      GUILayout.Label(NoActionItemContextMsg, noWrapLabelStyle);
    }
  }

  void GuiDebugItem(int windowID) {
    if (debugItem != null) {
      KIS_Shared.EditField("moveSndPath", ref debugItem.prefabModule.moveSndPath);
      KIS_Shared.EditField("shortcutKeyAction(drop,equip,custom)",
                           ref debugItem.prefabModule.shortcutKeyAction);
      KIS_Shared.EditField("usableFromEva", ref debugItem.prefabModule.usableFromEva);
      KIS_Shared.EditField("usableFromContainer", ref debugItem.prefabModule.usableFromContainer);
      KIS_Shared.EditField("usableFromPod", ref debugItem.prefabModule.usableFromPod);
      KIS_Shared.EditField("usableFromEditor", ref debugItem.prefabModule.usableFromEditor);
      KIS_Shared.EditField("useName", ref debugItem.prefabModule.useName);
      KIS_Shared.EditField("equipMode(model,physic)", ref debugItem.prefabModule.equipMode);
      KIS_Shared.EditField("equipSlot", ref debugItem.prefabModule.equipSlot);
      KIS_Shared.EditField("equipable", ref debugItem.prefabModule.equipable);
      KIS_Shared.EditField("stackable", ref debugItem.prefabModule.stackable);
      KIS_Shared.EditField("carriable", ref debugItem.prefabModule.carriable);
      KIS_Shared.EditField("equipSkill(<blank>,RepairSkill,ScienceSkill,etc...)",
                           ref debugItem.prefabModule.equipSkill);
      KIS_Shared.EditField("equipRemoveHelmet", ref debugItem.prefabModule.equipRemoveHelmet);
      KIS_Shared.EditField("volumeOverride(0 = auto)", ref debugItem.prefabModule.volumeOverride);

      scrollPositionDbg = GUILayout.BeginScrollView(scrollPositionDbg,
                                                    GUILayout.Width(400), GUILayout.Height(200));
      var skmrs =
          new List<SkinnedMeshRenderer>(part.GetComponentsInChildren<SkinnedMeshRenderer>());
      foreach (var skmr in skmrs) {
        GUILayout.Label("--- " + skmr.name + " ---");
        foreach (var bone in skmr.bones) {
          if (GUILayout.Button(new GUIContent(bone.name, ""))) {
            debugItem.prefabModule.equipMeshName = skmr.name;
            debugItem.prefabModule.equipBoneName = bone.name;
            debugItem.ReEquip();
          }
        }
      }

      GUILayout.EndScrollView();
      if (KIS_Shared.EditField("equipPos", ref debugItem.prefabModule.equipPos)) {
        debugItem.ReEquip();
      }
      if (KIS_Shared.EditField("equipDir", ref debugItem.prefabModule.equipDir)) {
        debugItem.ReEquip();
      }
    }
    if (GUILayout.Button("Close")) {
      debugItem = null;
    }
    GUI.DragWindow();
  }

 void GuiInventory() {
    int slotIndex = 0;
    KIS_Item mouseOverItem = null;
    for (var x = 0; x < slotsY; x++) {
      GUILayout.BeginHorizontal();
      for (var y = 0; y < slotsX; y++) {
        GUILayout.Box("", GUILayout.Width(slotSize), GUILayout.Height(slotSize));
        Rect textureRect = GUILayoutUtility.GetLastRect();
        if (items.ContainsKey(slotIndex)) {
          GuiHandleUsedSlot(textureRect, slotIndex);
          GuiUpdateHoveredItem(textureRect, slotIndex, ref mouseOverItem);
        } else {
          GuiHandleEmptySlot(textureRect, slotIndex);
        }
        slotIndex++;
      }
      GUILayout.EndHorizontal();
    }
    tooltipItem = mouseOverItem;
  }

  /// <summary>Handles mouse over a slot.</summary>
  /// <param name="textureRect">Slot's rect in UI.</param>
  /// <param name="slotIndex">Slot's index in inventory.</param>
  /// <param name="mouseOverItem">[out] Hovered item if any.</param>
  void GuiUpdateHoveredItem(Rect textureRect, int slotIndex, ref KIS_Item mouseOverItem) {
    // Mouse over a slot
    if (Event.current.type == EventType.Repaint
        && textureRect.Contains(Event.current.mousePosition)
        && !KISAddonPickup.draggedPart) {
      mouseOverItem = items[slotIndex];
      mouseOverItem.icon.Rotate();
      if (mouseOverItem != tooltipItem && tooltipItem != null) {
        tooltipItem.icon.ResetPos();
      }
    }
  }

  /// <summary>Handles GUI drawing and events in a slot with item.</summary>
  /// <param name="textureRect">Slot's rect in UI.</param>
  /// <param name="slotIndex">Slot's index in inventory.</param>
  void GuiHandleUsedSlot(Rect textureRect, int slotIndex) {
    var item = items[slotIndex];
    GUI.DrawTexture(textureRect, item.icon.texture, ScaleMode.ScaleToFit);
    // Part's vessel is null when in the editor mode.
    if (part.vessel != null && FlightGlobals.ActiveVessel == part.vessel
        && FlightGlobals.ActiveVessel.isEVA) {
      // Keyboard shortcut
      //TODO(ihsoft): Show the slot shorcut instead.
      int slotNb = slotIndex + 1;
      GUI.Label(textureRect, SlotIdContextCaption.Format(slotNb), upperLeftStyle);
      if (item.carried) {
        GUI.Label(textureRect, CarriedItemContextCaption, upperRightStyle);
      } else if (item.equipped) {
        GUI.Label(textureRect, EquippedItemContextCaption, upperRightStyle);
      }
    }
    if (item.stackable) {
      // Quantity
      GUI.Label(textureRect, MultipleItemsContextCaption.Format(item.quantity), lowerRightStyle);
    }

    if (Event.current.type == EventType.MouseDown
        && textureRect.Contains(Event.current.mousePosition)) {
      // Pickup part
      if (Event.current.button == 0) {
        KISAddonPickup.instance.Pickup(items[slotIndex]);
      }
      // Context menu
      if (Event.current.button == 1) {
        contextClick = true;
        contextItem = items[slotIndex];
        contextRect = textureRect;
        contextSlot = slotIndex;
      }
    }

    // Mouse up on used slot
    if (Event.current.type == EventType.MouseUp && Event.current.button == 0
        && textureRect.Contains(Event.current.mousePosition) && KISAddonPickup.draggedPart) {
      if (KISAddonPickup.draggedItem != items[slotIndex]) {
        ModuleKISInventory srcInventory = null;
        if (items[slotIndex].stackable
            && items[slotIndex].availablePart.name == KISAddonPickup.draggedPart.partInfo.name) {
          // Stack similar item
          if (KISAddonPickup.draggedItem != null) {
            srcInventory = KISAddonPickup.draggedItem.inventory;
            // Part come from inventory
            bool checkVolume = srcInventory != this;
            if (items[slotIndex].StackAdd(KISAddonPickup.draggedItem.quantity, checkVolume)) {
              KISAddonPickup.draggedItem.Delete();
              items[slotIndex].OnMove(srcInventory, this);
            }
          } else {
            // Part comes from scene.
            if (items[slotIndex].CanStackAdd(1)) {
              ConsumePartFromScene(KISAddonPickup.draggedPart, afterDie: x => {
                items[slotIndex].StackAdd(1);
                items[slotIndex].OnMove(srcInventory, this);
              });
            }
          }
        } else {
          // Exchange part slot
          if (KISAddonPickup.draggedItem != null) {
            if (KISAddonPickup.draggedItem.inventory == items[slotIndex].inventory) {
              KIS_Item srcItem = KISAddonPickup.draggedItem;
              int srcSlot = srcItem.slot;
              srcInventory = KISAddonPickup.draggedItem.inventory;

              KIS_Item destItem = items[slotIndex];
              int destSlot = slotIndex;
              ModuleKISInventory destInventory = this;

              // Move src to dest
              destInventory.items.Remove(destSlot);
              destInventory.items.Add(destSlot, srcItem);
              srcItem.inventory = destInventory;
              destInventory.RefreshMassAndVolume();

              // Move dest to src
              srcInventory.items.Remove(srcSlot);
              srcInventory.items.Add(srcSlot, destItem);
              destItem.inventory = srcInventory;
              srcInventory.RefreshMassAndVolume();
              items[slotIndex].OnMove(srcInventory, this);
            }
          }
        }
      }
    }
  }

  /// <summary>Handles GUI drawing and events in an empty slot.</summary>
  /// <param name="textureRect">Slot's rect in UI.</param>
  /// <param name="slotIndex">Slot's index in inventory.</param>
  void GuiHandleEmptySlot(Rect textureRect, int slotIndex) {
    // Mouse up on a free slot
    if (Event.current.type == EventType.MouseUp && Event.current.button == 0
        && textureRect.Contains(Event.current.mousePosition) && KISAddonPickup.draggedPart) {
      // Check if part can be carried
      bool carryPart = false;
      bool storePart = true;
      var draggedItemModule = KISAddonPickup.draggedPart.GetComponent<ModuleKISItem>();
      if (!draggedItemModule && KISAddonPickup.draggedItem != null) {
        draggedItemModule = KISAddonPickup.draggedItem.prefabModule;
      }

      if (draggedItemModule && draggedItemModule.carriable) {
        if (HighLogic.LoadedSceneIsEditor && podSeat != -1) {
          ScreenMessaging.ShowPriorityScreenMessage(CarriableItemsNotForSeatsInventoryMsg);
          storePart = false;
        } else if (HighLogic.LoadedSceneIsFlight && invType == InventoryType.Eva) {
          carryPart = true;
          foreach (KeyValuePair<int, KIS_Item> enumeratedItem in items) {
            if (enumeratedItem.Value.equipSlot == draggedItemModule.equipSlot
                && enumeratedItem.Value.carriable) {
              if (KISAddonPickup.draggedItem != null) {
                // Ignore self
                if (enumeratedItem.Value == KISAddonPickup.draggedItem) {
                  break;
                }
              }
              carryPart = false;
              storePart = false;
              ScreenMessaging.ShowPriorityScreenMessage(PartAlreadyCarriedMsg);
              break;
            }
          }
        }
      }

      // Store item or part
      if (storePart) {
        if (KISAddonPickup.draggedItem != null) {
          // Picked part from an inventory
          if (carryPart) {
            MoveItem(KISAddonPickup.draggedItem, this, slotIndex);
            if (!KISAddonPickup.draggedItem.equipped) {
              KISAddonPickup.draggedItem.Equip();
            }
          } else {
            if (VolumeAvailableFor(KISAddonPickup.draggedItem)) {
              MoveItem(KISAddonPickup.draggedItem, this, slotIndex);
            }
          }
        } else if (KISAddonPickup.draggedPart != part) {
          // Picked part from scene
          if (carryPart) {
            ConsumePartFromScene(KISAddonPickup.draggedPart, beforeDie: p => {
              KIS_Shared.SendKISMessage(p, KIS_Shared.MessageAction.Store);
              var carryItem = AddItem(p, 1, slotIndex);
              carryItem.Equip();
            });
          } else {
            if (VerifyIsNotAssembly(KISAddonPickup.draggedPart)
                && VolumeAvailableFor(KISAddonPickup.draggedPart)) {
              ConsumePartFromScene(KISAddonPickup.draggedPart, beforeDie: p => {
                KIS_Shared.SendKISMessage(p, KIS_Shared.MessageAction.Store);
                AddItem(p, 1, slotIndex);
              });
            }
          }
        }
      }
    }
  }

  // Sets icon, ensuring any old icon is Disposed
  private void EnableIcon() {
    DisableIcon();
    icon = new KIS_IconViewer(part, selfIconResolution);
  }

  // Clears icon, ensuring it is Disposed
  private void DisableIcon() {
    if (icon != null) {
      icon.Dispose();
      icon = null;
    }
  }

  /// <summary>Properly detaches part from the parent and destroys it.</summary>
  /// <param name="p">Part to destroy.</param>
  /// <param name="beforeDie">
  /// Callback to execute after decouple is complete but before the destruction.
  /// </param>
  /// <param name="afterDie">Callback to execute when part is destroyed.</param>
  IEnumerator AsyncConsumePartFromScene(
      Part p, KIS_Shared.OnPartReady beforeDie, KIS_Shared.OnPartReady afterDie) {
    var formerParent = p.parent;

    yield return KIS_Shared.AsyncDecoupleAssembly(p);
    if (beforeDie != null) {
      beforeDie(p);
    }

    if (!HighLogic.LoadedSceneIsEditor) {
      // Parts in the editor are not connected.
      HostedDebugLog.Info(this, "Destroy consumed part: {0}", p);
      p.Die();

      // Do cleanup in case of we're separating nodes.
      if (formerParent != null) {
        formerParent.FindModulesImplementing<ModuleDockingNode>()
            .Where(x => x.otherNode != null && x.otherNode.part == p)
            .ToList()
            .ForEach(KIS_Shared.ResetDockingNode);
      }
    }
    if (afterDie != null) {
      afterDie(null);
    }
  }

  /// <summary>Convinience method to schedule <see cref="AsyncConsumePartFromScene"/>.</summary>
  void ConsumePartFromScene(
      Part p, KIS_Shared.OnPartReady beforeDie = null, KIS_Shared.OnPartReady afterDie = null) {
    StartCoroutine(AsyncConsumePartFromScene(p, beforeDie, afterDie));
  }
}
  
}  // namespace
