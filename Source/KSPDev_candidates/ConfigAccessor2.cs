// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using System;
using System.Linq;
using KSPDev.FSUtils;
using KSPDev.LogUtils;

namespace KSPDev.ConfigUtils {

/// <summary>A service class that simplifies accessing configuration files.</summary>
/// <remarks>This class provides a lot of useful methods to deal with values in game's configuration
/// files. There are low level methods that deal with nodes and values, and there are high level
/// methods that use metadata from the annotated fields.</remarks>
/// <seealso cref="PersistentFieldAttribute"/>
/// <seealso cref="PersistentFieldsFileAttribute"/>
/// <seealso cref="PersistentFieldsDatabaseAttribute"/>
public static class ConfigAccessor2 {
  static readonly StandardOrdinaryTypesProto standardTypesProto =
      new StandardOrdinaryTypesProto();

  /// <summary>
  /// Reads a value of arbitrary type <typeparamref name="T"/> from a config node.
  /// </summary>
  /// <param name="node">A node to read data from.</param>
  /// <param name="path">A string path to the node. Path components should be separated by '/'
  /// symbol.</param>
  /// <param name="value">A variable to read value into. The <paramref name="typeProto"/> handler
  /// must know how to convert value's type from string.</param>
  /// <param name="typeProto">A proto capable to handle the type of <paramref name="value"/>. If not
  /// set then <see cref="StandardOrdinaryTypesProto"/> is used.</param>
  /// <returns><c>true</c> if value was successfully read and stored.</returns>
  /// <typeparam name="T">The value type to read. Type proto must be able to handle it.
  /// </typeparam>
  /// <exception cref="ArgumentException">If type cannot be handled by the proto.</exception>
  public static T? GetValueByPath<T>(
      ConfigNode node, string path, AbstractOrdinaryValueTypeProto typeProto = null)
      where T : struct {
    return GetValueByPath<T>(node, ConfigAccessor.StrToPath(path), typeProto);
  }

  /// <summary>
  /// Reads a value of arbitrary type <typeparamref name="T"/> from a config node.
  /// </summary>
  /// <param name="node">A node to read data from.</param>
  /// <param name="pathKeys">An array of values that makes the full path. First node in the array is
  /// the top most component of the path.</param>
  /// <param name="value">A variable to read value into. The <paramref name="typeProto"/> handler
  /// must know how to convert value's type from string.</param>
  /// <param name="typeProto">A proto capable to handle the type of <paramref name="value"/>. If not
  /// set then <see cref="StandardOrdinaryTypesProto"/> is used.</param>
  /// <returns><c>true</c> if value was successfully read and stored.</returns>
  /// <typeparam name="T">The value type to read. Type proto must be able to handle it.
  /// </typeparam>
  /// <exception cref="ArgumentException">If type cannot be handled by the proto.</exception>
  public static T? GetValueByPath<T>(
      ConfigNode node, string[] pathKeys, AbstractOrdinaryValueTypeProto typeProto = null)
      where T : struct {
    if (typeProto == null) {
      typeProto = standardTypesProto;
    }
    if (!typeProto.CanHandle(typeof(T))) {
      throw new ArgumentException(string.Format(
          "Proto {0} cannot handle type {1}", typeProto.GetType(), typeof(T)));
    }
    var strValue = ConfigAccessor.GetValueByPath(node, pathKeys);
    return strValue == null
        ? null
        : (T?)typeProto.ParseFromString(strValue, typeof(T));
  }
}
  
}  // namespace
