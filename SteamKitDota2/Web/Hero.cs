using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamKitDota2.Web;

public partial class DotaEconApi
{
    public class Hero
    {
        public int Id { get; set; }
        /// <summary>
        /// Например npc_dota_hero_antimage
        /// </summary>
        public string Name { get; set; }
        public string? LocalizedName { get; set; }

        public Hero(KeyValue keyValue)
        {
            Id = keyValue["id"].AsInteger(-1);
            Name = keyValue["name"].AsString() ?? "Empty";
            LocalizedName = keyValue["localized_name"].AsString();
        }
    }
}
