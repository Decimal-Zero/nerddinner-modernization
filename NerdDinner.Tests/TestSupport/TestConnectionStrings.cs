using System;
using System.Collections.Concurrent;
using System.Configuration;

namespace NerdDinner.Tests.TestSupport
{
    /// <summary>
    /// Reads connection strings directly out of this assembly's own compiled
    /// config file (NerdDinner.Tests.dll.config), instead of going through
    /// ConfigurationManager's ambient AppDomain config resolution.
    ///
    /// Under Visual Studio's IDE-hosted Test Explorer, that ambient
    /// resolution doesn't reliably point at this test assembly's own
    /// config -- the same class of AppDomain-hosting mismatch DL-023 found
    /// for AppDomain.CurrentDomain.BaseDirectory (there, the native
    /// SqlServerSpatial DLL path; here, connection strings), producing
    /// "No connection string named 'X' could be found in the application
    /// config file" even though the string is right there in the compiled
    /// .config sitting next to this DLL. Rejected an AppDomain.SetData
    /// ("APP_CONFIG_FILE", ...) override as a fix: it only works if set
    /// before ConfigurationManager is touched anywhere in the AppDomain,
    /// and xUnit runs test collections in parallel by default -- an
    /// unrelated collection (e.g. GeolocationServiceTests reading
    /// ConfigurationManager.AppSettings) could easily win that race and
    /// cache the wrong config first. Reading the file directly has no such
    /// race. See decision-log.md DL-023/DL-024.
    /// </summary>
    internal static class TestConnectionStrings
    {
        private static readonly ConcurrentDictionary<string, string> Cache =
            new ConcurrentDictionary<string, string>();

        public static string Get(string name)
        {
            return Cache.GetOrAdd(name, LoadFromConfigFile);
        }

        private static string LoadFromConfigFile(string name)
        {
            var codeBaseUri = new Uri(typeof(TestConnectionStrings).Assembly.CodeBase);
            var assemblyPath = codeBaseUri.LocalPath;
            var configPath = assemblyPath + ".config";

            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = configPath };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            var entry = config.ConnectionStrings.ConnectionStrings[name];
            if (entry == null)
            {
                throw new InvalidOperationException(string.Format(
                    "No connection string named '{0}' could be found in '{1}'.",
                    name, configPath));
            }

            return entry.ConnectionString;
        }
    }
}
