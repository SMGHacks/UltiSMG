using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using AnarchGalaxy2.Bcsv;

namespace AnarchGalaxy2.BcsvObject
{
    public class BCSVObject : ICloneable, ICustomTypeDescriptor
    {
        private Field[] _Fields = null;
        private Dictionary<string, int> _FieldKey = null;

        public BCSVObject(FieldDefinition[] fields)
        {
            _Fields = new Field[fields.Length];
            _FieldKey = new Dictionary<string, int>();
            
            for (int i = 0; i < fields.Length; i++)
            {
                _Fields[i] = new Field(fields[i], FieldDefinition.GetDefaultValue(fields[i].Type));
                _FieldKey.Add(fields[i].Name, i);
            }
        }

        public FieldDefinition[] GetFieldDefinitions()
        {
            return _Fields.Select(x => x.Definition).ToArray();
        }

        public FieldDefinition GetFieldDefinition(int index)
        {
            return _Fields[index].Definition;
        }

        public FieldDefinition GetFieldDefinition(string name)
        {
            return GetFieldDefinition(_FieldKey[name]);
        }

        public Type GetFieldSystemType(int index)
        {
            return _Fields[index].Definition.GetSystemType();
        }

        public Type GetFieldSystemType(string name)
        {
            return GetFieldSystemType(_FieldKey[name]);
        }

        public int GetFieldType(int index)
        {
            return _Fields[index].Definition.Type;
        }

        public int GetFieldType(string name)
        {
            return GetFieldType(_FieldKey[name]);
        }

        public string GetFieldName(int index)
        {
            return _Fields[index].Definition.Name;
        }

        public bool HasField(string name)
        {
            return _Fields.Any(x => x.Definition.Name == name);
        }

        public T GetField<T>(int index)
        {
            return (T)_Fields[index].Value;
        }

        public T GetField<T>(string name)
        {
            return GetField<T>(_FieldKey[name]);
        }

        public void SetField<T>(int index, T value)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (value.GetType() != _Fields[index].Definition.GetSystemType())
                throw new Exception(string.Format("Invalid value type: {0}", value.GetType().ToString()));

            _Fields[index].Value = value;
        }

        public void SetField<T>(string name, T value)
        {
            SetField<T>(_FieldKey[name], value);
        }

        #region ICloneable Members

        public BCSVObject Clone()
        {
            BCSVObject result = new BCSVObject(GetFieldDefinitions());
            for (int i = 0; i < _Fields.Length; i++)
                result.SetField(i, GetField<object>(i));

            return result;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        #region ICustomTypeDescriptor Members

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptor[] descriptors = new PropertyDescriptor[_Fields.Length];

            for (int i = 0; i < _Fields.Length; i++)
                descriptors[i] = new FieldDescriptor(_Fields[i].Definition.Name, attributes, _Fields[i].Definition.GetSystemType());

            return new PropertyDescriptorCollection(descriptors);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion
        
        #region Static Members

        public static BCSVObject[] FromFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            return FromPtr(bytes);
        }

        public static BCSVObject[] FromFile(string path, out FieldDefinition[] fields)
        {
            byte[] bytes = File.ReadAllBytes(path);

            return FromPtr(bytes, out fields);            
        }

        public static BCSVObject[] FromPtr(byte[] bytes)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            BCSVObject[] result = FromPtr(ptr);
            Marshal.FreeHGlobal(ptr);

            return result;
        }

        public static BCSVObject[] FromPtr(byte[] bytes, out FieldDefinition[] fields)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            BCSVObject[] result = FromPtr(ptr, out fields);
            Marshal.FreeHGlobal(ptr);

            return result;
        }

        public static BCSVObject[] FromPtr(VoidPtr ptr)
        {
            BCSV bcsv = BCSV.Wrap(ptr);           

            BCSVObject[] result = new BCSVObject[bcsv.GetItemCount()];
            FieldDefinition[] fields = bcsv.GetFieldDefinitions(); 

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new BCSVObject(fields);

                for (int j = 0; j < bcsv.GetFieldCount(); j++)
                    result[i].SetField(j, bcsv.GetFieldValue(i, j));
            }

            return result;
        }

        public static BCSVObject[] FromPtr(VoidPtr ptr, out FieldDefinition[] fields)
        {
            BCSV bcsv = BCSV.Wrap(ptr);

            BCSVObject[] result = new BCSVObject[bcsv.GetItemCount()];
            fields = bcsv.GetFieldDefinitions();

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new BCSVObject(fields);

                for (int j = 0; j < bcsv.GetFieldCount(); j++)
                    result[i].SetField(j, bcsv.GetFieldValue(i, j));
            }

            return result;
        }

        #endregion
        
        [DebuggerDisplay("{_Definition._Name} = {_Value}")]
        private class Field
        {
            public FieldDefinition Definition { get { return _Definition; } }
            private FieldDefinition _Definition; 

            public object Value { get { return _Value; } set { _Value = value; } }
            private object _Value;

            public Field(FieldDefinition definition, object value)
            {
                _Definition = definition;
                _Value = value;
            }
        }
    }
}
