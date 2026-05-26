using System;
using System.Collections.Generic;

public static class PristineMouseoverCore
{
    public const string Marker = " <color=#00ff00>(P)</color>";

    private const string DamagedSprite = "<sprite=\"FontSprites\" index=16>";
    private const string SelectedSuffix = "]</color>";

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

    public static string GetPristineMarkedName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (value.Contains("(P)")) return value;
        return value + Marker;
    }

    public static string AddMarkerToFirstLine(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (value.Contains("(P)")) return value;

        int newlineIndex = value.IndexOf('\n');
        if (newlineIndex < 0) return value + Marker;

        return value.Insert(newlineIndex, Marker);
    }

    public static string AddMarkersToWorldHoverList(string value, IList<CondOwner> cos)
    {
        if (string.IsNullOrWhiteSpace(value) || cos == null || cos.Count == 0) return value;

        string[] lines = value.Split('\n');
        int coIndex = 0;

        for (int i = 0; i < lines.Length && coIndex < cos.Count; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) continue;

            if (IsEligiblePristine(cos[coIndex]))
                lines[i] = AddMarkerToWorldHoverLine(lines[i]);

            coIndex++;
        }

        return string.Join("\n", lines);
    }

    private static string AddMarkerToWorldHoverLine(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (value.Contains("(P)")) return value;

        int damagedIndex = value.IndexOf(DamagedSprite, StringComparison.Ordinal);
        if (damagedIndex >= 0)
            return value.Insert(damagedIndex, Marker);

        int selectedIndex = value.IndexOf(SelectedSuffix, StringComparison.Ordinal);
        if (selectedIndex >= 0)
            return value.Insert(selectedIndex, Marker);

        return value + Marker;
    }
}
