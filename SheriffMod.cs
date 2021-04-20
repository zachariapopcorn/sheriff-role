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
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                log.LogMessage("SetInfectedPatch is running...");
                List<PlayerControl> crewmates = GetCrewmates(PlayerControl.AllPlayerControls);
                int index = new System.Random().Next(0, crewmates.Count);
                SheriffRole.sheriffID = crewmates[index].Data.PlayerId;
                log.LogMessage("Set Sheriff to user with name of the following: " + crewmates[index].name);
                MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)SheriffRole.SetSheriff, Hazel.SendOption.Reliable);
                writer.Write(SheriffRole.sheriffID);
                writer.EndMessage();
            }
            public static List<PlayerControl> GetCrewmates(List<PlayerControl> players)
            {
                List<PlayerControl> crewmates = new List<PlayerControl>();
                for(int i = 0; i < players.Count; i++)
                {
                    if(!players[i].Data.IsImpostor)
                    {
                        crewmates.Add(players[i]);
                    }
                }
                return crewmates;
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class HandleRpcClass
        {
            public static void Postfix(byte ONIABIILFGF, MessageReader JIGFBHFFNFI)
            {
                if(ONIABIILFGF == (byte)SheriffRole.SetSheriff)
                {
                    byte sheriffID = JIGFBHFFNFI.ReadByte();
                    PlayerControl sheriff = GetPlayerById(sheriffID);
                    SheriffRole.sheriffID = sheriff.Data.PlayerId;
                }
            }
            public static PlayerControl GetPlayerById(byte id)
            {
                List<PlayerControl> players = PlayerControl.AllPlayerControls;
                for(int i = 0; i < players.Count; i++)
                {
                    PlayerControl player = players[i];
                    if(player.Data.PlayerId == id)
                    {
                        return player;
                    }
                }
                return null;
            }
        }
        [HarmonyPatch(typeof(IntroCutscene.Nested_0), nameof(IntroCutscene.Nested_0.MoveNext))]
        public static class IntroPatch
        {
            private static bool didLog = false;
            public static void Postfix(IntroCutscene.Nested_0 __instance)
            {
                if(!didLog)
                {
                    log.LogMessage("IntroPatch is running");
                    didLog = true;
                }
                byte sheriffID = SheriffRole.sheriffID;
                
                if(sheriffID == PlayerControl.LocalPlayer.Data.PlayerId)
                {
                    __instance.__this.Title.text = SheriffRole.name;
                    __instance.__this.Title.color = SheriffRole.roleColor;
                    __instance.__this.ImpostorText.text = "Kill the Imposter, but be smart about who you kill";
                    __instance.__this.BackgroundBar.material.color = SheriffRole.roleColor;
                    GetPlayerById(sheriffID).nameText.color = SheriffRole.roleColor;
                }
            }
            public static PlayerControl GetPlayerById(byte id)
            {
                List<PlayerControl> players = PlayerControl.AllPlayerControls;
                for (int i = 0; i < players.Count; i++)
                {
                    PlayerControl player = players[i];
                    if(player.Data.PlayerId == id)
                    {
                        return player;
                    }
                }
                return null;
            }
        }
        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class HudStartPatch
        {
            public static void Postfix(HudManager __instance)
            {
                if(SheriffRole.sheriffID == null) return;
                SheriffRole.sheriifKillButton = new CooldownButton(
                    () =>
                    {
                        PlayerControl playerKilled = getClosestPlayer(PlayerControl.LocalPlayer);
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
            public static PlayerControl getClosestPlayer(PlayerControl player)
            {
                double number = double.MaxValue;
                var players = PlayerControl.AllPlayerControls;
                PlayerControl result = null;

                for(int i = 0; i < players.Count; i++)
                {
                    if(players[i].Data.IsDead) continue;
                    if(players[i].Data.Disconnected) continue;
                    if(players[i].Data.PlayerId == player.Data.PlayerId) continue;
                    float distance = getDistanceBetweenPlayers(player, players[i]);
                    if(distance > PlayerControl.GameOptions.KillDistance) continue;
                    if(!(distance < number)) continue;
                    number = distance;
                    result = players[i];
                }
                return result;
            }
            public static float getDistanceBetweenPlayers(PlayerControl player1, PlayerControl player2)
            {
                Vector2 player1Position = player1.GetTruePosition();
                Vector2 player2Position = player2.GetTruePosition();
                float difference = Vector2.Distance(player1Position, player2Position);
                return difference;
            }
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPatch
        {
            public static bool Prefix(PlayerControl __instance, PlayerControl DGDGDKCCKHJ)
            {
                if(SheriffRole.sheriffID == __instance.Data.PlayerId)
                {
                    __instance.Data.IsImpostor = true;
                }
                return true;
            }
            public static void Postfix(PlayerControl __instance, PlayerControl DGDGDKCCKHJ)
            {
                if (SheriffRole.sheriffID == __instance.Data.PlayerId)
                {
                    __instance.Data.IsImpostor = false;
                }
            }
        }
    }
}