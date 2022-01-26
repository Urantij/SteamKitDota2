using SteamKit2;
using SteamKitDota2.Models;

namespace SteamKitDota2
{
    //https://github.com/SteamRE/SteamKit/blob/master/Samples/6.WebAPI/Program.cs
    public class DotaApi : IDisposable
    {
        readonly WebAPI.AsyncInterface matchInterface;

        public DotaApi(string apiKey)
        {
            matchInterface = WebAPI.GetAsyncInterface("IDOTA2Match_570", apiKey);
        }

        public async Task<MatchDetails> GetMatchDetails(ulong matchId)
        {
            var matchArgs = new Dictionary<string, object?>
            {
                ["match_id"] = matchId
            };

            KeyValue result = await matchInterface.CallAsync(System.Net.Http.HttpMethod.Get, "GetMatchDetails", version: 1, args: matchArgs);

            return new MatchDetails(result);
        }

        public void Dispose()
        {
            matchInterface.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}