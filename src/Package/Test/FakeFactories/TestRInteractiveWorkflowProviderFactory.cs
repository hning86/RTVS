﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core.Shell;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Components.History;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.PackageManager;
using Microsoft.R.Components.Settings;
using Microsoft.R.Components.Test.Fakes.InteractiveWindow;
using Microsoft.R.Components.Test.StubFactories;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Mocks;
using Microsoft.R.Support.Settings;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Package.Test.Mocks;
using Microsoft.VisualStudio.R.Package.Utilities;

namespace Microsoft.VisualStudio.R.Package.Test.FakeFactories {
    public static class TestRInteractiveWorkflowProviderFactory {
        public static IRInteractiveWorkflowProvider Create(IRSessionProvider sessionProvider = null
            , IRHistoryProvider historyProvider = null
            , IRPackageManagerProvider packagesProvider = null
            , IActiveWpfTextViewTracker activeTextViewTracker = null
            , IDebuggerModeTracker debuggerModeTracker = null
            , ICoreShell shell = null
            , IRSettings settings = null) {
            sessionProvider = sessionProvider ?? new RSessionProviderMock();
            historyProvider = historyProvider ?? RHistoryProviderStubFactory.CreateDefault();
            packagesProvider = packagesProvider ?? RPackageManagerProviderStubFactory.CreateDefault();

            activeTextViewTracker = activeTextViewTracker ?? new ActiveTextViewTrackerMock(string.Empty, RContentTypeDefinition.ContentType);
            debuggerModeTracker = debuggerModeTracker ?? new VsDebuggerModeTracker();

           return new TestRInteractiveWorkflowProvider(sessionProvider, historyProvider, packagesProvider, activeTextViewTracker, debuggerModeTracker, shell ?? VsAppShell.Current, settings ?? RToolsSettings.Current);
        }
    }
}
