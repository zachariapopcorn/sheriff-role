using BepInEx;
using BepInEx.IL2CPP;
using HarmonyLib;
using Reactor;
using Il2CppSystem.Collections.Generic;
using RoleManager;
using Hazel;
using ButtonManager;
using UnityEngine;

namespace SheriffMod
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    public class SheriffMod : BasePlugin
    {
        public const string Id = "SheriffMod";
        public static BepInEx.Logging.ManualLogSource log;

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            log = Log;
            log.LogMessage("Sheriff Mod has loaded");
            Harmony.PatchAll();
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Awake))]
        public static class ModManagerClass
        {
            public static void Postfix()
            {
                ModManager.Instance.ShowModStamp();
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                List<PlayerControl> crewmates = Utils.GetCrewmates(PlayerControl.AllPlayerControls);
                int index = new System.Random().Next(0, crewmates.Count);
                SheriffRole.sheriffID = crewmates[index].Data.PlayerId;
                MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)SheriffRole.SetSheriff, Hazel.SendOption.Reliable);
                writer.Write(SheriffRole.sheriffID);
                writer.EndMessage();
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class HandleRpcClass
        {
            public static void Postfix(byte callId, MessageReader reader)
            {
                if(callId == (byte)SheriffRole.SetSheriff)
                {
                    byte sheriffID = reader.ReadByte();
                    PlayerControl sheriff = Utils.GetPlayerById(sheriffID);
                    SheriffRole.sheriffID = sheriff.Data.PlayerId;
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        public static class IntroPatch
        {
            public static void Postfix(IntroCutscene __instance)
            {
                byte sheriffID = SheriffRole.sheriffID;
                
                if(sheriffID == PlayerControl.LocalPlayer.Data.PlayerId)
                {
                    __instance.Title.text = SheriffRole.name;
                    __instance.Title.color = SheriffRole.roleColor;
                    __instance.ImpostorText.text = "Kill the Imposter, but be smart about who you kill";
                    __instance.BackgroundBar.material.color = SheriffRole.roleColor;
                    Utils.GetPlayerById(sheriffID).nameText.color = SheriffRole.roleColor;
                }
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class HudStartPatch
        {
            public static void Postfix(HudManager __instance)
            {
                SheriffRole.sheriifKillButton = new CooldownButton(
                    () =>
                    {
                        PlayerControl playerKilled = Utils.getClosestPlayer(PlayerControl.LocalPlayer);
                        if(playerKilled == null) return;
                        if(playerKilled.Data.IsImpostor)
                        {
                            PlayerControl.LocalPlayer.RpcMurderPlayer(playerKilled);
                        } else
                        {
                            PlayerControl.LocalPlayer.RpcMurderPlayer(PlayerControl.LocalPlayer);
                        }
                    },
                    PlayerControl.GameOptions.KillCooldown - 4,
                    Properties.Resources.KillButton,
                    new Vector2(-0.125f, 0.125f),
                    () =>
                    {
                        if(PlayerControl.LocalPlayer.Data.IsDead) return false;
                        if(PlayerControl.LocalPlayer.Data.IsImpostor) return false;
                        if(!AmongUsClient.Instance.IsGameStarted) return false;
                        if(SheriffRole.sheriffID == PlayerControl.LocalPlayer.Data.PlayerId) return true;
                        return false;
                    },
                    __instance
                    );
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPatch
        {
            public static void Prefix(PlayerControl __instance, PlayerControl target)
            {
                if(SheriffRole.sheriffID == __instance.Data.PlayerId)
                {
                    __instance.Data.IsImpostor = true;
                }
            }
            public static void Postfix(PlayerControl __instance, PlayerControl target)
            {
                if (SheriffRole.sheriffID == __instance.Data.PlayerId)
                {
                    __instance.Data.IsImpostor = false;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class FixButtonPatch
        {
            public static void Postfix()
            {
                if(SheriffRole.sheriifKillButton == null) return;
                if(!AmongUsClient.Instance.IsGameStarted) return;
                PlayerControl closestPlayer = Utils.getClosestPlayer(PlayerControl.LocalPlayer);
                if(closestPlayer == null)
                {
                    SheriffRole.sheriifKillButton.Enabled = false;
                } else
                {
                    SheriffRole.sheriifKillButton.Enabled = true;
                }
            }
        }
    }
}