﻿using System;

namespace Microsoft.Languages.Core.Text
{
    public static class TextProviderExtensions
    {
        public static bool IsWhitespaceBeforePosition(this ITextProvider textProvider, int position)
        {
            char charBefore = position > 0 ? textProvider[position - 1] : 'x';
            return Char.IsWhiteSpace(charBefore);
        }

        public static bool IsNewLineBeforePosition(this ITextProvider textProvider, int position)
        {
            int newLinePosition; // Don't care about the value for this function overload

            return TryGetNewLineBeforePosition(textProvider, position, out newLinePosition);
        }

        public static bool TryGetNewLineBeforePosition(this ITextProvider textProvider, int position, out int newLinePosition)
        {
            newLinePosition = -1;

            if (position > 0) // fxcop fake-out
            {
                for (int i = position - 1; i >= 0; i--)
                {
                    char ch = textProvider[i];

                    if (!Char.IsWhiteSpace(ch))
                        return false;

                    if (ch == '\r' || ch == '\n')
                    {
                        newLinePosition = i;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Counts number of line breaks between position and the nearest 
        /// non-whitespace character that precedes the position
        /// </summary>
        public static int LineBreaksBeforePosition(this ITextProvider textProvider, int position)
        {
            return textProvider.LineBreaksBeforePosition(position, 0);
        }

        /// <summary>
        /// Counts number of line breaks between position and the nearest 
        /// non-whitespace character that precedes the position
        /// </summary>
        public static int LineBreaksBeforePosition(this ITextProvider textProvider, int position, int limit)
        {
            int count = 0;

            if (position > limit) // fxcop fake-out
            {
                for (int i = position - 1; i >= limit; i--)
                {
                    char ch = textProvider[i];

                    if (!Char.IsWhiteSpace(ch))
                        return count;

                    if (ch == '\r' || ch == '\n')
                    {
                        if (i > 0 && (textProvider[i - 1] == '\r' || textProvider[i - 1] == '\n'))
                        {
                            i--;
                        }

                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Counts number of line breaks between position and the nearest 
        /// non-whitespace character that follows the position
        /// </summary>
        public static int LineBreaksAfterPosition(this ITextProvider textProvider, int position)
        {
            return textProvider.LineBreaksAfterPosition(position, textProvider.Length);
        }

        /// <summary>
        /// Counts number of line breaks between position and the nearest 
        /// non-whitespace character that follows the position
        /// </summary>
        public static int LineBreaksAfterPosition(this ITextProvider textProvider, int position, int limit)
        {
            int count = 0;

            for (int i = position; i < limit; i++)
            {
                char ch = textProvider[i];

                if (!Char.IsWhiteSpace(ch))
                    return count;

                if (ch == '\r' || ch == '\n')
                {
                    if (i < textProvider.Length - 1 && (textProvider[i + 1] == '\r' || textProvider[i + 1] == '\n'))
                    {
                        i++;
                    }

                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Determines if there is a line break between given position
        /// and the neares following non-whitespace character
        /// </summary>
        public static bool IsNewLineAfterPosition(ITextProvider textProvider, int position)
        {
            if (position < Int32.MaxValue) // fxcop fakeout
            {
                for (int i = position + 1; i < textProvider.Length; i++)
                {
                    char ch = textProvider[i];

                    if (!Char.IsWhiteSpace(ch))
                        return false;

                    if (ch == '\r' || ch == '\n')
                        return true;
                }
            }

            return false;
        }

        public static int GetLineStart(this ITextProvider textProvider, int position)
        {
            int start = position;
            while(start > 0)
            {
                char ch = textProvider[start - 1];
                if (ch == '\r' || ch == '\n')
                    break;

                start -= 1;
            }

            return start;
        }

        public static string GetLineLeadingWhitespace(this ITextProvider textProvider, int position)
        {
            int start = position;
            int firstNonWhitespacePosition = position;

            while (start > 0)
            {
                char ch = textProvider[start - 1];

                if (ch == '\r' || ch == '\n')
                    break;

                if (!Char.IsWhiteSpace(ch))
                {
                    firstNonWhitespacePosition = start - 1;
                }

                start -= 1;
            }

            return textProvider.GetText(TextRange.FromBounds(start, firstNonWhitespacePosition));
        }
    }
}