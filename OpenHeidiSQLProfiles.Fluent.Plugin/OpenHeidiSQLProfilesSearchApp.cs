using Blast.API.Core.Processes;
using Blast.API.Processes;
using Blast.API.Settings;
using Blast.Core;
using Blast.Core.Interfaces;
using Blast.Core.Objects;
using Blast.Core.Results;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenHeidiSQLProfiles.Fluent.Plugin
{
    public class OpenHeidiSQLProfilesSearchApp : ISearchApplication
    {
        private const string SearchAppName = "HeidiSQL";
        private const string RegistryPath = @"Software\HeidiSQL\Servers";
        private readonly List<SearchTag> _searchTags;
        private readonly SearchApplicationInfo _applicationInfo;
        private readonly List<ISearchOperation> _supportedOperations;
        private List<string> _profileNames = new();
        private string _heidiSqlPath;
        private readonly HeidiSQLSettingsPage _settingsPage;

        public OpenHeidiSQLProfilesSearchApp()
        {
            _searchTags = new List<SearchTag>
            {
            };

            _supportedOperations = new List<ISearchOperation>
            {
                OpenProfileSearchOperation.OpenProfileOperation
            };

            _applicationInfo = new SearchApplicationInfo(SearchAppName,
                "Search and open HeidiSQL database profiles",
                _supportedOperations)
            {
                MinimumSearchLength = 1,
                IsProcessSearchEnabled = false,
                IsProcessSearchOffline = true,
                ApplicationIconGlyph = "\uEE94",
                SearchAllTime = ApplicationSearchTime.Fast,
                DefaultSearchTags = _searchTags
            };

            _settingsPage = new HeidiSQLSettingsPage(_applicationInfo);
            _applicationInfo.SettingsPage = _settingsPage;
        }

        public ValueTask LoadSearchApplicationAsync()
        {
            _heidiSqlPath = GetHeidiSQLPath(_settingsPage.HeidiSQLPath);

            if (!string.IsNullOrEmpty(_heidiSqlPath) && string.IsNullOrWhiteSpace(_settingsPage.HeidiSQLPath))
            {
                _settingsPage.HeidiSQLPath = _heidiSqlPath;
            }

            return ValueTask.CompletedTask;
        }

        public SearchApplicationInfo GetApplicationInfo()
        {
            return _applicationInfo;
        }

        /// <summary>
        /// Search through HeidiSQL profiles.
        /// </summary>
        public async IAsyncEnumerable<ISearchResult> SearchAsync(SearchRequest searchRequest,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            RetrieveHeidiSQLProfiles();
            if (cancellationToken.IsCancellationRequested || searchRequest.SearchType == SearchType.SearchProcess)
                yield break;

            string searchedText = searchRequest.SearchedText.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(searchedText))
                yield break;

            foreach (var profileName in _profileNames)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                string lowerProfileName = profileName.ToLowerInvariant();
                if (lowerProfileName.Contains(searchedText))
                {
                    double score = 1.0;
                    if (lowerProfileName.StartsWith(searchedText))
                        score = 2.0;
                    if (lowerProfileName == searchedText)
                        score = 3.0;

                    yield return new HeidiSQLProfileSearchResult(
                        SearchAppName,
                        profileName,
                        searchedText,
                        score,
                        _supportedOperations,
                        _searchTags)
                    {
                        IconGlyph = "\uEE94"
                    };
                }
            }
        }

        /// <summary>
        /// Handle the selected result.
        /// OpenProfileSearchOperation: Launch HeidiSQL with the selected profile.
        /// </summary>
        public ValueTask<IHandleResult> HandleSearchResult(ISearchResult searchResult)
        {
            if (searchResult is not HeidiSQLProfileSearchResult heidiSqlResult)
            {
                return new ValueTask<IHandleResult>(new HandleResult(false, false));
            }

            switch (searchResult.SelectedOperation)
            {
                case OpenProfileSearchOperation:
                    IProcessManager managerInstance = ProcessUtils.GetManagerInstance();
                    if (string.IsNullOrEmpty(_heidiSqlPath))
                    {
                        return new ValueTask<IHandleResult>(new HandleResult(false, true));
                    }
                    managerInstance.StartNewProcess(_heidiSqlPath, $"-d \"{heidiSqlResult.ProfileName}\"");
                    return new ValueTask<IHandleResult>(new HandleResult(true, false));
                default:
                    return new ValueTask<IHandleResult>(new HandleResult(false, false));
            }
        }

        /// <summary>
        /// Retrieve HeidiSQL profiles from the Windows Registry.
        /// </summary>
        private void RetrieveHeidiSQLProfiles()
        {
            _profileNames.Clear();

            using (var root = Registry.CurrentUser.OpenSubKey(RegistryPath))
            {
                if (root != null)
                {
                    ReadRegistryProfiles(root, string.Empty);
                }
            }

            _profileNames = _profileNames.Distinct().ToList();
        }

        /// <summary>
        /// Recursively read registry subkeys to extract HeidiSQL profile names with groups.
        /// </summary>
        private void ReadRegistryProfiles(RegistryKey registryKey, string parentPath)
        {
            if (registryKey.GetValueNames().Contains("Host"))
            {
                if (!string.IsNullOrWhiteSpace(parentPath))
                {
                    _profileNames.Add(parentPath);
                }
            }

            foreach (var subKeyName in registryKey.GetSubKeyNames())
            {
                using (var subKey = registryKey.OpenSubKey(subKeyName))
                {
                    if (subKey != null)
                    {
                        string newPath = string.IsNullOrEmpty(parentPath)
                            ? subKeyName
                            : $"{parentPath}\\{subKeyName}";

                        ReadRegistryProfiles(subKey, newPath);
                    }
                }
            }
        }

        /// <summary>
        /// Find HeidiSQL exe.
        /// </summary>
        private static string GetHeidiSQLPath(string configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim().Trim('"'));

                if (Directory.Exists(expandedPath))
                {
                    string exePath = Path.Combine(expandedPath, "heidisql.exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }

                if (File.Exists(expandedPath))
                {
                    return expandedPath;
                }

                if (!Path.IsPathRooted(expandedPath) &&
                    !expandedPath.Contains(Path.DirectorySeparatorChar) &&
                    !expandedPath.Contains(Path.AltDirectorySeparatorChar))
                {
                    return expandedPath;
                }
            }

            string[] possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "HeidiSQL", "heidisql.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "HeidiSQL", "heidisql.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeidiSQL", "heidisql.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }
    }
}
