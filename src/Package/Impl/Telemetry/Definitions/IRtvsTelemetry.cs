﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.R.Package.Telemetry.Definitions {
    /// <summary>
    /// Represents telemetry operations in RTVS
    /// </summary>
    internal interface IRtvsTelemetry : IDisposable {
        void ReportConfiguration();
        void ReportSettings();
        void ReportWindowLayout(IVsUIShell shell);
    }
}
