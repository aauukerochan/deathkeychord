using System.Linq;

namespace DeathKeyChord;

internal static class ChordFormatting
{
    public static string Format(Configuration c)
    {
        string main = VkChoices.Common.FirstOrDefault(x => x.Vk == c.MainVk).Label;
        if (string.IsNullOrEmpty(main)) main = $"VK 0x{c.MainVk:X2}";

        string mods = "";
        if (c.ModCtrl) mods += "Ctrl+";
        if (c.ModAlt) mods += "Alt+";
        if (c.ModShift) mods += "Shift+";
        if (c.ModWin) mods += "Win+";

        return mods + main;
    }
}
