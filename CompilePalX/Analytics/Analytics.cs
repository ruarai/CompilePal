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
        private static Options options;

        private const bool debuggerCheckOverride = false;

        public static bool Enabled = true;

        private static Client? client;
        private static Client? legacyClient; // don't know who has access to this client. Keep sending it analytics in case it's still being used

        static AnalyticsManager()
        {
            anonymousUserID = getUniqueComputerID();

            OperatingSystem os = Environment.OSVersion;
            options = new Options { 
                Context = { 
                    ["direct"] = true,
                    ["os"] = new Dict() {
                        ["name"] = os.Platform,
                        ["version"] = os.VersionString,
                    },
                    ["app"] = new Dict()
                    {
                        ["name"] = "CompilePal",
                        ["version"] = UpdateManager.CurrentVersion
                    }
                } 
            };

            if (!enabled) return;

            client = new Client("KPCewjPBrksT73NkIGqh5pkXBvmrnKAT");
            legacyClient = new Client("5IMRGFe2K7JV76e1NzyC40oBADmta5Oh");
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
            Track(anonymousUserID, "Launch");
        }
        public static void Compile()
        {
            Track(anonymousUserID, "Compile");
        }
        public static void NewPreset()
        {
            Track(anonymousUserID, "NewPreset");
        }
        public static void ModifyPreset()
        {
            Track(anonymousUserID, "ModifyPreset");
        }
        public static void NewGameConfiguration(string game)
        {
            Track(anonymousUserID, "NewGameConfiguration", new Dict() { ["game"] = game});
        }
        public static void ModifyGameConfiguration(string game)
        {
            Track(anonymousUserID, "ModifyGameConfiguration", new Dict() { ["game"] = game});
        }
        public static void SelectGameConfiguration(string game)
        {
            Track(anonymousUserID, "SelectGameConfiguration", new Dict() { ["game"] = game});
        }
        public static void Error()
        {
            Track(anonymousUserID, "Error");
        }
        public static void CompileError()
        {
            Track(anonymousUserID, "CompileError");
        }

        private static void Track(string userId, string eventName, IDictionary<string, object>? additionalProperties = null)
        {
            if (enabled)
            {
                var properties = new Segment.Model.Properties()
                {
                    {"game", GameConfigurationManager.GameConfiguration?.Name}
                };

                if (additionalProperties is not null)
                {
                    foreach (var item in additionalProperties)
                    {
                        properties[item.Key] = item.Value;
                    }
                }

                client!.Track(userId, eventName, properties, options);
                legacyClient!.Track(userId, eventName, properties, options);
            }
        }

        private static bool enabled => (!System.Diagnostics.Debugger.IsAttached || debuggerCheckOverride) && Enabled;
    }
}
