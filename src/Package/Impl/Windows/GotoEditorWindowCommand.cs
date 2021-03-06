﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core;
using Microsoft.Languages.Editor.EditorHelpers;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.R.Package.Windows {
    internal sealed class GotoEditorWindowCommand : PackageCommand {
        private readonly IActiveWpfTextViewTracker _viewTracker;
        private readonly IContentType _rContentType;

        public GotoEditorWindowCommand(IActiveWpfTextViewTracker viewTracker, IContentTypeRegistryService svc) :
            base(RGuidList.RCmdSetGuid, RPackageCommandId.icmdShowEditorWindow) {
            _viewTracker = viewTracker;
            _rContentType = svc.GetContentType(RContentTypeDefinition.ContentType);
        }

        protected override void SetStatus() {
            Supported = true;
            Enabled = _viewTracker.GetLastActiveTextView(_rContentType) != null;
        }
        protected override void Handle() {
            var view = _viewTracker.GetLastActiveTextView(_rContentType);
            var textDoc = view?.TextBuffer.GetTextDocument();
            var filePath = textDoc?.FilePath;
            if (filePath != null) {
                var frame = FindDocumentFrame(filePath);
                frame?.Show();
            }
        }

        private IVsWindowFrame FindDocumentFrame(string filePath) {
            var uiShell = VsAppShell.Current.GetGlobalService<IVsUIShell>(typeof(SVsUIShell));
            IVsWindowFrame[] frames = new IVsWindowFrame[1];
            IEnumWindowFrames windowEnum;
            uiShell.GetDocumentWindowEnum(out windowEnum);
            if (windowEnum != null) {
                uint fetched = 0;
                while (VSConstants.S_OK == windowEnum.Next(1, frames, out fetched) && fetched == 1) {
                    object documentPathObject;
                    if (VSConstants.S_OK == frames[0].GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out documentPathObject)) {
                        var docPath = documentPathObject as string;
                        if (docPath != null && docPath.EqualsIgnoreCase(filePath)) {
                            return frames[0];
                        }
                    }
                }
            }
            return null;
        }
    }
}
