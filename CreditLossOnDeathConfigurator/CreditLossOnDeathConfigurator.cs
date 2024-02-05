using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CreditLossOnDeathConfigurator
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class CreditLossOnDeathConfigurator : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "GiGaGon";
        public const string PluginName = "CreditLossOnDeathConfigurator";
        public const string PluginVersion = "1.0.1";
        private static ILHook _hook = null!;
        public static ConfigFile configFile = null!;
        public static ConfigEntry<float> creditPercentageLostPerUnrecoveredBody = null!;
        public static ConfigEntry<float> creditPercentageLostPerRecoveredBody = null!;
        private void Awake()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{PluginName}.cfg"), true);
            creditPercentageLostPerUnrecoveredBody = configFile.Bind("General", "Credit Percentage Lost Per Unrecovered Body", 0.20f, "");
            creditPercentageLostPerRecoveredBody   = configFile.Bind("General", "Credit Percentage Lost Per Recovered Body",   0.08f, "By default 8% is lost on recovered bodies, though in vanilla it is not stated nor displayed on the end card.");

            _hook = new ILHook(
                typeof(HUDManager).GetMethod("ApplyPenalty", BindingFlags.Instance | BindingFlags.Public), 
                SetBodyCreditReductionMultipliers
            );
        }
        private static void Unhook()
        {
            _hook?.Dispose();
        }

        public void SetBodyCreditReductionMultipliers(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchLdcR4(0.2f)
            );
            c.Remove();
            c.EmitDelegate<Func<float>>(() =>
            {
                configFile.Reload();
                return creditPercentageLostPerUnrecoveredBody.Value;
            });

            c.Goto(0);
            c.GotoNext(
                x => x.MatchLdloc(2),
                x => x.MatchConvR4(),
                x => x.MatchLdloc(0)
            );
            c.Index += 2;
            c.Remove();
            c.EmitDelegate<Func<float>>(() =>
            {
                return creditPercentageLostPerUnrecoveredBody.Value / creditPercentageLostPerRecoveredBody.Value;
            });

            c.GotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchBox<int>(),
                x => x.MatchLdloc(0)
            );
            c.Index += 2;
            c.RemoveRange(8);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate<Func<int, int, float>>((playersDead, bodiesInsured) =>
            {
                return 100f * ((creditPercentageLostPerUnrecoveredBody.Value * (playersDead - bodiesInsured)) + (creditPercentageLostPerRecoveredBody.Value * bodiesInsured));
            });
        }
    }
}
