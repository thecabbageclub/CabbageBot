using System;
using System.Collections.Generic;
using System.Text;

namespace CabbageBot.Tools.McDonald.FriesHit
{
    public class TopScoreData
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string score { get; set; }
    }

    public class Data
    {
        public List<TopScoreData> topScoreData { get; set; }
    }

    public class LeaderbordResponse
    {
        public Data data { get; set; }
        public int success { get; set; }
    }
}
