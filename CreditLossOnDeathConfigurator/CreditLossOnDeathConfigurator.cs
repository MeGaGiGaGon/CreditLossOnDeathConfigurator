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
        public const string PluginVersion = "1.0.2";
        private static ILHook _hook = null!;
        public static ConfigFile configFile = null!;
        public static ConfigEntry<float> creditPercentageLostPerUnrecoveredBody = null!;
        public static ConfigEntry<float> creditPercentageLostPerRecoveredBody   = null!;
        public static ConfigEntry<bool>  disableCreditLossAtCompany             = null!;
        public static ConfigEntry<bool>  scaleCreditLossByLobbySize             = null!;
        private void Awake()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{PluginName}.cfg"), true);
            creditPercentageLostPerUnrecoveredBody = configFile.Bind("General", "Credit Percentage Lost Per Unrecovered Body", 0.20f, "");
            creditPercentageLostPerRecoveredBody   = configFile.Bind("General", "Credit Percentage Lost Per Recovered Body",   0.08f, "By default 8% is lost on recovered bodies, though in vanilla it is not stated nor displayed on the end card.");
            disableCreditLossAtCompany             = configFile.Bind("General", "Disable Credit Loss At Company",              false, "Stops all credit loss from recovered/unrecovered bodies while at the company.");
            scaleCreditLossByLobbySize             = configFile.Bind("General", "Scale Credit Loss By Lobby Size",             false, "Scales based on a 4 player lobby to make it so that if only 1 person survives, the credit loss is the same accross all sizes.\nExample: 4 player lobby with 20% unrecovered, max is 20% * 3 = 60%. \nA five player lobby would be 20% * 3 (base max) / 4 (max unrecoverable) = 16% per body, since 15% * 4 = 60%. \nA 3 player lobby would be 20% * 3 / 2 = 30% per.\nThe same math applies to the recovered body percentage.");

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
            // orig code: float num = 0.2f;
            // replaces:              ^^^^
            c.GotoNext(
                x => x.MatchLdcR4(0.2f)
            );
            c.Remove();
            c.EmitDelegate<Func<float>>(() =>
            {
                configFile.Reload();
                if (disableCreditLossAtCompany.Value && StartOfRound.Instance.currentLevel.sceneName == "CompanyBuilding")
                {
                    return 0f;
                }
                else if (scaleCreditLossByLobbySize.Value)
                {
                    return creditPercentageLostPerUnrecoveredBody.Value * (3 / (StartOfRound.Instance.connectedPlayersAmount - 1));
                }
                else
                {
                    return creditPercentageLostPerUnrecoveredBody.Value;
                }
            });

            // orig code: terminal.groupCredits -= (int)((float)groupCredits * (num / 2.5f));
            // replaces:                                                              ^^^^
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
                if (disableCreditLossAtCompany.Value && StartOfRound.Instance.currentLevel.sceneName == "CompanyBuilding")
                {
                    return 0f;
                }
                else if (scaleCreditLossByLobbySize.Value)
                {
                    return creditPercentageLostPerUnrecoveredBody.Value / (creditPercentageLostPerRecoveredBody.Value * (3 / (StartOfRound.Instance.connectedPlayersAmount - 1)));
                } 
                else
                {
                    return creditPercentageLostPerUnrecoveredBody.Value / creditPercentageLostPerRecoveredBody.Value;
                }
            });

            // orig code: statsUIElements.penaltyAddition.text = $"{playersDead} casualties: -{num * 100f * (float)(playersDead - bodiesInsured)}%\n({bodiesInsured} bodies recovered)";
            // replaces:                                                                       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
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
                if (disableCreditLossAtCompany.Value && StartOfRound.Instance.currentLevel.sceneName == "CompanyBuilding")
                {
                    return 0f;
                }
                else if (scaleCreditLossByLobbySize.Value)
                {
                    return 100f * ((creditPercentageLostPerUnrecoveredBody.Value * (playersDead - bodiesInsured) * (3 / (StartOfRound.Instance.connectedPlayersAmount - 1))) + (creditPercentageLostPerRecoveredBody.Value * bodiesInsured * (3 / (StartOfRound.Instance.connectedPlayersAmount - 1))));
                }
                else
                {
                    return 100f * ((creditPercentageLostPerUnrecoveredBody.Value * (playersDead - bodiesInsured)) + (creditPercentageLostPerRecoveredBody.Value * bodiesInsured));
                }
            });
        }
    }
}
