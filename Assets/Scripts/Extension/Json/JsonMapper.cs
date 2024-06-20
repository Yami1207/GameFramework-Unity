using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public static class JsonMapper
{
    private struct PropertyMetadata
    {
        public MemberInfo Info;
        public bool IsField;
        public Type Type;
    }

    private static readonly int s_MaxNestingDepth;

    private static readonly IDictionary<Type, IList<PropertyMetadata>> s_TypeProperties;
    private static readonly object s_TypePropertiesLock = new Object();

    static JsonMapper()
    {
        s_MaxNestingDepth = 100;

        s_TypeProperties = new Dictionary<Type, IList<PropertyMetadata>>();
    }

    public static void ToJson(object obj, JsonWriter writer)
    {
        WriteValue(obj, writer, false, 0);
        writer.Flush();
    }

    private static void WriteValue(object obj, JsonWriter writer, bool isPrivate, int depth)
    {
        if (depth > s_MaxNestingDepth)
            throw new JsonException(String.Format("Max allowed object depth reached while trying to export from type {0}", obj.GetType()));

        if (obj == null)
        {
            writer.WriteNull();
            return;
        }

        if (obj is String)
        {
            writer.WriteValue((string)obj);
            return;
        }

        if (obj is Double)
        {
            writer.WriteValue((double)obj);
            return;
        }

        if (obj is Single)
        {
            writer.WriteValue((Single)obj);
            return;
        }

        if (obj is Int32)
        {
            writer.WriteValue((int)obj);
            return;
        }

        if (obj is Boolean)
        {
            writer.WriteValue((bool)obj);
            return;
        }

        if (obj is Int64)
        {
            writer.WriteValue(((long)obj).ToString());
            return;
        }

        if (obj is Array)
        {
            writer.WriteStartArray();
            foreach (object elem in (Array)obj)
                WriteValue(elem, writer, isPrivate, depth + 1);
            writer.WriteEndArray();
            return;
        }

        if (obj is IList)
        {
            writer.WriteStartArray();
            foreach (object elem in (IList)obj)
                WriteValue(elem, writer, isPrivate, depth + 1);
            writer.WriteEndArray();
            return;
        }

        if (obj is IDictionary)
        {
            writer.WriteStartObject();
            foreach (DictionaryEntry entry in (IDictionary)obj)
            {
                writer.WritePropertyName((string)entry.Key);
                WriteValue(entry.Value, writer, isPrivate, depth + 1);
            }
            writer.WriteEndObject();
            return;
        }

        Type objType = obj.GetType();

        if (obj is Enum)
        {
            Type e_type = Enum.GetUnderlyingType(objType);
            if (e_type == typeof(long) || e_type == typeof(uint) || e_type == typeof(ulong))
                writer.WriteValue((ulong)obj);
            else
                writer.WriteValue((int)obj);

            return;
        }

        AddTypeProperties(objType);
        IList<PropertyMetadata> props = s_TypeProperties[objType];

        writer.WriteStartObject();
        foreach (PropertyMetadata data in props)
        {
            if (data.IsField)
            {
                writer.WritePropertyName(data.Info.Name);
                WriteValue(((FieldInfo)data.Info).GetValue(obj), writer, isPrivate, depth + 1);
            }
            else
            {
                PropertyInfo info = (PropertyInfo)data.Info;
                if (info.CanRead)
                {
                    writer.WritePropertyName(data.Info.Name);
                    WriteValue(info.GetValue(obj, null), writer, isPrivate, depth + 1);
                }
            }
        }
        writer.WriteEndObject();
    }

    private static void AddTypeProperties(Type type)
    {
        if (s_TypeProperties.ContainsKey(type))
            return;

        IList<PropertyMetadata> props = new List<PropertyMetadata>();
        foreach (PropertyInfo info in type.GetProperties())
        {
            if (info.Name == "Item")
                continue;

            PropertyMetadata data = new PropertyMetadata();
            data.Info = info;
            data.IsField = false;
            props.Add(data);
        }

        foreach (FieldInfo info in type.GetFields())
        {
            PropertyMetadata data = new PropertyMetadata();
            data.Info = info;
            data.IsField = true;
            props.Add(data);
        }

        lock (s_TypePropertiesLock)
        {
            try
            {
                s_TypeProperties.Add(type, props);
            }
            catch (ArgumentException)
            {
                return;
            }
        }
    }
}
