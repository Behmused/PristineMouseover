using BepInEx;
using HarmonyLib;
using System;

[BepInPlugin("bemused.ostranauts.pristinemouseover", "Pristine Mouseover", "1.1.1")]
public class PristineMouseoverPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        new Harmony("bemused.ostranauts.pristinemouseover").PatchAll();
        Logger.LogInfo("Pristine Mouseover 1.1.1 loaded.");
    }
}

internal static class PristineMouseoverUtil
{
    private const string MarkerPlain = " (P)";
    private const string MarkerColor = " <color=#00ff00>(P)</color>";

    public static bool ShouldMark(CondOwner co)
    {
        if (co == null) return false;
        if (!co.HasCond("IsPristine")) return false;
        if (string.IsNullOrWhiteSpace(co.strNameFriendly)) return false;
        if (co.strNameFriendly.StartsWith("Floor:")) return false;
        if (co.strNameFriendly == "Compartment") return false;

        return true;
    }

    public static void AddMarkerToTooltipText(CondOwner co, ref string text)
    {
        if (!ShouldMark(co)) return;
        if (string.IsNullOrWhiteSpace(text)) return;
        if (text.Contains("(P)")) return;

        string name = co.FriendlyName;
        if (string.IsNullOrWhiteSpace(name))
            name = co.strNameFriendly;
        if (string.IsNullOrWhiteSpace(name))
            name = co.strName;

        if (!string.IsNullOrWhiteSpace(name) && text.Contains(name))
        {
            text = text.Replace(name, name + MarkerColor);
            return;
        }

        string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
        if (lines.Length == 0) return;

        lines[0] = lines[0] + MarkerPlain;
        text = string.Join("\n", lines);
    }
}

[HarmonyPatch(typeof(GUITooltip), "TooltipTextFormat1")]
public static class Patch_GUITooltip_TooltipTextFormat1
{
    private static void Postfix(CondOwner condOwner, ref string __result)
    {
        PristineMouseoverUtil.AddMarkerToTooltipText(condOwner, ref __result);
    }
}

[HarmonyPatch(typeof(GUITooltip), "TooltipTextFormat2")]
public static class Patch_GUITooltip_TooltipTextFormat2
{
    private static void Postfix(CondOwner condOwner, ref string __result)
    {
        PristineMouseoverUtil.AddMarkerToTooltipText(condOwner, ref __result);
    }
}

[HarmonyPatch(typeof(GUITooltip), "TooltipTextFormat3")]
public static class Patch_GUITooltip_TooltipTextFormat3
{
    private static void Postfix(CondOwner co, Task2 task, ref string __result)
    {
        PristineMouseoverUtil.AddMarkerToTooltipText(co, ref __result);
    }
}
