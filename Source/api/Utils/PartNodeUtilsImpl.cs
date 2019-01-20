// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using KSPDev.LogUtils;
using System.Collections.Generic;
using System.Linq;

namespace KISAPIv1 {

/// <summary>Various methods to deal with the parts configs.</summary>
public class PartNodeUtilsImpl {

  /// <summary>Gets scale modifier, applied by TweakScale mod.</summary>
  /// <param name="partNode">The part's persistent state config.</param>
  /// <returns>The scale ratio.</returns>
  public float GetTweakScaleSizeModifier(ConfigNode partNode) {
    var ratio = 1.0f;
    var tweakScaleNode = GetTweakScaleModule(partNode);
    if (tweakScaleNode != null) {
      var defaultScale = ConfigAccessor2.GetValueByPath<float>(tweakScaleNode, "defaultScale");
      var currentScale = ConfigAccessor2.GetValueByPath<float>(tweakScaleNode, "currentScale");
      if (defaultScale.HasValue && currentScale.HasValue) {
        ratio = currentScale.Value / defaultScale.Value;
      } else {
        DebugEx.Error("Bad TweakScale config:\n{0}", tweakScaleNode);
      }
    }
    return ratio;
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

  /// <summary>Gets <c>TweakScale</c> module config.</summary>
  /// <param name="partNode">
  /// The config to extract the module config from. It can be <c>null</c>.
  /// </param>
  /// <returns>The <c>TweakScale</c> module or <c>null</c>.</returns>
  public ConfigNode GetTweakScaleModule(ConfigNode partNode) {
    return partNode != null ? GetModuleNode(partNode, "TweakScale") : null;
  }

  /// <summary>Creates a simplified snapshot of the part's persistent state.</summary>
  /// <remarks>
  /// This is not the same as a complete part persistent state. This state only captures the key
  /// module settings.
  /// </remarks>
  /// <param name="part">The part to snapshot. It must be a fully activated part.</param>
  /// <returns>The part's snapshot.</returns>
  public ConfigNode PartSnapshot(Part part) {
    if (ReferenceEquals(part, part.partInfo.partPrefab)) {
      // HACK: Prefab may have fields initialized to "null". Such fields cannot be saved via
      //   BaseFieldList when making a snapshot. So, go thru the persistent fields of all prefab
      //   modules and replace nulls with a default value of the type. It's unlikely we break
      //   something since by design such fields are not assumed to be used until loaded, and it's
      //   impossible to have "null" value read from a config.
      CleanupModuleFieldsInPart(part);
    }

    var partNode = new ConfigNode("PART");
    var snapshot = new ProtoPartSnapshot(part, null);

    snapshot.attachNodes = new List<AttachNodeSnapshot>();
    snapshot.srfAttachNode = new AttachNodeSnapshot("attach,-1");
    snapshot.symLinks = new List<ProtoPartSnapshot>();
    snapshot.symLinkIdxs = new List<int>();
    snapshot.Save(partNode);

    // Prune unimportant data.
    partNode.RemoveValues("parent");
    partNode.RemoveValues("position");
    partNode.RemoveValues("rotation");
    partNode.RemoveValues("istg");
    partNode.RemoveValues("dstg");
    partNode.RemoveValues("sqor");
    partNode.RemoveValues("sidx");
    partNode.RemoveValues("attm");
    partNode.RemoveValues("srfN");
    partNode.RemoveValues("attN");
    partNode.RemoveValues("connected");
    partNode.RemoveValues("attached");
    partNode.RemoveValues("flag");

    partNode.RemoveNodes("ACTIONS");
    partNode.RemoveNodes("EVENTS");
    foreach (var moduleNode in partNode.GetNodes("MODULE")) {
      moduleNode.RemoveNodes("ACTIONS");
      moduleNode.RemoveNodes("EVENTS");
    }

    return partNode;
  }

  #region Local utility methods
  /// <summary>Walks thru all modules in the part and fixes null persistent fields.</summary>
  /// <remarks>Used to prevent NREs in methods that persist KSP fields.
  /// <para>
  /// Bad modules that cannot be fixed will be dropped which may make the part to be not behaving as
  /// expected. It's guaranteed that the <i>stock</i> modules that need fixing will be fixed
  /// successfully. So, the failures are only expected on the modules from the third-parties mods.
  /// </para></remarks>
  /// <param name="part">The part to fix.</param>
  static void CleanupModuleFieldsInPart(Part part) {
    var badModules = new List<PartModule>();
    foreach (var moduleObj in part.Modules) {
      var module = moduleObj as PartModule;
      try {
        CleanupFieldsInModule(module);
      } catch {
        badModules.Add(module);
      }
    }
    // Cleanup modules that block KIS. It's a bad thing to do but not working KIS is worse.
    foreach (var moduleToDrop in badModules) {
      DebugEx.Error(
          "Module on part prefab {0} is setup improperly: name={1}. Drop it!", part, moduleToDrop);
      part.RemoveModule(moduleToDrop);
    }
  }

  /// <summary>Fixes null persistent fields in the module.</summary>
  /// <remarks>Used to prevent NREs in methods that persist KSP fields.</remarks>
  /// <param name="module">The module to fix.</param>
  static void CleanupFieldsInModule(PartModule module) {
    // HACK: Fix uninitialized fields in science lab module.
    var scienceModule = module as ModuleScienceLab;
    if (scienceModule != null) {
      scienceModule.ExperimentData = new List<string>();
      DebugEx.Warning(
          "WORKAROUND. Fix null field in ModuleScienceLab module on the part prefab: {0}", module);
    }
    
    // Ensure the module is awaken. Otherwise, any access to base fields list will result in NRE.
    // HACK: Accessing Fields property of a non-awaken module triggers NRE. If it happens then do
    // explicit awakening of the *base* module class.
    try {
      module.Fields.GetEnumerator();
    } catch {
      DebugEx.Warning(
          "WORKAROUND. Module {0} on part prefab is not awaken. Call Awake on it", module);
      module.Awake();
    }
    foreach (var field in module.Fields) {
      var baseField = field as BaseField;
      if (baseField.isPersistant && baseField.GetValue(module) == null) {
        var proto = new StandardOrdinaryTypesProto();
        var defValue = proto.ParseFromString("", baseField.FieldInfo.FieldType);
        DebugEx.Warning("WORKAROUND. Found null field {0} in module prefab {1},"
                        + " fixing to default value of type {2}: {3}",
                        baseField.name, module, baseField.FieldInfo.FieldType, defValue);
        baseField.SetValue(defValue, module);
      }
    }
  }
  #endregion
}

}  // namespace
