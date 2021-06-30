using Il2CppSystem.Collections.Generic;
using UnityEngine;

public class Utils
{
    public static List<PlayerControl> GetCrewmates(List<PlayerControl> players)
    {
        List<PlayerControl> crewmates = new List<PlayerControl>();
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].Data.IsImpostor)
            {
                crewmates.Add(players[i]);
            }
        }
        return crewmates;
    }

    public static PlayerControl GetPlayerById(byte id)
    {
        List<PlayerControl> players = PlayerControl.AllPlayerControls;
        for (int i = 0; i < players.Count; i++)
        {
            PlayerControl player = players[i];
            if (player.Data.PlayerId == id)
            {
                return player;
            }
        }
        return null;
    }

    public static PlayerControl getClosestPlayer(PlayerControl player)
    {
        float killDistance = GameOptionsData.KillDistances[PlayerControl.GameOptions.KillDistance];
        var players = PlayerControl.AllPlayerControls;
        PlayerControl result = null;

        for (int i = 0; i < players.Count; i++)
        {
            if(players[i].Data.IsDead) continue;
            if(players[i].Data.Disconnected) continue;
            if(players[i].Data.PlayerId == player.Data.PlayerId) continue;
            float distance = getDistanceBetweenPlayers(player, players[i]);
            if(distance > killDistance) continue;
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