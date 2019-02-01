![Cover](https://github.com/ihsoft/KIS/raw/master/WikiImages/Cover.jpg)

KIS introduces new gameplay mechanics by adding a brand new inventory system and EVA usables items as tools. You want to build a rover on Duna from scratch? Or you've forgot to attach a solar panel to the planetary station? With KIS it's not a problem!

# Kerbal Inventory System (KIS)

The mod offers container parts of various size to deliver spare parts to the orbit or at the construction site.

* Tiny containers for light-weight rockets.
* Big containers for serious projects.
* Mk3 containers for really big constructions ideas!

Kerbals now have own backpack to hold small items, and some of them can be equipped on the model. AR goggles? Fancy hat? Or, maybe, a completely new helmet?! It's all possible now!

With special tools (provided by the mod) kerbals now can modifying teh existing vessels by adding or removing parts. It's even possible to build a whole new vessel from scratch!

_* Goggles, hats and helmets are provided by the third-party mods. E.g. "Kerbal Props"._

# Demo media

* MANUAL: [KIS for DUMMIES](https://github.com/ihsoft/KIS/blob/master/User%20Guide.pdf).
* VIDEO: [Debugging abilities for mod creators](https://www.youtube.com/watch?v=Mov6py7Mt4Y).
* VIDEO: [Full support for part variants and `TweakScale`](https://www.youtube.com/watch?v=K-jQbrXZMBc).

# Languages supported

![Русский](https://github.com/ihsoft/KIS/raw/master/WikiImages/Russian-small-flag.png) Русский

![Italiano](https://github.com/ihsoft/KIS/raw/master/WikiImages/Italian-small-flag.png) Italiano

![Español](https://github.com/ihsoft/KIS/raw/master/WikiImages/Spanish-small-flag.png) Español

![简体中文](https://github.com/ihsoft/KIS/raw/master/WikiImages/Chineese-small-flag.png) 简体中文

![Português](https://github.com/ihsoft/KIS/raw/master/WikiImages/Brazil-small-flag.png) Português

![Français](https://github.com/ihsoft/KIS/raw/master/WikiImages/French-small-flag.png) Français

# Support

You can support this and the other of my mods on [Patreon](https://www.patreon.com/ihsoft). This is where I post my ideas, prototypes and the features development progress.

# Other useful mods for EVA

If you want doing EVA comfortably, you really should consider adding these mods as well:

* [Kerbal Attachment System (KAS)](https://github.com/ihsoft/KAS). Need to link two vessels? Just send out your kerbals EVA! Don't forget to update their inventories, though.
* [Easy Vessel Switch (EVS)](https://github.com/ihsoft/EasyVesselSwitch). No more guessing how to switch to "that vessel" - simply point and click!
* [Surface Mounted Lights](https://github.com/ihsoft/SurfaceLights). Too dark for EVA on the other side of the moon? Problem solved with these ambient lights!

# How to install

* _Recommended_:
    * Install and run [CKAN](https://github.com/KSP-CKAN/CKAN/releases).
    * Search for "Kerbal Inventory System" or just "KIS", then install the mod.
    * Occasionally run CKAN client to update KIS (and other mods) to the latest version.
    * If you follow this path, then all the KIS dependencies will be updated _automatically_. It may save you a lot of time during the update.
* Manual:
    * Download the ZIP archive:
        * From [CurseForge](https://kerbal.curseforge.com/projects/kerbal-inventory-system-kis/files).
        * From [Spacedock](https://spacedock.info/mod/1909/Kerbal%20Inventory%20System%20%28KIS%29).
        * From [GitHub](https://github.com/ihsoft/KIS/releases).
    * If you have an older version of the mod in your game, you __must__ delete all the old files first! __Do not just copy over__, this will likely result in compatibility issues.
    * Unzip the release archive into the game's `GameData` folder.
        * Note, that names of the folders __must__ be exactly like in the archive or the mod __won't work__.
        * The release archive contains the minimum versions of the required dependencies: `ModuleManager` and `CommunityCatgeoryKit`. If your game has better versions, do not overwrite!
    * Verify the installation: the mod's `LICENSE.md` file must be located at `<game root>/GameData/KIS/LICENSE.md`.
* If you don't want seeing the fun parts in your game, you can remove them:
    * Find file `remove_fun_part_patch.txt` in the mod's folder.
    * Rename it into `remove_fun_part_patch.cfg`.
    * Move it one level up in the directory structure (into the `GameData` folder).
    * Now the fun parts won't show up even if you update the mod.

# Forum

Ask questions and propose suggestions on
[the forum](https://forum.kerbalspaceprogram.com/index.php?/topic/149848-15-kerbal-inventory-system-kis/).

# Development

To start your local building environment read [BUILD.md](https://github.com/ihsoft/KIS/blob/master/BUILD.md).

If you're going to make a pull request, please, read [the code rules](https://github.com/ihsoft/KIS/blob/master/Source/README.md) first.
Changes that don't follow the rules will be **rejected**.
