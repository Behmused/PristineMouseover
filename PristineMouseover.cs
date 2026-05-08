using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

[BepInPlugin("bemused.ostranauts.pristinemouseover", "Pristine Mouseover", "1.1.0")]
public class PristineMouseoverPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        LoggerStatic.Log = Logger;

        new Harmony("bemused.ostranauts.pristinemouseover").PatchAll();
        Logger.LogInfo("Pristine Mouseover loaded.");
    }
}

public static class LoggerStatic
{
    public static BepInEx.Logging.ManualLogSource Log;

    public static void LogInfo(string msg)
    {
        if (Log != null) Log.LogInfo(msg);
    }
}

[HarmonyPatch(typeof(CrewSim), "FindCOsAtMousePosition")]
public static class Patch_CrewSim_FindCOsAtMousePosition
{
    private static readonly Dictionary<CondOwner, Dictionary<string, string>> OriginalNames =
    new Dictionary<CondOwner, Dictionary<string, string>>();

    private static readonly string[] NameFields =
    {
        "strNameFriendly",
        "strName",
        "strNameShort"
    };

    private static void Postfix(List<CondOwner> __result)
    {
        RestoreOriginalNames();

        if (__result == null || __result.Count == 0) return;

        for (int i = 0; i < __result.Count; i++)
        {
            CondOwner co = __result[i];
            if (co == null) continue;
            if (string.IsNullOrWhiteSpace(co.strNameFriendly)) continue;
            if (co.strNameFriendly.StartsWith("Floor:")) continue;
            if (co.strNameFriendly == "Compartment") continue;
            if (!co.HasCond("IsPristine")) continue;

            ApplyPristineMarkerPublic(co);
            return;
        }
    }

    private static void RestoreOriginalNames()
    {
        foreach (var coEntry in OriginalNames)
        {
            CondOwner co = coEntry.Key;
            if (co == null) continue;

            foreach (var fieldEntry in coEntry.Value)
            {
                FieldInfo field = GetStringField(fieldEntry.Key);
                if (field == null) continue;

                field.SetValue(co, fieldEntry.Value);
            }
        }

        OriginalNames.Clear();
    }

    public static void ApplyPristineMarkerPublic(CondOwner co)
    {
        foreach (string fieldName in NameFields)
        {
            FieldInfo field = GetStringField(fieldName);
            if (field == null) continue;

            string value = field.GetValue(co) as string;
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (value.Contains("(P)")) continue;

            StoreOriginal(co, fieldName, value);
            field.SetValue(co, value + " <color=#00ff00>(P)</color>");
        }
    }

    private static void StoreOriginal(CondOwner co, string fieldName, string value)
    {
        if (!OriginalNames.ContainsKey(co))
            OriginalNames[co] = new Dictionary<string, string>();

        if (!OriginalNames[co].ContainsKey(fieldName))
            OriginalNames[co][fieldName] = value;
    }

    private static FieldInfo GetStringField(string fieldName)
    {
        return typeof(CondOwner).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
    }
}

[HarmonyPatch(typeof(GUITooltip))]
[HarmonyPatch("SetTooltip")]
[HarmonyPatch(new System.Type[] { typeof(CondOwner), typeof(GUITooltip.TooltipWindow) })]
public static class Patch_GUITooltip_SetTooltip_Inventory
{
    private static void Prefix(CondOwner co, GUITooltip.TooltipWindow window)
    {
        if (co == null) return;
        if (!co.HasCond("IsPristine")) return;

        Patch_CrewSim_FindCOsAtMousePosition.ApplyPristineMarkerPublic(co);
    }
}
