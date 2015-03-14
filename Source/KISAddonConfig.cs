using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;

namespace KIS
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    class KISAddonConfig : MonoBehaviour
    {
        public static List<string> stackableList = new List<string>();
        public static List<string> stackableModules = new List<string>();

        public void Awake()
        {
            // Set inventory module for every eva kerbal
            KIS_Shared.DebugLog("Set KIS config...");
            ConfigNode nodeSettings = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/KIS/settings.cfg") ?? new ConfigNode();

            // Set global settings
            ConfigNode nodeGlobal = nodeSettings.GetNode("Global");
            if (nodeGlobal.HasValue("itemDebug"))
            {
                ModuleKISInventory.debugContextMenu = bool.Parse(nodeGlobal.GetValue("itemDebug"));
            }

            ConfigNode nodeEvaInventory = nodeSettings.GetNode("EvaInventory");
            ConfigNode nodeEvaPickup = nodeSettings.GetNode("EvaPickup");
            ConfigNode nodeStackable = nodeSettings.GetNode("StackableItemOverride");
            ConfigNode nodeStackableModule = nodeSettings.GetNode("StackableModule");

            // Set stackable items list
            stackableList.Clear();
            foreach (string partName in nodeStackable.GetValues("partName"))
            {
                stackableList.Add(partName);
            }

            // Set stackable module list
            stackableModules.Clear();
            foreach (string moduleName in nodeStackableModule.GetValues("moduleName"))
            {
                stackableModules.Add(moduleName);
            }

            Part evaPrefab = PartLoader.getPartInfoByName("kerbalEVA").partPrefab;
            try
            {
                // Adding module to EVA cause an unknown error but work
                evaPrefab.AddModule("ModuleKISInventory");
            }
            catch
            {
            }
            try
            {
                // Adding module to EVA cause an unknown error but work
                evaPrefab.AddModule("ModuleKISPickup");
            }
            catch
            {
            }

            // Set inventory module for eva
            ModuleKISInventory evaInventory = evaPrefab.GetComponent<ModuleKISInventory>();
            if (evaInventory)
            {
                SetInventoryConfig(nodeEvaInventory, evaInventory);
                evaInventory.invType = ModuleKISInventory.InventoryType.Eva;
                KIS_Shared.DebugLog("Eva inventory module loaded successfully");
            }

            // Set pickup module for eva
            ModuleKISPickup evaPickup = evaPrefab.GetComponent<ModuleKISPickup>();
            if (evaPickup)
            {
                if (nodeEvaPickup.HasValue("grabKey")) KISAddonPickup.grabKey = nodeEvaPickup.GetValue("grabKey");
                if (nodeEvaPickup.HasValue("canDetach")) evaPickup.canDetach = bool.Parse(nodeEvaPickup.GetValue("canDetach"));
                if (nodeEvaPickup.HasValue("maxDistance")) evaPickup.maxDistance = float.Parse(nodeEvaPickup.GetValue("maxDistance"));
                if (nodeEvaPickup.HasValue("maxMass")) evaPickup.maxMass = float.Parse(nodeEvaPickup.GetValue("maxMass"));
                if (nodeEvaPickup.HasValue("dropSndPath")) evaPickup.dropSndPath = nodeEvaPickup.GetValue("dropSndPath");
                if (nodeEvaPickup.HasValue("attachSndPath")) evaPickup.attachSndPath = nodeEvaPickup.GetValue("attachSndPath");
                if (nodeEvaPickup.HasValue("draggedIconResolution")) KISAddonPickup.draggedIconResolution = int.Parse(nodeEvaPickup.GetValue("draggedIconResolution"));
                KIS_Shared.DebugLog("Eva pickup module loaded successfully");
            }

            // Set inventory module for every pod with crew capacity
            KIS_Shared.DebugLog("Loading pod inventory...");
            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (avPart.name == "kerbalEVA") continue;
                if (avPart.name == "kerbalEVA_RD") continue;
                if (!avPart.partPrefab) continue;
                if (avPart.partPrefab.CrewCapacity < 1) continue;
                KIS_Shared.DebugLog("Found part with CrewCapacity : " + avPart.partPrefab.name);


                for (int i = 0; i < avPart.partPrefab.CrewCapacity; i++)
                {
                    try
                    {
                        ModuleKISInventory moduleInventory = avPart.partPrefab.AddModule("ModuleKISInventory") as ModuleKISInventory;
                        SetInventoryConfig(nodeEvaInventory, moduleInventory);
                        moduleInventory.podSeat = i;
                        moduleInventory.invType = ModuleKISInventory.InventoryType.Pod;
                        KIS_Shared.DebugLog("Pod inventory module(s) for seat " + i + " loaded successfully");
                    }
                    catch
                    {
                        KIS_Shared.DebugWarning("Pod inventory module(s) for seat " + i + " can't be loaded !");
                    }
                }
            }
        }

        private void SetInventoryConfig(ConfigNode node, ModuleKISInventory moduleInventory)
        {
            if (node.HasValue("inventoryKey")) moduleInventory.evaInventoryKey = node.GetValue("inventoryKey");
            if (node.HasValue("rightHandKey")) moduleInventory.evaRightHandKey = node.GetValue("rightHandKey");
            if (node.HasValue("slotsX")) moduleInventory.slotsX = int.Parse(node.GetValue("slotsX"));
            if (node.HasValue("slotsY")) moduleInventory.slotsY = int.Parse(node.GetValue("slotsY"));
            if (node.HasValue("slotSize")) moduleInventory.slotSize = int.Parse(node.GetValue("slotSize"));
            if (node.HasValue("itemIconResolution")) moduleInventory.itemIconResolution = int.Parse(node.GetValue("itemIconResolution"));
            if (node.HasValue("selfIconResolution")) moduleInventory.selfIconResolution = int.Parse(node.GetValue("selfIconResolution"));
            if (node.HasValue("maxVolume")) moduleInventory.maxVolume = float.Parse(node.GetValue("maxVolume"));
            if (node.HasValue("openSndPath")) moduleInventory.openSndPath = node.GetValue("openSndPath");
            if (node.HasValue("closeSndPath")) moduleInventory.closeSndPath = node.GetValue("closeSndPath");
        }
    }

}