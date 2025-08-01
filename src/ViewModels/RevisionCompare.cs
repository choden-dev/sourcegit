﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SourceGit.Utils;

namespace SourceGit.ViewModels
{
    public class RevisionCompare : ObservableObject, IDisposable
    {
        public string RepositoryPath
        {
            get => _repo;
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public object StartPoint
        {
            get => _startPoint;
            private set => SetProperty(ref _startPoint, value);
        }

        public object EndPoint
        {
            get => _endPoint;
            private set => SetProperty(ref _endPoint, value);
        }

        public bool CanSaveAsPatch { get; }

        public List<Models.Change> VisibleChanges
        {
            get => _visibleChanges;
            private set => SetProperty(ref _visibleChanges, value);
        }

        public List<Models.Change> SelectedChanges
        {
            get => _selectedChanges;
            set
            {
                if (SetProperty(ref _selectedChanges, value))
                {
                    if (value is { Count: 1 })
                    {
                        var option = new Models.DiffOption(GetSHA(_startPoint), GetSHA(_endPoint), value[0]);
                        DiffContext = new DiffContext(_repo, option, _diffContext, gitStrategy: _gitStrategyType);
                    }
                    else
                    {
                        DiffContext = null;
                    }
                }
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                    RefreshVisible();
            }
        }

        public DiffContext DiffContext
        {
            get => _diffContext;
            private set => SetProperty(ref _diffContext, value);
        }

        private readonly Utils.CommandExtensions.GitStrategyType _gitStrategyType;

        public RevisionCompare(string repo, Models.Commit startPoint, Models.Commit endPoint, Utils.CommandExtensions.GitStrategyType gitStrategyType)
        {
            _repo = repo;
            _startPoint = (object)startPoint ?? new Models.Null();
            _endPoint = (object)endPoint ?? new Models.Null();
            CanSaveAsPatch = startPoint != null && endPoint != null;
            _gitStrategyType = gitStrategyType;

            Task.Run(Refresh);
        }

        public void Dispose()
        {
            _repo = null;
            _startPoint = null;
            _endPoint = null;
            _changes?.Clear();
            _visibleChanges?.Clear();
            _selectedChanges?.Clear();
            _searchFilter = null;
            _diffContext = null;
        }

        public void OpenChangeWithExternalDiffTool(Models.Change change)
        {
            var opt = new Models.DiffOption(GetSHA(_startPoint), GetSHA(_endPoint), change);
            var toolType = Preferences.Instance.ExternalMergeToolType;
            var toolPath = Preferences.Instance.ExternalMergeToolPath;
            new Commands.DiffTool(_repo, toolType, toolPath, opt)
                .WithGitStrategy(Utils.CommandExtensions.GitStrategyType.Remote).Open();
        }

        public void NavigateTo(string commitSHA)
        {
            var launcher = App.GetLauncher();
            if (launcher == null)
                return;

            foreach (var page in launcher.Pages)
            {
                if (page.Data is Repository repo && repo.FullPath.Equals(_repo))
                {
                    repo.NavigateToCommit(commitSHA);
                    break;
                }
            }
        }

        public void Swap()
        {
            (StartPoint, EndPoint) = (_endPoint, _startPoint);
            VisibleChanges = [];
            SelectedChanges = [];
            IsLoading = true;
            Task.Run(Refresh);
        }

        public void SaveAsPatch(string saveTo)
        {
            Task.Run(async () =>
            {
                var succ = await Commands.SaveChangesAsPatch.ProcessRevisionCompareChangesAsync(_repo, _changes,
                    GetSHA(_startPoint), GetSHA(_endPoint), saveTo);
                if (succ)
                    App.SendNotification(_repo, App.Text("SaveAsPatchSuccess"));
            });
        }

        public void ClearSearchFilter()
        {
            SearchFilter = string.Empty;
        }

        private void RefreshVisible()
        {
            if (_changes == null)
                return;

            if (string.IsNullOrEmpty(_searchFilter))
            {
                VisibleChanges = _changes;
            }
            else
            {
                var visible = new List<Models.Change>();
                foreach (var c in _changes)
                {
                    if (c.Path.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(c);
                }

                VisibleChanges = visible;
            }
        }

        private void Refresh()
        {
            _changes = new Commands.CompareRevisions(_repo, GetSHA(_startPoint), GetSHA(_endPoint)).ReadAsync().Result;

            var visible = _changes;
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                visible = [];
                foreach (var c in _changes)
                {
                    if (c.Path.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(c);
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                VisibleChanges = visible;
                IsLoading = false;

                if (VisibleChanges.Count > 0)
                    SelectedChanges = [VisibleChanges[0]];
                else
                    SelectedChanges = [];
            });
        }

        private string GetSHA(object obj)
        {
            return obj is Models.Commit commit ? commit.SHA : string.Empty;
        }

        private string _repo;
        private bool _isLoading = true;
        private object _startPoint = null;
        private object _endPoint = null;
        private List<Models.Change> _changes = null;
        private List<Models.Change> _visibleChanges = null;
        private List<Models.Change> _selectedChanges = null;
        private string _searchFilter = string.Empty;
        private DiffContext _diffContext = null;
    }
}
