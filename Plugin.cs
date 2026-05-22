using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

[BepInPlugin("bemused.ostranauts.pristinemouseover", "Pristine Mouseover", "1.3.0")]
public class PristineMouseoverPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        LoggerStatic.Log = Logger;

        new Harmony("bemused.ostranauts.pristinemouseover").PatchAll();
        Logger.LogInfo("Pristine Mouseover 1.3.0 loaded.");
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
