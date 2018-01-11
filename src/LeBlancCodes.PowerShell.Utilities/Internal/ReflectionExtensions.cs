using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace LeBlancCodes.PowerShell.Utilities.Internal
{
    internal static class ReflectionExtensions
    {
        public static bool IsVirtual(this PropertyInfo property)
        {
            Error.ArgumentNull(property, nameof(property));

            var methodInfo = property.GetGetMethod(true);
            if (methodInfo != null && methodInfo.IsVirtual)
                return true;

            methodInfo = property.GetSetMethod(true);
            return methodInfo != null && methodInfo.IsVirtual;
        }

        public static bool IsGenericDefinition(this Type type, Type genericTypeDefinition)
        {
            Error.ArgumentNull(type, nameof(type));

            if (!type.IsGenericType)
                return false;

            var t = type.GetGenericTypeDefinition();
            return t == genericTypeDefinition;
        }

        public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition) => ImplementsGenericDefinition(type, genericInterfaceDefinition, out _);

        public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition, out Type implementingType)
        {
            Error.ArgumentNull(type, nameof(type));
            Error.ArgumentNull(genericInterfaceDefinition, nameof(genericInterfaceDefinition));

            if (!genericInterfaceDefinition.IsInterface || !genericInterfaceDefinition.IsGenericTypeDefinition)
                throw new ArgumentException($"{type.Name} is not a generic interface definition", nameof(genericInterfaceDefinition));

            if (type.IsInterface)
            {
                if (type.IsGenericType)
                {
                    var interfaceDefinition = type.GetGenericTypeDefinition();

                    if (interfaceDefinition == genericInterfaceDefinition)
                    {
                        implementingType = type;
                        return true;
                    }
                }
            }

            foreach (var t in type.GetInterfaces())
            {
                if (t.IsGenericType)
                {
                    var interfaceDefinition = t.GetGenericTypeDefinition();

                    if (interfaceDefinition == genericInterfaceDefinition)
                    {
                        implementingType = t;
                        return true;
                    }
                }
            }

            implementingType = default(Type);
            return false;
        }

        public static Type GetCollectionElementType(this Type type)
        {
            Error.ArgumentNull(type, nameof(type));

            if (type.IsArray)
                return type.GetElementType();

            if (ImplementsGenericDefinition(type, typeof(IEnumerable<>), out var genericListType) && !genericListType.IsGenericTypeDefinition)
                return genericListType.GetGenericArguments()[0];

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return null;

            throw new ArgumentException($"Type {type.Name} is not a collection", nameof(type));
        }

        public static void GetDictionaryKeyValueTypes(this Type type, out Type keyType, out Type valueType)
        {
            Error.ArgumentNull(type, nameof(type));

            if (ImplementsGenericDefinition(type, typeof(IDictionary<,>), out var genericDictionaryType) && !genericDictionaryType.IsGenericTypeDefinition)
            {
                var dictionaryTypeArguments = genericDictionaryType.GenericTypeArguments;
                keyType = dictionaryTypeArguments[0];
                valueType = dictionaryTypeArguments[1];
                return;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                keyType = null;
                valueType = null;
                return;
            }

            throw new ArgumentException($"Type {type.Name} is not a dictionary type.", nameof(type));
        }
    }
}
