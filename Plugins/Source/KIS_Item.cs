using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{
    public class KIS_Item
    {
        public ConfigNode configNode;
        public AvailablePart availablePart;
        public float quantity;
        public KIS_IconViewer icon;
        public bool stackable = false;
        public string equipSlot;
        public bool usableFromEva = false;
        public bool usableFromContainer = false;
        public bool usableFromPod = false;
        public bool usableFromEditor = false;
        public bool carryable = false;
        public float volume;
        public bool equipable = false;
        public bool equipped = false;
        public ModuleKISInventory inventory;
        public ModuleKISItem prefabModule;
        private GameObject equippedModel;
        private Part equippedPart;
        Transform evaTransform;
        public enum ActionType { Drop, Equip, Custom }
        public enum UseFrom { KeyDown, KeyUp, InventoryShortcut, ContextMenu }
        public enum EquipMode { Model, Physic }
        public float resourceMass = 0;
        public float contentMass = 0;
        public float contentCost = 0;

        public struct ResourceInfo
        {
            public string resourceName;
            public double amount;
            public double maxAmount;
        }

        public EquipMode equipMode
        {
            get
            {
                EquipMode mode = EquipMode.Model;
                if (prefabModule)
                {
                    if (prefabModule.equipMode == "physic")
                    {
                        mode = EquipMode.Physic;
                    }
                }
                return mode;
            }
        }

        public ActionType shortcutAction
        {
            get
            {
                ActionType mode = ActionType.Drop;
                if (prefabModule)
                {
                    if (prefabModule.shortcutKeyAction == "equip")
                    {
                        mode = ActionType.Equip;
                    }
                    if (prefabModule.shortcutKeyAction == "custom")
                    {
                        mode = ActionType.Custom;
                    }
                }
                return mode;
            }
        }

        public bool carried
        {
            get
            {
                if (carryable && inventory.invType == ModuleKISInventory.InventoryType.Eva && HighLogic.LoadedSceneIsFlight)
                {
                    return true;
                }
                return false;
            }
        }

        public int slot { get { return inventory.items.FirstOrDefault(x => x.Value == this).Key; } }
        public float stackVolume { get { return volume * quantity; } }
        public float dryMass { get { return availablePart.partPrefab.mass; } }
        public float stackDryMass { get { return dryMass * quantity; } }
        public float cost { get { return availablePart.cost; } }
        public float totalCost { get { return (cost + contentCost) * quantity; } }
        public float totalMass { get { return stackDryMass + resourceMass + contentMass; } }

        public KIS_Item(AvailablePart availablePart, ConfigNode itemNode, ModuleKISInventory inventory, float quantity = 1) // Part from save
        {
            // Get part node
            this.availablePart = availablePart;
            this.configNode = new ConfigNode();
            this.configNode.AddValue("partName", this.availablePart.name);
            ConfigNode newPartNode = this.configNode.AddNode("PART");
            ConfigNode partNode = itemNode.GetNode("PART");
            partNode.CopyTo(newPartNode);
            // init config
            this.InitConfig(availablePart, inventory, quantity);
            // Get mass
            if (itemNode.HasValue("resourceMass")) resourceMass = float.Parse(itemNode.GetValue("resourceMass"));
            else resourceMass = availablePart.partPrefab.GetResourceMass();
            if (itemNode.HasValue("contentMass")) contentMass = float.Parse(itemNode.GetValue("contentMass"));
            if (itemNode.HasValue("contentCost")) contentCost = float.Parse(itemNode.GetValue("contentCost"));
        }

        public KIS_Item(Part part, ModuleKISInventory inventory, float quantity = 1) // New part from scene
        {
            // Get part node
            this.availablePart = part.partInfo;
            this.configNode = new ConfigNode();
            this.configNode.AddValue("partName", this.availablePart.name);
            ConfigNode newPartNode = this.configNode.AddNode("PART");
            KIS_Shared.PartSnapshot(part).CopyTo(newPartNode);
            // init config
            this.InitConfig(availablePart, inventory, quantity);
            // Get mass
            this.resourceMass = part.GetResourceMass();
            ModuleKISInventory itemInventory = part.GetComponent<ModuleKISInventory>();
            if (itemInventory)
            {
                this.contentMass = itemInventory.GetContentMass();
                this.contentCost = itemInventory.GetContentCost();
            }
        }

        private void InitConfig(AvailablePart availablePart, ModuleKISInventory inventory, float quantity)
        {
            this.inventory = inventory;
            this.quantity = quantity;
            this.prefabModule = availablePart.partPrefab.GetComponent<ModuleKISItem>();
            this.volume = KIS_Shared.GetPartVolume(availablePart.partPrefab);

            if (this.prefabModule)
            {
                if (this.prefabModule.volumeOverride > 0)
                {
                    this.volume = this.prefabModule.volumeOverride;
                }
                this.equipable = prefabModule.equipable;
                this.stackable = prefabModule.stackable;
                this.equipSlot = prefabModule.equipSlot;
                this.usableFromEva = prefabModule.usableFromEva;
                this.usableFromContainer = prefabModule.usableFromContainer;
                this.usableFromPod = prefabModule.usableFromPod;
                this.usableFromEditor = prefabModule.usableFromEditor;
                this.carryable = prefabModule.carryable;
            }
            int nonStackableModule = 0;
            foreach (PartModule pModule in availablePart.partPrefab.Modules)
            {
                if (!KISAddonConfig.stackableModules.Contains(pModule.moduleName))
                {
                    KIS_Shared.DebugLog("Module <" + pModule.moduleName + "> is not set as stackable in settings.cfg");
                    nonStackableModule++;
                }
            }
            if (nonStackableModule == 0 && GetResources().Count == 0)
            {
                KIS_Shared.DebugLog("No non-stackable module and ressource found on the part, set item as stackable");
                this.stackable = true;
            }
            if (KISAddonConfig.stackableList.Contains(availablePart.name))
            {
                KIS_Shared.DebugLog("Part name present in settings.cfg (node StackableItemOverride), force item as stackable");
                this.stackable = true;
            }
        }

        public List<ResourceInfo> GetResources()
        {
            List<ResourceInfo> resources = new List<ResourceInfo>();
            if (this.configNode.HasNode("PART"))
            {
                foreach (ConfigNode node in this.configNode.GetNode("PART").GetNodes("RESOURCE"))
                {
                    if (node.HasValue("name") && node.HasValue("amount") && node.HasValue("maxAmount"))
                    {
                        ResourceInfo rInfo = new ResourceInfo();
                        rInfo.resourceName = node.GetValue("name");
                        rInfo.amount = double.Parse(node.GetValue("amount"));
                        rInfo.maxAmount = double.Parse(node.GetValue("maxAmount"));
                        resources.Add(rInfo);
                    }
                }
            }
            return resources;
        }

        public void SetResource(string name, double amount)
        {
            if (this.configNode.HasNode("PART"))
            {
                foreach (ConfigNode node in this.configNode.GetNode("PART").GetNodes("RESOURCE"))
                {
                    if (node.HasValue("name") && node.HasValue("amount") && node.HasValue("maxAmount"))
                    {
                        if (node.GetValue("name") == name)
                        {
                            node.SetValue("amount", amount.ToString());
                            return;
                        }
                    }
                }
            }
        }

        public void EnableIcon(int resolution)
        {
            icon = new KIS_IconViewer(availablePart.partPrefab, resolution);
        }

        public void DisableIcon()
        {
            icon = null;
        }

        public void Update()
        {
            if (evaTransform)
            {
                equippedModel.transform.rotation = evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
                equippedModel.transform.position = evaTransform.TransformPoint(prefabModule.equipPos);
            }
            if (prefabModule) prefabModule.OnItemUpdate(this);
        }

        public void GUIUpdate()
        {
            if (prefabModule) prefabModule.OnItemGUI(this);
        }

        public void PlaySound(string sndPath, bool loop = false)
        {
            inventory.PlaySound(sndPath, loop);
        }

        public bool StackAdd(float qty)
        {
            if (qty <= 0) return false;
            float newVolume = inventory.totalVolume + (volume * qty);
            if (newVolume > inventory.maxVolume)
            {
                ScreenMessages.PostScreenMessage("Max destination volume reached (+" + (newVolume - inventory.maxVolume).ToString("0.0000") + ")", 5f);
                return false;
            }
            else
            {
                quantity += qty;
                inventory.RefreshMassAndVolume();
                return true;
            }
        }

        public bool StackRemove(float qty = 1)
        {
            if (qty <= 0) return false;
            if (quantity - qty <= 0)
            {
                Delete();
                return false;
            }
            else
            {
                quantity -= qty;
                inventory.RefreshMassAndVolume();
                return true;
            }
        }

        public void Delete()
        {
            if (inventory.showGui) DisableIcon();
            if (equipped) Unequip();
            inventory.items.Remove(slot);
            inventory.RefreshMassAndVolume();
        }

        public void ShowHelmet()
        {
            List<SkinnedMeshRenderer> skmrs = new List<SkinnedMeshRenderer>(inventory.part.GetComponentsInChildren<SkinnedMeshRenderer>() as SkinnedMeshRenderer[]);
            foreach (SkinnedMeshRenderer skmr in skmrs)
            {
                if (skmr.name == "helmet" || skmr.name == "visor")
                {
                    skmr.renderer.enabled = true;
                }
            }
        }

        public void HideHelmet()
        {
            List<SkinnedMeshRenderer> skmrs = new List<SkinnedMeshRenderer>(inventory.part.GetComponentsInChildren<SkinnedMeshRenderer>() as SkinnedMeshRenderer[]);
            foreach (SkinnedMeshRenderer skmr in skmrs)
            {
                if (skmr.name == "helmet" || skmr.name == "visor")
                {
                    skmr.renderer.enabled = false;
                }
            }
        }

        public void ShortcutKeyPress()
        {
            if (shortcutAction == ActionType.Drop)
            {
                KISAddonPickup.instance.Drop(this);
            }
            if (shortcutAction == ActionType.Equip)
            {
                if (equipped) Unequip();
                else Equip();
            }
            if (shortcutAction == ActionType.Custom)
            {
                if (prefabModule) prefabModule.OnItemUse(this, KIS_Item.UseFrom.InventoryShortcut);
            }
        }

        public void Use(UseFrom useFrom)
        {
            if (prefabModule) prefabModule.OnItemUse(this, useFrom);
        }

        public void Equip()
        {
            if (!prefabModule) return;
            KIS_Shared.DebugLog("Equip item " + this.availablePart.name);
            //Check skill if needed
            if (prefabModule.equipSkill != null && prefabModule.equipSkill != "")
            {
                bool skillFound = false;
                List<ProtoCrewMember> protoCrewMembers = inventory.vessel.GetVesselCrew();
                foreach (Experience.ExperienceEffect expEffect in protoCrewMembers[0].experienceTrait.Effects)
                {
                    if (expEffect.ToString().Replace("Experience.Effects.", "") == prefabModule.equipSkill)
                    {
                        skillFound = true;
                    }
                }
                if (!skillFound)
                {
                    ScreenMessages.PostScreenMessage("This item can only be used by a kerbal with the skill : " + prefabModule.equipSkill, 5f, ScreenMessageStyle.UPPER_CENTER);
                    PlaySound(KIS_Shared.bipWrongSndPath);
                    return;
                }
            }

            // Check if already carried
            if (equipSlot != null)
            {
                KIS_Item equippedItem = inventory.GetEquipedItem(equipSlot);
                if (equippedItem != null)
                {
                    if (equippedItem.carryable)
                    {
                        ScreenMessages.PostScreenMessage("Cannot equip item, slot <" + equipSlot + "> already used for carrying " + equippedItem.availablePart.title, 5f, ScreenMessageStyle.UPPER_CENTER);
                        PlaySound(KIS_Shared.bipWrongSndPath);
                        return;
                    }
                    equippedItem.Unequip();
                }
            }

            if (equipMode == EquipMode.Model)
            {
                GameObject modelGo = availablePart.partPrefab.FindModelTransform("model").gameObject;
                equippedModel = Mesh.Instantiate(modelGo) as GameObject;
                foreach (Collider col in equippedModel.GetComponentsInChildren<Collider>())
                {
                    UnityEngine.Object.DestroyImmediate(col);
                }
                evaTransform = null;
                List<SkinnedMeshRenderer> skmrs = new List<SkinnedMeshRenderer>(inventory.part.GetComponentsInChildren<SkinnedMeshRenderer>() as SkinnedMeshRenderer[]);
                foreach (SkinnedMeshRenderer skmr in skmrs)
                {
                    if (skmr.name != prefabModule.equipMeshName) continue;
                    foreach (Transform bone in skmr.bones)
                    {
                        if (bone.name != prefabModule.equipBoneName) continue;
                        evaTransform = bone.transform;
                        break;
                    }
                }

                if (!evaTransform)
                {
                    KIS_Shared.DebugError("evaTransform not found ! ");
                    UnityEngine.Object.Destroy(equippedModel);
                    return;
                }

                if (prefabModule.equipRemoveHelmet)
                {
                    HideHelmet();
                }
            }
            if (equipMode == EquipMode.Physic)
            {
                Collider evaCollider = KIS_Shared.GetEvaCollider(inventory.part.vessel, "jetpackCollider");
                Vector3 pos = evaCollider.transform.TransformPoint(prefabModule.equipPos);
                Quaternion rot = evaCollider.transform.rotation * Quaternion.Euler(prefabModule.equipDir);
                if (this.configNode.HasNode("PART"))
                {
                    ConfigNode partNode = this.configNode.GetNode("PART");
                    equippedPart = KIS_Shared.CreatePart(partNode, pos, rot, this.inventory.part, false);
                }
                else
                {
                    equippedPart = KIS_Shared.CreatePart(this.availablePart, pos, rot, this.inventory.part, false);
                }
                //Destroy joint to avoid buggy eva move
                if (equippedPart.attachJoint)
                {
                    equippedPart.attachJoint.DestroyJoint();
                }
                if (this.inventory.part.attachJoint)
                {
                    this.inventory.part.attachJoint.DestroyJoint();
                }

                FixedJoint evaJoint = equippedPart.gameObject.AddComponent<FixedJoint>();
                evaJoint.connectedBody = evaCollider.attachedRigidbody;
                evaJoint.breakForce = 5;
                evaJoint.breakTorque = 5;

            }
            PlaySound(prefabModule.moveSndPath);
            equipped = true;
            prefabModule.OnEquip(this);
        }

        public void Unequip()
        {
            if (!prefabModule) return;
            if (equipMode == EquipMode.Model)
            {
                evaTransform = null;
                UnityEngine.Object.Destroy(equippedModel);
                if (prefabModule.equipRemoveHelmet)
                {
                    ShowHelmet();
                }
            }
            if (equipMode == EquipMode.Physic)
            {
                equippedPart.Die();
                equippedPart = null;
            }
            equipped = false;
            PlaySound(prefabModule.moveSndPath);
            prefabModule.OnUnEquip(this);
        }

        public void Drop(Part fromPart = null)
        {
            KIS_Shared.DebugLog("Drop item");
            if (fromPart == null) fromPart = inventory.part;
            Quaternion rot;
            Vector3 pos;
            if (prefabModule)
            {
                rot = evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
                pos = evaTransform.TransformPoint(prefabModule.equipPos);
            }
            else
            {
                rot = inventory.part.transform.rotation;
                pos = inventory.part.transform.position + new Vector3(0, 1, 0);
            }
            if (configNode.HasNode("PART"))
            {
                ConfigNode partNode = configNode.GetNode("PART");
                KIS_Shared.CreatePart(partNode, pos, rot, fromPart);
            }
            else
            {
                KIS_Shared.CreatePart(availablePart, pos, rot, fromPart);
            }
            StackRemove(1);
        }

        public void EquipToogle()
        {
            if (equipped)
            {
                Unequip();
            }
            else
            {
                Equip();
            }
        }

        public void ReEquip()
        {
            if (equipped)
            {
                Unequip();
                Equip();
            }
        }

        public void OnMove(ModuleKISInventory srcInventory, ModuleKISInventory destInventory)
        {
            if (srcInventory != destInventory && equipped)
            {
                Unequip();
            }
            if (prefabModule)
            {
                PlaySound(prefabModule.moveSndPath);
            }
            else
            {
                PlaySound(inventory.defaultMoveSndPath);
            }
        }

        public void DragToPart(Part destPart)
        {
            if (prefabModule) prefabModule.OnDragToPart(this, destPart);
        }

        public void DragToInventory(ModuleKISInventory destInventory, int destSlot)
        {
            if (prefabModule) prefabModule.OnDragToInventory(this, destInventory, destSlot);
        }

    }
}