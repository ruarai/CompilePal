using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CompilePalX.Compiling;
using Microsoft.Win32;

namespace CompilePalX
{
    /// <summary>
    /// Handles reading and writing values for CompilePal's registry
    /// </summary>
    public static class RegistryManager
    {
        public static bool Write(string key, object value)
        {
            try
            {
                RegistryKey software = Registry.CurrentUser.OpenSubKey("Software", true);
                RegistryKey compilePalRegistryKey = software.CreateSubKey("CompilePal");

                compilePalRegistryKey.SetValue(key, value);
                return true;

            }
            catch (Exception er)
            {
                CompilePalLogger.LogLine("Failed to edit registry: " + er.Message);
                CompilePalLogger.LogDebug($"Key: {key}\nValue: {value}");
                return false;
            }
        }

        public static T Read<T>(string key)
        {
            try
            {
                RegistryKey software = Registry.CurrentUser.OpenSubKey("Software", true);
                RegistryKey compilePalRegistryKey = software.CreateSubKey("CompilePal");

                return (T) compilePalRegistryKey.GetValue(key);
            }
            catch (Exception er)
            {
                CompilePalLogger.LogLine("Failed to read registry: " + er.Message);
                CompilePalLogger.LogLineDebug($"Key: {key}");
                return default(T);
            }
        }
    }
}
