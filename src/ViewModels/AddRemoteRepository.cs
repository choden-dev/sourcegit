using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using SourceGit.Utils;

namespace SourceGit.ViewModels
{
    public class AddRemoteRepository : Popup
    {
        [CustomValidation(typeof(AddRemoteRepository), nameof(ValidateSshConfigPath))]
        public string SshConfigPath
        {
            get => _sshConfigPath;
            set => SetProperty(ref _sshConfigPath, value, true);
        }

        [Required(ErrorMessage = "Host name is required")]
        [CustomValidation(typeof(AddRemoteRepository), nameof(ValidateHostName))]
        public string HostName
        {
            get => _hostName;
            set => SetProperty(ref _hostName, value, true);
        }

        [Required(ErrorMessage = "Working directory is required")]
        [CustomValidation(typeof(AddRemoteRepository), nameof(ValidateWorkingDirectory))]
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set => SetProperty(ref _workingDirectory, value, true);
        }

        public AddRemoteRepository()

        {
            // Initialize with default values
            _sshConfigPath = string.Empty;
            _hostName = string.Empty;
            _workingDirectory = string.Empty;
        }

        public static ValidationResult ValidateHostName(string hostName, ValidationContext ctx)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return new ValidationResult("Host name cannot be empty!");

            // Basic validation for hostname format
            if (hostName.Contains(' '))
                return new ValidationResult("Host name cannot contain spaces!");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateWorkingDirectory(string workingDirectory, ValidationContext ctx)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
                return new ValidationResult("Working directory cannot be empty!");

            // Basic validation for directory path format
            if (workingDirectory.Contains('\\'))
                return new ValidationResult("Use forward slashes (/) for remote paths!");

            return ValidationResult.Success;
        }

        public static ValidationResult ValidateSshConfigPath(string sshConfigPath, ValidationContext ctx)
        {
            // SSH config path is optional, so empty is valid
            if (string.IsNullOrWhiteSpace(sshConfigPath))
                return ValidationResult.Success;

            // If provided, check if file exists
            if (!File.Exists(sshConfigPath))
                return new ValidationResult("SSH config file not found!");

            return ValidationResult.Success;
        }

        public override async Task<bool> Sure()
        {
            ProgressDescription = "Initializing remote repository...";

            // Validate all properties before proceeding
            ValidateAllProperties();

            // Check if there are any validation errors
            if (HasErrors)
            {
                return false;
            }

            var log = new CommandLog("Initialize Remote Repository");
            Use(log);

            Preferences.Instance.AddNodeMapping("current", _hostName, _workingDirectory);

            var success = await new Commands.Init(_hostName, _workingDirectory).WithGitStrategy(
                Utils.CommandExtensions.GitStrategyType.Remote).Use(log).ExecAsync();

            log.Complete();

            if (success)
            {
                // Tech debt: is node the right place to store this?
                Preferences.Instance.FindOrAddNodeByRepositoryPath(_hostName, null, true, isRemoteRepository: true, name: _workingDirectory);
                Welcome.Instance.Refresh();
            }

            return success;
        }

        private string _sshConfigPath = string.Empty;
        private string _hostName = string.Empty;
        private string _workingDirectory = string.Empty;
    }
}
