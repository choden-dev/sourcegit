using System.Threading.Tasks;
using SourceGit.Models;
using SourceGit.ViewModels;

namespace SourceGit.Strategies
{
    public interface IRepositoryStrategy
    {
        void OpenRepository(Repository repository);
        void InitializeComponents(Repository repository);
        RepositorySettings LoadSettings(Repository repository);
        Watcher SetupWatcher(Repository repository);
        void InitializeViews(Repository repository);
        Task RefreshRepository(Repository repository);
    }
}
