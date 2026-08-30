using System;
using System.Collections.Concurrent;
using System.Configuration;

namespace NerdDinner.Tests.TestSupport
{
    /// <summary>
    /// Reads appSettings directly out of this assembly's own compiled
    /// config file, same technique and same reason as
    /// TestConnectionStrings -- see decision-log.md DL-024/DL-026.
    /// ConfigurationManager.AppSettings itself is unreliable under Visual
    /// Studio's IDE-hosted Test Explorer (confirmed: it resolves to some
    /// other host's config there, containing only a stray
    /// "TestProjectRetargetTo35Allowed" key, not anything from this
    /// project's own App.config).
    /// </summary>
    internal static class TestAppSettings
    {
        private static readonly ConcurrentDictionary<string, string> Cache =
            new ConcurrentDictionary<string, string>();

        public static string Get(string key)
        {
            return Cache.GetOrAdd(key, LoadFromConfigFile);
        }

        private static string LoadFromConfigFile(string key)
        {
            var codeBaseUri = new Uri(typeof(TestAppSettings).Assembly.CodeBase);
            var assemblyPath = codeBaseUri.LocalPath;
            var configPath = assemblyPath + ".config";

            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = configPath };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            var entry = config.AppSettings.Settings[key];
            return entry == null ? null : entry.Value;
        }
    }
}
