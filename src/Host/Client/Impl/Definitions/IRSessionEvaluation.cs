﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.R.Host.Client {
    public interface IRSessionEvaluation : IRSessionContext, IRExpressionEvaluator, IDisposable {
        bool IsMutating { get; }
    }
}