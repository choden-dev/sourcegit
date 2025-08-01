using System.Threading.Tasks;
using SourceGit.Models;
using SourceGit.ViewModels;

namespace SourceGit.Strategies
{
    public class RemoteRepositoryStrategy : IRepositoryStrategy
    {
        public void OpenRepository(Repository repository)
        {
            throw new System.NotImplementedException();
        }

        public void InitializeComponents(Repository repository)
        {
            throw new System.NotImplementedException();
        }

        public RepositorySettings LoadSettings(Repository repository)
        {
            // TODO: Implement loading settings for remote repositories.
            return new RepositorySettings();
        }

        public Watcher SetupWatcher(Repository repository)
        {
            return new Watcher(repository, repository.FullPath, string.Empty, true);
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
