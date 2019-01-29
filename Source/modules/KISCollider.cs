// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.DebugUtils;
using KSPDev.LogUtils;
using KSPDev.ModelUtils;
using KSPDev.KSPInterfaces;
using UnityEngine;

namespace KIS {

/// <summary>Module to add a simple collider to the game object.</summary>
/// <remarks>
/// Any existing collider on that object will be deleted. The new collider will be sized so that
/// it's completely <i>inside</i> the mesh. I.e. there can be mesh elements that don't have
/// collider (unless the selected type is "mesh collider").
/// <para>The skinned meshes are not supported!</para>
/// </remarks>
public sealed class KISCollider : PartModule,
    // KSPDev interfaces.
    IHasDebugAdjustables,
    // KSPDev sugar interfaces.
    IPartModule {

  #region Part's config
  /// <summary>Tells if a mesdh collider needs to be added.</summary>
  /// <remarks>
  /// This is an expensive collider, but it gives the best precision of the collision tests.
  /// </remarks>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Is mesh collider")]
  public bool meshCollider;

  /// <summary>The primitive type to which a simple collider will try to match.</summary>
  /// <remarks>
  /// The simple colliders are much cheaper in the collision tests, but that only approximate the
  /// shape of the mesh. The following shapes are supported, in order of increasing the collision
  /// test cost:
  /// <list type="bullet">
  /// <item><code>PrimitiveType.Cube</code>. Uses a box collider.</item>
  /// <item><code>PrimitiveType.Sphere</code>. Uses a sphere collider.</item>
  /// <item><code>PrimitiveType.Capsule</code>. Uses a capsule collider.</item>
  /// </list>
  /// </remarks>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Primitive type")]
  public PrimitiveType primitiveShape = PrimitiveType.Capsule;

  /// <summary>Path in the part's model to find the object.</summary>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  /// <seealso href="https://ihsoft.github.io/KSPDev_Utils/v1.0/html/M_KSPDev_ModelUtils_Hierarchy_FindPartModelByPath_1.htm"/>
  [KSPField]
  [Debug.KISDebugAdjustableAttribute("Mesh path")]
  public string meshPath = "";
  #endregion

  #region IHasDebugAdjustables implementation
  /// <inheritdoc/>
  public void OnBeforeDebugAdjustablesUpdate() {
    if (!meshCollider) {
    }
  }

  /// <inheritdoc/>
  public void OnDebugAdjustablesUpdated() {
    UpdateOrCreateCollider();
  }
  #endregion

  #region IPartModule implementation
  /// <inheritdoc/>
  public override void OnStart(StartState state) {
    base.OnStart(state);
    UpdateOrCreateCollider();
  }
  #endregion

  #region Local utility methods
  /// <summary>Updates or creates the collider.</summary>
  void UpdateOrCreateCollider() {
    var meshTransform = Hierarchy.FindPartModelByPath(part, meshPath);
    if (meshTransform == null) {
      HostedDebugLog.Error(this, "Cannot find mesh for path: {0}", meshPath);
      return;
    }
    var meshObj = meshTransform.gameObject;

    // Add or update the custom colldier.
    if (meshCollider) {
      UnityEngine.Object.DestroyImmediate(meshObj.GetComponent<Collider>());
      var collider = meshObj.AddComponent<MeshCollider>();
      collider.convex = true;
      HostedDebugLog.Info(this, "Added a mesh collider at {0}", collider.transform);
    } else {
      var meshBounds = default(Bounds);
      foreach (var filter in meshObj.GetComponents<MeshFilter>()) {
        meshBounds.Encapsulate(filter.sharedMesh.bounds);
      }
      HostedDebugLog.Fine(this, "Mesh bounds: {0}", meshBounds);
      if (meshBounds.extents.magnitude < float.Epsilon) {
        HostedDebugLog.Warning(
            this, "The mesh bounds are zero, not adding any collider: {0}", meshTransform);
        return;
      }

      // Add collider basing on the requested type.
      if (primitiveShape == PrimitiveType.Cube) {
        UnityEngine.Object.DestroyImmediate(meshObj.GetComponent<Collider>());
        var collider = meshObj.AddComponent<BoxCollider>();
        collider.center = meshBounds.center;
        collider.size = meshBounds.size;
        HostedDebugLog.Info(this, "Added a cube collider at {0}: center={1}, size={2}",
                            meshTransform, collider.center, collider.size);
      } else if (primitiveShape == PrimitiveType.Capsule) {
        UnityEngine.Object.DestroyImmediate(meshObj.GetComponent<Collider>());
        // TODO(ihsoft): Choose direction so that the volume is minimized.
        var collider = meshObj.AddComponent<CapsuleCollider>();
        collider.center = meshBounds.center;
        collider.direction = 2;  // Z axis
        collider.height = meshBounds.size.z;
        collider.radius = Mathf.Min(meshBounds.extents.x, meshBounds.extents.y);
        HostedDebugLog.Info(
            this, "Added a capsule collider at {0}: center={1}, height={2}, radius={3}",
            meshTransform, collider.center, collider.height, collider.radius);
      } else if (primitiveShape == PrimitiveType.Sphere) {
        UnityEngine.Object.DestroyImmediate(meshObj.GetComponent<Collider>());
        var collider = meshObj.AddComponent<SphereCollider>();
        collider.center = meshBounds.center;
        collider.radius = Mathf.Min(
            meshBounds.extents.x, meshBounds.extents.y, meshBounds.extents.z);
        HostedDebugLog.Info(
            this, "Added a spehere collider at {0}: center={1}, radius={2}",
            meshTransform, collider.center, collider.radius);
      } else {
        DebugEx.Error("Unsupported collider: {0}. Ignoring", primitiveShape);
      }
    }
  }
  #endregion
}

}  // namespace
