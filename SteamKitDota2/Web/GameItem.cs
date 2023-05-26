using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamKitDota2.Web;

public partial class DotaEconApi
{
    // https://wiki.teamfortress.com/wiki/WebAPI/GetGameItems
    public class GameItem
    {
        public int Id { get; set; }
        /// <summary>
        /// The tokenized string for the name of the hero.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The in-game gold cost of the item.
        /// </summary>
        public int Cost { get; set; }
        /// <summary>
        /// Boolean - true if the item is only available in the secret shop.
        /// </summary>
        public bool SecretShop { get; set; }
        /// <summary>
        /// Boolean - true if the item is available in the side shop.
        /// </summary>
        public bool SideShop { get; set; }
        /// <summary>
        /// Boolean - true if the item is a recipe type item.
        /// </summary>
        public bool Recipe { get; set; }
        /// <summary>
        /// The localized name of the hero for use in name display. You will get it only if specifie 'language' parameter.
        /// </summary>
        public string? LocalizedName { get; set; }

        public GameItem(int id, string name, int cost, bool secretShop, bool sideShop, bool recipe, string? localizedName)
        {
            Id = id;
            Name = name;
            Cost = cost;
            SecretShop = secretShop;
            SideShop = sideShop;
            Recipe = recipe;
            LocalizedName = localizedName;
        }

        public GameItem(KeyValue keyValue)
        {
            Id = keyValue["id"].AsInteger(-1);
            Name = keyValue["name"].AsString() ?? "Empty";
            Cost = keyValue["cost"].AsInteger(-1);
            SecretShop = keyValue["secret_shop"].AsBoolean();
            SideShop = keyValue["side_shop"].AsBoolean();
            Recipe = keyValue["recipe"].AsBoolean();
            LocalizedName = keyValue["localized_name"].AsString();
        }
    }
}
