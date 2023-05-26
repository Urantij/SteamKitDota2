using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamKitDota2.Web;

public partial class DotaEconApi : IDisposable
{
    readonly WebAPI.AsyncInterface econInterface;
    readonly WebAPI.AsyncInterface betaEconInterface;

    public DotaEconApi(string apiKey)
    {
        econInterface = WebAPI.GetAsyncInterface("IEconDOTA2_570", apiKey);
        betaEconInterface = WebAPI.GetAsyncInterface("IEconDOTA2_205790", apiKey);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="language">The language to provide item names in. Если не указать, не будет локализации.</param>
    /// <returns></returns>
    public async Task<DotaEconApi.GameItem[]> GetGameItemsAsync(string? language = "en")
    {
        var requestArgs = new Dictionary<string, object?>
        {
            ["language"] = language
        };

        KeyValue result = await betaEconInterface.CallAsync(HttpMethod.Get, "GetGameItems", version: 1, args: requestArgs);

        return result["items"].Children.Select(child => new GameItem(child)).ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="language">The language to provide hero names in. Если не указать, не будет локализации.</param>
    /// <param name="itemizedOnly">Return a list of itemized heroes only.</param>
    /// <returns></returns>
    public async Task<DotaEconApi.Hero[]> GetHeroesAsync(string? language = "en", bool? itemizedOnly = null)
    {
        var requestArgs = new Dictionary<string, object?>
        {
            ["language"] = language,
            ["itemizedOnly"] = itemizedOnly
        };

        KeyValue result = await econInterface.CallAsync(HttpMethod.Get, "GetHeroes", version: 1, args: requestArgs);

        return result["heroes"].Children.Select(child => new Hero(child)).ToArray();
    }

    public void Dispose()
    {
        econInterface.Dispose();

        GC.SuppressFinalize(this);
    }
}
