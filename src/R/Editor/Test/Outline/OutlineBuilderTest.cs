﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Languages.Editor.Outline;
using Microsoft.Languages.Editor.Shell;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Editor.Outline;
using Microsoft.R.Editor.Test.Mocks;
using Microsoft.R.Editor.Tree;
using Microsoft.UnitTests.Core.Threading;
using Microsoft.UnitTests.Core.XUnit;
using Microsoft.VisualStudio.Editor.Mocks;
using Xunit;

namespace Microsoft.R.Editor.Test.Outline {
    [ExcludeFromCodeCoverage]
    [Category.R.Outlining]
    public class ROutlineBuilderTest01 {
        private readonly EditorTestFilesFixture _testFiles;

        public ROutlineBuilderTest01(EditorTestFilesFixture testFiles) {
            _testFiles = testFiles;
        }

        [Test]
        public void ConstructionTest() {
            TextBufferMock textBuffer = new TextBufferMock(string.Empty, RContentTypeDefinition.ContentType);
            EditorTree tree = new EditorTree(textBuffer);
            EditorDocumentMock editorDocument = new EditorDocumentMock(tree);

            ROutlineRegionBuilder ob = new ROutlineRegionBuilder(editorDocument);

            ob.EditorDocument.Should().NotBeNull();
            ob.EditorTree.Should().NotBeNull();

            editorDocument.DocumentClosing.GetInvocationList().Should().ContainSingle();

            FieldInfo treeUpdateField = tree.GetType().GetField("UpdateCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
            var d = (MulticastDelegate)treeUpdateField.GetValue(tree);
            d.GetInvocationList().Should().ContainSingle();

            ob.Dispose();

            editorDocument.DocumentClosing.Should().BeNull();
            treeUpdateField.GetValue(tree).Should().BeNull();
        }

        [Test(ThreadType.UI)]
        public void EmptyTest() {
            OutlineRegionCollection rc = OutlineTest.BuildOutlineRegions("");

            rc.Should().BeEmpty();
            rc.Start.Should().Be(0);
            rc.Length.Should().Be(0);
        }

        [Test(ThreadType.UI)]
        public void Conditionals() {
            string content =
@"if (ncol(x) == 1L) {
    xnames < -1
} else {
    xnames < -paste0(1, 1L:ncol(x))
  }
  if (intercept) {
    x<- cbind(1, x)
    xnames<- c(0, xnames)
  }
";
            OutlineRegionCollection rc = OutlineTest.BuildOutlineRegions(content);

            rc.Should().HaveCount(3);

            rc[0].Start.Should().Be(0);
            rc[0].Length.Should().Be(89);

            rc[1].Start.Should().Be(41);
            rc[1].End.Should().Be(89);
            rc[1].DisplayText.Should().Be("else...");

            rc[2].Start.Should().Be(93);
            rc[2].End.Should().Be(162);
            rc[2].DisplayText.Should().Be("if...");
        }

        [CompositeTest]
        [InlineData("01.r")]
        [InlineData("02.r")]
        public void OutlineFile(string name) {
            Action a = () => OutlineTest.OutlineFile(_testFiles, name);
            a.ShouldNotThrow();
        }
    }

    [ExcludeFromCodeCoverage]
    [Category.R.Outlining]
    [Collection(CollectionNames.NonParallel)]
    public class ROutlineBuilderTest02 {
        [Test]
        public void Sections() {
            string content =
@"# NAME1 -----
x <- 1
# NAME2 -----
";
            TextBufferMock textBuffer = null;
            int calls = 0;
            OutlineRegionsChangedEventArgs args = null;

            UIThreadHelper.Instance.Invoke(() => {
                textBuffer = new TextBufferMock(content, RContentTypeDefinition.ContentType);
                var tree = new EditorTree(textBuffer);
                tree.Build();
                var editorDocument = new EditorDocumentMock(tree);

                var ob = new ROutlineRegionBuilder(editorDocument);
                var rc1 = new OutlineRegionCollection(0);
                ob.BuildRegions(rc1);

                rc1.Should().HaveCount(2);
                rc1[0].DisplayText.Should().Be("# NAME1");
                rc1[1].DisplayText.Should().Be("# NAME2");

                ob.RegionsChanged += (s, e) => {
                    calls++;
                    args = e;
                };

                textBuffer.Insert(2, "A");
                editorDocument.EditorTree.EnsureTreeReady();

                // Wait for background/idle tasks to complete
                var start = DateTime.Now;
                while (calls == 0 && (DateTime.Now - start).TotalMilliseconds < 2000) {
                    EditorShell.Current.DoIdle();
                }
            });

            calls.Should().Be(1);
            args.Should().NotBeNull();
            args.ChangedRange.Start.Should().Be(0);
            args.ChangedRange.End.Should().Be(textBuffer.CurrentSnapshot.Length);
            args.Regions.Should().HaveCount(2);

            args.Regions[0].DisplayText.Should().Be("# ANAME1");
            args.Regions[1].DisplayText.Should().Be("# NAME2");
        }
    }
}
