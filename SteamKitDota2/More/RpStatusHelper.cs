using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamKitDota2.More;

public record RP_WAIT_FOR_PLAYERS_TO_LOAD(string LobbyType);
public record RP_HERO_SELECTION(string LobbyType);
public record RP_STRATEGY_TIME(string LobbyType);
public record RP_PLAYING_AS(string LobbyType, int Level, string Hero);

/// <summary>
/// Всё кидает ошибки, если параметры не те. Мб изменилось что-то.
/// </summary>
public static class RpStatusHelper
{
    public static RP_WAIT_FOR_PLAYERS_TO_LOAD Parse_DOTA_RP_WAIT_FOR_PLAYERS_TO_LOAD(IReadOnlyDictionary<string, string?> @params)
    {
        string? lobbyType = @params["param0"];
        if (string.IsNullOrEmpty(lobbyType))
            throw new ArgumentException("param0 is null or empty");

        return new RP_WAIT_FOR_PLAYERS_TO_LOAD(lobbyType);
    }

    public static RP_HERO_SELECTION Parse_DOTA_RP_HERO_SELECTION(IReadOnlyDictionary<string, string?> @params)
    {
        string? lobbyType = @params["param0"];
        if (string.IsNullOrEmpty(lobbyType))
            throw new ArgumentException("param0 is null or empty");

        return new RP_HERO_SELECTION(lobbyType);
    }

    public static RP_STRATEGY_TIME Parse_DOTA_RP_STRATEGY_TIME(IReadOnlyDictionary<string, string?> @params)
    {
        string? lobbyType = @params["param0"];
        if (string.IsNullOrEmpty(lobbyType))
            throw new ArgumentException("param0 is null or empty");

        return new RP_STRATEGY_TIME(lobbyType);
    }

    public static RP_PLAYING_AS Parse_DOTA_RP_PLAYING_AS(IReadOnlyDictionary<string, string?> @params)
    {
        if (!@params.TryGetValue("param0", out var lobbyType) || string.IsNullOrEmpty(lobbyType))
        {
            throw new ArgumentException("param0 is null or empty");
        }

        if (!@params.TryGetValue("param1", out var levelString) || !int.TryParse(levelString, out int level))
        {
            throw new ArgumentException("param1 is null or empty");
        }

        if (!@params.TryGetValue("param2", out var hero) || string.IsNullOrEmpty(hero))
        {
            throw new ArgumentException("param2 is null or empty");
        }

        return new RP_PLAYING_AS(lobbyType, level, hero);
    }
}
