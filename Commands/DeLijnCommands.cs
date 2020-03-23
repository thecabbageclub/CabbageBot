// 
// The code required to run these commands are private :/
// 

#if ISFERIB
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using CabbageBot.Config;
using CabbageBot.Tools.Bitcoin;
using DeLijnReversed;

namespace CabbageBot.Commands
{
    [Group("delijn", CanInvokeWithoutSubcommand = true)]
    [Description("DeLijn tool")]
    public class DeLijnCommands
    {
        //Timeout Text Messages (SMS)
        private static Dictionary<ulong, DateTime> SmsTimeoutDict = new Dictionary<ulong, DateTime>();
        private static TimeSpan SmsTimeout = config.DeLijnSMSTimeout;

        //unlocked
        private static Dictionary<ulong, DateTime> UnlockDict = new Dictionary<ulong, DateTime>();

        //Timeout lookup
        private static Dictionary<ulong, DateTime> LookupTimeoutDict = new Dictionary<ulong, DateTime>();
        private static TimeSpan LookupTimeout = config.DeLijnLookupTimeout;

        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await ctx.RespondAsync("The following sub commands are available: ``sms``, ``lookup``");
        }

        [Command("sms"), Aliases("text"), Description("Sends a DeLijn text message to a given phone number")]
        public async Task Sms(CommandContext ctx, string phonenr)
        {
            long longnr = 0;

            await ctx.TriggerTypingAsync();

            ulong userid = 0;

            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else if (ctx.User != null)
                userid = ctx.User.Id;


            if (ctx.Member.Roles.ToList().FindIndex(x => x.Name == config.PremiumRoleName) != -1 
                || (UnlockDict.ContainsKey(userid) && UnlockDict[userid].AddMinutes(2) > DateTime.UtcNow ))
            {
                //premium cooldown 10sec
                if (SmsTimeoutDict.ContainsKey(userid) && SmsTimeoutDict[userid].Add(SmsTimeout / 18) > DateTime.UtcNow)
                {
                    await ctx.RespondAsync($"Calm down <@{userid}>, Please try again in ``{((SmsTimeoutDict[userid].Add(SmsTimeout / 18)) - DateTime.UtcNow)}``");
                    return;
                }
            }
            else
            {
                //Non-premium cooldown
                if (SmsTimeoutDict.ContainsKey(userid) && SmsTimeoutDict[userid].Add(SmsTimeout) > DateTime.UtcNow)
                {
                    await ctx.RespondAsync($"Calm down <@{userid}>, Please try again in ``{((SmsTimeoutDict[userid].Add(SmsTimeout)) - DateTime.UtcNow)}``");
                    return;
                }
            }
           

            if(DeLijn.Instance == null)
                new DeLijn();

            if(!long.TryParse(phonenr.Replace("+",""), out longnr))
            {
                await ctx.RespondAsync($"Text message faild");
                return;
            }

            if (SmsTimeoutDict.ContainsKey(userid))
                SmsTimeoutDict.Remove(userid);

            SmsTimeoutDict.Add(userid, DateTime.UtcNow);

            DeLijn.Instance.GetSMSRegistraties(longnr, "abc1de23-c8c9-48db-bdb9-8c49a8814701");

            await ctx.RespondAsync($"Text message sent to {phonenr}!");
        }

        [Command("lookup"), Description("Returns DeLijn info for the given phone number")]
        public async Task Lookup(CommandContext ctx, string phonenr)
        {
            long longnr = 0;

            await ctx.TriggerTypingAsync();

            ulong userid = 0;

            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else if (ctx.User != null)
                userid = ctx.User.Id;

            if (LookupTimeoutDict.ContainsKey(userid) && LookupTimeoutDict[userid].Add(LookupTimeout) > DateTime.UtcNow)
            {
                await ctx.RespondAsync($"Calm down <@{userid}>, Please try again in ``{((LookupTimeoutDict[userid].Add(LookupTimeout)) - DateTime.UtcNow)}``");
                return;
            }

            if (DeLijn.Instance == null)
                new DeLijn();

            if (!long.TryParse(phonenr.Replace("+", ""), out longnr))
            {
                await ctx.RespondAsync($"DeLijn Lookup faild");
                return;
            }

            if (LookupTimeoutDict.ContainsKey(userid))
                LookupTimeoutDict.Remove(userid);

            LookupTimeoutDict.Add(userid, DateTime.UtcNow);

            var Response = DeLijn.Instance.GetRegistraties(longnr);

            string respString = "";
            foreach(var r in Response.registraties)
            {
                respString += $"{r.uniqueId} - enabled: {r.enabled} - useSms: {r.useSmsCode}";
            }
            respString += $"\nTotal: {Response.registraties.Count}";

            var embed = new DiscordEmbedBuilder
            {
                Title = "DeLijn Lookup",
                Description = respString
            };
            var msg = await ctx.RespondAsync(embed: embed);
        }

        [Command("unlock"), Description("Unlock anti spam for 5 min")]
        public async Task Unlock(CommandContext ctx)
        {
            ulong userid = 0;
            if (ctx.Member != null)
                userid = ctx.Member.Id;
            else
                userid = ctx.User.Id;

            //check if user is donator?
            if (BitcoinAPI.Instance == null)
                new BitcoinAPI();

            string dollars = BitcoinAPI.Instance.GetBitcoinLicenseInfo(userid.ToString()).DollarsPayd;
            double dDollars = 0;
            bool xxx = double.TryParse(dollars, out dDollars);
            if (!double.TryParse(dollars, out dDollars) || dDollars < 10.0)
            {
                await ctx.RespondAsync($"Please donate more then 10$\n*(current balance: ``{dollars}``$)*");
                return;
            }

            //first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();

            //specify the emoji
            var accept = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var reject = DiscordEmoji.FromName(ctx.Client, ":x:");

            //announce
            var requestMsg = await ctx.RespondAsync($"Complete offer with {accept} or {reject}");
            await Task.Delay(100);
            await requestMsg.CreateReactionAsync(accept); //check
            await Task.Delay(50);
            await requestMsg.CreateReactionAsync(reject); //cross
            await Task.Delay(50);

            var em = await interactivity.WaitForReactionAsync(xe => xe == accept || xe == reject, ctx.User, TimeSpan.FromSeconds(10));
            if (em != null)
            {
                //user reacted
                if(em.Emoji == accept)
                {
                    

                    //Do stuff
                    await ctx.RespondAsync("Accepted! SMS rate limit is set to 10 seconds for the next 2 minutes.");
                    if (UnlockDict.ContainsKey(userid))
                        UnlockDict.Remove(userid);
                    UnlockDict.Add(userid, DateTime.UtcNow);
                }
                await requestMsg.DeleteAsync();
            }
            else
            {
                //timeout
                await requestMsg.DeleteAsync();
            }
        }
    }
}
#endif