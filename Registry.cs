

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using AssetBundles;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RerollArtifact
{
    public static class Registry
    {
        public static bool rerollPossible = true;
        public static List<string> registry = new List<string>();
        public static Dictionary<string, int> storedStrength = new Dictionary<string, int>();
        public static int NUM_BATTLES = 1;

        public static void Register(string artifact)
        {
            if (!storedStrength.ContainsKey(artifact))
            {
                registry.Add(artifact);
                storedStrength.Add(artifact, NUM_BATTLES);
            }
        }

        public static void SetBattlesForReroll(int amount)
        {
            NUM_BATTLES = amount;
            var keys = storedStrength.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                storedStrength[keys[i]] = amount;
            }
        }
    }

    [HarmonyPatch]
    public static class RefreshPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RunCtrl), nameof(RunCtrl.StartCampaign))]
        public static void ResetAllRolls()
        {
            foreach (var artifact in Registry.registry)
            {
                Registry.storedStrength[artifact] = Registry.NUM_BATTLES;
            }
            Registry.rerollPossible = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BC), nameof(BC.EndBattle))]
        public static void ResetBattleRerolls()
        {
            bool b = false;
            foreach (var artifact in Registry.registry)
            {
                bool next_charged = Registry.storedStrength[artifact] == Registry.NUM_BATTLES-1;
                Registry.storedStrength[artifact] = Math.Min(Registry.storedStrength[artifact]+1, Registry.NUM_BATTLES);
                var arts = S.I.batCtrl.currentPlayer.artObjs.Where(artObj => artObj.itemID == artifact);
                if (next_charged && arts.Count() > 0)
                {
                    foreach (var art in arts)
                    {
                        art.Replete();
                    }
                    if (!b)
                    {
                        b = true;
                    }
                }
            }
            if (b)
            {
                var AllAudioClips = Traverse.Create(S.I.itemMan).Field("allAudioClips").GetValue<Dictionary<String, AudioClip>>();
                S.I.batCtrl.currentPlayer.PlayOnce(AllAudioClips["laser_recharge"]);
            }
            Registry.rerollPossible = true;
        }
    }

    [HarmonyPatch]
    public static class UsePatches
    {

        public static bool level = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostCtrl), nameof(PostCtrl.EndLoot))]
        static void EndLootPostix(PostCtrl __instance, RewardType rewardType, bool skipped)
        {
            if (rewardType !=  RewardType.Upgrade && skipped && Registry.rerollPossible)
            {
                var arts = S.I.batCtrl.currentPlayer.artObjs.Where(artObj => (!artObj.depleted && Registry.registry.Contains(artObj.itemID)));
                if (arts.Count() > 0)
                {
                    Registry.storedStrength[arts.ToList()[0].itemID] = 0;
                    arts.ToList()[0].Deplete();
                    __instance.GenerateLootOptions(rewardType);
                    Registry.rerollPossible = false;
                    return;
                }            
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PostCtrl), nameof(PostCtrl.StartLevelUpOptions))]
        static bool StartLevelPrefix(PostCtrl __instance)
        {
            var field = AccessTools.Field(typeof(PostCtrl), "levelsGained");
            if ((int)field.GetValue(__instance) > 0) level = true;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostCtrl), nameof(PostCtrl.UpdateExpBar))]
        static void UpdateEXPPostfix(PostCtrl __instance)
        {
            Registry.rerollPossible = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PostCtrl), nameof(PostCtrl.EndLevelUpOptions))]
        static bool EndLevelPrefix(PostCtrl __instance, bool skipped)
        {
            if (level && skipped && Registry.rerollPossible)
            {
                var arts = S.I.batCtrl.currentPlayer.artObjs.Where(artObj => (!artObj.depleted && Registry.registry.Contains(artObj.itemID)));
                if (arts.Count() > 0)
                {
                    var list = arts.ToList();
                    Registry.storedStrength[list[0].itemID] = 0;
                    list[0].Deplete();
                    var method = AccessTools.Method(typeof(PostCtrl), "ClearAndHideCards");
                    method.Invoke(__instance, null);
                    var field = AccessTools.Field(typeof(PostCtrl), "levelsGained");
                    field.SetValue(__instance, (int)field.GetValue(__instance) + 1);
                    __instance.StartLevelUpOptions();
                    Registry.rerollPossible = false;
                }
            }
            level = false;
            return true;
        }
    }
}


