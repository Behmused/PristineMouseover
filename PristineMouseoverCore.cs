using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

//────────────────────────────────────
// Core marker state and helpers
// Temporarily appends the pristine marker to CondOwner name fields during tooltip rendering,
// then restores the original strings before other UI systems can persist them.
//────────────────────────────────────
public static class PristineMouseoverCore
{
    public const string Marker = " <color=#00ff00>(P)</color>";

    private static readonly FieldInfo StrNameFriendlyField = GetStringField("strNameFriendly");
    private static readonly FieldInfo StrNameField = GetStringField("strName");
    private static readonly FieldInfo StrNameShortField = GetStringField("strNameShort");

    private static readonly Dictionary<CondOwner, Dictionary<string, string>> OriginalNames =
        new Dictionary<CondOwner, Dictionary<string, string>>();

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
        changedAny |= TryApplyPristineMarker(co, "strNameFriendly", StrNameFriendlyField);
        changedAny |= TryApplyPristineMarker(co, "strName", StrNameField);
        changedAny |= TryApplyPristineMarker(co, "strNameShort", StrNameShortField);

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
                FieldInfo field = GetCachedNameField(fieldEntry.Key);
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

    public static string GetPristineMarkedName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (value.Contains("(P)")) return value;
        return value + Marker;
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

    private static bool TryApplyPristineMarker(CondOwner co, string fieldName, FieldInfo field)
    {
        if (field == null)
        {
            LoggerStatic.Warn("Pristine Mouseover: missing CondOwner field: " + fieldName);
            return false;
        }

        string value = field.GetValue(co) as string;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Contains("(P)")) return false;

        StoreOriginal(co, fieldName, value);
        field.SetValue(co, value + Marker);
        return true;
    }

    private static FieldInfo GetCachedNameField(string fieldName)
    {
        if (string.Equals(fieldName, "strNameFriendly", StringComparison.Ordinal))
            return StrNameFriendlyField;

        if (string.Equals(fieldName, "strName", StringComparison.Ordinal))
            return StrNameField;

        if (string.Equals(fieldName, "strNameShort", StringComparison.Ordinal))
            return StrNameShortField;

        return null;
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
