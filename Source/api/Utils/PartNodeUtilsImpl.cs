// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using System.Linq;

namespace KISAPIv1 {

/// <summary>Various methods to deal with the parts configs.</summary>
public class PartNodeUtilsImpl {
  /// <summary>Gets various scale modifiers, applied by the third-party mods.</summary>
  /// <remarks>Only <c>TweakScale</c> is currently supported.</remarks>
  /// <param name="partNode">The part's persistent state config.</param>
  /// <returns>The scale ratio, comparing to the default's one.</returns>
  public float GetPartExternalScaleModifier(ConfigNode partNode) {
    var tweakScale = GetModuleNode(partNode, "TweakScale");
    if (tweakScale != null) {
      var defaultScale = ConfigAccessor2.GetValueByPath<float>(tweakScale, "defaultScale") ?? 1.0f;
      var currentScale = ConfigAccessor2.GetValueByPath<float>(tweakScale, "currentScale") ?? 1.0f;
      return currentScale / defaultScale;
    }
    return 1.0f;
  }

  /// <summary>Extracts a module config node from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <param name="moduleName">The name of the module to extract.</param>
  /// <returns>The module node or <c>null</c> if not found.</returns>
  public ConfigNode GetModuleNode(ConfigNode partNode, string moduleName) {
    var res = GetModuleNodes(partNode, moduleName);
    return res.Length > 0 ? res[0] : null;
  }

  /// <summary>Extracts a module config node from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <returns>The module node or <c>null</c> if not found.</returns>
  /// <typeparam name="T">The type of the module to get node for.</typeparam>
  public ConfigNode GetModuleNode<T>(ConfigNode partNode) {
    return GetModuleNode(partNode, typeof(T).Name);
  }

  /// <summary>Extracts all module config nodes from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <param name="moduleName">The name of the module to extract.</param>
  /// <returns>The array of found module nodes.</returns>
  public ConfigNode[] GetModuleNodes(ConfigNode partNode, string moduleName) {
    if (partNode.HasNode("PART")) {
      partNode = partNode.GetNode("PART");
    }
    return partNode.GetNodes("MODULE")
        .Where(m => m.GetValue("name") == moduleName)
        .ToArray();
  }

  /// <summary>Extracts all module config nodes from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <returns>The array of found module nodes.</returns>
  /// <typeparam name="T">The type of the module to get node for.</typeparam>
  public ConfigNode[] GetModuleNodes<T>(ConfigNode partNode) {
    return GetModuleNodes(partNode, typeof(T).Name);
  }
}

}  // namespace
