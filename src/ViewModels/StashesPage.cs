﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SourceGit.Utils;

namespace SourceGit.ViewModels
{
    public class StashesPage : ObservableObject, IDisposable
    {
        public string RepositoryPath
        {
            get => _repo.FullPath;
        }

        public List<Models.Stash> Stashes
        {
            get => _stashes;
            set
            {
                if (SetProperty(ref _stashes, value))
                    RefreshVisible();
            }
        }

        public List<Models.Stash> VisibleStashes
        {
            get => _visibleStashes;
            private set
            {
                if (SetProperty(ref _visibleStashes, value))
                    SelectedStash = null;
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

        public Models.Stash SelectedStash
        {
            get => _selectedStash;
            set
            {
                if (SetProperty(ref _selectedStash, value))
                {
                    if (value == null)
                    {
                        Changes = null;
                        _untracked.Clear();
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            var changes = await new Commands.CompareRevisions(_repo.FullPath, $"{value.SHA}^", value.SHA)
                                .WithGitStrategy(Utils.CommandExtensions.GitStrategyType.Remote)
                                .ReadAsync()
                                .ConfigureAwait(false);

                            var untracked = new List<Models.Change>();
                            if (value.Parents.Count == 3)
                            {
                                untracked = await new Commands.CompareRevisions(_repo.FullPath, Models.Commit.EmptyTreeSHA1, value.Parents[2])
                                    .WithGitStrategy(Utils.CommandExtensions.GitStrategyType.Remote)
                                    .ReadAsync()
                                    .ConfigureAwait(false);

                                var needSort = changes.Count > 0 && untracked.Count > 0;
                                changes.AddRange(untracked);
                                if (needSort)
                                    changes.Sort((l, r) => Models.NumericSort.Compare(l.Path, r.Path));
                            }

                            Dispatcher.UIThread.Post(() =>
                            {
                                if (value.SHA.Equals(_selectedStash?.SHA ?? string.Empty, StringComparison.Ordinal))
                                {
                                    _untracked = untracked;
                                    Changes = changes;
                                }
                            });
                        });
                    }
                }
            }
        }

        public List<Models.Change> Changes
        {
            get => _changes;
            private set
            {
                if (SetProperty(ref _changes, value))
                    SelectedChanges = value is { Count: > 0 } ? [value[0]] : [];
            }
        }

        public List<Models.Change> SelectedChanges
        {
            get => _selectedChanges;
            set
            {
                if (SetProperty(ref _selectedChanges, value))
                {
                    if (value is not { Count: 1 })
                        DiffContext = null;
                    else if (_untracked.Contains(value[0]))
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption(Models.Commit.EmptyTreeSHA1, _selectedStash.Parents[2], value[0]), _diffContext, gitStrategy: _repo.GitStrategyType);
                    else
                        DiffContext = new DiffContext(_repo.FullPath, new Models.DiffOption(_selectedStash.Parents[0], _selectedStash.SHA, value[0]), _diffContext, _repo.GitStrategyType);
                }
            }
        }

        public DiffContext DiffContext
        {
            get => _diffContext;
            private set => SetProperty(ref _diffContext, value);
        }

        public StashesPage(Repository repo)
        {
            _repo = repo;
        }

        public void Dispose()
        {
            _stashes?.Clear();
            _changes?.Clear();
            _selectedChanges?.Clear();
            _untracked.Clear();

            _repo = null;
            _selectedStash = null;
            _diffContext = null;
        }

        public void ClearSearchFilter()
        {
            SearchFilter = string.Empty;
        }

        public void Apply(Models.Stash stash)
        {
            if (_repo.CanCreatePopup())
                _repo.ShowPopup(new ApplyStash(_repo, stash));
        }

        public void Drop(Models.Stash stash)
        {
            if (_repo.CanCreatePopup())
                _repo.ShowPopup(new DropStash(_repo, stash));
        }

        public async Task SaveStashAsPathAsync(Models.Stash stash, string saveTo)
        {
            var opts = new List<Models.DiffOption>();
            var changes = await new Commands.CompareRevisions(_repo.FullPath, $"{stash.SHA}^", stash.SHA)
                .WithGitStrategy(Utils.CommandExtensions.GitStrategyType.Remote)
                .ReadAsync()
                .ConfigureAwait(false);

            foreach (var c in changes)
                opts.Add(new Models.DiffOption(_selectedStash.Parents[0], _selectedStash.SHA, c));

            if (stash.Parents.Count == 3)
            {
                var untracked = await new Commands.CompareRevisions(_repo.FullPath, Models.Commit.EmptyTreeSHA1, stash.Parents[2])
                    .WithGitStrategy(Utils.CommandExtensions.GitStrategyType.Remote)
                    .ReadAsync()
                    .ConfigureAwait(false);

                foreach (var c in untracked)
                    opts.Add(new Models.DiffOption(Models.Commit.EmptyTreeSHA1, _selectedStash.Parents[2], c));

                changes.AddRange(untracked);
            }

            var succ = await Commands.SaveChangesAsPatch.ProcessStashChangesAsync(_repo.FullPath, opts, saveTo);
            if (succ)
                App.SendNotification(_repo.FullPath, App.Text("SaveAsPatchSuccess"));
        }

        public void OpenChangeWithExternalDiffTool(Models.Change change)
        {
            Models.DiffOption opt;
            if (_untracked.Contains(change))
                opt = new Models.DiffOption(Models.Commit.EmptyTreeSHA1, _selectedStash.Parents[2], change);
            else
                opt = new Models.DiffOption(_selectedStash.Parents[0], _selectedStash.SHA, change);

            var toolType = Preferences.Instance.ExternalMergeToolType;
            var toolPath = Preferences.Instance.ExternalMergeToolPath;
            new Commands.DiffTool(_repo.FullPath, toolType, toolPath, opt).Open();
        }

        public async Task CheckoutSingleFileAsync(Models.Change change)
        {
            var fullPath = Native.OS.GetAbsPath(_repo.FullPath, change.Path);
            var log = _repo.CreateLog($"Reset File to '{_selectedStash.SHA}'");

            if (_untracked.Contains(change))
            {
                await Commands.SaveRevisionFile.RunAsync(_repo.FullPath, _selectedStash.Parents[2], change.Path, fullPath);
            }
            else if (change.Index == Models.ChangeState.Added)
            {
                await Commands.SaveRevisionFile.RunAsync(_repo.FullPath, _selectedStash.SHA, change.Path, fullPath);
            }
            else
            {
                await new Commands.Checkout(_repo.FullPath)
                    .Use(log)
                    .FileWithRevisionAsync(change.Path, $"{_selectedStash.SHA}");
            }

            log.Complete();
        }

        private void RefreshVisible()
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                VisibleStashes = _stashes;
            }
            else
            {
                var visible = new List<Models.Stash>();
                foreach (var s in _stashes)
                {
                    if (s.Message.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        visible.Add(s);
                }

                VisibleStashes = visible;
            }
        }

        private Repository _repo = null;
        private List<Models.Stash> _stashes = [];
        private List<Models.Stash> _visibleStashes = [];
        private string _searchFilter = string.Empty;
        private Models.Stash _selectedStash = null;
        private List<Models.Change> _changes = null;
        private List<Models.Change> _untracked = [];
        private List<Models.Change> _selectedChanges = [];
        private DiffContext _diffContext = null;
    }
}
