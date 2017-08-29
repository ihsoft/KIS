# Tag string format

The tag is a concatenation of three values:
1. Literal `#kisLOC_`.
2. The string represenation of a KIS module _number_ formatted as a two-digit decimal value. E.g. literal
   `09` means a decimal value `9`.
3. The number of a string within the module formatted as a three-difit decimal value. E.g. literal `009`
   means a decimal value `9`.

For example, tag `#kisLOC_12345` defines a string `345` (three hundred forty five) in the module `12` (twelve).

# Tag namespace reservation

_Every_ KIS module **must** explicitly reserve a module number here. The descendent modules must
not use the same namespace as the parent. No modules are allowed to define own values for the tags within the
`#kisLOC_00000` - `#kisLOC_99999` namespace.

| Module name                | Module | Namespace start | Namespace end |
| -------------------------- | ------ | --------------- | ------------- |
| ModuleKISInventory         | 0      | #kisLOC_00000   | #kisLOC_00999 |
| KISAddonPickup             | 1      | #kisLOC_01000   | #kisLOC_01999 |
| KIS_Item                   | 2      | #kisLOC_02000   | #kisLOC_02999 |
| KISAddonPointer            | 3      | #kisLOC_03000   | #kisLOC_03999 |
| ModuleKISItemAttachTool    | 4      | #kisLOC_04000   | #kisLOC_04999 |
| ModuleKISItemBomb          | 5      | #kisLOC_05000   | #kisLOC_05999 |
| ModuleKISItem              | 6      | #kisLOC_06000   | #kisLOC_06999 |
| ModuleKISItemBook          | 7      | #kisLOC_07000   | #kisLOC_07999 |
| ModuleKISItemEvaPropellant | 8      | #kisLOC_08000   | #kisLOC_08999 |
| Specail classes            | 99     | #kisLOC_99000   | #kisLOC_99999 |
| _Next available value_     | 9      | #kasLOC_09000   | #kisLOC_09999 |
