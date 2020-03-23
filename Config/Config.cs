using System;
using System.Collections.Generic;
using System.Text;

namespace CabbageBot.Config
{
    class config
    {
        //General
        public static string PremiumRoleName = "Premium";   //Name of payd roles (or boosted ones)

        //Bitcoin API
        public static string BitcoinApiServerURL = "http://localhost:9001";

        //DeLijn
        public static TimeSpan DeLijnSMSTimeout = TimeSpan.FromMinutes(3);
        public static TimeSpan DeLijnLookupTimeout = TimeSpan.FromSeconds(5);

        //Lidl Plus
        public static TimeSpan LidlPlusSMSTimeout = TimeSpan.FromMinutes(3);


    }
}
