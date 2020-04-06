using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;

namespace CabbageBot.Tools.McDonald.FriesHit
{
    class FriesHit
    {
        public RestClient web { get; set; }
        public static FriesHit Instance {get; private set;}

        public FriesHit()
        {
            this.web = new RestClient("https://mcd-games-api.lwprod.nl/");
            this.web.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";
            Instance = this;
        }

        public Data getLeaderbordList(int limit = 10)
        {
            //0bf41b79-8b88-49ef-af0d-2c7a6cfeece7 == FriesHit
            var request = new RestRequest($"games/getTopScores?gameId=0bf41b79-8b88-49ef-af0d-2c7a6cfeece7&limit={limit}", Method.GET);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            var response = this.web.Execute(request);
            Console.WriteLine(response.StatusCode);
            if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Content != "")
                return JsonConvert.DeserializeObject<LeaderbordResponse>(response.Content).data;
            return null;
        }
    }
}
