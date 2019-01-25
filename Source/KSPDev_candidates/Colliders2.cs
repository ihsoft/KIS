// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using KSPDev.LogUtils;
using System;
using System.Linq;
using UnityEngine;

namespace KSPDev.ModelUtils {

/// <summary>Various tools to deal with procedural colliders.</summary>
public static class Colliders2 {
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
  public static float? GetSqrDistanceToPart(
      Vector3 point, Part part, Func<Collider, bool> filterFn = null) {
    float? minDistance = null;
    var colliders = part.transform.GetComponentsInChildren<Collider>()
        .Where(c => !c.isTrigger && c.enabled && c.gameObject.activeInHierarchy
               && (filterFn == null || filterFn(c)));
    foreach (var collider in colliders) {
      Vector3 closetsPoint;
      if (collider is WheelCollider) {
        // Wheel colliders don't support closets point check.
        closetsPoint = collider.ClosestPointOnBounds(point);
      } else {
        closetsPoint = collider.ClosestPoint(point);
        if (closetsPoint == collider.transform.position) {
          // The point it inside the collider or on the boundary.
          return 0.0f;
        }
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
  public static float GetSqrDistanceToPartOrDefault(
      Vector3 point, Part part,
      float defaultValue = float.PositiveInfinity, Func<Collider, bool> filterFn = null) {
    return GetSqrDistanceToPart(point, part) ?? defaultValue;
  }
}

}  // namespace
