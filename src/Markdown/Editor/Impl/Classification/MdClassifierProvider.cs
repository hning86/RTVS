﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Languages.Core.Classification;
using Microsoft.Languages.Editor.Composition;
using Microsoft.Languages.Editor.Services;
using Microsoft.Markdown.Editor.ContentTypes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Markdown.Editor.Classification.MD {
    [Export(typeof(IClassifierProvider))]
    [ContentType(MdContentTypeDefinition.ContentType)]
    internal sealed class MdClassifierProvider : MarkdownClassifierProvider<MdClassifierProvider> {
        private readonly IClassificationTypeRegistryService _classificationRegistryService;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IEnumerable<Lazy<IClassificationNameProvider, IComponentContentTypes>> _classificationNameProviders;

        [ImportingConstructor]
        public MdClassifierProvider(IClassificationTypeRegistryService crs, IContentTypeRegistryService ctrs,
                                    [ImportMany] IEnumerable<Lazy<IClassificationNameProvider, IComponentContentTypes>> cnp) {
            _classificationRegistryService = crs;
            _contentTypeRegistryService = ctrs;
            _classificationNameProviders = cnp;
        }

        protected override IClassifier CreateClassifier(ITextBuffer textBuffer) {
            var classifier = ServiceManager.GetService<MdClassifier>(textBuffer);
            if (classifier == null) {
                classifier = new MdClassifier(textBuffer, _classificationRegistryService, _contentTypeRegistryService, _classificationNameProviders);
            }
            return classifier;
        }
    }
}
