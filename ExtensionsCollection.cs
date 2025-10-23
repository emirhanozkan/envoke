using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Envoke
{
    /// <summary>
    /// Provides extension methods for converting objects to dictionaries.
    /// These methods are useful for serializing complex objects into key-value pairs
    /// for HTTP requests or logging purposes.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Converts an object to a dictionary of property names and values.
        /// This method recursively processes object properties and collections,
        /// converting them into a flat dictionary structure.
        /// </summary>
        /// <typeparam name="TSelf">The type of the object to convert</typeparam>
        /// <param name="obj">The object to convert to a dictionary</param>
        /// <param name="pName">Optional property name prefix for the root object</param>
        /// <param name="includeNull">Whether to include null values in the dictionary</param>
        /// <param name="enumToInt">Whether to convert enum values to their integer representation</param>
        /// <returns>A dictionary containing property names as keys and string values</returns>
        public static Dictionary<string, string> ConvertToDictionary<TSelf>(this TSelf obj, string pName = null, bool includeNull = false, bool enumToInt = false)
        {
            var result = new Dictionary<string, string>();
            if (obj == null)
            {
                if (!string.IsNullOrEmpty(pName) && includeNull)
                    result.Add(pName, null!);

                return result;
            }

            IEnumerable enumerable = obj as IEnumerable;
            var not = new TypeCode[] { TypeCode.Empty, TypeCode.Object, TypeCode.DBNull };
            if (!not.Any(x => x == Type.GetTypeCode(obj.GetType())))
            {
                if (obj.GetType().IsEnum && enumToInt)
                    result.Add(pName ?? "value", Convert.ToInt32(obj).ToString());
                else
                    result.Add(pName ?? "value", obj.ToString() ?? string.Empty);
            }
            else if (enumerable != null)
            {
                foreach (var listItem in enumerable)
                {
                    foreach (var item in listItem.ConvertToDictionary(pName, includeNull, enumToInt))
                        result.Add(item.Key, item.Value);
                }
            }
            else
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                foreach (var p in obj.GetType().GetProperties(flags))
                {
                    if (!p.CanRead || !p.CanWrite) continue;
                    var currentNodeType = p.PropertyType;
                    var typeCode = Type.GetTypeCode(currentNodeType);
                    var flag = !not.Any(x => x == typeCode);
                    if (flag)
                    {
                        var value = p.GetValue(obj, null)?.ToString();
                        if (includeNull || !string.IsNullOrEmpty(value))
                            result.Add(p.Name, value ?? string.Empty);
                    }
                    else if (currentNodeType != typeof(object) && Type.GetTypeCode(currentNodeType) == TypeCode.Object)
                    {
                        foreach (var item in p.GetValue(obj, null).ConvertToDictionary(p.Name, includeNull, enumToInt))
                            result.Add(item.Key, item.Value);
                    }
                }
            }

            return result;
        }
    }
}
