// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

namespace KSPDev.GUIUtils {

/// <summary>
/// Localized message formatting class for a numeric value that represents a <i>pressure</i>. The
/// resulted message may have a unit specification.
/// </summary>
/// <remarks>
/// <para>
/// Use it as a generic parameter when creating a <see cref="LocalizableMessage"/> descendants.
/// </para>
/// <para>
/// The class uses the unit name localizations from the stock module <c>ModuleEnviroSensor</c>. In
/// case of this module is deprecated or the tags are changed, the default English values will be
/// used for the unit names.
/// </para>
/// </remarks>
/// <include file="SpecialDocTags.xml" path="Tags/MessageTypeWithArg/*"/>
/// <include file="SpecialDocTags.xml" path="Tags/MessageArgumentType/*"/>
//FIXME: examples
public sealed class PressureType {
  /// <summary>Localized suffix for the "kilopascal" units. Scale x1.</summary>
  public static readonly Message kiloPascal = new Message(
      "#autoLOC_7001408", defaultTemplate: " kPa",
      description: "Kilopascal unit for a pressure value");

  /// <summary>A wrapped numeric value.</summary>
  /// <remarks>This is the original non-rounded and unscaled value.</remarks>
  public readonly double value;

  /// <summary>Constructs an object from a numeric value.</summary>
  /// <param name="value">The numeric value in kilonewtons.</param>
  /// <seealso cref="Format"/>
  //FIXME: examples
  public PressureType(double value) {
    this.value = value;
  }

  /// <summary>Coverts a numeric value into a type object.</summary>
  /// <param name="value">The numeric value to convert.</param>
  /// <returns>An object.</returns>
  public static implicit operator PressureType(double value) {
    return new PressureType(value);
  }

  /// <summary>Converts a type object into a numeric value.</summary>
  /// <param name="obj">The object type to convert.</param>
  /// <returns>A numeric value.</returns>
  public static implicit operator double(PressureType obj) {
    return obj.value;
  }

  /// <summary>Formats the value into a human friendly string with a unit specification.</summary>
  /// <remarks>
  /// <para>
  /// The method tries to keep the resulted string meaningful and as short as possible. For this
  /// reason the big values may be scaled down and/or rounded.
  /// </para>
  /// <para>
  /// The base pressure unit in the game is <i>kilonewton</i>. I.e. value <c>1.0</c> in the game
  /// units is <i>one kilopascal</i>. Keep it in mind when passing the argument.
  /// </para>
  /// </remarks>
  /// <param name="value">The numeric value to format.</param>
  /// <param name="format">
  /// The specific float number format to use. If the format is not specified, then it's choosen
  /// basing on the value.
  /// </param>
  /// <returns>A formatted and localized string</returns>
  //FIXME: examples
  public static string Format(double value, string format = null) {
    if (format != null) {
      return value.ToString(format) + kiloPascal.Format();
    }
    if (value < 0.1) {
      return value.ToString("0.00#") + kiloPascal.Format();
    }
    if (value < 1.0) {
      return value.ToString("0.00") + kiloPascal.Format();
    }
    if (value < 10.0) {
      return value.ToString("0.0#") + kiloPascal.Format();
    }
    if (value < 100.0) {
      return value.ToString("0.#") + kiloPascal.Format();
    }
    return value.ToString("0") + kiloPascal.Format();
  }

  /// <summary>Returns a string formatted as a human friendly pressure specification.</summary>
  /// <returns>A string representing the value.</returns>
  /// <seealso cref="Format"/>
  public override string ToString() {
    return Format(value);
  }
}

}  // namespace
