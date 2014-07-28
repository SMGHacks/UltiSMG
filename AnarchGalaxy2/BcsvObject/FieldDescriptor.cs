using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;

using AnarchGalaxy2.Bcsv;

namespace AnarchGalaxy2.BcsvObject
{
    public class FieldDescriptor : PropertyDescriptor
    {
        private Type _Type = null;

        public FieldDescriptor(string name, Attribute[] attr, Type type)
            : base(name, attr)
        {
            _Type = type;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }
        
        public override Type ComponentType
        {
            get { return typeof(BCSVObject); }
        }

        public override TypeConverter Converter
        {
            get { return new FieldConverter(); }
        }

        public override object GetValue(object component)
        {
            BCSVObject bcsv = component as BCSVObject;

            if (bcsv.HasField(Name))
                return bcsv.GetField<object>(Name);

            return "NULL";
        }

        public override string Description
        {
            get { return GetDescription(); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return _Type; }
        }

        public override void ResetValue(object component)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object component, object value)
        {
            BCSVObject bcsv = component as BCSVObject;

            if (bcsv.HasField(Name))
                bcsv.SetField(Name, value);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public string GetDescription()
        {
            string result = null;
            if (!_Descriptions.TryGetValue(Name, out result))
                return "";

            return result;
        }

        #region Static Members

        private static Dictionary<string, string> _Descriptions = null;

        static FieldDescriptor()
        {
            _Descriptions = new Dictionary<string, string>();
        }

        public static void LoadFieldDescriptions(string path)
        {
            string[] descriptions = File.ReadAllLines(path);

            _Descriptions.Clear();
            foreach (string description in descriptions)
            {
                string[] parts = description.Split(':').Select(x => x.Trim()).ToArray();

                if (parts.Length == 2 && !_Descriptions.ContainsKey(parts[0]))
                    _Descriptions.Add(parts[0], parts[1]);
            }
        }

        #endregion
    }
}
