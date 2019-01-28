// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using System;

namespace KSPDev.MathUtils {

/// <summary>Gives extra methods for handling float values in the game.</summary>
public static class Mathf2 {
  /// <summary>Value which can be safely considered to be <c>0</c>.</summary>
  public const double Epsilon = 1E-06;

  /// <summary>Tells if the two floats are the same, allowing some small error.</summary>
  /// <remarks>
  /// This method requires the difference between the values to be negligible. The absolute values
  /// are not counted.
  /// </remarks>
  /// <param name="a">The first value to test.</param>
  /// <param name="b">The second value to test.</param>
  /// <returns>
  /// <c>true</c> if the values difference is negligible.
  /// </returns>
  public static bool AreSame(float a, float b) {
    return Math.Abs(b - a) < Epsilon;
  }
}

}  // namespace
