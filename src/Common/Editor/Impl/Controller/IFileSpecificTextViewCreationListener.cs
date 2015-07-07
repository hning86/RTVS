﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.Languages.Editor.Controller
{
    /// <summary>
    /// Exported via MEF for a particular content type and file name by code that is interested in tracking 
    /// view creation for that content type but for a specific file name only (e.g. "bower.json" or "package.json").
    /// Interface is called only for a specific file name of the specific content type when a view is created and 
    /// received aggregate focus. Specify the file name usine [FileName("filename.blah")] attribute.
    /// Useful for initialization of cache for certain components (e.g. Bower package info cache, etc.)
    /// </summary>
    public interface IFileSpecificTextViewCreationListener
    {
        void OnTextViewCreated(ITextView textView, ITextBuffer textBuffer);
    }
}