#if !UNITY_EDITOR

using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using GamePanelHUDCore.Models;
using GamePanelHUDKill.Models;
using KmyTarkovReflection;
using UnityEngine;

namespace GamePanelHUDKill
{
    public partial class GamePanelHUDKillPlugin
    {
        // AI PMCs turn out to fire BOTH Player.OnBeenKilledByAggressor AND
        // BotOwner.OnBeenKilledByAggressor for the same death (plain Scavs apparently only fire the
        // BotOwner one - that asymmetry is exactly why Scav kills were missing before hooking
        // BotOwner, and why PMC kills double up now that both are hooked). A given victim ProfileId
        // can only die once per raid, so dedupe on that rather than trying to guess which single
        // call site is "the" authoritative one for every AI type. Cleared on raid end in
        // Controllers/KillHUDController.cs alongside the existing KillCount reset.
        internal static readonly HashSet<string> ShownKillProfileIds = new HashSet<string>();

        // Reflecting the live 0.16.9.40743 client turned up TWO distinct death-notification call
        // sites, not one:
        //   EFT.Player.OnBeenKilledByAggressor(IPlayer, DamageInfo, EBodyPart, EDamageType)
        //     - virtual, overridden by EFT.LocalPlayer. This is the path used when the dying
        //       character is a real (local/observed) player.
        //   EFT.BotOwner.OnBeenKilledByAggressor(Player player, IPlayer lastAggressor,
        //       DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
        //     - a completely separate, non-virtual method on the AI controller, taking the dying
        //       Player as an explicit parameter rather than being called *on* it.
        // Only the first was hooked before, which explains the exact reported symptom: hit markers
        // (a different, always-fires signal) worked, but the kill feed - wired only to
        // Player.OnBeenKilledByAggressor - never appeared for AI/bot kills, since bots are notified
        // of their own death through BotOwner instead. Hooking both covers PvP and PvE kills.
        // Called from GamePanelHUDKillPlugin.Start(), alongside the existing Player-level hook.
        internal static void RegisterBotHook()
        {
            RefHelper.HookRef.Create(typeof(BotOwner), "OnBeenKilledByAggressor")
                .Add(typeof(GamePanelHUDKillPlugin), nameof(OnBotBeenKilledByAggressor));
        }

        private static void OnBeenKilledByAggressor(Player __instance, IPlayer aggressor, DamageInfo damageInfo,
            EBodyPart bodyPart)
        {
            if (aggressor != HUDCoreModel.Instance.YourPlayer)
                return;

            ShowKillFeed(__instance, aggressor, damageInfo, bodyPart);
        }

        private static void OnBotBeenKilledByAggressor(BotOwner __instance, Player player, IPlayer lastAggressor,
            DamageInfo lastDamageInfo, EBodyPart lastBodyPart)
        {
            if (lastAggressor != HUDCoreModel.Instance.YourPlayer)
                return;

            ShowKillFeed(player, lastAggressor, lastDamageInfo, lastBodyPart);
        }

        private static void ShowKillFeed(Player victim, IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart)
        {
            if (!ShownKillProfileIds.Add(victim.ProfileId))
                return;

            var killHUDModel = KillHUDModel.Instance;
            var settings = victim.Profile.Info.Settings;

            var hasMarkOfUnknown = aggressor.HasMarkOfUnknown(out var markOfUnknown);

            var killModel = new KillModel
            {
                PlayerName = victim.Profile.Nickname,
                WeaponName = damageInfo.Weapon.ShortName,
                Part = bodyPart,
                Distance = Vector3.Distance(aggressor.Position, victim.Position),
                Level = victim.Profile.Info.Level,
                Side = victim.Profile.Info.Side,
                Exp = settings.Experience,
                Role = settings.Role,
                KillCount = killHUDModel.KillCount++,
                ScavKillExpPenalty = markOfUnknown?.ScavKillExpPenalty ?? 0,
                HasMarkOfUnknown = hasMarkOfUnknown,
                IsAI = victim.IsAI,
            };

            killHUDModel.ShowKill(killModel);
        }
    }
}

#endif
