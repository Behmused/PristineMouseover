using BepInEx;
using HarmonyLib;

[BepInPlugin("bemused.ostranauts.pristinemouseover", "Pristine Mouseover", "1.3.2")]
public class PristineMouseoverPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        LoggerStatic.Log = Logger;

        new Harmony("bemused.ostranauts.pristinemouseover").PatchAll();
        Logger.LogInfo("Pristine Mouseover 1.3.2 loaded.");
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
