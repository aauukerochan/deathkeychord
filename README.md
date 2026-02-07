# DeathKeyChord

Automatically mutes you in Discord when you die in Final Fantasy XIV.

---

## Overview

**DeathKeyChord** is a Dalamud plugin that sends a configurable keybind when your character dies and releases it when you are revived.

It is designed for use with **Discord Push-to-Mute or Toggle Mute**, without relying on Discord APIs or bots.

---

## Default behavior

- **Key:** `F24`
- **Mode:** Hold while dead (recommended)

When you die:
- The key is held → Discord mutes you

When you are revived:
- The key is released → Discord unmutes you

---

## Discord setup

1. Open **Discord → Settings → Keybinds**
2. Add a keybind:
   - **Action:** Push-to-Mute *(recommended)*  
     *(Toggle Mute also works if tap mode is enabled in the plugin)*
3. Press **F24** as the key

**Why F24?**  
F24 is a standard function key supported by the OS and Discord, but it is almost never used by applications or games.  
This avoids conflicts with FFXIV, overlays, or other software.

---

## Configuration

The plugin settings allow you to:

- Enable or disable the plugin
- Choose between **Hold while dead** and **Tap on death / revive**
- Change the key or key chord (modifier keys supported)
- Test the configured keybind
- Reset to the recommended default (F24)

---

## Platform support

- **Windows:** Supported  
- **Linux (via Wine / Proton):** Supported  
- **Native Linux / macOS:** Not supported  

---

## Notes

- No Discord API usage
- No bots or webhooks
- All muting is performed by sending a local keybind
- The plugin does not modify gameplay or network behavior

---

## Recommended setup

For the most reliable experience:

- Use **Push-to-Mute** in Discord
- Keep **Hold while dead** enabled
- Leave the key set to **F24**

