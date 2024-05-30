using Octokit;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Services
{
    internal class NewestVersionChecker
    {

        private static string newVersionString;

        public static string NewVersionString { get => newVersionString; set => newVersionString = value; }

        public static async Task<bool> IsNewerVersionOnGitHubAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("WatchedFilmsWPF"));
            try
            {
                var latestRelease = await client.Repository.Release.GetLatest("OskarKamil", "WatchedFilmsWPF");
                Debug.WriteLine($"Latest release: {latestRelease.TagName} - {latestRelease.Name}");
                if (latestRelease.Name != null)
                {
                    int localVersion = Int32.Parse(Regex.Replace(ProgramInformation.VERSION, "[^0-9]", ""));
                    int githubVersion = Int32.Parse(Regex.Replace(latestRelease.Name, "[^0-9]", ""));
                    NewVersionString = latestRelease.Name;

                    Debug.WriteLine($"Current version: {localVersion}.\nGitHub version: {githubVersion}");

                    if (githubVersion > localVersion)
                    {
                        Debug.WriteLine($"Update is available.");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"Update not available.");
                        return false;
                    }
                }
                throw new Exception("Latest release name is null");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occured while checking for the latest relaese: {ex.Message}"); return false;
            }
        }
    }
}