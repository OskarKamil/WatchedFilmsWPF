using Octokit;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WatchedFilmsTracker.Source.BackgroundServices
{
    internal class CheckForUpdateService
    {
        public static Version NewVersion { get; set; }

        public static UpdateStatusCheck updateStatus { get; set; }

        public enum UpdateStatusCheck
        {
            NoUpdate, UpdateAvailable, ErrorChecking
        }

        public static async Task<UpdateStatusCheck> IsNewerVersionOnGitHubAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("WatchedFilmsWPF"));
            try
            {
                string versionRegexPattern = @"\b\d+(\.\d+){1,3}\b$";

                var releaseOnGitHub = await client.Repository.Release.GetLatest("OskarKamil", "WatchedFilmsWPF");
                Debug.WriteLine($"Latest GitHub release: {releaseOnGitHub.TagName} - {releaseOnGitHub.Name}");

                if (!string.IsNullOrEmpty(releaseOnGitHub.TagName))
                {
                    Match localMatch = Regex.Match(ProgramInformation.VERSION, versionRegexPattern);
                    Match githubMatch = Regex.Match(releaseOnGitHub.TagName, versionRegexPattern);

                    if (localMatch.Success && githubMatch.Success)
                    {
                        Version localVersion = Version.Parse(localMatch.Value);
                        Version githubVersion = Version.Parse(githubMatch.Value);

                        Debug.WriteLine($"Local version: {localVersion}\nGitHub version: {githubVersion}");

                        if (githubVersion.CompareTo(localVersion) == 1)
                        {
                            Debug.WriteLine($"Update is available.");
                            updateStatus = UpdateStatusCheck.UpdateAvailable;
                            NewVersion = githubVersion;
                        }
                        else
                        {
                            Debug.WriteLine($"Update not available.");
                            updateStatus = UpdateStatusCheck.NoUpdate;
                        }
                        return updateStatus;
                    }
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