﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Microsoft.VisualStudio.R.Package.Options.Attributes
{
    internal class EnumTypeConverter<T> : TypeConverter
    {
        private List<T> _enumValues;
        private List<string> _displayNames;

        protected EnumTypeConverter(string[] localizableDisplayNames)
            : this(0, localizableDisplayNames)
        {
        }

        protected EnumTypeConverter(int startIndex, string[] localizableDisplayNames)
        {
            Array values = Enum.GetValues(typeof(T));
            int nameCount = localizableDisplayNames.Length;

            if (startIndex < 0 || startIndex >= values.Length)
            {
                throw new ArgumentException("Start index is out of range", "startIndex");
            }

            if (startIndex + nameCount > values.Length)
            {
                throw new ArgumentException("Wrong number of localized display names", "localizableDisplayNames");
            }

            _enumValues = new List<T>(nameCount);
            _displayNames = new List<string>(nameCount);

            for (int i = 0; i < nameCount; i++)
            {
                // There might be holes in the list of names, that's OK just skip them
                if (!string.IsNullOrEmpty(localizableDisplayNames[i]))
                {
                    ResourceManager resourceManager = Resources.ResourceManager;
                    string name = resourceManager.GetString(localizableDisplayNames[i]);
                    if (name == null)
                    {
                        throw new ArgumentException("Invalid localized display name", "localizableDisplayNames");
                    }

                    _enumValues.Add((T)values.GetValue(i + startIndex));
                    _displayNames.Add(name);
                }
            }
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(T);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value.GetType() == typeof(T))
            {
                return value;
            }

            if (value.GetType() == typeof(string))
            {
                string valueName = value as string;
                for (int i = 0; i < _displayNames.Count; i++)
                {
                    if (_displayNames[i].Equals(valueName))
                    {
                        return _enumValues[i];
                    }
                }
            }

            return null;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(T) || destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value.GetType() == destinationType)
            {
                return value;
            }

            if (destinationType == typeof(string) && value.GetType() == typeof(T))
            {
                T enumValue = (T)value;

                for (int i = 0; i < _enumValues.Count; i++)
                {
                    if (_enumValues[i].Equals(enumValue))
                    {
                        return _displayNames[i];
                    }
                }

                // Have to return something to avoid an ArgumentNull exception
                return string.Empty;
            }

            return null;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            // Only the enum values can be chosen
            return true;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(_enumValues);
        }
    }
}