using SteamKit2;

namespace SteamKitDota2.Web;

//https://github.com/SteamRE/SteamKit/blob/master/Samples/6.WebAPI/Program.cs
public partial class DotaApi : IDisposable
{
    readonly WebAPI.AsyncInterface matchInterface;

    public DotaApi(string apiKey)
    {
        matchInterface = WebAPI.GetAsyncInterface("IDOTA2Match_570", apiKey);
    }

    public async Task<MatchDetails> GetMatchDetails(ulong matchId)
    {
        var requestArgs = new Dictionary<string, object?>
        {
            ["match_id"] = matchId
        };

        KeyValue result = await matchInterface.CallAsync(HttpMethod.Get, "GetMatchDetails", version: 1, args: requestArgs);

        return new MatchDetails(result);
    }

    public void Dispose()
    {
        matchInterface.Dispose();

        GC.SuppressFinalize(this);
    }
}