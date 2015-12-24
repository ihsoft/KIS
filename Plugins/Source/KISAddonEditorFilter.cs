using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS
{
    //FIXME: Should be in editor only.
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
            KSP_Dev.LoggedCallWrapper.Action(Internal_Awake);
        }
        
        private void Internal_Awake()
        {
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
            PartCategorizer.AddCustomSubcategoryFilter(Filter, subCategoryTitle, icon, p => EditorItemsFilter(p));

            RUIToggleButtonTyped button = Filter.button.activeButton;
            button.SetFalse(button, RUIToggleButtonTyped.ClickType.FORCED);
            button.SetTrue(button, RUIToggleButtonTyped.ClickType.FORCED);
        }
    }
}

