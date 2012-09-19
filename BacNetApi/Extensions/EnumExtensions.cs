using System;
using System.Reflection;
using BacNetApi.Attributes;

namespace BacNetApi.Extensions
{
    public static class EnumExtensions
    {
        public static string GetStringValue(this Enum value)
        {
            Type type = value.GetType();

            FieldInfo fieldInfo = type.GetField(value.ToString());

            var attributes = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];

            return attributes != null && attributes.Length > 0 ? attributes[0].StringValue : null;
        }
    }
}
