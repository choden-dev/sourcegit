using System.Diagnostics;

namespace SourceGit.Models
{
    public interface IGitExecutionStrategy
    {
        ProcessStartInfo CreateGitStartInfo(CommandStartInfoOptions commandStartInfoOptions);
    }

    public class CommandStartInfoOptions
    {
        public bool Redirect { get; set; }
        public string RemoteHost { get; set; }
        public string RemoteDirectory { get; set; }

    }
}
