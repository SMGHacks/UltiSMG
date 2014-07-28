using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AnarchGalaxy2.BcsvObject
{
    public class FieldConverter : TypeConverter
    {
        public static bool QuietErrors { get { return _QuietErrors; } set { _QuietErrors = value; } }
        private static bool _QuietErrors = true;

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                try
                {
                    string s = value as string;

                    if (context.PropertyDescriptor.PropertyType == typeof(uint))
                        return uint.Parse(s, NumberStyles.HexNumber);

                    if (context.PropertyDescriptor.PropertyType == typeof(float))
                        return float.Parse(s);

                    if (context.PropertyDescriptor.PropertyType == typeof(ushort))
                        return ushort.Parse(s, NumberStyles.HexNumber);

                    if (context.PropertyDescriptor.PropertyType == typeof(byte))
                        return byte.Parse(s, NumberStyles.HexNumber);

                    if (context.PropertyDescriptor.PropertyType == typeof(string))
                        return value;
                }
                catch (Exception ex)
                {
                    if (_QuietErrors)
                        return context.PropertyDescriptor.GetValue(context.Instance);
                    else
                        throw ex;
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is uint)
                {
                    uint v = (uint)value;
                    return v.ToString("X8");
                }

                if (value is ushort)
                {
                    ushort v = (ushort)value;
                    return v.ToString("X4");
                }

                if (value is byte)
                {
                    byte v = (byte)value;
                    return v.ToString("X2");
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
