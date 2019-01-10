// Kerbal Inventory System
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;

namespace KISAPIv1 {

public interface IKISInventoryItem {
}

public interface IKISInventory {
  bool accessibleExternally { get; }
  bool accessibleInternally { get; }
//  IKISInventoryItem[] items { get; }
//  bool RemoveItem(IKISInventoryItem item);
//  bool AddItem(IKISInventoryItem item);
//  IKISInventoryItem CreateItem(AvailablePart avPart);
//  IKISInventoryItem CreateItem(Part prefabPart);
//  IKISInventoryItem CreateItem(ConfigNode partNode);
}

public interface IKISEvaInventory : IKISInventory {
}

public interface IKISPodInventory : IKISEvaInventory {
  bool isSeatOccupied { get; }
}

public interface IKISContainerInventory : IKISInventory {
}

}  // namespace
