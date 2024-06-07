using Octokit;
using System.Diagnostics;
using System.Text.RegularExpressions;
using WatchedFilmsTracker.Source.Models;

namespace WatchedFilmsTracker.Source.Services
{
    internal class CheckForUpdateService
    {
        public enum UpdateStatusCheck
        {
            NoUpdate, UpdateAvailable, ErrorChecking
        }

        public static string NewVersionString { get; set; }
        public static UpdateStatusCheck updateStatus { get; set; }

        public static async Task<UpdateStatusCheck> IsNewerVersionOnGitHubAsync()
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
                        updateStatus = UpdateStatusCheck.UpdateAvailable;
                    }
                    else
                    {
                        Debug.WriteLine($"Update not available.");
                        updateStatus = UpdateStatusCheck.NoUpdate;
                    }
                    return updateStatus;
                }
                throw new Exception("Latest release name is null");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occured while checking for the latest relaese: {ex.Message}");
                return UpdateStatusCheck.ErrorChecking;
            }
        }
    }
}