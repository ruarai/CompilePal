using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Segment;
using Segment.Model;

namespace CompilePalX
{
    static class AnalyticsManager
    {
        private static string anonymousUserID;
        private static Segment.Model.Properties userProperties;

        private const bool debuggerCheckOverride = false;
        static AnalyticsManager()
        {
            //anonymise the machine name so it's not too stalkery
            anonymousUserID = GetHashString(Environment.MachineName);

            userProperties= new Segment.Model.Properties() {{"version",UpdateManager.Version}};
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
        public static void Launch()
        {
            if (!System.Diagnostics.Debugger.IsAttached || debuggerCheckOverride)
            {
                Analytics.Initialize("5IMRGFe2K7JV76e1NzyC40oBADmta5Oh");

                Analytics.Client.Track(anonymousUserID,"Launch",userProperties);
            }
        }
        public static void Compile()
        {
            if (!System.Diagnostics.Debugger.IsAttached || debuggerCheckOverride)
            {
                Analytics.Client.Track(anonymousUserID, "Compile", userProperties);
            }
        }
    }
}
