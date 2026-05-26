using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

//────────────────────────────────────
// Standard item tooltip support
// GUITooltip.Update rebuilds normal inventory/world item tooltips from TooltipTextFormat1 every frame.
// Mark only the rendered first line so the underlying CondOwner names remain untouched.
//────────────────────────────────────
[HarmonyPatch(typeof(GUITooltip), "TooltipTextFormat1")]
public static class Patch_GUITooltip_TooltipTextFormat1
{
    private static void Postfix(CondOwner condOwner, ref string __result)
    {
        if (!PristineMouseoverCore.IsEligiblePristine(condOwner)) return;
        __result = PristineMouseoverCore.AddMarkerToFirstLine(__result);
    }
}

//────────────────────────────────────
// Multi-object tooltip support
// Task and stacked tooltip lines are built through TooltipTextFormat3.
// Apply the pristine marker to the rendered line instead of mutating shared name fields.
//────────────────────────────────────
[HarmonyPatch(typeof(GUITooltip), "TooltipTextFormat3")]
public static class Patch_GUITooltip_TooltipTextFormat3
{
    private static void Postfix(CondOwner co, ref string __result)
    {
        if (!PristineMouseoverCore.IsEligiblePristine(co)) return;
        __result = PristineMouseoverCore.AddMarkerToFirstLine(__result);
    }
}

//────────────────────────────────────
// World hover list support
// GUIItemList.Update assembles the on-screen mouseover list from CondOwner short names.
// Rewrite the finished list string so world hover keeps the original feature without editing CondOwner state.
//────────────────────────────────────
[HarmonyPatch(typeof(GUIItemList), "Update")]
public static class Patch_GUIItemList_Update
{
    private static readonly FieldInfo ItemListTextField = AccessTools.Field(typeof(GUIItemList), "txt_itemList");
    private static readonly FieldInfo ItemListField = AccessTools.Field(typeof(GUIItemList), "_itemList");
    private static readonly FieldInfo ItemCosField = AccessTools.Field(typeof(GUIItemList), "m_aCOs");

    private static void Postfix(GUIItemList __instance)
    {
        TMP_Text txtItemList = ItemListTextField?.GetValue(__instance) as TMP_Text;
        if (txtItemList == null) return;

        IList<CondOwner> cos = ItemCosField?.GetValue(__instance) as IList<CondOwner>;
        if (cos == null || cos.Count == 0) return;

        string markedText = PristineMouseoverCore.AddMarkersToWorldHoverList(txtItemList.text, cos);
        if (markedText == txtItemList.text) return;

        txtItemList.text = markedText;
        ItemListField?.SetValue(__instance, markedText);
    }
}

//────────────────────────────────────
// Quick bar title support
// The right-click action panel title is written directly from CondOwner.ShortName in GUIQuickBar.set_COTarget.
// Mark the displayed title here without changing CondOwner live fields.
//────────────────────────────────────
[HarmonyPatch(typeof(GUIQuickBar), "set_COTarget")]
public static class Patch_GUIQuickBar_set_COTarget
{
    private static readonly FieldInfo TxtTitleField = AccessTools.Field(typeof(GUIQuickBar), "_txtTitle");

    private static void Postfix(GUIQuickBar __instance, CondOwner value)
    {
        if (__instance == null) return;
        if (!PristineMouseoverCore.IsEligiblePristine(value)) return;

        TMP_Text txtTitle = TxtTitleField?.GetValue(__instance) as TMP_Text;
        if (txtTitle == null) return;

        txtTitle.text = PristineMouseoverCore.GetPristineMarkedName(txtTitle.text);
    }
}
