﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Languages.Core.Text {
    /// <summary>
    /// Implements <seealso cref="ITextProvider"/> over a string
    /// </summary>
    public class TextStream : ITextProvider {
        string _text;

        // Array access (i.e. converting string to an array)
        // is faster, but takes more memory.

        [DebuggerStepThrough]
        public TextStream(string text) {
            _text = text;
        }

        [DebuggerStepThrough]
        public override string ToString() {
            return _text;
        }

        #region ITextStream

        /// <summary>
        /// Text length
        /// </summary>
        public int Length {
            get { return _text.Length; }
        }

        /// <summary>
        /// Retrieves character at a given position
        /// </summary>
        public char this[int position] {
            get {
                if (position < 0 || position >= _text.Length)
                    return '\0';

                return _text[position];
            }
        }

        /// <summary>
        /// Retrieves a substring given start position and length
        /// </summary>
        public string GetText(int position, int length) {
            if (length == 0)
                return String.Empty;

            Debug.Assert(position >= 0 && length >= 0 && position + length <= _text.Length);
            return _text.Substring(position, length);
        }

        /// <summary>
        /// Retrieves substring given text range
        /// </summary>
        [DebuggerStepThrough]
        public string GetText(ITextRange range) {
            return GetText(range.Start, range.Length);
        }

        /// <summary>
        /// Searches text for a given character starting at specified position
        /// </summary>
        /// <param name="ch">Character to find</param>
        /// <param name="startPosition">Starting position</param>
        /// <returns>Character index of the first string appearance or -1 if string was not found</returns>
        [DebuggerStepThrough]
        public int IndexOf(char ch, int startPosition) {
            return _text.IndexOf(ch, startPosition);
        }

        /// <summary>
        /// Searches text for a given character with the specified range
        /// </summary>
        /// <param name="ch">Character to find</param>
        /// <param name="range">Range to search in</param>
        /// <returns>Character index of the first string appearance or -1 if string was not found</returns>
        [DebuggerStepThrough]
        public int IndexOf(char ch, ITextRange range) {
            return _text.IndexOf(ch, range.Start, range.Length);
        }

        /// <summary>
        /// Searches text for a given string starting at specified position
        /// </summary>
        /// <param name="stringToFind">String to find</param>
        /// <param name="startPosition">Starting position</param>
        /// <param name="ignoreCase">True if search should be case-insensitive</param>
        /// <returns>Character index of the first string appearance or -1 if string was not found</returns>
        public int IndexOf(string stringToFind, int startPosition, bool ignoreCase) {
            return _text.IndexOf(stringToFind, startPosition, ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Searches text for a given string within text fragment 
        /// that starts at specified position 
        /// </summary>
        /// <param name="stringToFind">String to find</param>
        /// <param name="range">Range to search in</param>
        /// <param name="ignoreCase">True if search should be case-insensitive</param>
        /// <returns>Character index of the first string appearance or -1 if string was not found</returns>
        public int IndexOf(string stringToFind, ITextRange range, bool ignoreCase) {
            if (range.Start + stringToFind.Length > _text.Length)
                return -1;

            if (range.End > _text.Length)
                return -1;

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return _text.IndexOf(stringToFind, range.Start, range.Length, comparison);
        }

        public bool CompareTo(int position, int length, string compareTo, bool ignoreCase) {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return String.Compare(_text, position, compareTo, 0, length, comparison) == 0;
        }

        public ITextProvider Clone() {
            return new TextStream(_text);
        }

        public int Version { get { return 0; } }

        // static string text provider does not fire text change event
#pragma warning disable 0067
        public event EventHandler<TextChangeEventArgs> OnTextChange;
#pragma warning restore 0067

        #endregion

        #region Dispose
        [DebuggerStepThrough]
        public void Dispose() {
            _text = null;
        }
        #endregion
    }
}
