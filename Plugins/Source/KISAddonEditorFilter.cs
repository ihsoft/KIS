using KSPDev.LogUtils;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace KIS
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KISAddonEditorFilter : MonoBehaviour
    {
        private static List<AvailablePart> avPartItems = new List<AvailablePart>();
        internal string category = "Filter by Function";
        internal string subCategoryTitle = "EVA Items";
        internal string defaultTitle = "KIS";
        internal string iconName = "R&D_node_icon_evatech";
        internal bool filter = true;

        void Awake()
        {
            // FIXME: Drop on release.
            Logger.logWarning("Test version of KIS detected!!!");
            PopupDialog.SpawnPopupDialog(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "KIS Pre-Release",
                "You're using a test version of KIS that is intended to be used for testing"
                + " purposes only.\nMake sure you've made backups of your savefiles since they"
                + " may get badly broken!",
                "I agree to take this risk", false /* persistAcrossScenes */, HighLogic.UISkin,
                isModal: false);

            GameEvents.onGUIEditorToolbarReady.Add(SubCategories);

            avPartItems.Clear();
            foreach (AvailablePart avPart in PartLoader.LoadedPartsList)
            {
                if (avPart.name == "kerbalEVA" || avPart.name == "kerbalEVA_RD" || !avPart.partPrefab) continue;
                ModuleKISItem moduleItem = avPart.partPrefab.GetComponent<ModuleKISItem>();
                if (moduleItem)
                {
                    if (moduleItem.editorItemsCategory)
                    {
                        avPartItems.Add(avPart);
                    }
                }
            }

        }

        private bool EditorItemsFilter(AvailablePart avPart)
        {
            return avPartItems.Contains(avPart);
        }

        private void SubCategories()
        {
            RUI.Icons.Selectable.Icon icon = PartCategorizer.Instance.iconLoader.GetIcon(iconName);
            PartCategorizer.Category Filter = PartCategorizer.Instance.filters.Find(f => f.button.categoryName == category);
            PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, icon, EditorItemsFilter);
        }
    }
}

