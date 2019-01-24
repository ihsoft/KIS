// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using System;
using System.Linq;

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
  /// Reads a value of an arbitrary type <typeparamref name="T"/> from the config node.
  /// </summary>
  /// <param name="node">The node to read data from.</param>
  /// <param name="path">
  /// The path to the node. The path components should be separated by '/' symbol.
  /// </param>
  /// <param name="typeProto">
  /// A proto that can parse values of type <typeparamref name="T"/>. If not set, then
  /// <see cref="StandardOrdinaryTypesProto"/> is used.
  /// </param>
  /// <returns>The parsed value or <c>null</c> if not found.</returns>
  /// <typeparam name="T">
  /// The value type to write. The <paramref name="typeProto"/> instance must be able to handle it.
  /// </typeparam>
  /// <exception cref="ArgumentException">If type cannot be handled by the proto.</exception>
  /// <seealso cref="SetValueByPath&lt;T&gt;"/>
  public static T? GetValueByPath<T>(
      ConfigNode node, string path, AbstractOrdinaryValueTypeProto typeProto = null)
      where T : struct {
    return GetValueByPath<T>(node, ConfigAccessor.StrToPath(path), typeProto);
  }

  /// <summary>
  /// Reads a value of an arbitrary type <typeparamref name="T"/> from the config node.
  /// </summary>
  /// <param name="node">The node to read data from.</param>
  /// <param name="pathKeys">
  /// The array of values that makes the full path. The first node in the array is the top most
  /// component of the path.
  /// </param>
  /// <param name="typeProto">
  /// A proto that can parse values of type <typeparamref name="T"/>. If not set, then
  /// <see cref="StandardOrdinaryTypesProto"/> is used.
  /// </param>
  /// <returns>The parsed value or <c>null</c> if not found.</returns>
  /// <typeparam name="T">
  /// The value type to write. The <paramref name="typeProto"/> instance must be able to handle it.
  /// </typeparam>
  /// <exception cref="ArgumentException">If type cannot be handled by the proto.</exception>
  /// <seealso cref="SetValueByPath&lt;T&gt;"/>
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

  /// <summary>
  /// Saves a value of an arbitrary type <typeparamref name="T"/> to the config node.
  /// </summary>
  /// <param name="node">The node to write data to.</param>
  /// <param name="path">
  /// The path to the node. The path components should be separated by '/' symbol.
  /// </param>
  /// <param name="value">The value to write.</param>
  /// <param name="typeProto">
  /// A proto that can serialize values of type <typeparamref name="T"/>. If not set, then
  /// <see cref="StandardOrdinaryTypesProto"/> is used.
  /// </param>
  /// <typeparam name="T">
  /// The value type to write. The <paramref name="typeProto"/> instance must be able to handle it.
  /// </typeparam>
  /// <exception cref="ArgumentException">If type cannot be handled by the proto.</exception>
  /// <seealso cref="GetValueByPath&lt;T&gt;"/>
  public static void SetValueByPath<T>(
      ConfigNode node, string path, T value, AbstractOrdinaryValueTypeProto typeProto = null)
      where T : struct {
    SetValueByPath<T>(node, ConfigAccessor.StrToPath(path), value, typeProto);
  }

  /// <summary>
  /// Saves a value of an arbitrary type <typeparamref name="T"/> to the config node.
  /// </summary>
  /// <param name="node">The node to write data to.</param>
  /// <param name="pathKeys">
  /// The array of values that makes the full path. The first node in the array is the top most
  /// component of the path.
  /// </param>
  /// <param name="value">The value to write.</param>
  /// <param name="typeProto">
  /// A proto that can serialize values of type <typeparamref name="T"/>. If not set, then
  /// <see cref="StandardOrdinaryTypesProto"/> is used.
  /// </param>
  /// <typeparam name="T">
  /// The value type to write. The <paramref name="typeProto"/> instance must be able to handle it.
  /// </typeparam>
  /// <exception cref="ArgumentException">If type cannot be handled by the proto.</exception>
  /// <seealso cref="GetValueByPath&lt;T&gt;"/>
  public static void SetValueByPath<T>(
      ConfigNode node, string[] pathKeys, T value, AbstractOrdinaryValueTypeProto typeProto = null)
      where T : struct {
    if (typeProto == null) {
      typeProto = standardTypesProto;
    }
    ConfigAccessor.SetValueByPath(node, pathKeys, typeProto.SerializeToString(value));
    var strValue = ConfigAccessor.GetValueByPath(node, pathKeys);
  }
}
  
}  // namespace
