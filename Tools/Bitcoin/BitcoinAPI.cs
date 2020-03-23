using System;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using Newtonsoft.Json;
using CabbageBot.Config;

namespace CabbageBot.Tools.Bitcoin
{
    class BitcoinAPI
    {
        public static BitcoinAPI Instance { private set; get; }
        public RestClient restClient;

        public BitcoinAPI()
        {
            Instance = this;
            this.restClient = new RestClient(config.BitcoinApiServerURL); //a private Bitcoin API server is hosted on this address!!
        }

        public BitcoinLicense GetBitcoinLicenseInfo(string token)
        {
#if ISFERIB
            // NOT IMPLEMENTED YET !!

            //var request = new RestRequest($"{token}"); //Token will be the Discord UserId
            //var result = this.restClient.Execute(request);
            //BitcoinLicense bitcoinLicense = null;
            //try
            //{
            //    bitcoinLicense = JsonConvert.DeserializeObject<BitcoinLicense>(result.Content);
            //    bitcoinLicense.token = token;
            //    bitcoinLicense.LastUpdate = DateTime.UtcNow;
            //}
            //catch { }
            //return bitcoinLicense;
#endif
            BitcoinLicense temp = new BitcoinLicense
            {
                BitcoinAddress = "NotYetImplemented",
                LastUpdate = DateTime.UtcNow,
                DollarsPayd = "9.95" //hardcoded for testing
            };
            return temp;
        }
    }

    class BitcoinLicense
    {
        public string token;
        public string BitcoinAddress;
        public string DollarsPayd;
        public string BitcoinQRBase64;
        public DateTime LastUpdate;

        public string getBitcoinQRBase64()
        {
            this.BitcoinQRBase64 = "";
            return this.BitcoinQRBase64;
        }
    }
}
