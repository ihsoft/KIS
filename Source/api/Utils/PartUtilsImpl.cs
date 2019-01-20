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

  /// <summary>Returns the part's model, used to make the perview icon.</summary>
  /// <remarks>
  /// Note, that this is not the actual part appearance. It's an optimized version, specifically
  /// made for the icon preview. In particular, the model is scaled to fit the icon's constrains.
  /// </remarks>
  /// <param name="avPart">The part proto to get the model from.</param>
  /// <param name="variant">
  /// The part's variant to apply. If <c>null</c>, then variant will be extracted from
  /// <paramref name="partNode"/>.
  /// </param>
  /// <param name="partNode">
  /// The part's persistent state. It's used to extract the part's variant. It can be <c>null</c>.
  /// </param>
  /// <param name="skipVariantsShader">
  /// Tells if the variant shaders must not be applied to the model. For the purpose of making a
  /// preview icon it's usually undesirable to have the shaders changed.
  /// </param>
  /// <returns>The model of the part. Don't forget to destroy it when not needed.</returns>
  public GameObject GetIconPrefab(
      AvailablePart avPart,
      PartVariant variant = null, ConfigNode partNode = null, bool skipVariantsShader = true) {
    var iconPrefab = UnityEngine.Object.Instantiate(avPart.iconPrefab);
    iconPrefab.SetActive(true);
    if (variant == null && partNode != null) {
      variant = GetCurrentPartVariant(avPart, partNode);
    }
    if (variant != null) {
      DebugEx.Fine(
          "Applying variant to the iconPrefab: part={0}, variant={1}", avPart, variant.Name);
      ModulePartVariants.ApplyVariant(
          null,
          Hierarchy.FindTransformByPath(iconPrefab.transform, "**/model"),
          variant,
          KSP.UI.Screens.EditorPartIcon.CreateMaterialArray(iconPrefab),
          skipVariantsShader);
    }
    return iconPrefab;
  }

  /// <summary>Returns the part's model.</summary>
  /// <remarks>The returned model is a copy from the part prefab.</remarks>
  /// <param name="avPart">The part proto to get the model from.</param>
  /// <param name="variant">
  /// The part's variant to apply. If <c>null</c>, then variant will be extracted from
  /// <paramref name="partNode"/>.
  /// </param>
  /// <param name="partNode">
  /// The part's persistent state. It's used to extract the external scale modifiers and part's
  /// variant. It can be <c>null</c>.
  /// </param>
  /// <returns>The model of the part. Don't forget to destroy it when not needed.</returns>
  public GameObject GetPartModel(
      AvailablePart avPart,
      PartVariant variant = null, ConfigNode partNode = null) {
    if (variant == null && partNode != null) {
      variant = GetCurrentPartVariant(avPart, partNode);
    }
    GameObject modelObj = null;
    ExecuteAtPartVariant(avPart, variant, p => {
      var partPrefabModel = Hierarchy.GetPartModelTransform(avPart.partPrefab).gameObject;
      modelObj = UnityEngine.Object.Instantiate(partPrefabModel);
      modelObj.SetActive(true);
    });

    // Handle TweakScale settings.
    if (partNode != null) {
      var scale = KISAPI.PartNodeUtils.GetTweakScaleSizeModifier(partNode);
      if (Mathf.Abs(1.0f - scale) > float.Epsilon) {
        DebugEx.Fine("Applying TweakScale size modifier: {0}", scale);
        var scaleRoot = new GameObject("TweakScale");
        scaleRoot.transform.localScale = new Vector3(scale, scale, scale);
        modelObj.transform.SetParent(scaleRoot.transform, worldPositionStays: false);
        modelObj = scaleRoot;
      }
    }
    
    return modelObj;
  }

  /// <summary>Gets the part's variant.</summary>
  /// <param name="avPart">The part proto to get the variant for.</param>
  /// <param name="partNode">The part's persistent state.</param>
  /// <returns>The part's variant.</returns>
  public PartVariant GetCurrentPartVariant(AvailablePart avPart, ConfigNode partNode) {
    var variantsModule = KISAPI.PartNodeUtils.GetModuleNode<ModulePartVariants>(partNode);
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

  /// <summary>Returns part's volume basing on its geometrics.</summary>
  /// <remarks>
  /// The volume is calculated basing on the smallest boundary box that encapsulates all the meshes
  /// in the part. The deployable parts can take much more space in teh deployed state.
  /// </remarks>
  /// <param name="avPart">The part proto to get the models from.</param>
  /// <param name="variant">
  /// The part's variant. If it's <c>null</c>, then the variant will be attempted to read from
  /// <paramref name="partNode"/>.
  /// </param>
  /// <param name="partNode">
  /// The part's persistent config. It will be looked up for the variant if it's not specified.
  /// </param>
  /// <returns>The volume in liters.</returns>
  public float GetPartVolume(
      AvailablePart avPart, PartVariant variant = null, ConfigNode partNode = null) {
    var model = GetPartModel(avPart, variant: variant, partNode: partNode);
    var boundsSize = model.GetRendererBounds().size;
    UnityEngine.Object.DestroyImmediate(model);
    return boundsSize.x * boundsSize.y * boundsSize.z * 1000f;
  }

  /// <summary>Returns part's volume basing on its geometrics.</summary>
  /// <remarks>
  /// The volume is calculated basing on the smallest boundary box that encapsulates all the meshes
  /// in the part. The deployable parts can take much more space in teh deployed state.
  /// </remarks>
  /// <param name="part">The actual part, that exists in the scene.</param>
  /// <returns>The volume in liters.</returns>
  public float GetPartVolume(Part part) {
    var partNode = KISAPI.PartNodeUtils.PartSnapshot(part);
    return GetPartVolume(part.partInfo, partNode: partNode);
  }

  /// <summary>Calculates part's dry mass given the config and the variant.</summary>
  /// <param name="avPart">The part's proto.</param>
  /// <param name="variant">
  /// The part's variant. If it's <c>null</c>, then the variant will be attempted to read from
  /// <paramref name="partNode"/>.
  /// </param>
  /// <param name="partNode">
  /// The part's persistent config. It will be looked up for the variant if it's not specified.
  /// </param>
  /// <returns>The dry cost of the part.</returns>
  public float GetPartDryMass(
      AvailablePart avPart, PartVariant variant = null, ConfigNode partNode = null) {
    var itemMass = avPart.partPrefab.mass;
    if (variant == null && partNode != null) {
      variant = GetCurrentPartVariant(avPart, partNode);
    }
    ExecuteAtPartVariant(avPart, variant, p => itemMass += p.GetModuleMass(p.mass));
    return itemMass;
  }

  /// <summary>Calculates part's dry cost given the config and the variant.</summary>
  /// <param name="avPart">The part's proto.</param>
  /// <param name="variant">
  /// The part's variant. If it's <c>null</c>, then the variant will be attempted to read from
  /// <paramref name="partNode"/>.
  /// </param>
  /// <param name="partNode">
  /// The part's persistent config. It will be looked up for the various cost modifiers.
  /// </param>
  /// <returns>The dry cost of the part.</returns>
  public float GetPartDryCost(
      AvailablePart avPart, PartVariant variant = null, ConfigNode partNode = null) {
    // TweakScale compatibility
    if (partNode != null) {
      var tweakScale = KISAPI.PartNodeUtils.GetTweakScaleModule(partNode);
      if (tweakScale != null) {
        var tweakedCost = ConfigAccessor2.GetValueByPath<float>(tweakScale, "DryCost");
        if (tweakedCost.HasValue) {
          // TODO(ihsoft): Get back to this code once TweakScale supports variants.
          return tweakedCost.Value;
        }
        DebugEx.Error("No dry cost specified in a tweaked part {0}:\n{1}", avPart.name, tweakScale);
      }
    }
    var itemCost = avPart.cost;
    if (variant == null && partNode != null) {
      variant = GetCurrentPartVariant(avPart, partNode);
    }
    ExecuteAtPartVariant(avPart, variant, p => itemCost += p.GetModuleCosts(avPart.cost));
    return itemCost;
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
  public void ExecuteAtPartVariant(
      AvailablePart avPart, PartVariant variant, Action<Part> fn) {
    var oldPartVariant = GetCurrentPartVariant(avPart.partPrefab);
    if (oldPartVariant != null) {
      variant = variant ?? avPart.partPrefab.baseVariant;
      avPart.partPrefab.variants.SetVariant(variant.Name);  // Set.
      fn(avPart.partPrefab);  // Run on the updated part.
      avPart.partPrefab.variants.SetVariant(oldPartVariant.Name);  // Restore.
    } else {
      fn(avPart.partPrefab);
    }
  }
}

}  // namespace
