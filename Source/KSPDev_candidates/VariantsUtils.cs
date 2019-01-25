// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using KSPDev.ConfigUtils;
using System;
using System.Linq;

namespace KSPDev.PartUtils {

/// <summary>Various methods to deal with the part variants.</summary>
public static class VariantsUtils {
  /// <summary>Gets the part's variant.</summary>
  /// <param name="avPart">The part proto to get the variant for.</param>
  /// <param name="partNode">The part's persistent state.</param>
  /// <returns>The part's variant.</returns>
  public static PartVariant GetCurrentPartVariant(AvailablePart avPart, ConfigNode partNode) {
    var variantsModule = PartNodeUtils.GetModuleNode<ModulePartVariants>(partNode);
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
  public static PartVariant GetCurrentPartVariant(Part part) {
    var variantsModule = part.Modules.OfType<ModulePartVariants>().FirstOrDefault();
    if (variantsModule != null) {
      return variantsModule.SelectedVariant;
    }
    return null;
  }

  /// <summary>Executes an action on a part with an arbitrary variant applied.</summary>
  /// <remarks>
  /// If the part doesn't support variants, then the action is executed for the unchanged prefab.
  /// </remarks>
  /// <param name="avPart">The part proto.</param>
  /// <param name="variant">
  /// The variant to apply. Set it to <c>null</c> to use the default part variant.
  /// </param>
  /// <param name="fn">
  /// The action to call once the variant is applied. The argument is a prefab part with the variant
  /// applied, so changing it or obtaining any hard references won't be a good idea. The prefab
  /// part's variant will be reverted before the method return.
  /// </param>
  public static void ExecuteAtPartVariant(
      AvailablePart avPart, PartVariant variant, Action<Part> fn) {
    var oldPartVariant = GetCurrentPartVariant(avPart.partPrefab);
    if (oldPartVariant != null) {
      variant = variant ?? avPart.partPrefab.baseVariant;
      avPart.partPrefab.variants.SetVariant(variant.Name);  // Set.
      ApplyVariantOnAttachNodes(avPart.partPrefab, variant);
      fn(avPart.partPrefab);  // Run on the updated part.
      avPart.partPrefab.variants.SetVariant(oldPartVariant.Name);  // Restore.
      ApplyVariantOnAttachNodes(avPart.partPrefab, oldPartVariant);
    } else {
      fn(avPart.partPrefab);
    }
  }

  /// <summary>Applies variant settinsg to the part attach nodes.</summary>
  /// <remarks>
  /// The stock apply variant method only does it when the active scene is editor. So if there is a
  /// part in the flight scene with a variant, it needs to be updated for the proper KIS behavior.
  /// </remarks>
  /// <param name="part">The part to apply the chnages to.</param>
  /// <param name="variant">The variant to apply.</param>
  /// <param name="updatePartPosition">
  /// Tells if any connected parts at the attach nodes need to be repositioned accordingly. This may
  /// trigger collisions in the scene, so use carefully.
  /// </param>
  public static void ApplyVariantOnAttachNodes(Part part, PartVariant variant,
                                               bool updatePartPosition = false) {
    foreach (var partAttachNode in part.attachNodes) {
      foreach (var variantAttachNode in variant.AttachNodes) {
        if (partAttachNode.id == variantAttachNode.id) {
          if (updatePartPosition) {
            ModulePartVariants.UpdatePartPosition(partAttachNode, variantAttachNode);
          }
          partAttachNode.originalPosition = variantAttachNode.originalPosition;
          partAttachNode.position = variantAttachNode.position;
          partAttachNode.size = variantAttachNode.size;
        }
      }
    }
  }
}

}  // namespace
