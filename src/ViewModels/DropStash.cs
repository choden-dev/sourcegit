using System.Threading.Tasks;
using SourceGit.Utils;

namespace SourceGit.ViewModels
{
    public class DropStash : Popup
    {
        public Models.Stash Stash { get; }

        public DropStash(Repository repo, Models.Stash stash)
        {
            _repo = repo;
            Stash = stash;
        }

        public override async Task<bool> Sure()
        {
            ProgressDescription = $"Dropping stash: {Stash.Name}";

            var log = _repo.CreateLog("Drop Stash");
            Use(log);

            await new Commands.Stash(_repo.FullPath)
                .WithGitStrategy(_repo.GitStrategyType)
                .Use(log)
                .DropAsync(Stash.Name);

            log.Complete();
            return true;
        }

        private readonly Repository _repo;
    }
}
