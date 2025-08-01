using System;
using System.Diagnostics;
using System.Text;
using SourceGit.Models;

namespace SourceGit.Commands
{
    public class LocalGitExecutionStrategy(Command command) : IGitExecutionStrategy
    {
        public ProcessStartInfo CreateGitStartInfo(CommandStartInfoOptions commandStartInfoOptions)
        {
            var start = new ProcessStartInfo();
            start.FileName = Native.OS.GitExecutable;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            if (commandStartInfoOptions.Redirect)
            {
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.StandardOutputEncoding = Encoding.UTF8;
                start.StandardErrorEncoding = Encoding.UTF8;
            }

            // SSH configuration
            var selfExecFile = Process.GetCurrentProcess().MainModule!.FileName;
            start.Environment.Add("SSH_ASKPASS", selfExecFile);
            start.Environment.Add("SSH_ASKPASS_REQUIRE", "force");
            start.Environment.Add("SOURCEGIT_LAUNCH_AS_ASKPASS", "TRUE");
            if (!start.Environment.ContainsKey("GIT_SSH_COMMAND") &&
                !string.IsNullOrEmpty(command.SSHKey))
            {
                start.Environment.Add("GIT_SSH_COMMAND",
                    $"ssh -o AddKeysToAgent=yes -i {command.SSHKey.Quoted()}");
            }

            // Locale configuration
            if (OperatingSystem.IsLinux())
            {
                start.Environment.Add("LANG", "C");
                start.Environment.Add("LC_ALL", "C");
            }

            // Build arguments
            var builder = new StringBuilder();
            builder.Append("--no-pager -c core.quotepath=off -c credential.helper=")
                .Append(Native.OS.CredentialHelper)
                .Append(' ');
            switch (command.Editor)
            {
                case Command.EditorType.CoreEditor:
                    builder.Append($"""-c core.editor="\"{selfExecFile}\" --core-editor" """);
                    break;
                case Command.EditorType.RebaseEditor:
                    builder.Append(
                        $"""-c core.editor="\"{selfExecFile}\" --rebase-message-editor" -c sequence.editor="\"{selfExecFile}\" --rebase-todo-editor" -c rebase.abbreviateCommands=true """);
                    break;
                default:
                    builder.Append("-c core.editor=true ");
                    break;
            }

            builder.Append(command.Args);
            start.Arguments = builder.ToString();
            if (!string.IsNullOrEmpty(command.WorkingDirectory))
            {
                start.WorkingDirectory = command.WorkingDirectory;
            }

            return start;
        }
    }
}
