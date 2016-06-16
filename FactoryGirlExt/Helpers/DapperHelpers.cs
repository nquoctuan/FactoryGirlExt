using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace FactoryGirlExt.Helpers
{
    public class DapperHelpers
    {
        public static T CastDynamicToClass<T>(object value) where T : class, new()
        {
            IDictionary<string, object> dapperRowProperties = value as IDictionary<string, object>;

            T newObject = new T();

            Type businessEntityType = typeof(T);
            PropertyInfo[] properties = businessEntityType.GetProperties();
            Hashtable hashtable = new Hashtable();
            foreach (PropertyInfo info in properties)
            {
                hashtable[info.Name.ToUpper()] = info;
            }

            foreach (KeyValuePair<string, object> property in dapperRowProperties)
            {
                PropertyInfo info = (PropertyInfo)hashtable[property.Key.ToUpper()];

                if ((info != null) && info.CanWrite)
                {
                    if (property.Value == null)
                    {
                        info.SetValue(newObject, null);
                        continue;
                    }

                    var converter = GetTypeConverter(info.PropertyType);
                    info.SetValue(newObject, converter.ConvertFrom(property.Value.ToString()));
                }
            }
            return newObject;
        }

        private static readonly ConcurrentDictionary<Type, TypeConverter> TypeConverterDictionary =
            new ConcurrentDictionary<Type, TypeConverter>();

        private static TypeConverter GetTypeConverter(Type propertyType)
        {
            return TypeConverterDictionary.ContainsKey(propertyType)
                ? TypeConverterDictionary[propertyType]
                : TypeConverterDictionary.GetOrAdd(propertyType, TypeDescriptor.GetConverter(propertyType));
        }
    }
}