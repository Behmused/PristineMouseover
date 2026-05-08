# PristineMouseover

Ostranauts mouseover mod that identifies Pristine items in-game by adding `(P)` to the standard hover tooltip.

---

# Updates

Currently being updated for build 0.15.0.29, v1.0.0 works but there is an error and it leaks (P) into the chat.
While not ideal, I've removed the broken release until it can be fixed. You may use 1.0.0 but beware of bugs.

---

## Features

* Adds `(P)` to item mouseover tooltip if item is Pristine
* Does not modify or depend on the mega tooltip
* Works automatically when hovering over items
* ![Pristine Mouseover Example](images/Screenshot_20260504_233453.png)
* ![Pristine Mouseover Example](images/Screenshot_20260504_233436.png)
* ![Pristine Mouseover Example](images/Screenshot_20260504_233411.png)

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

* Updated for Ostranauts Build 0.15.0.29
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
