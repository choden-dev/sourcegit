using System;
using System.Threading.Tasks;

namespace SourceGit.Utils
{
    public class SshDirectory
    {
        public static async Task<bool> ExistsAsync(string path)
        {
            var (output, _) =
                await SshCommand.ExecuteCommandAsync($"[ -d \"{path}\" ] && echo \"true\" || echo \"false\"");
            return bool.Parse(output.Trim());
        }

        public static async Task CreateDirectoryAsync(string path)
        {
            await SshCommand.ExecuteCommandAsync($"mkdir -p \"{path}\"");
        }

        public static async Task DeleteAsync(string path, bool recursive = false)
        {
            await SshCommand.ExecuteCommandAsync($"rm {(recursive ? "-rf" : "-f")} \"{path}\"");
        }

        public static async Task<string[]> GetFilesAsync(string path)
        {
            var (output, _) = await SshCommand.ExecuteCommandAsync($"find \"{path}\" -maxdepth 1 -type f");
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }

        public static async Task<string[]> GetDirectoriesAsync(string path)
        {
            var (output, _) = await SshCommand.ExecuteCommandAsync($"find \"{path}\" -maxdepth 1 -type d");
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
