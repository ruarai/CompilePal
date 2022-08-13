using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
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
        private static Options options;

        private const bool debuggerCheckOverride = false;

        public static bool Enabled = true;

        private static Client client;
        private static Client legacyClient; // don't know who has access to this client. Keep sending it analytics in case it's still being used

        static AnalyticsManager()
        {
            anonymousUserID = getUniqueComputerID();

            userProperties = new Segment.Model.Properties()
                            {
                                {"version",UpdateManager.CurrentVersion},
                                {"game",GameConfigurationManager.GameConfiguration?.Name}
                            };

            options = new Options {Context = {{"direct", true}}};
        }

        //Returns a unique, anonymised ID that should allow for more accurate counting of number of users
        //This method is probably overkill, but MachineName is too common across different computers
        static string getUniqueComputerID()
        {
            string id = Environment.MachineName;

            try
            {
                ManagementClass mc = new ManagementClass("win32_processor");
                var cpus = mc.GetInstances();
                foreach (var cpu in cpus)//Just for all those people with two cpus
                    id += cpu.Properties["processorID"].Value.ToString();
            }
            catch { }
            try
            {
                ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid=\"C:\"");
                disk.Get();
                id += disk["VolumeSerialNumber"].ToString();
            }
            catch { }
            
            return GetHashString(id);
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString + "ssaalltt"));
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
            if (!enabled) return;

            client = new Client("KPCewjPBrksT73NkIGqh5pkXBvmrnKAT");
            legacyClient= new Client("5IMRGFe2K7JV76e1NzyC40oBADmta5Oh");
            Track(anonymousUserID, "Launch", userProperties,options );
        }
        public static void Compile()
        {
            Track(anonymousUserID, "Compile", userProperties, options);
        }
        public static void NewPreset()
        {
            Track(anonymousUserID, "NewPreset", userProperties, options);
        }
        public static void ModifyPreset()
        {
            Track(anonymousUserID, "ModifyPreset", userProperties, options);
        }
        public static void Error()
        {
            Track(anonymousUserID, "Error", userProperties, options);
        }
        public static void CompileError()
        {
            Track(anonymousUserID, "CompileError", userProperties, options);
        }

        private static void Track(string userId, string eventName, IDictionary<string, object> properties, Options options)
        {
            if (enabled)
            {
                client.Track(userId, eventName, properties, options);
                legacyClient.Track(userId, eventName, properties, options);
            }
        }

        private static bool enabled => (!System.Diagnostics.Debugger.IsAttached || debuggerCheckOverride) && Enabled;
    }
}
