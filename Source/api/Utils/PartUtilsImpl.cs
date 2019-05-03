// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using KSPDev.ModelUtils;
using KSPDev.LogUtils;
using KSPDev.PartUtils;
using System;
using System.Collections.Generic;
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
      variant = VariantsUtils.GetCurrentPartVariant(avPart, partNode);
    }
    if (variant != null) {
      DebugEx.Fine(
          "Applying variant to the iconPrefab: part={0}, variant={1}", avPart.name, variant.Name);
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
      variant = VariantsUtils.GetCurrentPartVariant(avPart, partNode);
    }
    GameObject modelObj = null;
    VariantsUtils.ExecuteAtPartVariant(avPart, variant, p => {
      var partPrefabModel = Hierarchy.GetPartModelTransform(p).gameObject;
      modelObj = UnityEngine.Object.Instantiate(partPrefabModel);
      modelObj.SetActive(true);
    });

    // Handle TweakScale settings.
    if (partNode != null) {
      var scale = KISAPI.PartNodeUtils.GetTweakScaleSizeModifier(partNode);
      if (Math.Abs(1.0 - scale) > double.Epsilon) {
        DebugEx.Fine("Applying TweakScale size modifier: {0}", scale);
        var scaleRoot = new GameObject("TweakScale");
        scaleRoot.transform.localScale = new Vector3((float) scale, (float) scale, (float) scale);
        modelObj.transform.SetParent(scaleRoot.transform, worldPositionStays: false);
        modelObj = scaleRoot;
      }
    }
    
    return modelObj;
  }

  /// <summary>Collects all the models in the part or hierarchy.</summary>
  /// <remarks>
  /// The result of this method only includes meshes and renderers. Any colliders, animations or
  /// effects will be dropped.
  /// <para>
  /// Note, that this method captures the current model state fro the part, which may be affected
  /// by animations or third-party mods. That said, each call for the same part may return different
  /// results.
  /// </para>
  /// </remarks>
  /// <param name="rootPart">The part to start scanning the assembly from.</param>
  /// <param name="goThruChildren">
  /// Tells if the parts down the hierarchy need to be captured too.
  /// </param>
  /// <returns>
  /// The root game object of the new hirerarchy. This object must be explicitly disposed when not
  /// needed anymore.
  /// </returns>
  public GameObject GetSceneAssemblyModel(Part rootPart, bool goThruChildren = true) {
    var modelObj = UnityEngine.Object.Instantiate<GameObject>(
        Hierarchy.GetPartModelTransform(rootPart).gameObject);
    modelObj.SetActive(true);

    // This piece of code was stolen from PartLoader.CreatePartIcon (alas, it's private).
    PartLoader.StripComponent<EffectBehaviour>(modelObj);
    PartLoader.StripGameObject<Collider>(modelObj, "collider");
    PartLoader.StripComponent<Collider>(modelObj);
    PartLoader.StripComponent<WheelCollider>(modelObj);
    PartLoader.StripComponent<SmokeTrailControl>(modelObj);
    PartLoader.StripComponent<FXPrefab>(modelObj);
    PartLoader.StripComponent<ParticleSystem>(modelObj);
    PartLoader.StripComponent<Light>(modelObj);
    PartLoader.StripComponent<Animation>(modelObj);
    PartLoader.StripComponent<DAE>(modelObj);
    PartLoader.StripComponent<MeshRenderer>(modelObj, "Icon_Hidden", true);
    PartLoader.StripComponent<MeshFilter>(modelObj, "Icon_Hidden", true);
    PartLoader.StripComponent<SkinnedMeshRenderer>(modelObj, "Icon_Hidden", true);
    
    if (goThruChildren) {
      foreach (var childPart in rootPart.children) {
        var childObj = GetSceneAssemblyModel(childPart);
        childObj.transform.parent = modelObj.transform;
        childObj.transform.localRotation =
            rootPart.transform.rotation.Inverse() * childPart.transform.rotation;
        childObj.transform.localPosition =
            rootPart.transform.InverseTransformPoint(childPart.transform.position);
      }
    }
    return modelObj;
  }

  /// <summary>Returns part's volume basing on its geometrics.</summary>
  /// <remarks>
  /// The volume is calculated basing on the smallest boundary box that encapsulates all the meshes
  /// in the part. The deployable parts can take much more space in the deployed state.
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
  public double GetPartVolume(
      AvailablePart avPart, PartVariant variant = null, ConfigNode partNode = null) {
    var itemModule = avPart.partPrefab.Modules.OfType<KIS.ModuleKISItem>().FirstOrDefault();
    if (itemModule != null && itemModule.volumeOverride > 0) {
      return itemModule.volumeOverride  // Ignore geometry.
          * KISAPI.PartNodeUtils.GetTweakScaleSizeModifier(partNode);  // But respect TweakScale.
    }
    var bounds = default(Bounds);
    VariantsUtils.ExecuteAtPartVariant(avPart, variant, p => {
      var partModel = GetSceneAssemblyModel(p).transform;
      bounds.Encapsulate(GetMeshBounds(partModel));
      UnityEngine.Object.DestroyImmediate(partModel.gameObject);
    });
    var boundsSize = bounds.size;
    return boundsSize.x * boundsSize.y * boundsSize.z * 1000f;
  }

  /// <summary>Returns part's volume basing on its geometrics.</summary>
  /// <remarks>
  /// The volume is calculated basing on the smallest boundary box that encapsulates all the meshes
  /// in the part. The deployable parts can take much more space in the deployed state.
  /// </remarks>
  /// <param name="part">The actual part, that exists in the scene.</param>
  /// <returns>The volume in liters.</returns>
  public double GetPartVolume(Part part) {
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
  public double GetPartDryMass(
      AvailablePart avPart, PartVariant variant = null, ConfigNode partNode = null) {
    var itemMass = avPart.partPrefab.mass;
    if (variant == null && partNode != null) {
      variant = VariantsUtils.GetCurrentPartVariant(avPart, partNode);
    }
    VariantsUtils.ExecuteAtPartVariant(avPart, variant, p => itemMass += p.GetModuleMass(p.mass));
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
  public double GetPartDryCost(
      AvailablePart avPart, PartVariant variant = null, ConfigNode partNode = null) {
    // TweakScale compatibility
    if (partNode != null) {
      var tweakScale = KISAPI.PartNodeUtils.GetTweakScaleModule(partNode);
      if (tweakScale != null) {
        var tweakedCost = ConfigAccessor.GetValueByPath<double>(tweakScale, "DryCost");
        if (tweakedCost.HasValue) {
          // TODO(ihsoft): Get back to this code once TweakScale supports variants.
          return tweakedCost.Value;
        }
        DebugEx.Error("No dry cost specified in a tweaked part {0}:\n{1}", avPart.name, tweakScale);
      }
    }
    var itemCost = avPart.cost;
    if (variant == null && partNode != null) {
      variant = VariantsUtils.GetCurrentPartVariant(avPart, partNode);
    }
    VariantsUtils.ExecuteAtPartVariant(avPart, variant,
                                       p => itemCost += p.GetModuleCosts(avPart.cost));
    return itemCost;
  }

  /// <summary>Traverses thru the hierarchy and gathers all the meshes from it.</summary>
  /// <param name="model">The root model to start from.</param>
  /// <param name="meshCombines">The collection to accumulate the meshes.</param>
  /// <param name="worldTransform">
  /// The optional world matrix to apply to the mesh. If not set, then the models world's matrix
  /// will be taken.
  /// </param>
  /// <param name="considerInactive">Tells if the inactive objects must be checked as well.</param>
  public void CollectMeshesFromModel(Transform model,
                                     ICollection<CombineInstance> meshCombines,
                                     Matrix4x4? worldTransform = null,
                                     bool considerInactive = false) {
    // Always use world transformation from the root.
    var rootWorldTransform = worldTransform ?? model.localToWorldMatrix.inverse;

    // Get all meshes from the part's model.
    var meshFilters = model
        .GetComponentsInChildren<MeshFilter>()
        // Prefab models are always inactive, so ignore the check.
        .Where(mf => considerInactive || mf.gameObject.activeInHierarchy)
        .ToArray();
    Array.ForEach(meshFilters, meshFilter => {
      var combine = new CombineInstance();
      combine.mesh = meshFilter.sharedMesh;
      combine.transform = rootWorldTransform * meshFilter.transform.localToWorldMatrix;
      meshCombines.Add(combine);
    });

    // Skinned meshes are baked on every frame before rendering.
    var skinnedMeshRenderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
    if (skinnedMeshRenderers.Length > 0) {
      foreach (var skinnedMeshRenderer in skinnedMeshRenderers) {
        var combine = new CombineInstance();
        combine.mesh = new Mesh();
        skinnedMeshRenderer.BakeMesh(combine.mesh);
        // BakeMesh() gives mesh in world scale, so don't apply it twice.
        var localToWorldMatrix = Matrix4x4.TRS(
            skinnedMeshRenderer.transform.position,
            skinnedMeshRenderer.transform.rotation,
            Vector3.one);
        combine.transform = rootWorldTransform * localToWorldMatrix;
        meshCombines.Add(combine);
      }
    }
  }

  /// <summary>Calculates bounds from the actual meshes of the model.</summary>
  /// <remarks>Note that the result depends on the model orientation.</remarks>
  /// <param name="model">The model to find the bounds for.</param>
  /// <param name="considerInactive">Tells if inactive meshes should be considered.</param>
  /// <returns></returns>
  Bounds GetMeshBounds(Transform model, bool considerInactive = false) {
    var combines = new List<CombineInstance>();
    CollectMeshesFromModel(model, combines, considerInactive: considerInactive);
    var bounds = default(Bounds);
    foreach (var combine in combines) {
      var mesh = new Mesh();
      mesh.CombineMeshes(new[] { combine });
      bounds.Encapsulate(mesh.bounds);
    }
    return bounds;
  }
}

}  // namespace
