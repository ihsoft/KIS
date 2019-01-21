// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Linq;
using UnityEngine;

namespace KISAPIv1 {

/// <summary>Various methods to deal with the part's colliders.</summary>
public class ColliderUtilsImpl {

  /// <summary>
  /// Returns the minimum square distance to the nearest point on the part's collider surface.
  /// </summary>
  /// <remarks>This method skips triggers and inactive colliders.</remarks>
  /// <param name="point">The reference point to find distance for.</param>
  /// <param name="part">The part to check for.</param>
  /// <param name="filterFn">
  /// The filter function to apply to every collider. Return <c>false</c> from it to sklip the
  /// collider in the following checks.
  /// </param>
  /// <returns>The square distance or <c>null</c> if no colliders found.</returns>
  public float? GetSqrDistanceToPart(
      Vector3 point, Part part, Func<Collider, bool> filterFn = null) {
    float? minDistance = null;
    var colliders = part.transform.GetComponentsInChildren<Collider>()
        .Where(c => !c.isTrigger && c.enabled && c.gameObject.activeInHierarchy
               && (filterFn == null || filterFn(c)));
    foreach (var collider in colliders) {
      var closetsPoint = collider.ClosestPoint(point);
      if (closetsPoint == collider.transform.position) {
        // The point it inside the collider or on the boundary.
        return 0.0f;
      }
      var sqrMagnitude = (closetsPoint - point).sqrMagnitude;
      minDistance = Mathf.Min(minDistance ?? float.PositiveInfinity, sqrMagnitude);
    }
    return minDistance;
  }

  /// <summary>
  /// Returns the minimum square distance to the nearest point on the part's collider surface.
  /// </summary>
  /// <remarks>This method skips triggers and inactive colliders.</remarks>
  /// <param name="point">The reference point to find distance for.</param>
  /// <param name="part">The part to check for.</param>
  /// <param name="defaultValue">The value to return if no suitable colliders found.</param>
  /// <param name="filterFn">
  /// The filter function to apply to every collider. Return <c>false</c> from it to sklip the
  /// collider in the following checks.
  /// </param>
  /// <returns>
  /// The square distance or <paramref name="defaultValue"/> if no colliders found.
  /// </returns>
  public float GetSqrDistanceToPartOrDefault(Vector3 point, Part part,
                                             float defaultValue = float.PositiveInfinity,
                                             Func<Collider, bool> filterFn = null) {
    return GetSqrDistanceToPart(point, part) ?? defaultValue;
  }
}

}  // namespace
