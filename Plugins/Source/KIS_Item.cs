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
        public ConfigNode partNode;
        public AvailablePart availablePart;
        public float quantity;
        public KIS_IconViewer icon;
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
        private GameObject equippedGameObj;
        public Part equippedPart;
        Transform evaTransform;
        public enum ActionType { Drop, Equip, Custom }
        public enum UseFrom { KeyDown, KeyUp, InventoryShortcut, ContextMenu }
        public enum EquipMode { Model, Part, Physic }
        public float resourceMass = 0;
        public float contentMass = 0;
        public float contentCost = 0;
        public string inventoryName = "";

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
                    if (prefabModule.equipMode == "part")
                    {
                        mode = EquipMode.Part;
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
                if (carriable && inventory.invType == ModuleKISInventory.InventoryType.Eva && HighLogic.LoadedSceneIsFlight)
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
        public float totalCost { get { return (cost + contentCost) * quantity; } }
        public float totalMass { get { return stackDryMass + resourceMass + contentMass; } }

        public KIS_Item(AvailablePart availablePart, ConfigNode itemNode, ModuleKISInventory inventory, float quantity = 1) // Part from save
        {
            // Get part node
            this.availablePart = availablePart;
            this.partNode = new ConfigNode();
            ConfigNode savedPartNode = itemNode.GetNode("PART");
            savedPartNode.CopyTo(partNode);
            // init config
            this.InitConfig(availablePart, inventory, quantity);
            // Get mass
            if (itemNode.HasValue("resourceMass")) resourceMass = float.Parse(itemNode.GetValue("resourceMass"));
            else resourceMass = availablePart.partPrefab.GetResourceMass();
            if (itemNode.HasValue("contentMass")) contentMass = float.Parse(itemNode.GetValue("contentMass"));
            if (itemNode.HasValue("contentCost")) contentCost = float.Parse(itemNode.GetValue("contentCost"));
            if (itemNode.HasValue("inventoryName")) inventoryName = itemNode.GetValue("inventoryName");
        }

        public KIS_Item(Part part, ModuleKISInventory inventory, float quantity = 1) // New part from scene
        {
            // Get part node
            this.availablePart = PartLoader.getPartInfoByName(part.partInfo.name);
            this.partNode = new ConfigNode();
            KIS_Shared.PartSnapshot(part).CopyTo(this.partNode);
            // init config
            this.InitConfig(availablePart, inventory, quantity);
            // Get mass
            this.resourceMass = part.GetResourceMass();
            ModuleKISInventory itemInventory = part.GetComponent<ModuleKISInventory>();
            if (itemInventory)
            {
                this.contentMass = itemInventory.GetContentMass();
                this.contentCost = itemInventory.GetContentCost();
                if (itemInventory.invName != "") this.inventoryName = itemInventory.invName;
            }
        }

        private void InitConfig(AvailablePart availablePart, ModuleKISInventory inventory, float quantity)
        {
            this.inventory = inventory;
            this.quantity = quantity;
            this.prefabModule = availablePart.partPrefab.GetComponent<ModuleKISItem>();
            this.volume = GetVolume();
            this.cost = GetCost();

            // Set launchID
            if (this.partNode.HasValue("launchID"))
            {
                if (int.Parse(this.partNode.GetValue("launchID")) == 0)
                {
                    this.partNode.SetValue("launchID", this.inventory.part.launchID.ToString(), true);
                }
            }
            else
            {
                this.partNode.SetValue("launchID", this.inventory.part.launchID.ToString(), true);
            }

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
                this.carriable = prefabModule.carriable;
            }
            int nonStackableModule = 0;
            foreach (PartModule pModule in availablePart.partPrefab.Modules)
            {
                if (!KISAddonConfig.stackableModules.Contains(pModule.moduleName))
                {
                    nonStackableModule++;
                }
            }
            if (nonStackableModule == 0 && GetResources().Count == 0)
            {
                KSP_Dev.Logger.logInfo("No non-stackable module and ressource found on the part,"
                                       + " set item as stackable");
                this.stackable = true;
            }
            if (KISAddonConfig.stackableList.Contains(availablePart.name)
                || availablePart.name.IndexOf('.') != -1 && KISAddonConfig.stackableList.Contains(availablePart.name.Replace('.', '_')))
            {
                KSP_Dev.Logger.logInfo("Part name present in settings.cfg (node"
                                       + " StackableItemOverride), force item as stackable");
                this.stackable = true;
            }
        }

        public void OnSave(ConfigNode node)
        {
            node.AddValue("partName", this.availablePart.name);
            node.AddValue("slot", slot);
            node.AddValue("quantity", quantity);
            node.AddValue("equipped", equipped);
            node.AddValue("resourceMass", resourceMass);
            node.AddValue("contentMass", contentMass);
            node.AddValue("contentCost", contentCost);
            if (inventoryName != "") node.AddValue("inventoryName", inventoryName);
            if (equipped && (equipMode == EquipMode.Part || equipMode == EquipMode.Physic))
            {
                KSP_Dev.Logger.logInfo(
                    "Update config node of equipped part: {0}", this.availablePart.title);
                partNode.ClearData();
                KIS_Shared.PartSnapshot(equippedPart).CopyTo(partNode);
            }
            partNode.CopyTo(node.AddNode("PART"));
        }

        public List<ResourceInfo> GetResources()
        {
            List<ResourceInfo> resources = new List<ResourceInfo>();
            foreach (ConfigNode node in this.partNode.GetNodes("RESOURCE"))
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
            return resources;
        }

        public List<ScienceData> GetSciences()
        {
            List<ScienceData> sciences = new List<ScienceData>();
            foreach (ConfigNode module in this.partNode.GetNodes("MODULE"))
            {
                foreach (ConfigNode experiment in module.GetNodes("ScienceData"))
                {
                    ScienceData scienceData = new ScienceData(experiment);
                    sciences.Add(scienceData);
                }
            }
            return sciences;
        }

        public float GetScale()
        {
            // TweakScale compatibility
            foreach (ConfigNode node in this.partNode.GetNodes("MODULE"))
            {
                if (node.HasValue("name"))
                {
                    if (node.GetValue("name") == "TweakScale" && node.HasValue("currentScale") && node.HasValue("defaultScale"))
                    {
                        double scaleRatio = (1 / double.Parse(node.GetValue("defaultScale"))) * double.Parse(node.GetValue("currentScale"));
                        return (float)scaleRatio;
                    }
                }
            }
            return 1;
        }

        public float GetVolume()
        {
            // TweakScale compatibility
            foreach (ConfigNode node in this.partNode.GetNodes("MODULE"))
            {
                if (node.HasValue("name"))
                {
                    if (node.GetValue("name") == "TweakScale" && node.HasValue("currentScale") && node.HasValue("defaultScale"))
                    {
                        return KIS_Shared.GetPartVolume(availablePart.partPrefab) * (float)Math.Pow(double.Parse(node.GetValue("currentScale")) / double.Parse(node.GetValue("defaultScale")), 3);
                    }
                }
            }
            return KIS_Shared.GetPartVolume(availablePart.partPrefab);
        }

        public float GetCost()
        {
            // TweakScale compatibility
            foreach (ConfigNode node in this.partNode.GetNodes("MODULE"))
            {
                if (node.HasValue("name"))
                {
                    if (node.GetValue("name") == "TweakScale" && node.HasValue("DryCost"))
                    {
                        double ressourcesCost = 0;
                        foreach (ResourceInfo resource in GetResources())
                        {
                            PartResourceDefinition pRessourceDef = PartResourceLibrary.Instance.GetDefinition(resource.resourceName);
                            ressourcesCost += resource.amount * pRessourceDef.unitCost;
                        }
                        return float.Parse(node.GetValue("DryCost")) + (float)ressourcesCost;
                    }
                }
            }
            return availablePart.cost;
        }

        public void SetResource(string name, double amount)
        {
            foreach (ConfigNode node in this.partNode.GetNodes("RESOURCE"))
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

        public void EnableIcon(int resolution)
        {
            icon = new KIS_IconViewer(availablePart.partPrefab, resolution);
        }

        public void DisableIcon()
        {
            icon = null;
        }

        public void Update() {
            KSP_Dev.LoggedCallWrapper.Action(Internal_Update);
        }
        
        private void Internal_Update()
        {
            if (equippedGameObj)
            {
                equippedGameObj.transform.rotation = evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
                equippedGameObj.transform.position = evaTransform.TransformPoint(prefabModule.equipPos);
                if (equippedPart) if (equippedPart.rigidbody) equippedPart.rigidbody.velocity = equippedPart.vessel.rootPart.rigidbody.velocity;
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

        public bool StackAdd(float qty, bool checkVolume = true)
        {
            if (qty <= 0) return false;
            float newVolume = inventory.totalVolume + (volume * qty);
            if (checkVolume && newVolume > inventory.maxVolume)
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
            KSP_Dev.Logger.logInfo("Equip item {0}", this.availablePart.name);

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
                    if (equippedItem.carriable)
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
                equippedGameObj = Mesh.Instantiate(modelGo) as GameObject;
                foreach (Collider col in equippedGameObj.GetComponentsInChildren<Collider>())
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
                    KSP_Dev.Logger.logError("evaTransform not found ! ");
                    UnityEngine.Object.Destroy(equippedGameObj);
                    return;
                }
            }
            if (equipMode == EquipMode.Part || equipMode == EquipMode.Physic)
            {
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
                    KSP_Dev.Logger.logError("evaTransform not found ! ");
                    return;
                }

                Part alreadyEquippedPart = this.inventory.part.vessel.Parts.Find(p => p.partInfo.name == this.availablePart.name);
                if (alreadyEquippedPart)
                {
                    KSP_Dev.Logger.logInfo("Part: {0} already found on eva",
                                           this.availablePart.name);
                    equippedPart = alreadyEquippedPart;
                    OnEquippedPartCoupled(equippedPart);
                }
                else
                {
                    Vector3 equipPos =  evaTransform.TransformPoint(prefabModule.equipPos);
                    Quaternion equipRot = evaTransform.rotation * Quaternion.Euler(prefabModule.equipDir);
                    equippedPart = KIS_Shared.CreatePart(partNode, equipPos, equipRot, this.inventory.part, this.inventory.part, null, null, OnEquippedPartCoupled);
                }
                if (equipMode == EquipMode.Part) equippedGameObj = equippedPart.gameObject;
            }

            if (prefabModule.equipRemoveHelmet)
            {
                inventory.SetHelmet(false);
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
                UnityEngine.Object.Destroy(equippedGameObj);
                if (prefabModule.equipRemoveHelmet)
                {
                    inventory.SetHelmet(true);
                }
            }
            if (equipMode == EquipMode.Part || equipMode == EquipMode.Physic)
            {
                KSP_Dev.Logger.logInfo("Update config node of equipped part: {0}",
                                       this.availablePart.title);
                partNode.ClearData();
                KIS_Shared.PartSnapshot(equippedPart).CopyTo(partNode);
                equippedPart.Die();
            }
            evaTransform = null;
            equippedPart = null;
            equippedGameObj = null;
            equipped = false;
            PlaySound(prefabModule.moveSndPath);
            prefabModule.OnUnEquip(this);
        }

        public void OnEquippedPartCoupled(Part createdPart, Part tgtPart = null, AttachNode tgtAttachNode = null)
        {
            if (equipMode == EquipMode.Part)
            {
                //Disable colliders
                foreach (Collider col in equippedPart.gameObject.GetComponentsInChildren<Collider>())
                {
                    col.isTrigger = true;
                }

                //Destroy joint to avoid buggy eva move
                if (equippedPart.attachJoint)
                {
                    equippedPart.attachJoint.DestroyJoint();
                }

                //Destroy rigidbody
                equippedPart.physicalSignificance = Part.PhysicalSignificance.NONE;
                if (equippedPart.collisionEnhancer)
                {
                    UnityEngine.Object.DestroyImmediate(equippedPart.collisionEnhancer);
                }
                UnityEngine.Object.Destroy(equippedPart.rigidbody);
            }

            if (equipMode == EquipMode.Physic)
            {
                equippedPart.mass = 0.001f;
                //Disable colliders
                foreach (Collider col in equippedPart.gameObject.GetComponentsInChildren<Collider>())
                {
                    col.isTrigger = true;
                }

                //Destroy joint to avoid buggy eva move
                if (equippedPart.attachJoint)
                {
                    equippedPart.attachJoint.DestroyJoint();
                }

                //Create physic join
                FixedJoint evaJoint = equippedPart.gameObject.AddComponent<FixedJoint>();
                evaJoint.connectedBody = this.inventory.part.rigidbody;//evaCollider.attachedRigidbody;
                evaJoint.breakForce = 5;
                evaJoint.breakTorque = 5;
                KIS_Shared.ResetCollisionEnhancer(equippedPart);
            }
        }

        public void Drop(Part fromPart = null)
        {
            KSP_Dev.Logger.logInfo("Drop item");
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
            KIS_Shared.CreatePart(partNode, pos, rot, fromPart);
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