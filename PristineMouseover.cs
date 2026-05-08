using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

[BepInPlugin("bemused.ostranauts.pristinemouseover", "Pristine Mouseover", "1.2.0")]
public class PristineMouseoverPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        LoggerStatic.Log = Logger;

        new Harmony("bemused.ostranauts.pristinemouseover").PatchAll();
        Logger.LogInfo("Pristine Mouseover 1.2.0 loaded.");
    }
}

//────────────────────────────────────
// Logging
// Keeps normal release logging minimal. Runtime debug spam from TEST builds has been removed.
//────────────────────────────────────
public static class LoggerStatic
{
    public static BepInEx.Logging.ManualLogSource Log;

    public static void Warn(string msg)
    {
        if (Log != null) Log.LogWarning(msg);
    }
}

//────────────────────────────────────
// Core marker state and helpers
// Temporarily appends the pristine marker to CondOwner name fields during tooltip rendering,
// then restores the original strings before other UI systems can persist them.
//────────────────────────────────────
public static class PristineMouseoverCore
{
    public const string Marker = " <color=#00ff00>(P)</color>";

    private static readonly Dictionary<CondOwner, Dictionary<string, string>> OriginalNames =
    new Dictionary<CondOwner, Dictionary<string, string>>();

    private static readonly string[] NameFields =
    {
        "strNameFriendly",
        "strName",
        "strNameShort"
    };

    private static bool _inventoryTooltipPendingRestore;

    public static bool IsEligiblePristine(CondOwner co)
    {
        if (co == null) return false;

        if (string.IsNullOrWhiteSpace(co.strNameFriendly) &&
            string.IsNullOrWhiteSpace(co.strName) &&
            string.IsNullOrWhiteSpace(co.strNameShort))
            return false;

        // Compartment is a container/background target, not an item tooltip target.
        if (co.strNameFriendly == "Compartment" || co.strName == "Compartment" || co.strNameShort == "Compartment")
            return false;

        try
        {
            return co.HasCond("IsPristine");
        }
        catch (Exception ex)
        {
            LoggerStatic.Warn("Pristine Mouseover: HasCond(IsPristine) failed: " + ex.GetType().Name + ": " + ex.Message);
            return false;
        }
    }

    public static bool ApplyPristineMarker(CondOwner co)
    {
        if (co == null) return false;

        bool changedAny = false;

        foreach (string fieldName in NameFields)
        {
            FieldInfo field = GetStringField(fieldName);
            if (field == null)
            {
                LoggerStatic.Warn("Pristine Mouseover: missing CondOwner field: " + fieldName);
                continue;
            }

            string value = field.GetValue(co) as string;
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (value.Contains("(P)")) continue;

            StoreOriginal(co, fieldName, value);
            field.SetValue(co, value + Marker);
            changedAny = true;
        }

        return changedAny;
    }

    public static void RestoreOriginalNames()
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

    public static void MarkInventoryTooltipPendingRestore()
    {
        _inventoryTooltipPendingRestore = true;
    }

    public static bool ConsumeInventoryTooltipPendingRestore()
    {
        bool pending = _inventoryTooltipPendingRestore;
        _inventoryTooltipPendingRestore = false;
        return pending;
    }

    public static bool IsInventoryWindow(GUITooltip.TooltipWindow window)
    {
        return string.Equals(window.ToString(), "Inventory", StringComparison.OrdinalIgnoreCase);
    }

    // Used by generic tooltip patches to avoid feeding temporary name mutations into chat/log or MegaTooltip paths.
    public static bool IsBlockedContext()
    {
        string stack = StackSummary();

        return stack.IndexOf("MegaToolTip", StringComparison.OrdinalIgnoreCase) >= 0 ||
        stack.IndexOf("MegaTooltip", StringComparison.OrdinalIgnoreCase) >= 0 ||
        stack.IndexOf("Chat", StringComparison.OrdinalIgnoreCase) >= 0 ||
        stack.IndexOf("MessageLog", StringComparison.OrdinalIgnoreCase) >= 0 ||
        stack.IndexOf("LogEntry", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string StripPristineMarker(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        string cleaned = value.Replace(Marker, "");
        cleaned = cleaned.Replace(" <color=#00ff00>(P)</color>", "");
        cleaned = cleaned.Replace("<color=#00ff00>(P)</color>", "");
        cleaned = cleaned.Replace("(P)", "");
        return cleaned.TrimEnd();
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

    private static string StackSummary()
    {
        try
        {
            StackTrace trace = new StackTrace(2, false);
            StackFrame[] frames = trace.GetFrames();
            if (frames == null || frames.Length == 0) return string.Empty;

            List<string> names = new List<string>();
            int max = Math.Min(frames.Length, 8);
            for (int i = 0; i < max; i++)
            {
                MethodBase method = frames[i].GetMethod();
                if (method == null) continue;

                string typeName = method.DeclaringType != null ? method.DeclaringType.FullName : string.Empty;
                names.Add(typeName + "." + method.Name);
            }

            return string.Join(" | ", names.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }
}

//────────────────────────────────────
// World mouseover tooltip support
// The world hover list is produced by CrewSim.FindCOsAtMousePosition. The game later uses the returned CondOwners
// to render the visible world mouseover text, so all eligible pristine CondOwners are temporarily marked here.
//────────────────────────────────────
[HarmonyPatch(typeof(CrewSim), "FindCOsAtMousePosition")]
public static class Patch_CrewSim_FindCOsAtMousePosition
{
    private static void Prefix()
    {
        // Clear previous temporary world markers before calculating the next mouseover frame.
        PristineMouseoverCore.RestoreOriginalNames();
    }

    private static void Postfix(List<CondOwner> __result)
    {
        if (__result == null || __result.Count == 0) return;

        for (int i = 0; i < __result.Count; i++)
        {
            CondOwner co = __result[i];
            if (!PristineMouseoverCore.IsEligiblePristine(co)) continue;

            PristineMouseoverCore.ApplyPristineMarker(co);
        }
    }
}

//────────────────────────────────────
// Multi-object tooltip support
// Applies the marker during normal multi-CondOwner tooltip generation, then restores immediately after the call.
// Blocked contexts restore first so MegaTooltip/chat paths do not receive temporary names.
//────────────────────────────────────
[HarmonyPatch(typeof(GUITooltip))]
[HarmonyPatch("SetTooltipMulti")]
[HarmonyPatch(new Type[] { typeof(List<CondOwner>), typeof(GUITooltip.TooltipWindow) })]
public static class Patch_GUITooltip_SetTooltipMulti
{
    private static void Prefix(List<CondOwner> aCOs, GUITooltip.TooltipWindow window, ref bool __state)
    {
        __state = false;

        if (PristineMouseoverCore.IsBlockedContext())
        {
            PristineMouseoverCore.RestoreOriginalNames();
            return;
        }

        if (aCOs == null || aCOs.Count == 0) return;

        for (int i = 0; i < aCOs.Count; i++)
        {
            CondOwner co = aCOs[i];
            if (!PristineMouseoverCore.IsEligiblePristine(co)) continue;

            bool changed = PristineMouseoverCore.ApplyPristineMarker(co);
            if (changed) __state = true;
        }
    }

    private static void Postfix(bool __state)
    {
        if (__state)
            PristineMouseoverCore.RestoreOriginalNames();
    }
}

//────────────────────────────────────
// Single-object tooltip and inventory tooltip support
// Standard tooltips can be restored immediately after SetTooltip. Inventory tooltips build/render later,
// so inventory markers remain active until GUITooltip.Update finishes the render pass.
//────────────────────────────────────
[HarmonyPatch(typeof(GUITooltip))]
[HarmonyPatch("SetTooltip")]
[HarmonyPatch(new Type[] { typeof(CondOwner), typeof(GUITooltip.TooltipWindow) })]
public static class Patch_GUITooltip_SetTooltip_CondOwner
{
    private static void Prefix(CondOwner co, GUITooltip.TooltipWindow window, ref bool __state)
    {
        __state = false;

        if (PristineMouseoverCore.IsBlockedContext())
        {
            PristineMouseoverCore.RestoreOriginalNames();
            return;
        }

        if (!PristineMouseoverCore.IsEligiblePristine(co)) return;

        __state = PristineMouseoverCore.ApplyPristineMarker(co);

        if (__state && PristineMouseoverCore.IsInventoryWindow(window))
            PristineMouseoverCore.MarkInventoryTooltipPendingRestore();
    }

    private static void Postfix(bool __state, GUITooltip.TooltipWindow window)
    {
        if (!__state) return;

        if (PristineMouseoverCore.IsInventoryWindow(window))
            return;

        PristineMouseoverCore.RestoreOriginalNames();
    }
}

//────────────────────────────────────
// Interaction/task tooltip support
// Intentionally does not restore names here. Task/action UI can refresh while world mouseover is active;
// restoring in this path caused visible (P) flicker during task/chat updates in testing.
//────────────────────────────────────
[HarmonyPatch(typeof(GUITooltip))]
[HarmonyPatch("SetTooltipIA")]
[HarmonyPatch(new Type[] { typeof(Interaction), typeof(GUITooltip.TooltipWindow) })]
public static class Patch_GUITooltip_SetTooltipIA
{
    private static void Prefix(Interaction ia, GUITooltip.TooltipWindow window)
    {
        // No operation by design.
    }
}

//────────────────────────────────────
// MegaTooltip guard
// MegaTooltip reads item names through its own selection update path. Restore before that path consumes
// temporary mouseover names so Pristine MegaTooltip remains responsible for its own display.
//────────────────────────────────────
[HarmonyPatch]
public static class Patch_GUIMegaToolTip_OnSelectionUpdated
{
    private static MethodBase TargetMethod()
    {
        Type megaTooltipType = AccessTools.TypeByName("Ostranauts.UI.MegaToolTip.GUIMegaToolTip");
        if (megaTooltipType == null)
        {
            LoggerStatic.Warn("Pristine Mouseover: GUIMegaToolTip type not found; MegaTooltip guard was not applied.");
            return null;
        }

        MethodBase method = AccessTools.Method(megaTooltipType, "OnSelectionUpdated");
        if (method == null)
            LoggerStatic.Warn("Pristine Mouseover: GUIMegaToolTip.OnSelectionUpdated not found; MegaTooltip guard was not applied.");

        return method;
    }

    private static void Prefix()
    {
        PristineMouseoverCore.RestoreOriginalNames();
    }
}

//────────────────────────────────────
// Inventory render restore
// Inventory tooltip text is rendered after SetTooltip. This restores the temporary inventory marker after
// GUITooltip.Update has had a chance to render the marked name.
//────────────────────────────────────
[HarmonyPatch(typeof(GUITooltip), "Update")]
public static class Patch_GUITooltip_Update
{
    private static void Postfix()
    {
        if (!PristineMouseoverCore.ConsumeInventoryTooltipPendingRestore()) return;

        PristineMouseoverCore.RestoreOriginalNames();
    }
}

//────────────────────────────────────
// Chat/log guard
// Chat can capture item names while world mouseover markers are active. Strip the marker from message arguments
// without restoring live names, avoiding both chat leaks and mouseover flicker during task updates.
//────────────────────────────────────
[HarmonyPatch(typeof(CondOwner), "LogMessage")]
[HarmonyPatch(new Type[] { typeof(string), typeof(string), typeof(string), typeof(string) })]
public static class Patch_CondOwner_LogMessage
{
    private static void Prefix(ref string strMsg, string strColor, ref string strOwner, ref string strShort)
    {
        strMsg = PristineMouseoverCore.StripPristineMarker(strMsg);
        strOwner = PristineMouseoverCore.StripPristineMarker(strOwner);
        strShort = PristineMouseoverCore.StripPristineMarker(strShort);
    }
}
