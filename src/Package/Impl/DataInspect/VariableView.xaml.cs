﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Common.Core;
using Microsoft.R.DataInspection;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.R.StackTracing;
using Microsoft.R.Support.Settings.Definitions;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.R.Package.Shell;
using static Microsoft.R.DataInspection.REvaluationResultProperties;

namespace Microsoft.VisualStudio.R.Package.DataInspect {
    public partial class VariableView : UserControl, IDisposable {
        private readonly IRToolsSettings _settings;
        private readonly IRSession _session;
        private readonly IREnvironmentProvider _environmentProvider;
        private readonly IObjectDetailsViewerAggregator _aggregator;

        private ObservableTreeNode _rootNode;

        private static List<REnvironment> _defaultEnvironments = new List<REnvironment>() { new REnvironment(Package.Resources.VariableExplorer_EnvironmentName) };

        public VariableView() : this(null) { }

        public VariableView(IRToolsSettings settings) {
            _settings = settings;

            InitializeComponent();

            _aggregator = VsAppShell.Current.ExportProvider.GetExportedValue<IObjectDetailsViewerAggregator>();

            SetRootNode(VariableViewModel.Ellipsis);

            SortDirection = ListSortDirection.Ascending;
            RootTreeGrid.Sorting += RootTreeGrid_Sorting;

            var sessionProvider = VsAppShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            _session = sessionProvider.GetInteractiveWindowRSession();

            _environmentProvider = new REnvironmentProvider(_session);
            EnvironmentComboBox.DataContext = _environmentProvider;
            _environmentProvider.RefreshEnvironmentsAsync().DoNotWait();
        }

        public void Dispose() {
            RootTreeGrid.Sorting -= RootTreeGrid_Sorting;
            _environmentProvider?.Dispose();
        }

        private void RootTreeGrid_Sorting(object sender, DataGridSortingEventArgs e) {
            // SortDirection
            if (SortDirection == ListSortDirection.Ascending) {
                SortDirection = ListSortDirection.Descending;
            } else {
                SortDirection = ListSortDirection.Ascending;
            }

            _rootNode.Sort();
            e.Handled = true;
        }

        private void EnvironmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if ((EnvironmentComboBox.ItemsSource != _defaultEnvironments) && (e.AddedItems.Count > 0)) {
                var env = e.AddedItems[0] as REnvironment;
                if (env != null) {
                    if (env.Kind == REnvironmentKind.Error) {
                        SetRootNode(VariableViewModel.Error(env.Name));
                    } else {
                        SetRootModelAsync(env).DoNotWait();
                    }
                }
            }
        }

        private async Task SetRootModelAsync(REnvironment env) {
            await TaskUtilities.SwitchToBackgroundThread();
            const REvaluationResultProperties properties = ClassesProperty | ExpressionProperty | TypeNameProperty | DimProperty | LengthProperty;

            IRValueInfo result;
            try {
                result = await _session.EvaluateAndDescribeAsync(env.EnvironmentExpression, properties, null);
            } catch (RException ex) {
                VsAppShell.Current.DispatchOnUIThread(() => SetRootNode(VariableViewModel.Error(ex.Message)));
                return;
            }

            var wrapper = new VariableViewModel(result, _aggregator);
            var rootNodeModel = new VariableNode(_settings, wrapper);
            VsAppShell.Current.DispatchOnUIThread(() => _rootNode.Model = rootNodeModel);
        }

        private void SetRootNode(VariableViewModel evaluation) {
            _rootNode = new ObservableTreeNode(
                new VariableNode(_settings, evaluation),
                Comparer<ITreeNode>.Create(Comparison));

            RootTreeGrid.ItemsSource = new TreeNodeCollection(_rootNode).ItemList;
        }

        private ListSortDirection SortDirection { get; set; }

        private int Comparison(ITreeNode left, ITreeNode right) {
            return VariableNode.Comparison((VariableNode)left, (VariableNode)right, SortDirection);
        }

        private void GridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            HandleDefaultAction();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e) {
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                e.Handled = true;
                return;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e) {
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                HandleDefaultAction();
                e.Handled = true;
                return;
            } else if (e.Key == Key.Delete || e.Key == Key.Back) {
                DeleteCurrentVariableAsync().DoNotWait();
            }
            base.OnPreviewKeyUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            // Suppress Enter navigation
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            if (e.Key == Key.Enter || e.Key == Key.Return) {
                return;
            }
            base.OnKeyUp(e);
        }

        private void HandleDefaultAction() {
            var node = RootTreeGrid.SelectedItem as ObservableTreeNode;
            var ew = node?.Model?.Content as VariableViewModel;
            if (ew != null && ew.CanShowDetail) {
                ew.ShowDetailCommand.Execute(ew);
            }
        }

        private Task DeleteCurrentVariableAsync() {
            var node = RootTreeGrid.SelectedItem as ObservableTreeNode;
            var ew = node?.Model?.Content as VariableViewModel;
            return ew != null ? ew.DeleteAsync() : Task.CompletedTask;
        }

        private ImageMoniker GetVariableIcon(IREvaluationResultInfo info) {
            if (info is IRActiveBindingInfo) {
                return KnownMonikers.Property;
            } else if (info is IRPromiseInfo) {
                return KnownMonikers.Delegate;
            } else if (info is IRErrorInfo) {
                return KnownMonikers.StatusInvalid;
            }

            var value = info as IRValueInfo;
            if (value != null) {
                // Order of checks here is important, as some categories are subsets of others, and hence have to be checked first.
                // For example, all dataframes are also lists, and so we need to check for class "data.frame", and supply an icon
                // for it, before we check for type "list".
                if (value.TypeName == "S4") {
                    return KnownMonikers.Class;
                } else if (value.Classes.Contains("refObjectGenerator")) {
                    return KnownMonikers.NewClass;
                } else if (value.TypeName == "closure" || value.TypeName == "builtin") {
                    return KnownMonikers.Procedure;
                } else if (value.Classes.Contains("formula")) {
                    return KnownMonikers.MemberFormula;
                } else if (value.TypeName == "symbol" || value.TypeName == "language" || value.TypeName == "expression") {
                    return KnownMonikers.Code;
                } else if (value.Classes.Contains("data.frame")) {
                    return KnownMonikers.Table;
                } else if (value.Classes.Contains("matrix")) {
                    return KnownMonikers.Matrix;
                } else if (value.TypeName == "environment") {
                    return KnownMonikers.BulletList;
                } else if (value.TypeName == "list" || (value.IsAtomic() && value.Length > 1)) {
                    return KnownMonikers.OrderedList;
                } else {
                    return KnownMonikers.BinaryRegistryValue;
                }
            }

            return KnownMonikers.UnknownMember;
        }

        private ImageMoniker GetEnvironmentIcon(REnvironmentKind kind) {
            switch (kind) {
                case REnvironmentKind.Global:
                    return KnownMonikers.GlobalVariable;
                case REnvironmentKind.Function:
                    return KnownMonikers.Procedure;
                case REnvironmentKind.Package:
                    return KnownMonikers.Package;
                case REnvironmentKind.Error:
                    return KnownMonikers.StatusInvalid;
                default:
                    return KnownMonikers.BulletList;
            }
        }
    }
}
