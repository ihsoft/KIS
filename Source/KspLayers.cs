using System;

namespace KIS {

/// <summary>Flags for the collision layers in KSP.</summary>
/// <remarks>It's not a full set of the layers. More investigation is needed to reveal all of them.
/// </remarks>
[Flags]
public enum KspLayers {
  NONE = 0,
  
  /// <summary>A level for a regular part.</summary>
  PARTS = 1 << 0,
  
  /// <summary>A layer to set bounds of a celestial body.</summary>
  /// <remarks>A very rough boundary of a planet, moon or asteroid.</remarks>
  SERVICE_LAYER = 1 << 10,  // 

  /// <summary>"Zero" level of a static structure on the surface.</summary>
  /// <remarks>E.g. a launchpad.</remarks>
  SURFACE = 1 << 15,

  /// <summary>A layer for FX.</summary>
  /// <remarks>E.g. <c>PadFXReceiver</c> on the Kerbins VAB launchpad.</remarks>
  FX = 1 << 30,

  /// <summary>An unindentified layer number 1.</summary>
  UNKNOWN_1,

  /// <summary>An unindentified layer number 19.</summary>
  UNKNOWN_19,

  /// <summary>A set of layers to check when finding parts attach place.</summary>
  /// <remarks>This is a set of flags used in KIS by default. Real value of layers 1 and 19 is
  /// unknown.</remarks>
  COMMON = PARTS | SURFACE | UNKNOWN_1 | UNKNOWN_19,
};

}  // namespace
