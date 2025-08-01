using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

#nullable enable

namespace SourceGit.Views
{
    public partial class AddRemoteRepository : UserControl
    {
        public AddRemoteRepository()
        {
            InitializeComponent();

            // Wire up event handlers
            var sshConfigTextBox = this.FindControl<TextBox>("SshConfigPathTextBox");
            var hostNameTextBox = this.FindControl<TextBox>("HostNameTextBox");
            var workingDirTextBox = this.FindControl<TextBox>("WorkingDirectoryTextBox");
            var browseButton = this.FindControl<Button>("BrowseSshConfigButton");

            if (sshConfigTextBox != null)
                sshConfigTextBox.TextChanged += OnSshConfigPathChanged;

            if (hostNameTextBox != null)
                hostNameTextBox.TextChanged += OnHostNameChanged;

            if (workingDirTextBox != null)
                workingDirTextBox.TextChanged += OnWorkingDirectoryChanged;

            if (browseButton != null)
                browseButton.Click += OnBrowseSshConfigClicked;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // Subscribe to validation error events when DataContext is set
            if (DataContext is ViewModels.AddRemoteRepository viewModel)
            {
                if (viewModel is INotifyDataErrorInfo errorInfo)
                {
                    errorInfo.ErrorsChanged += OnValidationErrorsChanged;
                }
            }
        }

        private void OnValidationErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
        {
            if (sender is not (ViewModels.AddRemoteRepository viewModel and INotifyDataErrorInfo errorInfo))
            {
                return;
            }

            // Update error display based on which property changed
            switch (e.PropertyName)
            {
                case nameof(viewModel.SshConfigPath):
                    UpdateErrorDisplay("SshConfigErrorText", errorInfo.GetErrors(nameof(viewModel.SshConfigPath)));
                    break;
                case nameof(viewModel.HostName):
                    UpdateErrorDisplay("HostNameErrorText", errorInfo.GetErrors(nameof(viewModel.HostName)));
                    break;
                case nameof(viewModel.WorkingDirectory):
                    UpdateErrorDisplay("WorkingDirectoryErrorText", errorInfo.GetErrors(nameof(viewModel.WorkingDirectory)));
                    break;
            }
        }

        private void UpdateErrorDisplay(string errorTextBlockName, System.Collections.IEnumerable errors)
        {
            var errorTextBlock = this.FindControl<TextBlock>(errorTextBlockName);
            if (errorTextBlock != null)
            {
                var errorList = errors.Cast<object>().ToList();
                if (errorList.Any())
                {
                    errorTextBlock.Text = string.Join(", ", errorList.Select(e => e.ToString()));
                    errorTextBlock.IsVisible = true;
                }
                else
                {
                    errorTextBlock.Text = string.Empty;
                    errorTextBlock.IsVisible = false;
                }
            }
        }

        private void OnSshConfigPathChanged(object? sender, TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.AddRemoteRepository viewModel && sender is TextBox textBox)
            {
                viewModel.SshConfigPath = textBox.Text ?? string.Empty;
            }
        }

        private void OnHostNameChanged(object? sender, TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.AddRemoteRepository viewModel && sender is TextBox textBox)
            {
                viewModel.HostName = textBox.Text ?? string.Empty;
            }
        }

        private void OnWorkingDirectoryChanged(object? sender, TextChangedEventArgs e)
        {
            if (DataContext is ViewModels.AddRemoteRepository viewModel && sender is TextBox textBox)
            {
                viewModel.WorkingDirectory = textBox.Text ?? string.Empty;
            }
        }

        private async void OnBrowseSshConfigClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                    return;

                var options = new FilePickerOpenOptions
                {
                    Title = "Select SSH Config File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Config files")
                        {
                            Patterns = new[] { "config", "*" }
                        }
                    }
                };

                var result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
                if (result.Count > 0)
                {
                    var selectedFile = result[0];
                    var path = selectedFile.Path.LocalPath;

                    // Update the textbox
                    var sshConfigTextBox = this.FindControl<TextBox>("SshConfigPathTextBox");
                    if (sshConfigTextBox != null)
                    {
                        sshConfigTextBox.Text = path;
                    }

                    // Update the view model
                    if (DataContext is ViewModels.AddRemoteRepository viewModel)
                    {
                        viewModel.SshConfigPath = path;
                    }
                }
            }
            catch (Exception ex)
            {
                throw; // TODO handle exception
            }
        }
    }
}
