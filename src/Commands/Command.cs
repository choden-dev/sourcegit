using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SourceGit.Models;

namespace SourceGit.Commands
{
    public partial class Command
    {
        public IGitExecutionStrategy ExecutionStrategy { get; set; }

        public Command(IGitExecutionStrategy strategy = null)
        {
            strategy ??= new LocalGitExecutionStrategy(this);
            ExecutionStrategy = strategy;
        }

        public class Result
        {
            public bool IsSuccess { get; set; } = false;
            public string StdOut { get; set; } = string.Empty;
            public string StdErr { get; set; } = string.Empty;

            public static Result Failed(string reason) => new Result() { StdErr = reason };
        }

        public enum EditorType
        {
            None,
            CoreEditor,
            RebaseEditor,
        }

        public string Context { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = null;
        public EditorType Editor { get; set; } = EditorType.CoreEditor;

        public string SSHKey { get; set; } = string.Empty;
        public string Args { get; set; } = string.Empty;

        // Only used in `ExecAsync` mode.
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
        public bool RaiseError { get; set; } = true;
        public Models.ICommandLog Log { get; set; } = null;

        public string RemoteDirectory
        {
            get;
            set;
        }

        public string RemoteHost
        {
            get;
            set;
        }

        public void Exec()
        {
            RemoveWorkingDirectoryIfRemote();
            try
            {
                var start = ExecutionStrategy.CreateGitStartInfo(new CommandStartInfoOptions { Redirect = true });
                Process.Start(start);
            }
            catch (Exception ex)
            {
                App.RaiseException(Context, ex.Message);
            }
        }

        public async Task<bool> ExecAsync()
        {
            Log?.AppendLine($"$ git {Args}\n");

            RemoveWorkingDirectoryIfRemote();

            var errs = new List<string>();

            using var proc = new Process();
            proc.StartInfo = ExecutionStrategy.CreateGitStartInfo(new CommandStartInfoOptions
            {
                Redirect = true,
                RemoteDirectory = RemoteDirectory,
                RemoteHost = RemoteHost
            });
            proc.OutputDataReceived += (_, e) => HandleOutput(e.Data, errs);
            proc.ErrorDataReceived += (_, e) => HandleOutput(e.Data, errs);

            Process dummy = null;
            var dummyProcLock = new object();
            try
            {
                proc.Start();

                // Not safe, please only use `CancellationToken` in readonly commands.
                if (CancellationToken.CanBeCanceled)
                {
                    dummy = proc;
                    CancellationToken.Register(() =>
                    {
                        lock (dummyProcLock)
                        {
                            if (dummy is { HasExited: false })
                                dummy.Kill();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                if (RaiseError)
                    App.RaiseException(Context, e.Message);

                Log?.AppendLine(string.Empty);
                return false;
            }

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            try
            {
                await proc.WaitForExitAsync(CancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                HandleOutput(e.Message, errs);
            }

            if (dummy != null)
            {
                lock (dummyProcLock)
                {
                    dummy = null;
                }
            }

            Log?.AppendLine(string.Empty);

            if (!CancellationToken.IsCancellationRequested && proc.ExitCode != 0)
            {
                if (RaiseError)
                {
                    var errMsg = string.Join("\n", errs).Trim();
                    if (!string.IsNullOrEmpty(errMsg))
                        App.RaiseException(Context, errMsg);
                }

                return false;
            }

            return true;
        }

        protected async Task<Result> ReadToEndAsync()
        {
            RemoveWorkingDirectoryIfRemote();
            using var proc = new Process()
            {
                StartInfo = ExecutionStrategy.CreateGitStartInfo(new CommandStartInfoOptions
                {
                    Redirect = true,
                    RemoteDirectory = RemoteDirectory,
                    RemoteHost = RemoteHost
                })
            };

            try
            {
                proc.Start();
            }
            catch (Exception e)
            {
                return Result.Failed(e.Message);
            }

            var rs = new Result() { IsSuccess = true };
            rs.StdOut = await proc.StandardOutput.ReadToEndAsync(CancellationToken).ConfigureAwait(false);
            rs.StdErr = await proc.StandardError.ReadToEndAsync(CancellationToken).ConfigureAwait(false);
            await proc.WaitForExitAsync(CancellationToken).ConfigureAwait(false);

            rs.IsSuccess = proc.ExitCode == 0;
            return rs;
        }

        private void HandleOutput(string line, List<string> errs)
        {
            if (line == null)
                return;

            Log?.AppendLine(line);

            // Lines to hide in error message.
            if (line.Length > 0)
            {
                if (line.StartsWith("remote: Enumerating objects:", StringComparison.Ordinal) ||
                    line.StartsWith("remote: Counting objects:", StringComparison.Ordinal) ||
                    line.StartsWith("remote: Compressing objects:", StringComparison.Ordinal) ||
                    line.StartsWith("Filtering content:", StringComparison.Ordinal) ||
                    line.StartsWith("hint:", StringComparison.Ordinal))
                    return;

                if (REG_PROGRESS().IsMatch(line))
                    return;
            }

            errs.Add(line);
        }

        private void RemoveWorkingDirectoryIfRemote()
        {
            if (IsUsingRemoteStrategy())
            {
                WorkingDirectory = "";
            }
        }

        protected bool IsUsingRemoteStrategy()
        {
            return ExecutionStrategy.GetType() == typeof(RemoteGitExecutionStrategy);
        }


        [GeneratedRegex(@"\d+%")]
        private static partial Regex REG_PROGRESS();
    }
}
