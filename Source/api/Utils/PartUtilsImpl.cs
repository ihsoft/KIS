// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using KSPDev.ModelUtils;
using KSPDev.LogUtils;
using System;
using System.Linq;
using UnityEngine;

namespace KISAPIv1 {

/// <summary>Various methods to deal with the parts.</summary>
public class PartUtilsImpl {

  /// <summary>Returns the part's models, used to make the perview icon.</summary>
  /// <remarks>
  /// It properly handles a variants modification, given it's defined in the part's config.
  /// </remarks>
  /// <param name="avPart">The part proto to get the models from.</param>
  /// <param name="variant">The part's variant to apply.</param>
  /// <param name="skipVariansShader">
  /// Tells if the variant shaders must not be applied to the model. For the purpose of making a
  /// preview icon it's usually undesirable to have the shaders changed.
  /// </param>
  /// <returns>The model of the part.</returns>
  public GameObject GetIconPrefab(AvailablePart avPart,
                                  PartVariant variant = null,
                                  bool skipVariansShader = true) {
    var iconPrefab = UnityEngine.Object.Instantiate(avPart.iconPrefab);
    iconPrefab.SetActive(true);
    if (variant != null) {
      DebugEx.Fine(
          "Applying variant to the iconPrefab: part={0}, variant={1}", avPart, variant.Name);
      ModulePartVariants.ApplyVariant(
          null,
          Hierarchy.FindTransformByPath(iconPrefab.transform, "**/model"),
          variant,
          KSP.UI.Screens.EditorPartIcon.CreateMaterialArray(iconPrefab),
          skipVariansShader);
    }
    return iconPrefab;
  }

  /// <summary>Gets the part's variant.</summary>
  /// <param name="avPart">The part proto to get the variant for.</param>
  /// <param name="partNode">The part's persistent state.</param>
  /// <returns>The part's variant.</returns>
  public PartVariant GetCurrentPartVariant(AvailablePart avPart, ConfigNode partNode) {
    var variantsModule = GetPartModuleNode(partNode, "ModulePartVariants");
    if (variantsModule == null) {
      return null;
    }
    var selectedVariantName = variantsModule.GetValue("selectedVariant")
        ?? avPart.partPrefab.baseVariant.Name;
    return avPart.partPrefab.variants.variantList
        .FirstOrDefault(v => v.Name == selectedVariantName);
  }

  /// <summary>Gets the part variant.</summary>
  /// <param name="part">The part to get variant for.</param>
  /// <returns>The part's variant.</returns>
  public PartVariant GetCurrentPartVariant(Part part) {
    var variantsModule = part.Modules.OfType<ModulePartVariants>().FirstOrDefault();
    if (variantsModule != null) {
      return variantsModule.SelectedVariant;
    }
    return null;
  }

  /// <summary>Extracts a module config node from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <param name="moduleName">The name of the module to extract.</param>
  /// <returns>The module node or <c>null</c> if not found.</returns>
  public ConfigNode GetPartModuleNode(ConfigNode partNode, string moduleName) {
    if (partNode.HasNode("PART")) {
      partNode = partNode.GetNode("PART");
    }
    return partNode.GetNodes("MODULE")
        .FirstOrDefault(m => m.GetValue("name") == moduleName);
  }
}

}  // namespace
