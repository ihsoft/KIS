// Kerbal Inventory System
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.DebugUtils;

namespace KIS.Debug {

/// <summary>Debug adjustable class for KIS.</summary>
/// <remarks>
/// Annotate fields, properties and methods with this attribute to have them revealed in the KIS
/// part adjustment tool, a KIS built-in ability to tweak the parts in flight.
/// </remarks>
public class KISDebugAdjustableAttribute : DebugAdjustableAttribute {

  /// <summary>Debug controls group fro the KAS modules.</summary>
  public const string DebugGroup = "KIS";

  /// <summary>Creates an attribute that marks a KIS tweakable member.</summary>
  /// <param name="caption">The user freindly string to present in GUI.</param>
  public KISDebugAdjustableAttribute(string caption) : base(caption, DebugGroup) {
  }
}

}  // namespace
