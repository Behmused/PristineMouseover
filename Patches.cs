using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

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
