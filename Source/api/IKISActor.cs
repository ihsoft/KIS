// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;

namespace KISAPIv1 {

/// <summary>Main actor interface that operate items and inventories.</summary>
public interface IKISActor {
  /// <summary>Tells if actor is a player controllable character.</summary>
  /// <summary>
  /// Kerbals can board the vessel, which changes their behavior in terms of accessing inventories.
  /// A non-kerbal part can be controlled remotely, but cannot board the vessel.
  /// </summary>
  /// <value><c>true</c> if the part is kerbal.</value>
  bool isKerbal { get; }

  /// <summary>Tells if kerbal is sitting <i>inside the vessel</i>.</summary>
  /// <remarks>A kerbal in the command seat is not boarded.</remarks>
  /// <value><c>true</c> if the part is kerbal and the kerbal is sitting inside a pod.</value>
  bool isBoarded { get; }

  /// <summary>Returns all the accessible inventories in range.</summary>
  /// <returns>The inventories that this actor can access.</returns>
  IKISInventory[] GetReachableInventories();
}

}  // namespace
