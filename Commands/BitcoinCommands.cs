using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using CabbageBot.Tools.Bitcoin;

namespace CabbageBot.Commands
{
    [Group("btc", CanInvokeWithoutSubcommand = true)]
    [Description("Bitcoin wallet tool")]
    public class BitcoinCommands
    {
        //cached addresses
        private static Dictionary<ulong, BitcoinLicense> BtcLicenseCache = new Dictionary<ulong, BitcoinLicense>();
        private static TimeSpan CacheTTL = TimeSpan.FromMinutes(10); //avg block spawn?


        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``balance``, ``address``");
        }

        [Command("balance"), Aliases("status"), Description("Shows your current available balance")]
        public async Task Ballance(CommandContext ctx)
        {
            ulong userid = 0;
            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else
                userid = ctx.User.Id;

            var info = await GetLicenseCached(userid);

            await ctx.RespondAsync($"Your balance is ``{info.DollarsPayd}``$");
        }

        [Command("address"), Aliases("addr"), Description("Returns the Bitcoin address that belongs to you")]
        public async Task Address(CommandContext ctx)
        {
            ulong userid = 0;
            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else
                userid = ctx.User.Id;

            var info = await GetLicenseCached(userid);

            await ctx.RespondAsync($"Your BTC address is ``{info.BitcoinAddress}``");
        }

        //helper functions
        private async Task<BitcoinLicense> GetLicenseCached(ulong userid)
        {
            BitcoinLicense result;

            if (BitcoinAPI.Instance == null)
                new BitcoinAPI();

            //check cache
            if (BtcLicenseCache.ContainsKey(userid) && BtcLicenseCache[userid].LastUpdate.Add(CacheTTL) > DateTime.UtcNow)
            {
                return BtcLicenseCache[userid];
            }
            else
            {
                if (BtcLicenseCache.ContainsKey(userid))
                    BtcLicenseCache.Remove(userid);
                result = BitcoinAPI.Instance.GetBitcoinLicenseInfo(userid.ToString());
                BtcLicenseCache.Add(userid, result);
            }

            return result;
        }
    }
}
