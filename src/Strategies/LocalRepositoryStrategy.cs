using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SourceGit.Models;
using SourceGit.ViewModels;

namespace SourceGit.Strategies
{
    public class LocalRepositoryStrategy : IRepositoryStrategy
    {
        public void OpenRepository(Repository repository)
        {

        }

        public void InitializeComponents(Repository repository)
        {
            throw new System.NotImplementedException();
        }

        public RepositorySettings LoadSettings(Repository repository)
        {
            var settingsFile = Path.Combine(repository.GitDir, "sourcegit.settings");
            if (File.Exists(settingsFile))
            {
                try
                {
                    using var stream = File.OpenRead(settingsFile);
                    return JsonSerializer.Deserialize(stream, JsonCodeGen.Default.RepositorySettings);
                }
                catch
                {
                    return new Models.RepositorySettings();
                }
            }

            return new Models.RepositorySettings();
        }

        public Watcher SetupWatcher(Repository repository)
        {
            try
            {
                // For worktrees, we need to watch the $GIT_COMMON_DIR instead of the $GIT_DIR.
                var gitDirForWatcher = repository.GitDir;
                if (repository.GitDir.Replace('\\', '/').IndexOf("/worktrees/", StringComparison.Ordinal) > 0)
                {
                    var commonDir = new Commands.QueryGitCommonDir(repository.FullPath).GetResultAsync().Result;
                    if (!string.IsNullOrEmpty(commonDir))
                        gitDirForWatcher = commonDir;
                }

                return new Models.Watcher(repository, repository.FullPath, gitDirForWatcher);
            }
            catch (Exception ex)
            {
                App.RaiseException(string.Empty, $"Failed to start watcher for repository: '{repository.FullPath}'. You may need to press 'F5' to refresh repository manually!\n\nReason: {ex.Message}");
                return null;
            }
        }

        public void InitializeViews(Repository repository)
        {
            throw new System.NotImplementedException();
        }

        public Task RefreshRepository(Repository repository)
        {
            throw new System.NotImplementedException();
        }
    }
}
