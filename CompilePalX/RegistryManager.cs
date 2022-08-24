using System;
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
                var software = Registry.CurrentUser.OpenSubKey("Software", true);
                var compilePalRegistryKey = software.CreateSubKey("CompilePal");

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
                var software = Registry.CurrentUser.OpenSubKey("Software", true);
                var compilePalRegistryKey = software.CreateSubKey("CompilePal");

                return (T)compilePalRegistryKey.GetValue(key);
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
