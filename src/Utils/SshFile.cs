using System;
using System.Threading.Tasks;

namespace SourceGit.Utils

{
    public class SshFile
    {
        public static async Task<bool> ExistsAsync(string path)
        {
            var (output, _) =
                await SshCommand.ExecuteCommandAsync($"[ -f \"{path}\" ] && echo \"true\" || echo \"false\"");
            return bool.Parse(output.Trim());
        }

        public static async Task DeleteAsync(string path)
        {
            await SshCommand.ExecuteCommandAsync($"rm \"{path}\"");
        }

        public static async Task WriteAllTextAsync(string path, string contents)
        {
            // Using echo with heredoc to handle multiline content and special characters better
            await SshCommand.ExecuteCommandAsync($"cat > \"{path}\" << 'EOF'\n{contents}\nEOF");
        }

        public static async Task AppendAllTextAsync(string path, string contents)
        {
            await SshCommand.ExecuteCommandAsync($"cat >> \"{path}\" << 'EOF'\n{contents}\nEOF");
        }

        public static async Task<string> ReadAllTextAsync(string path)
        {
            var (output, _) = await SshCommand.ExecuteCommandAsync($"cat \"{path}\"");
            return output;
        }

        public static async Task CopyAsync(string sourceFile, string destFile)
        {
            await SshCommand.ExecuteCommandAsync($"cp \"{sourceFile}\" \"{destFile}\"");
        }

        public static async Task MoveAsync(string sourceFile, string destFile)
        {
            await SshCommand.ExecuteCommandAsync($"mv \"{sourceFile}\" \"{destFile}\"");
        }

        public static async Task<DateTime> GetLastWriteTimeAsync(string path)
        {
            var (output, _) = await SshCommand.ExecuteCommandAsync($"stat -c %Y \"{path}\"");
            var timestamp = long.Parse(output.Trim());
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        public static async Task<long> GetFileSizeAsync(string path)
        {
            var (output, _) = await SshCommand.ExecuteCommandAsync($"stat -f %s \"{path}\"");
            return long.Parse(output.Trim());
        }
    }
}
