using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Steam_Guard_Code_Generator.Model
{
    internal static class SteamTime
    {
        private static bool _aligned = false;
        private static int _timeDifference = 0;
        public static long GetSystemUnixTime()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        public static long GetSteamTime()
        {
            if (!_aligned)
            {
                AlignTime();
            }
            return GetSystemUnixTime() + _timeDifference;
        }
        public static void AlignTime()
        {
            long currentTime = GetSystemUnixTime();
            using (WebClient client = new WebClient())
            {
                try
                {
                    string response = client.UploadString("https://api.steampowered.com/ITwoFactorService/QueryTime/v0001", "steamid=0");
                    TimeQuery query = JsonConvert.DeserializeObject<TimeQuery>(response);
                    _timeDifference = (int)(query.Response.ServerTime - currentTime);
                    _aligned = true;
                }
                catch (WebException)
                {
                    return;
                }
            }
        }

        private struct TimeQuery
        {
            [JsonProperty("response")]
            internal TimeQueryResponse Response { get; set; }

            internal class TimeQueryResponse
            {
                [JsonProperty("server_time")]
                public long ServerTime { get; set; }
            }

        }
    }
    public static class SteamCodeGenerator
    {
        private static byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        public static string GenerateCode(this SteamGuardData guardData)
        {
            return GenerateSteamGuardCodeForTime(guardData, SteamTime.GetSteamTime());
        }
        public static string GenerateSteamGuardCodeForTime(this SteamGuardData steamGuard, long time)
        {
            if (steamGuard.SharedSecret == null || steamGuard.SharedSecret.Length == 0)
            {
                return "";
            }

            string sharedSecretUnescaped = Regex.Unescape(steamGuard.SharedSecret);
            byte[] sharedSecretArray = Convert.FromBase64String(sharedSecretUnescaped);
            byte[] timeArray = new byte[8];

            time /= 30L;

            for (int i = 8; i > 0; i--)
            {
                timeArray[i - 1] = (byte)time;
                time >>= 8;
            }

            HMACSHA1 hmacGenerator = new HMACSHA1();
            hmacGenerator.Key = sharedSecretArray;
            byte[] hashedData = hmacGenerator.ComputeHash(timeArray);
            byte[] codeArray = new byte[5];
            try
            {
                byte b = (byte)(hashedData[19] & 0xF);
                int codePoint = (hashedData[b] & 0x7F) << 24 | (hashedData[b + 1] & 0xFF) << 16 | (hashedData[b + 2] & 0xFF) << 8 | (hashedData[b + 3] & 0xFF);

                for (int i = 0; i < 5; ++i)
                {
                    codeArray[i] = steamGuardCodeTranslations[codePoint % steamGuardCodeTranslations.Length];
                    codePoint /= steamGuardCodeTranslations.Length;
                }
            }
            catch (Exception)
            {
                return null; //Change later, catch-alls are bad!
            }
            return Encoding.UTF8.GetString(codeArray);
        }
    }
    public class SteamGuardData
    {
        private string _phoneNumber;


        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("steam_id")]
        public ulong SteamID { get; set; }

        [JsonProperty("device_id")]
        public string DeviceID { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                StringBuilder temp = new StringBuilder(value);
                temp.Replace("-", "").Replace("(", "").Replace(")", "");
                if (temp == null || temp.Length == 0) { _phoneNumber = String.Empty; return; }
                if (temp.ToString()[0] != '+') { _phoneNumber = String.Empty; return; }
                _phoneNumber = temp.ToString();
            }
        }

        [JsonProperty("shared_secret")]
        public string SharedSecret { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("revocation_code")]
        public string RevocationCode { get; set; }

        [JsonProperty("uri")]
        public string URI { get; set; }

        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("token_gid")]
        public string TokenGID { get; set; }

        [JsonProperty("identity_secret")]
        public string IdentitySecret { get; set; }

        [JsonProperty("secret_1")]
        public string Secret1 { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("fully_enrolled")]
        public bool FullyEnrolled { get; set; }
    }
}
