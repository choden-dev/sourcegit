using System;
using System.Diagnostics;
using System.Text;
using SourceGit.Models;

namespace SourceGit.Commands
{
    public class RemoteGitExecutionStrategy(Command command) : IGitExecutionStrategy
    {
        public ProcessStartInfo CreateGitStartInfo(CommandStartInfoOptions commandStartInfoOptions)
        {
            var start = new ProcessStartInfo();
            start.FileName = "ssh";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            if (commandStartInfoOptions.Redirect)
            {
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.StandardOutputEncoding = Encoding.UTF8;
                start.StandardErrorEncoding = Encoding.UTF8;
            }


            // Build remote command
            var remoteGitCommand = new StringBuilder();
            if (!string.IsNullOrEmpty(commandStartInfoOptions.RemoteDirectory))
            {
                remoteGitCommand.Append($"cd {commandStartInfoOptions.RemoteDirectory} && ");
            }

            remoteGitCommand.Append("git --no-pager -c core.quotepath=off ");
            remoteGitCommand.Append(command.Args);
            start.Arguments = $"{commandStartInfoOptions.RemoteHost}  \"{remoteGitCommand}\"";
            if (!string.IsNullOrEmpty(command.WorkingDirectory))
            {
                start.WorkingDirectory = command.WorkingDirectory;
            }

            // Log all info about the command
            Console.WriteLine(
                $"Executing remote git command: {start.FileName} {start.Arguments} in {start.WorkingDirectory ?? "current directory"}");

            return start;
        }
    }
}
