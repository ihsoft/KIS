using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{

    public class ModuleKISInventory : PartModule, IPartCostModifier
    {
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
        public string defaultMoveSndPath = "KIS/Sounds/itemMove";

        public string evaInventoryKey = "tab";
        public string evaRightHandKey = "x";
        public string openGuiName;
        public float totalVolume = 0;
        public int podSeat = 0;
        public InventoryType invType = InventoryType.Container;
        public enum InventoryType { Container, Pod, Eva }
        private float keyPressTime = 0f;
        public delegate void DelayedActionMethod(KIS_Item item);
        public string kerbalTrait;
        private List<KIS_Item> startEquip = new List<KIS_Item>();

        // GUI
        public bool showGui = false;
        GUIStyle lowerRightStyle, upperLeftStyle, upperRightStyle, buttonStyle, boxStyle;
        public Rect guiMainWindowPos;
        private Rect guiDebugWindowPos = new Rect(0, 50, 500, 300);
        private KIS_IconViewer icon;
        public static int OpenInventory = 0;
        private Rect defaultEditorPos = new Rect(Screen.width / 3, 40, 10, 10);
        private Rect defaultFlightPos = new Rect(0, 50, 10, 10);
        private Vector2 scrollPositionDbg;
        private float splitQty = 1;

        //Tooltip
        private KIS_Item tooltipItem;
        private bool mouseOverIcon = false;

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
        public static bool debugContextMenu = false;

        public override string GetInfo()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<b>Max Volume</b>: {0:F0}", maxVolume); sb.AppendLine();
            sb.AppendFormat("<b>Internal access </b>", internalAccess); sb.AppendLine();
            sb.AppendFormat("<b>External access </b>", externalAccess); sb.AppendLine();
            return sb.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.None) return;

            if (HighLogic.LoadedSceneIsEditor)
            {
                InputLockManager.RemoveControlLock("KISInventoryLock");
                guiMainWindowPos = defaultEditorPos;
            }
            else
            {
                guiMainWindowPos = defaultFlightPos;
            }

            Animation[] anim = this.part.FindModelAnimators(openAnimName);
            if (anim.Length > 0)
            {
                openAnim = this.part.FindModelAnimators(openAnimName)[0];
            }

            GameEvents.onCrewTransferred.Add(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(this.OnCrewTransferred));
            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(this.OnVesselChange));
            GameEvents.onPartActionUICreate.Add(new EventData<Part>.OnEvent(this.OnPartActionUICreate));

            if (invType == InventoryType.Eva)
            {
                List<ProtoCrewMember> protoCrewMembers = this.vessel.GetVesselCrew();
                kerbalTrait = protoCrewMembers[0].experienceTrait.Title;
            }
            sndFx.audio = part.gameObject.AddComponent<AudioSource>();
            sndFx.audio.volume = GameSettings.SHIP_VOLUME;
            sndFx.audio.rolloffMode = AudioRolloffMode.Linear;
            sndFx.audio.dopplerLevel = 0f;
            sndFx.audio.panLevel = 1f;
            sndFx.audio.maxDistance = 10;
            sndFx.audio.loop = false;
            sndFx.audio.playOnAwake = false;
            foreach (KIS_Item item in startEquip)
            {
                KIS_Shared.DebugLog("equip " + item.availablePart.name);
                item.Equip();
            }

            RefreshInfo();
        }

        void Update()
        {
            if (showGui)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightGlobals.ActiveVessel.isEVA)
                    {
                        float distEvaToContainer = Vector3.Distance(FlightGlobals.ActiveVessel.transform.position, this.part.transform.position);
                        ModuleKISPickup mPickup = KISAddonPickup.instance.GetActivePickupNearest(this.part);
                        if (mPickup)
                        {
                            if (distEvaToContainer > mPickup.maxDistance)
                            {
                                ShowInventory();
                            }
                        }
                        else
                        {
                            ShowInventory();
                        }
                    }
                }
            }
            UpdateKey();
        }

        void LateUpdate()
        {
            foreach (KeyValuePair<int, KIS_Item> item in items)
            {
                item.Value.Update();
            }
        }

        void UpdateKey()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (FlightGlobals.ActiveVessel != this.part.vessel) return;
            if (!FlightGlobals.ActiveVessel.isEVA) return;

            // Open inventory on keypress
            if (Input.GetKeyDown(evaInventoryKey.ToLower()))
            {
                ShowInventory();
            }
            // Use slot
            slotKeyPress(KeyCode.Alpha1, 0, 1);
            slotKeyPress(KeyCode.Alpha2, 1, 1);
            slotKeyPress(KeyCode.Alpha3, 2, 1);
            slotKeyPress(KeyCode.Alpha4, 3, 1);
            slotKeyPress(KeyCode.Alpha5, 4, 1);
            slotKeyPress(KeyCode.Alpha6, 5, 1);
            slotKeyPress(KeyCode.Alpha7, 6, 1);
            slotKeyPress(KeyCode.Alpha8, 7, 1);
            if (Input.GetKeyDown(evaRightHandKey))
            {
                KIS_Item rightHandItem = GetEquipedItem("rightHand");
                if (rightHandItem != null)
                {
                    rightHandItem.Use(KIS_Item.UseFrom.KeyDown);
                }
            }

            if (Input.GetKeyUp(evaRightHandKey))
            {
                KIS_Item rightHandItem = GetEquipedItem("rightHand");
                if (rightHandItem != null)
                {
                    rightHandItem.Use(KIS_Item.UseFrom.KeyUp);
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            foreach (ConfigNode cn in node.nodes)
            {
                if (cn.name == "ITEM")
                {
                    if (cn.HasValue("partName") && cn.HasValue("slot") && cn.HasValue("quantity"))
                    {
                        string availablePartName = cn.GetValue("partName");
                        AvailablePart availablePart = PartLoader.getPartInfoByName(availablePartName);
                        if (availablePart != null)
                        {
                            int slot = int.Parse(cn.GetValue("slot"));
                            int qty = int.Parse(cn.GetValue("quantity"));
                            KIS_Item item = null;
                            if (cn.HasNode("PART"))
                            {
                                ConfigNode partNode = cn.GetNode("PART");
                                item = AddItem(availablePart, partNode, qty, slot);
                            }
                            else
                            {
                                KIS_Shared.DebugWarning("No part node found on item " + availablePartName + ", creating new one from prefab");
                                item = AddItem(availablePart.partPrefab, qty, slot);

                            }
                            if (cn.HasValue("equipped"))
                            {
                                if (bool.Parse(cn.GetValue("equipped")) && this.invType == InventoryType.Eva)
                                {
                                    startEquip.Add(item);
                                }
                            }
                        }
                        else
                        {
                            KIS_Shared.DebugError("Unable to load " + availablePartName + " from inventory");
                        }
                    }
                    else
                    {
                        KIS_Shared.DebugError("Unable to load an item from inventory");
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (KeyValuePair<int, KIS_Item> item in items)
            {
                ConfigNode nodeD = node.AddNode("ITEM");
                nodeD.AddValue("slot", item.Key);
                nodeD.AddValue("quantity", item.Value.quantity);
                nodeD.AddValue("equipped", item.Value.equipped);
                item.Value.configNode.CopyTo(nodeD);

                // Science recovery works by retrieving all MODULE/ScienceData 
                // subnodes from the part node, so copy all experiments from 
                // contained parts to where it expects to find them. 
                // This duplicates data but allows recovery to work properly. 
                if (item.Value.configNode.HasNode("PART"))
                {
                    foreach (ConfigNode module in item.Value.configNode.GetNode("PART").GetNodes("MODULE"))
                    {
                        foreach (ConfigNode experiment in module.GetNodes("ScienceData"))
                        {
                            experiment.CopyTo(node.AddNode("ScienceData"));
                        }
                    }
                }
            }
        }

        public float GetModuleCost(float defaultCost)
        {
            float itemsCost = 0;
            foreach (KeyValuePair<int, KIS_Item> item in items)
            {
                itemsCost += item.Value.stackCost;
            }
            return itemsCost;
        }

        public void PlaySound(string sndPath, bool loop = false, bool uiSnd = true)
        {
            if (GameDatabase.Instance.ExistsAudioClip(sndPath))
            {
                sndFx.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
                sndFx.audio.loop = loop;
                if (uiSnd)
                {
                    sndFx.audio.volume = GameSettings.UI_VOLUME;
                    sndFx.audio.panLevel = 0;  //set as 2D audiosource
                }
                else
                {
                    sndFx.audio.volume = GameSettings.SHIP_VOLUME;
                    sndFx.audio.panLevel = 1;  //set as 3D audiosource
                }
            }
            else
            {
                KIS_Shared.DebugError("Sound not found in the game database !");
                ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check installation path !", 10, ScreenMessageStyle.UPPER_CENTER);
            }
            sndFx.audio.Play();
        }

        void OnVesselChange(Vessel vess)
        {
            if (showGui)
            {
                ShowInventory();
            }
        }

        void OnCrewTransferred(GameEvents.HostedFromToAction<ProtoCrewMember, Part> fromToAction)
        {
            if (fromToAction.from == this.part)
            {
                if (invType == InventoryType.Pod && fromToAction.to.vessel.isEVA)
                {
                    // pod to eva
                    KIS_Shared.DebugLog("Pod to eva");
                    InternalSeat seat = this.part.internalModel.seats[podSeat];
                    if (items.Count > 0 && !seat.taken)
                    {
                        ModuleKISInventory destInventory = fromToAction.to.GetComponent<ModuleKISInventory>();
                        KIS_Shared.DebugLog("Item transfer | source " + this.part.name + " (" + this.podSeat + ")");
                        KIS_Shared.DebugLog("Item transfer | destination :" + destInventory.part.name);
                        MoveItems(this.items, destInventory);
                        this.RefreshInfo();
                        destInventory.RefreshInfo();
                    }
                }
                if (invType == InventoryType.Pod && !fromToAction.to.vessel.isEVA)
                {
                    // pod to pod
                    KIS_Shared.DebugLog("Pod to pod");
                    InternalSeat seat = this.part.internalModel.seats[podSeat];
                    if (items.Count > 0 && !seat.taken)
                    {
                        KIS_Shared.DebugLog("Item transfer | source :" + this.part.name + " (" + podSeat + ")");
                        foreach (ModuleKISInventory destInventory in fromToAction.to.GetComponents<ModuleKISInventory>())
                        {
                            StartCoroutine(destInventory.WaitAndTransferItems(this.items, fromToAction.host, this));
                        }
                    }
                }
            }

            if (fromToAction.to == this.part)
            {
                if (invType == InventoryType.Pod && fromToAction.from.vessel.isEVA)
                {
                    // eva to pod
                    KIS_Shared.DebugLog("Eva to pod");
                    ModuleKISInventory evaInventory = fromToAction.from.GetComponent<ModuleKISInventory>();
                    Dictionary<int, KIS_Item> transferedItems = new Dictionary<int, KIS_Item>(evaInventory.items);
                    KIS_Shared.DebugLog("Item transfer | source " + fromToAction.host.name);
                    foreach (KeyValuePair<int, KIS_Item> item in evaInventory.items)
                    {
                        if (item.Value.equipped) item.Value.Unequip();
                    }
                    StartCoroutine(WaitAndTransferItems(transferedItems, fromToAction.host));
                }
            }
        }

        private IEnumerator WaitAndTransferItems(Dictionary<int, KIS_Item> transferedItems, ProtoCrewMember protoCrew, ModuleKISInventory srcInventory = null)
        {
            yield return new WaitForFixedUpdate();
            InternalSeat seat = this.part.internalModel.seats[podSeat];
            if (seat.crew == protoCrew)
            {
                MoveItems(transferedItems, this);
                KIS_Shared.DebugLog("Item transfer | destination :" + this.part.name + " (" + this.podSeat + ")");
                this.RefreshInfo();
                if (srcInventory) srcInventory.RefreshInfo();
            }
        }

        private void OnPartActionUICreate(Part p)
        {
            if (this.part != p) return;
            // Update context menu
            if (invType == InventoryType.Pod)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    Events["ShowInventory"].guiActive = Events["ShowInventory"].guiActiveUnfocused = true;
                    Events["ShowInventory"].guiName = "Open seat " + podSeat + " inventory";
                }
                else
                {
                    Events["ShowInventory"].guiActive = Events["ShowInventory"].guiActiveUnfocused = false;
                    foreach (ProtoCrewMember pcm in this.part.protoModuleCrew)
                    {
                        if (pcm.seatIdx == podSeat)
                        {
                            string kerbalName = pcm.name.Split(' ').FirstOrDefault();
                            Events["ShowInventory"].guiActive = Events["ShowInventory"].guiActiveUnfocused = true;
                            Events["ShowInventory"].guiName = "Open " + kerbalName + "'s inventory";// (" + podSeat + ")";
                        }
                    }
                }
            }
            else
            {
                Events["ShowInventory"].guiActive = Events["ShowInventory"].guiActiveUnfocused = true;
                Events["ShowInventory"].guiName = "Open inventory";
            }
        }

        public void RefreshInfo()
        {
            // Reset mass to default
            if (invType == InventoryType.Eva)
            {
                //partPrefab seem to don't exist on eva
                //AssetBase.GetPrefab("kerbal") or PartLoader.getPartInfoByName("kerbalEVA").partPrefab.mass;
                this.part.mass = 0.094f;
            }
            else
            {
                this.part.mass = this.part.partInfo.partPrefab.mass + this.part.GetResourceMass();
            }

            // Update mass
            foreach (ModuleKISInventory inventory in this.part.GetComponents<ModuleKISInventory>())
            {
                this.part.mass += inventory.GetContentMass();
            }

            // Update volume
            totalVolume = GetContentVolume();

            // Update vessel cost in editor
            if (HighLogic.LoadedSceneIsEditor) GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        public void OnDestroy()
        {
            GameEvents.onCrewTransferred.Remove(new EventData<GameEvents.HostedFromToAction<ProtoCrewMember, Part>>.OnEvent(this.OnCrewTransferred));
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(this.OnVesselChange));
            GameEvents.onPartActionUICreate.Remove(new EventData<Part>.OnEvent(this.OnPartActionUICreate));
            if (HighLogic.LoadedSceneIsEditor) EditorLogic.fetch.Unlock("KISInventoryLock");
        }

        private void slotKeyPress(KeyCode kc, int slot, int delay = 1)
        {
            if (Input.GetKeyDown(kc))
            {
                keyPressTime = Time.time;
            }
            if (Input.GetKey(kc))
            {
                if (Time.time - keyPressTime >= delay)
                {
                    if (items.ContainsKey(slot)) items[slot].Use(KIS_Item.UseFrom.InventoryShortcut);
                    keyPressTime = Mathf.Infinity;
                }
            }
            if (Input.GetKeyUp(kc))
            {
                if (keyPressTime != Mathf.Infinity)
                {
                    if (items.ContainsKey(slot)) items[slot].ShortcutKeyPress();
                }
                keyPressTime = 0;
            }
        }

        public KIS_Item AddItem(AvailablePart availablePart, ConfigNode partNode, float qty = 1, int slot = -1)
        {
            KIS_Item item = null;
            if (slot < 0)
            {
                slot = GetFreeSlot();
            }
            if (slot >= 0)
            {
                item = new KIS_Item(availablePart, partNode, this, qty);
                items.Add(slot, item);
                if (showGui) items[slot].EnableIcon(itemIconResolution);
                RefreshInfo();
            }
            return item;
        }

        public KIS_Item AddItem(Part part, float qty = 1, int slot = -1)
        {
            KIS_Item item = null;
            if (slot < 0)
            {
                slot = GetFreeSlot();
            }
            if (slot >= 0)
            {
                item = new KIS_Item(part, this, qty);
                items.Add(slot, item);
                if (showGui) items[slot].EnableIcon(itemIconResolution);
                RefreshInfo();
            }
            return item;
        }

        public void DeleteItem(int slot)
        {
            if (items.ContainsKey(slot))
            {
                items[slot].StackRemove();
            }
        }

        public bool isFull()
        {
            if (GetFreeSlot() < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public KIS_Item GetEquipedItem(string equipSlot)
        {
            foreach (KeyValuePair<int, KIS_Item> item in this.items)
            {
                if (item.Value.equipped && item.Value.equipSlot == equipSlot)
                {
                    return item.Value;
                }
            }
            return null;
        }

        public int GetFreeSlot()
        {
            int nbSlot = slotsX * slotsY;
            for (int i = 0; i <= nbSlot - 1; i++)
            {
                if (items.ContainsKey(i) == false)
                {
                    return i;
                }
            }
            return -1;
        }

        public void MoveItems(Dictionary<int, KIS_Item> srcItems, ModuleKISInventory destInventory)
        {
            destInventory.items.Clear();
            destInventory.items = new Dictionary<int, KIS_Item>(srcItems);
            foreach (KeyValuePair<int, KIS_Item> item in destInventory.items)
            {
                item.Value.inventory = destInventory;
            }
            srcItems.Clear();
            srcItems = null;
        }

        public float GetContentMass()
        {
            float contentMass = 0;
            foreach (KeyValuePair<int, KIS_Item> item in items)
            {
                contentMass += item.Value.stackMass;
            }
            return contentMass;
        }

        public float GetContentVolume()
        {
            float contentVolume = 0;
            foreach (KeyValuePair<int, KIS_Item> item in items)
            {
                contentVolume += item.Value.stackVolume;
            }
            return contentVolume;
        }

        private bool VolumeAvailableFor(Part p)
        {
            ModuleKISItem mItem = p.GetComponent<ModuleKISItem>();
            if (mItem)
            {
                if (mItem.volumeOverride > 0)
                {
                    float newTotalVolume = GetContentVolume() + mItem.volumeOverride;
                    if (newTotalVolume > maxVolume)
                    {
                        ScreenMessages.PostScreenMessage("Max destination volume reached. Part volume is : " + KIS_Shared.GetVolumeText(mItem.volumeOverride) + " (+" + KIS_Shared.GetVolumeText((newTotalVolume - maxVolume)) + ")", 5, ScreenMessageStyle.UPPER_CENTER);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            float newTotalVolume2 = GetContentVolume() + KIS_Shared.GetPartVolume(p.partInfo.partPrefab);
            if (newTotalVolume2 > maxVolume)
            {
                ScreenMessages.PostScreenMessage("Max destination volume reached. Part volume is : " + KIS_Shared.GetVolumeText(KIS_Shared.GetPartVolume(p.partInfo.partPrefab)) + " (+" + KIS_Shared.GetVolumeText((newTotalVolume2 - maxVolume)) + ")", 5, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }
            else
            {
                return true;
            }

        }

        private bool VolumeAvailableFor(KIS_Item item)
        {
            RefreshInfo();
            if (KISAddonPickup.draggedItem.inventory == this)
            {
                return true;
            }
            else
            {
                float newTotalVolume = GetContentVolume() + item.stackVolume;
                if (newTotalVolume > maxVolume)
                {
                    ScreenMessages.PostScreenMessage("Max destination volume reached. Part volume is : " + KIS_Shared.GetVolumeText(item.stackVolume) + " (+" + KIS_Shared.GetVolumeText((newTotalVolume - maxVolume)) + ")", 5, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public void DelayedAction(DelayedActionMethod actionMethod, KIS_Item item, float delay)
        {
            StartCoroutine(WaitAndDoAction(actionMethod, item, delay));
        }

        private IEnumerator WaitAndDoAction(DelayedActionMethod actionMethod, KIS_Item item, float delay)
        {
            yield return new WaitForSeconds(delay);
            actionMethod(item);
        }

        [KSPEvent(name = "ContextMenuShowInventory", guiActiveEditor = true, active = true, guiActive = true, guiActiveUnfocused = true, guiName = "")]
        public void ShowInventory()
        {
            if (showGui)
            {
                // Destroy icons viewer
                foreach (KeyValuePair<int, KIS_Item> item in items)
                {
                    item.Value.DisableIcon();
                }
                if (openAnim)
                {
                    openAnim[openAnimName].speed = -openAnimSpeed;
                    openAnim.Play(openAnimName);
                }
                icon = null;
                showGui = false;
                OpenInventory--;
                if (HighLogic.LoadedSceneIsEditor)
                {
                    PlaySound(closeSndPath);
                }
                else
                {
                    PlaySound(closeSndPath, false, false);
                }
            }
            else
            {
                // Check if inventory can be opened from interior/exterior
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (FlightGlobals.ActiveVessel.isEVA && !externalAccess)
                    {
                        ScreenMessages.PostScreenMessage("This storage is not accessible from the outside !", 4, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                    if (!FlightGlobals.ActiveVessel.isEVA && !internalAccess)
                    {
                        ScreenMessages.PostScreenMessage("This storage is not accessible from the inside !", 4, ScreenMessageStyle.UPPER_CENTER);
                        return;
                    }
                }

                // Create icons viewer
                foreach (KeyValuePair<int, KIS_Item> item in items)
                {
                    item.Value.EnableIcon(itemIconResolution);
                }
                icon = new KIS_IconViewer(this.part, selfIconResolution);
                
                if (OpenInventory == 1 && guiMainWindowPos.x == defaultFlightPos.x && guiMainWindowPos.y == defaultFlightPos.y)
                {
                    guiMainWindowPos.y += 250;
                }
                if (openAnim)
                {
                    openAnim[openAnimName].speed = openAnimSpeed;
                    openAnim.Play(openAnimName);
                }
                showGui = true;
                OpenInventory++;
                if (HighLogic.LoadedSceneIsEditor)
                {
                    PlaySound(openSndPath);
                }
                else
                {
                    PlaySound(openSndPath, false, false);
                }
            }
        }

        private void GUIStyles()
        {
            GUI.skin = HighLogic.Skin;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 11;
            boxStyle.padding.top = GUI.skin.box.padding.bottom = GUI.skin.box.padding.left = 5;
            boxStyle.alignment = TextAnchor.UpperLeft;

            upperRightStyle = new GUIStyle(GUI.skin.label);
            upperRightStyle.alignment = TextAnchor.UpperRight;
            upperRightStyle.fontSize = 9;
            upperRightStyle.normal.textColor = Color.yellow;

            upperLeftStyle = new GUIStyle(GUI.skin.label);
            upperLeftStyle.alignment = TextAnchor.UpperLeft;
            upperLeftStyle.fontSize = 11;
            upperLeftStyle.normal.textColor = Color.green;

            lowerRightStyle = new GUIStyle(GUI.skin.label);
            lowerRightStyle.alignment = TextAnchor.LowerRight;
            lowerRightStyle.fontSize = 10;
            lowerRightStyle.padding = new RectOffset(4, 4, 4, 4);
            lowerRightStyle.normal.textColor = Color.white;


            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }

        private void OnGUI()
        {
            if (!showGui) return;

            // Update GUI of items
            foreach (KeyValuePair<int, KIS_Item> item in items)
            {
                item.Value.GUIUpdate();
            }

            GUIStyles();

            guiMainWindowPos = GUILayout.Window(GetInstanceID(), guiMainWindowPos, GuiMain, "Inventory");
            
            if (mouseOverIcon)
            {
                if (contextItem == null)
                {
                    GUILayout.Window(GetInstanceID() + 780, new Rect(Event.current.mousePosition.x + 5, Event.current.mousePosition.y + 5, 400, 1), GuiTooltip, tooltipItem.availablePart.title);
                }
            }
            if (contextItem != null)
            {
                Rect contextRelativeRect = new Rect(guiMainWindowPos.x + contextRect.x + (contextRect.width / 2), guiMainWindowPos.y + contextRect.y + (contextRect.height / 2), 80, 10);
                GUILayout.Window(GetInstanceID() + 781, contextRelativeRect, GuiContextMenu, "Action");
                if (contextClick)
                {
                    contextClick = false;
                    splitQty = 1;
                }
                else if (Event.current.type == EventType.MouseDown)
                {
                    contextItem = null;
                }
            }

            if (debugItem != null)
            {
                guiDebugWindowPos = GUILayout.Window(GetInstanceID() + 782, guiDebugWindowPos, GuiDebugItem, "Debug item");
            }

            // Disable Editor Click through
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (guiMainWindowPos.Contains(Input.mousePosition))
                {
                    EditorTooltip.Instance.HideToolTip();
                    EditorLogic.fetch.Lock(false, false, false, "KISInventoryLock");
                }
                else if (!guiMainWindowPos.Contains(Input.mousePosition))
                {
                    EditorLogic.fetch.Unlock("KISInventoryLock");
                }
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                // Hide part rightclick menu.
                //if (!GUIUtility.hotControl.IsNull())
                //{
                if (guiMainWindowPos.Contains(Input.mousePosition) && GUIUtility.hotControl == 0)
                {
                    foreach (var window in GameObject.FindObjectsOfType(typeof(UIPartActionWindow))
                                  .OfType<UIPartActionWindow>().Where(p => p.Display == UIPartActionWindow.DisplayType.Selected))
                    {
                        window.enabled = false;
                        window.displayDirty = true;
                    }
                }
                //}
            }
        }

        private void GuiMain(int windowID)
        {
            GUIStyle guiStyleTitle = new GUIStyle(GUI.skin.label);
            guiStyleTitle.normal.textColor = Color.yellow;
            guiStyleTitle.fontStyle = FontStyle.Bold;
            guiStyleTitle.fontSize = 14;
            guiStyleTitle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            int width = 150;
            GUILayout.Box("", GUILayout.Width(width), GUILayout.Height(100));
            Rect textureRect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(textureRect, icon.texture, ScaleMode.ScaleToFit);
            GUILayout.Space(2);
            string title = this.part.partInfo.title;
            if (invType == InventoryType.Pod)
            {
                title = this.part.partInfo.title + " (" + podSeat + ")";
                if (!HighLogic.LoadedSceneIsEditor)
                {
                    if (this.part.internalModel)
                    {
                        InternalSeat seat = this.part.internalModel.seats[podSeat];
                        if (seat.taken)
                        {
                            title = seat.kerbalRef.crewMemberName;
                        }
                    }
                }
            }
            GUILayout.Label(title, guiStyleTitle, GUILayout.Width(width), GUILayout.Height(10));
            string text = kerbalTrait + "\n";
            text += "Mass : " + this.part.mass.ToString("0.000") + "\n";
            text += "Volume : " + KIS_Shared.GetVolumeText(this.totalVolume) + "/" + KIS_Shared.GetVolumeText(this.maxVolume) + "\n";
            GUILayout.Box(text, boxStyle, GUILayout.Width(width), GUILayout.Height(50));

            bool closeInv = false;
            if (GUILayout.Button(new GUIContent("Close", "Close container"), GUILayout.Width(width), GUILayout.Height(25)))
            {
                closeInv = true;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GuiInventory(windowID);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint && mouseOverIcon == false && tooltipItem != null)
            {
                tooltipItem.icon.ResetPos();
                tooltipItem = null;
            }
            if (contextItem == null) GUI.DragWindow(new Rect(0, 0, 10000, 30));
            if (closeInv)
            {
                ShowInventory();
            }
        }

        private void GuiDebugItem(int windowID)
        {
            if (debugItem != null)
            {
                KIS_Shared.EditField("moveSndPath", ref debugItem.prefabModule.moveSndPath);
                KIS_Shared.EditField("shortcutKeyAction(drop,equip,custom)", ref debugItem.prefabModule.shortcutKeyAction);
                KIS_Shared.EditField("usableFromEva", ref debugItem.prefabModule.usableFromEva);
                KIS_Shared.EditField("usableFromContainer", ref debugItem.prefabModule.usableFromContainer);
                KIS_Shared.EditField("usableFromPod", ref debugItem.prefabModule.usableFromPod);
                KIS_Shared.EditField("usableFromEditor", ref debugItem.prefabModule.usableFromEditor);
                KIS_Shared.EditField("useName", ref debugItem.prefabModule.useName);
                KIS_Shared.EditField("equipMode(model,physic)", ref debugItem.prefabModule.equipMode);
                KIS_Shared.EditField("equipSlot", ref debugItem.prefabModule.equipSlot);
                KIS_Shared.EditField("equipable", ref debugItem.prefabModule.equipable);
                KIS_Shared.EditField("stackable", ref debugItem.prefabModule.stackable);
                KIS_Shared.EditField("equipTrait(<blank>,pilot,scientist,engineer)", ref debugItem.prefabModule.equipTrait);
                KIS_Shared.EditField("equipRemoveHelmet", ref debugItem.prefabModule.equipRemoveHelmet);
                KIS_Shared.EditField("volumeOverride(0 = auto)", ref debugItem.prefabModule.volumeOverride);

                scrollPositionDbg = GUILayout.BeginScrollView(scrollPositionDbg, GUILayout.Width(400), GUILayout.Height(200));
                List<SkinnedMeshRenderer> skmrs = new List<SkinnedMeshRenderer>(this.part.GetComponentsInChildren<SkinnedMeshRenderer>() as SkinnedMeshRenderer[]);
                foreach (SkinnedMeshRenderer skmr in skmrs)
                {
                    GUILayout.Label("--- " + skmr.name + " ---");
                    foreach (Transform bone in skmr.bones)
                    {
                        if (GUILayout.Button(new GUIContent(bone.name, "")))
                        {
                            debugItem.prefabModule.equipMeshName = skmr.name;
                            debugItem.prefabModule.equipBoneName = bone.name;
                            debugItem.ReEquip();
                        }
                    }
                }

                GUILayout.EndScrollView();
                if (KIS_Shared.EditField("equipPos", ref debugItem.prefabModule.equipPos)) debugItem.ReEquip();
                if (KIS_Shared.EditField("equipDir", ref debugItem.prefabModule.equipDir)) debugItem.ReEquip();
            }
            if (GUILayout.Button("Close"))
            {
                debugItem = null;
            }
            GUI.DragWindow();
        }

        private void GuiContextMenu(int windowID)
        {
            GUI.FocusWindow(windowID);
            GUI.BringWindowToFront(windowID);
            bool noAction = true;

            //Equip
            if (contextItem != null)
            {
                if (contextItem.equipable && invType == InventoryType.Eva)
                {
                    noAction = false;
                    if (contextItem.equipped)
                    {
                        if (GUILayout.Button("Unequip"))
                        {
                            contextItem.Unequip();
                            contextItem = null;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Equip"))
                        {
                            contextItem.Equip();
                            contextItem = null;
                        }
                    }
                }
            }

            //Set stack quantity (editor only)
            if (contextItem != null && HighLogic.LoadedSceneIsEditor)
            {
                noAction = false;
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", buttonStyle, GUILayout.Width(20)))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (contextItem.quantity - 10 > 0)
                        {
                            if (contextItem.StackRemove(10) == false) contextItem = null;
                        }
                    }
                    else if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (contextItem.quantity - 100 > 0)
                        {
                            if (contextItem.StackRemove(100) == false) contextItem = null;
                        }
                    }
                    else
                    {
                        if (contextItem.quantity - 1 > 0)
                        {
                            if (contextItem.StackRemove(1) == false) contextItem = null;
                        }
                    }
                }
                if (GUILayout.Button("+", buttonStyle, GUILayout.Width(20)))
                {
                    if (contextItem.stackable)
                    {
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            contextItem.StackAdd(10);
                        }
                        else if (Input.GetKey(KeyCode.LeftControl))
                        {
                            contextItem.StackAdd(100);
                        }
                        else
                        {
                            contextItem.StackAdd(1);
                        }
                    }
                }
                if (contextItem != null) GUILayout.Label("Quantity : " + contextItem.quantity, GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }

            //Split
            if (contextItem != null && !HighLogic.LoadedSceneIsEditor)
            {
                if (contextItem.quantity > 1)
                {
                    noAction = false;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("-", buttonStyle, GUILayout.Width(20)))
                    {
                        if (splitQty - 1 > 0) splitQty -= 1;
                    }
                    if (GUILayout.Button("Split (" + splitQty + ")", buttonStyle))
                    {
                        if (this.isFull() == false)
                        {
                            contextItem.quantity -= splitQty;
                            AddItem(contextItem.availablePart.partPrefab, splitQty);
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage("Inventory is full, cannot split !", 5, ScreenMessageStyle.UPPER_CENTER);
                        }
                        contextItem = null;
                    }
                    if (GUILayout.Button("+", buttonStyle, GUILayout.Width(20)))
                    {
                        if (splitQty + 1 < contextItem.quantity) splitQty += 1;
                    }
                    GUILayout.EndHorizontal();
                }
            }

            // Use
            if (contextItem != null)
            {
                if ((HighLogic.LoadedSceneIsFlight && contextItem.usableFromEva && contextItem.inventory.invType == InventoryType.Eva)
                    || (HighLogic.LoadedSceneIsFlight && contextItem.usableFromContainer && contextItem.inventory.invType == InventoryType.Container)
                    || (HighLogic.LoadedSceneIsFlight && contextItem.usableFromPod && contextItem.inventory.invType == InventoryType.Pod)
                    || (HighLogic.LoadedSceneIsEditor && contextItem.usableFromEditor))
                {
                    noAction = false;
                    if (GUILayout.Button(contextItem.prefabModule.useName))
                    {
                        contextItem.Use(KIS_Item.UseFrom.ContextMenu);
                        contextItem = null;
                    }
                }
            }

            //Debug
            if (debugContextMenu)
            {
                if (contextItem != null && !HighLogic.LoadedSceneIsEditor && invType == InventoryType.Eva)
                {
                    if (contextItem.prefabModule != null)
                    {
                        noAction = false;
                        if (GUILayout.Button("Debug"))
                        {
                            debugItem = contextItem;
                            contextItem = null;
                        }
                    }
                }
            }
            if (noAction)
            {
                GUILayout.Label("No action");
            }
        }

        private void GuiTooltip(int windowID)
        {
            if (tooltipItem == null) return;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Box("", GUILayout.Width(100), GUILayout.Height(100));
            Rect textureRect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(textureRect, tooltipItem.icon.texture, ScaleMode.ScaleToFit);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            string text = "Cost : " + tooltipItem.availablePart.cost + " √" + "\n";
            text += "Volume : " + KIS_Shared.GetVolumeText(tooltipItem.volume) + "\n";
            text += "Stackable : " + tooltipItem.stackable + "\n";
            text += "Dry mass : " + tooltipItem.availablePart.partPrefab.mass + "\n";
            if (tooltipItem.equipSlot != null)
            {
                text += "Equip slot : " + tooltipItem.equipSlot + "\n";
                if (tooltipItem.equipSlot == "rightHand") text += "Press [" + evaRightHandKey + "] to use (equipped)" + "\n";
            }
            float rscMass = tooltipItem.availablePart.partPrefab.GetResourceMass();
            if (rscMass == 0)
            {
                text += "Ressource mass : " + tooltipItem.availablePart.partPrefab.GetResourceMass() + "\n";
            }
            GUILayout.Box(text, boxStyle, GUILayout.Width(150), GUILayout.Height(100));
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            string text2 = "";

            if (tooltipItem.quantity > 1)
            {
                // Show total if stacked
                GUI.Label(textureRect, "x" + tooltipItem.quantity.ToString() + " ", lowerRightStyle);
                text2 += "Total cost : " + tooltipItem.stackCost + " √" + "\n";
                text2 += "Total volume : " + KIS_Shared.GetVolumeText(tooltipItem.stackVolume) + "\n";
                text2 += "Total mass : " + tooltipItem.stackMass + "\n";
            }
            else
            {
                // Show resource if not stacked
                List<KIS_Item.ResourceInfo> resources = tooltipItem.GetResources();
                if (resources.Count > 0)
                {
                    foreach (KIS_Item.ResourceInfo resource in resources)
                    {
                        text2 += resource.resourceName + " : " + resource.amount.ToString("0.000") + " / " + resource.maxAmount.ToString("0.000") + "\n";
                    }
                }
                else
                {
                    text2 = "Part has no resources";
                }
            }
            GUILayout.Box(text2, boxStyle, GUILayout.Width(200), GUILayout.Height(100));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Box(tooltipItem.availablePart.description, boxStyle, GUILayout.Width(450), GUILayout.Height(100));
        }

        private void GuiInventory(int windowID)
        {
            mouseOverIcon = false;
            int i = 0;
            for (int x = 0; x < slotsY; x++)
            {
                GUILayout.BeginHorizontal();
                for (int y = 0; y < slotsX; y++)
                {
                    GUILayout.Box("", GUILayout.Width(slotSize), GUILayout.Height(slotSize));
                    Rect textureRect = GUILayoutUtility.GetLastRect();

                    if (items.ContainsKey(i))
                    {
                        GUI.DrawTexture(textureRect, items[i].icon.texture, ScaleMode.ScaleToFit);
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            if (FlightGlobals.ActiveVessel.isEVA && FlightGlobals.ActiveVessel == this.part.vessel)
                            {
                                // Keyboard shortcut
                                int slotNb = i + 1;
                                GUI.Label(textureRect, " " + slotNb.ToString(), upperLeftStyle);
                                if (items[i].equipped)
                                {
                                    GUI.Label(textureRect, " Equip.  ", upperRightStyle);
                                }
                            }
                        }
                        if (items[i].stackable)
                        {
                            // Quantity
                            GUI.Label(textureRect, "x" + items[i].quantity.ToString() + "  ", lowerRightStyle);
                        }

                        if (Event.current.type == EventType.MouseDown && textureRect.Contains(Event.current.mousePosition))
                        {
                            // Pickup part
                            if (Event.current.button == 0)
                            {
                                KISAddonPickup.instance.Pickup(items[i]);
                            }
                            // Context menu
                            if (Event.current.button == 1)
                            {
                                contextClick = true;
                                contextItem = items[i];
                                contextRect = textureRect;
                                contextSlot = i;
                            }
                        }

                        // Mouse over a slot
                        if (Event.current.type == EventType.Repaint && textureRect.Contains(Event.current.mousePosition) && !KISAddonPickup.draggedPart)
                        {
                            mouseOverIcon = true;
                            tooltipItem = items[i];
                            tooltipItem.icon.Rotate();
                        }
                        // Mouse up on used slot
                        if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && textureRect.Contains(Event.current.mousePosition) && KISAddonPickup.draggedPart)
                        {
                            if (KISAddonPickup.draggedItem != items[i])
                            {
                                ModuleKISInventory srcInventory = null;
                                if (items[i].stackable && items[i].availablePart == KISAddonPickup.draggedPart.partInfo)
                                {
                                    // Stack similar item
                                    if (KISAddonPickup.draggedItem != null)
                                    {
                                        srcInventory = KISAddonPickup.draggedItem.inventory;
                                        // Part come from inventory
                                        if (items[i].StackAdd(KISAddonPickup.draggedItem.quantity))
                                        {
                                            KISAddonPickup.draggedItem.Delete();
                                        }
                                    }
                                    else
                                    {
                                        // Part come from scene
                                        if (items[i].StackAdd(1))
                                        {
                                            KISAddonPickup.draggedPart.Die();
                                        }
                                    }
                                }
                                else
                                {
                                    // Exchange part slot
                                    if (KISAddonPickup.draggedItem != null)
                                    {
                                        if (KISAddonPickup.draggedItem.inventory == items[i].inventory)
                                        {
                                            KIS_Item srcItem = KISAddonPickup.draggedItem;
                                            int srcSlot = srcItem.slot;
                                            srcInventory = KISAddonPickup.draggedItem.inventory;

                                            KIS_Item destItem = items[i];
                                            int destSlot = i;
                                            ModuleKISInventory destInventory = this;

                                            // Move src to dest
                                            destInventory.items.Remove(destSlot);
                                            destInventory.items.Add(destSlot, srcItem);
                                            srcItem.inventory = destInventory;
                                            destInventory.RefreshInfo();

                                            // Move dest to src
                                            srcInventory.items.Remove(srcSlot);
                                            srcInventory.items.Add(srcSlot, destItem);
                                            destItem.inventory = srcInventory;
                                            srcInventory.RefreshInfo();
                                        }
                                    }
                                }
                                items[i].OnMove(srcInventory, this);
                            }
                        }
                    }
                    else
                    {
                        // Mouse up on a free slot
                        //if (Event.current.type == EventType.Repaint && Input.GetMouseButtonUp(0) && textureRect.Contains(Event.current.mousePosition) && KISAddonPickup.draggedPart)
                        if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && textureRect.Contains(Event.current.mousePosition) && KISAddonPickup.draggedPart)
                        {
                            ModuleKISInventory srcInventory = null;
                            if (KISAddonPickup.draggedItem != null)
                            {
                                srcInventory = KISAddonPickup.draggedItem.inventory;
                                if (VolumeAvailableFor(KISAddonPickup.draggedItem))
                                {
                                    // Picked part from an inventory
                                    KIS_Item movingItem = KISAddonPickup.draggedItem;
                                    int slot = movingItem.slot;
                                    this.items.Add(i, movingItem);
                                    movingItem.inventory.items.Remove(slot);
                                    movingItem.inventory.RefreshInfo();
                                    movingItem.inventory = this;
                                    RefreshInfo();
                                    items[i].OnMove(srcInventory, this);
                                }
                            }
                            else
                            {
                                if (VolumeAvailableFor(KISAddonPickup.draggedPart))
                                {
                                    // Picked part from scene
                                    AddItem(KISAddonPickup.draggedPart, 1, i);
                                    if (HighLogic.LoadedSceneIsEditor == false)
                                    {
                                        KISAddonPickup.draggedPart.Die();
                                    }
                                    items[i].OnMove(srcInventory, this);
                                }
                            }           
                        }
                    }
                    i++;
                }
                GUILayout.EndHorizontal();
            }
        }

    }
}