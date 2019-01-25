// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using System;
using System.Linq;

namespace KSPDev.ConfigUtils {

/// <summary>Various methods to deal with the config nodes of the parts.</summary>
public static class PartNodeUtils {
  /// <summary>Extracts a module config node from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <param name="moduleName">The name of the module to extract.</param>
  /// <returns>The module node or <c>null</c> if not found.</returns>
  public static ConfigNode GetModuleNode(ConfigNode partNode, string moduleName) {
    var res = GetModuleNodes(partNode, moduleName);
    return res.Length > 0 ? res[0] : null;
  }

  /// <summary>Extracts a module config node from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <returns>The module node or <c>null</c> if not found.</returns>
  /// <typeparam name="T">The type of the module to get node for.</typeparam>
  public static ConfigNode GetModuleNode<T>(ConfigNode partNode) {
    return GetModuleNode(partNode, typeof(T).Name);
  }

  /// <summary>Extracts all module config nodes from the part config.</summary>
  /// <param name="partNode">
  /// The part's config. It can be a top-level node or the <c>PART</c> node.
  /// </param>
  /// <param name="moduleName">The name of the module to extract.</param>
  /// <returns>The array of found module nodes.</returns>
  public static ConfigNode[] GetModuleNodes(ConfigNode partNode, string moduleName) {
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
  public static ConfigNode[] GetModuleNodes<T>(ConfigNode partNode) {
    return GetModuleNodes(partNode, typeof(T).Name);
  }
}

}  // namespace
