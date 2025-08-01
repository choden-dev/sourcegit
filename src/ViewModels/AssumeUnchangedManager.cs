using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using SourceGit.Utils;

namespace SourceGit.ViewModels
{
    public class AssumeUnchangedManager
    {
        public AvaloniaList<string> Files { get; private set; }

        public AssumeUnchangedManager(Repository repo)
        {
            _repo = repo;
            Files = new AvaloniaList<string>();

            Task.Run(async () =>
            {
                var collect = await new Commands.QueryAssumeUnchangedFiles(_repo.FullPath)
                    .WithGitStrategy(_repo.GitStrategyType)
                    .GetResultAsync()
                    .ConfigureAwait(false);
                Dispatcher.UIThread.Post(() => Files.AddRange(collect));
            });
        }

        public async Task RemoveAsync(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                var log = _repo.CreateLog("Remove Assume Unchanged File");

                await new Commands.AssumeUnchanged(_repo.FullPath, file, false)
                    .WithGitStrategy(_repo.GitStrategyType)
                    .Use(log)
                    .ExecAsync();

                log.Complete();
                Files.Remove(file);
            }
        }

        private readonly Repository _repo;
    }
}
