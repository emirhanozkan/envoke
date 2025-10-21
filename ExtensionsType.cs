using System;

namespace Envoke
{
    /// <summary>
    /// Provides extension methods for Type operations.
    /// These methods are used internally by Envoke for type handling and reflection operations.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets the default value for a given type.
        /// For value types, this returns a new instance created with Activator.CreateInstance.
        /// For reference types and void, this returns null.
        /// </summary>
        /// <param name="returnType">The type to get the default value for</param>
        /// <returns>The default value for the specified type</returns>
        public static object GetDefaultValue(this Type returnType)
        {
            if (returnType == typeof(void))
                return null;

            if (returnType.IsValueType)
                return Activator.CreateInstance(returnType);

            return null;
        }
    }
}
