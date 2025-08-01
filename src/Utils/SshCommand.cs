using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SourceGit.Utils
{
    public static class SshCommand
    {
        private const string Host = "devenv";

        public static async Task<(string output, string error)> ExecuteCommandAsync(string command)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ssh",
                    Arguments = $"{Host} \"{command.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            var output = new StringBuilder();
            var error = new StringBuilder();
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    error.AppendLine(e.Data);
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception($"SSH command failed: {error}");
            }

            return (output.ToString().TrimEnd(), error.ToString().TrimEnd());
        }
    }
}
