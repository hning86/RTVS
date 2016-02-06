﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Common.Core;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Controller.Command;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools;

namespace Microsoft.VisualStudio.R.Package.Repl.Commands {
    public sealed class WorkingDirectoryCommand : Command, IDisposable {
        private string _userDirectory;
        private IRSession _session;

        public WorkingDirectoryCommand() :
            base(new [] {
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdSelectWorkingDirectory),
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdGetDirectoryList),
                new CommandId(RGuidList.RCmdSetGuid, RPackageCommandId.icmdSetWorkingDirectory)
            }, false) {

            _session = VsAppShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>().GetInteractiveWindowRSession();
            _session.Connected += OnSessionConnected;
            _session.DirectoryChanged += OnCurrentDirectoryChanged;
        }

        public void Dispose() {
            if (_session != null) {
                _session.Connected -= OnSessionConnected;
                _session.DirectoryChanged -= OnCurrentDirectoryChanged;
            }
            _session = null;
        }

        private void OnCurrentDirectoryChanged(object sender, EventArgs e) {
            FetchRWorkingDirectoryAsync().DoNotWait();
        }

        private void OnSessionConnected(object sender, EventArgs e) {
            if (_userDirectory == null) {
                GetRUserDirectoryAsync()
                    .ContinueWith(async (t) => await FetchRWorkingDirectoryAsync())
                    .DoNotWait();
            } else {
                FetchRWorkingDirectoryAsync().DoNotWait();
            }
        }

        private async Task FetchRWorkingDirectoryAsync() {
            string directory = await GetRWorkingDirectoryAsync();
            if (!string.IsNullOrEmpty(directory)) {
                RToolsSettings.Current.WorkingDirectory = directory;
            }
        }

        public override CommandStatus Status(Guid group, int id) {
            if (ReplWindow.ReplWindowExists) {
                return CommandStatus.SupportedAndEnabled;
            }
            return CommandStatus.Supported;
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg) {
            switch (id) {
                case RPackageCommandId.icmdSelectWorkingDirectory:
                    SelectDirectory();
                    break;

                case RPackageCommandId.icmdGetDirectoryList:
                    // Return complete list
                    outputArg = GetFriendlyDirectoryNames();
                    break;

                case RPackageCommandId.icmdSetWorkingDirectory:
                    if (inputArg == null) {
                        // Return currently selected item
                        if (!string.IsNullOrEmpty(RToolsSettings.Current.WorkingDirectory)) {
                            outputArg = GetFriendlyDirectoryName(RToolsSettings.Current.WorkingDirectory);
                        }
                    } else {
                        SetDirectory(inputArg as string);
                    }
                    break;
            }

            return CommandResult.Executed;
        }

        internal Task SetDirectory(string friendlyName) {
            string currentDirectory = RToolsSettings.Current.WorkingDirectory;
            string newDirectory = GetFullPathName(friendlyName);
            if (newDirectory != null && currentDirectory != newDirectory) {
                RToolsSettings.Current.WorkingDirectory = newDirectory;
                return Task.Run(async () => {
                    await TaskUtilities.SwitchToBackgroundThread();
                    _session.ScheduleEvaluation(async e => {
                        await e.SetWorkingDirectory(newDirectory);
                    });
                });
            }

            return Task.CompletedTask;
        }

        internal void SelectDirectory() {
            IVsUIShell uiShell = VsAppShell.Current.GetGlobalService<IVsUIShell>(typeof(SVsUIShell));
            IntPtr dialogOwner;
            uiShell.GetDialogOwnerHwnd(out dialogOwner);

            string currentDirectory = RToolsSettings.Current.WorkingDirectory;
            string newDirectory = Dialogs.BrowseForDirectory(dialogOwner, currentDirectory, Resources.ChooseDirectory);
            SetDirectory(newDirectory);
        }

        internal string[] GetFriendlyDirectoryNames() {
            return RToolsSettings.Current.WorkingDirectoryList
                .Select(GetFriendlyDirectoryName)
                .ToArray();
        }

        internal string GetFriendlyDirectoryName(string directory) {
            if (!string.IsNullOrEmpty(_userDirectory)) {
                if (directory.StartsWithIgnoreCase(_userDirectory)) {
                    var relativePath = PathHelper.MakeRelative(_userDirectory, directory);
                    if (relativePath.Length > 0) {
                        return "~/" + relativePath.Replace('\\', '/');
                    }
                    return "~";
                }
            }
            return directory;
        }

        internal string GetFullPathName(string friendlyName) {
            string folder = friendlyName;
            if (friendlyName == null) {
                return folder;
            }

            if (!friendlyName.StartsWithIgnoreCase("~")) {
                return folder;
            }

            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (friendlyName.EqualsIgnoreCase("~")) {
                return myDocuments;
            }

            return PathHelper.MakeRooted(PathHelper.EnsureTrailingSlash(myDocuments), friendlyName.Substring(2));
        }

        internal async Task<string> GetRWorkingDirectoryAsync() {
            try {
                using (IRSessionEvaluation eval = await _session.BeginEvaluationAsync(false)) {
                    REvaluationResult result = await eval.EvaluateAsync("getwd()");
                    return ToWindowsPath(result.StringResult);
                }
            } catch (TaskCanceledException) { }
            return null;
        }

        internal Task GetRUserDirectoryAsync() {
            return Task.Run(async () => {
                using (IRSessionEvaluation eval = await _session.BeginEvaluationAsync(false)) {
                    REvaluationResult result = await eval.EvaluateAsync("Sys.getenv('R_USER')");
                    _userDirectory = ToWindowsPath(result.StringResult);
                }
            });
        }

        private static string ToWindowsPath(string rPath) {
            return rPath.Replace('/', '\\');
        }
    }
}
