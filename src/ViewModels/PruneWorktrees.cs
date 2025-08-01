using System.Threading.Tasks;
using SourceGit.Utils;

namespace SourceGit.ViewModels
{
    public class PruneWorktrees : Popup
    {
        public PruneWorktrees(Repository repo)
        {
            _repo = repo;
        }

        public override async Task<bool> Sure()
        {
            _repo.SetWatcherEnabled(false);
            ProgressDescription = "Prune worktrees ...";

            var log = _repo.CreateLog("Prune Worktrees");
            Use(log);

            await new Commands.Worktree(_repo.FullPath)
                .WithGitStrategy(_repo.GitStrategyType)
                .Use(log)
                .PruneAsync();

            log.Complete();
            _repo.SetWatcherEnabled(true);
            return true;
        }

        private readonly Repository _repo = null;
    }
}
