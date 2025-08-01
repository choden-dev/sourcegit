using SourceGit.Commands;
using SourceGit.ViewModels;

namespace SourceGit.Utils
{
    public static class CommandExtensions
    {
        public enum GitStrategyType
        {
            Local,
            Remote
        }

        public static T WithGitStrategy<T>(this T command, GitStrategyType gitStrategy)
            where T : Command
        {
            switch (gitStrategy)
            {
                case GitStrategyType.Local:
                    command.ExecutionStrategy = new LocalGitExecutionStrategy(command);
                    break;
                case GitStrategyType.Remote:
                    // Tech debt: properly map the open repo with the ssh info.
                    var nodeMapping = Preferences.Instance.GetNodeMapping("current");
                    command.RemoteDirectory = nodeMapping?.RemoteDirectory;
                    command.RemoteHost = nodeMapping?.Hostname;
                    command.ExecutionStrategy = new RemoteGitExecutionStrategy(command);
                    break;
            }

            return command;
        }
    }
}
