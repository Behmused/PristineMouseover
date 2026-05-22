# PristineMouseover

Adds a green (P) marker to Pristine items when hovering over them in-world or in inventory.

This update improves support for Ostranauts 0.15.0.29 (64), including stacked mouseover items and inventory tooltips. Multiple Pristine objects under the cursor can now be marked at the same time, such as floors, equipment, installed objects, and loose items.

Highlights:
- Adds (P) to Pristine in-world mouseovers
- Adds (P) to Pristine inventory tooltips
- Adds (P) to the right-click quick-bar/context title for Pristine items
- Supports multiple Pristine objects in the same mouseover stack
- Keeps the marker out of MegaTooltip and chat/log messages
- Lightweight UI-only helper for faster Pristine item identification

Tested/Working on Ostranauts 0.15.0.33 (64).

---

## Features

![Pristine Mouseover Example](images/Screenshot_20260504_233453.png)<br>&nbsp;<br>
![Pristine Mouseover Example](images/Screenshot_20260504_233436.png)<br>&nbsp;<br>
![Pristine Mouseover Example](images/Screenshot_20260504_233411.png)<br>&nbsp;<br>

---

## Requirements

* [BepInExPack_Ostranauts](https://new.thunderstore.io/c/ostranauts/p/BepInEx/BepInExPack_Ostranauts)

---

## Installation

1. Install BepInEx into your Ostranauts directory
2. Place `PristineMouseover.dll` into:

   ```
   Ostranauts/BepInEx/plugins/
   ```
3. Launch the game

---

## Compatibility

* Works alongside **PristineMegaTooltip**
* May conflict with other tooltip/UI mods

---

## Related Mod

**[Pristine MegaTooltip](https://github.com/Behmused/PristineMegaTooltip)**

* Removes clutter from the mega tooltip
* Displays condition labels (Pristine, Refurbished, Like New, Worn, Used)

---

## License

MIT
