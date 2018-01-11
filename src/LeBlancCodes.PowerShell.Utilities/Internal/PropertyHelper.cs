using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace LeBlancCodes.PowerShell.Utilities.Internal
{
    /// <summary>
    ///     An object that exposes fast dynamically created acessors for object properties
    /// </summary>
    public class PropertyHelper
    {
        private const BindingFlags PrivateStaticBindingFlags = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly ConcurrentDictionary<Type, PropertyHelper[]> ReflectionCache = new ConcurrentDictionary<Type, PropertyHelper[]>();
        private static readonly MethodInfo CallPropertyGetterOpenGenericMethod = GetMethod(nameof(CallPropertyGetter));
        private static readonly MethodInfo CallPropertyGetterByReferenceOpenGenericMethod = GetMethod(nameof(CallPropertyGetterByReference));

        private static readonly MethodInfo CallPropertySetterOpenGenericMethod = GetMethod(nameof(CallPropertySetter));

        private readonly Func<object, object> _valueGetter;
        private readonly Action<object, object> _valueSetter;

        /// <summary>
        ///     Initialize the PropertyHelper is a reflection object.
        /// </summary>
        /// <param name="property"></param>
        protected internal PropertyHelper(PropertyInfo property)
        {
            Error.ArgumentNull(property, nameof(property));

            Name = property.Name;

            CanRead = property.GetGetMethod() != null;
            if (CanRead)
                _valueGetter = MakeFastPropertyGetter(property);

            CanWrite = property.GetSetMethod() != null;
            if (CanWrite)
                _valueSetter = MakeFastPropertySetter<object>(property);
        }

        /// <summary>
        ///     Ths property's name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Can this property be read?
        /// </summary>
        public bool CanRead { get; }

        /// <summary>
        ///     Can this property be set?
        /// </summary>
        public bool CanWrite { get; }

        /// <summary>
        ///     Get the value of this property on an instance
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public object GetValue(object instance)
        {
            if (_valueGetter == null)
                throw new InvalidOperationException("This property cannot be read.");
            return _valueGetter(instance);
        }

        /// <summary>
        ///     Set the value of this property on an instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        public void SetValue(object instance, object value)
        {
            if (_valueSetter == null)
                throw new InvalidOperationException("This property is read-only.");

            _valueSetter(instance, value);
        }

        /// <summary>
        ///     Creates a single fast property getter. The result is not cached.
        /// </summary>
        /// <param name="property">property info to extract the getter for</param>
        /// <returns>a fast property getter</returns>
        /// <remarks>This method is more memory efficient than a dynamically compiled lambda, and about the same speed</remarks>
        /// <remarks>Adopted from http://sourcebrowser.io/Browse/ASP-NET-MVC/aspnetwebstack/src/Common/PropertyHelper.cs#93 </remarks>
        /// <exception cref="InvalidOperationException">If the property cannot be read or if the property is static</exception>
        public static Func<object, object> MakeFastPropertyGetter(PropertyInfo property)
        {
            Error.ArgumentNull(property, nameof(property));

            var getMethod = property.GetGetMethod();
            if (getMethod == null) throw new InvalidOperationException("No get method");
            if (getMethod.IsStatic) throw new InvalidOperationException("Static get method");
            if (getMethod.GetParameters().Length != 0) throw new InvalidOperationException("Index property");

            var inputType = getMethod.ReflectedType;
            if (inputType == null) throw new InvalidOperationException("No reflected type on the get method");
            var outputType = getMethod.ReturnType;
            if (outputType == null) throw new InvalidOperationException("No return type on the get method");

            var delegateType = inputType.IsValueType ? typeof(ByRefFunc<,>) : typeof(Func<,>);
            var delegateCaller = inputType.IsValueType ? CallPropertyGetterByReferenceOpenGenericMethod : CallPropertyGetterOpenGenericMethod;
            delegateType = delegateType.MakeGenericType(inputType, outputType);
            delegateCaller = delegateCaller.MakeGenericMethod(inputType, outputType);

            var propertyGetterAsFunc = getMethod.CreateDelegate(delegateType);
            var callPropertyGetter = Delegate.CreateDelegate(typeof(Func<object, object>), propertyGetterAsFunc, delegateCaller);
            return (Func<object, object>) callPropertyGetter;
        }

        /// <summary>
        ///     Creates a single fast property setter. The result is not cached.
        /// </summary>
        /// <typeparam name="T">The property declaring type</typeparam>
        /// <param name="property">The property to extract the setter for</param>
        /// <returns>A fast property setter</returns>
        /// <remarks>This method is more memory efficient than a dynamically compiled lambda, and about the same speed</remarks>
        /// <remarks>Adapted from http://sourcebrowser.io/Browse/ASP-NET-MVC/aspnetwebstack/src/Common/PropertyHelper.cs#40 </remarks>
        /// <exception cref="InvalidOperationException">The property is read-only.</exception>
        public static Action<T, object> MakeFastPropertySetter<T>(PropertyInfo property) where T : class
        {
            Error.ArgumentNull(property, nameof(property));

            var setMethod = property.GetSetMethod();
            if (setMethod == null) throw new InvalidOperationException("No set method");
            if (setMethod.IsStatic) throw new InvalidOperationException("Static set method");
            if (setMethod.GetParameters().Length != 1) throw new InvalidOperationException("Set method takes no parameters");
            if (property.ReflectedType?.IsValueType == true) throw new InvalidOperationException("Reflected type should be a class type");

            var inputType = setMethod.ReflectedType;
            if (inputType == null) throw new InvalidOperationException("No reflected type on the set method");
            var valueType = setMethod.GetParameters()[0].ParameterType;

            var delegateType = typeof(Action<,>).MakeGenericType(inputType, valueType);
            var delegateCaller = CallPropertySetterOpenGenericMethod.MakeGenericMethod(inputType, valueType);

            var propertySetterAsAction = setMethod.CreateDelegate(delegateType);
            var callPropertySetter = Delegate.CreateDelegate(typeof(Action<T, object>), propertySetterAsAction, delegateCaller);
            return (Action<T, object>) callPropertySetter;
        }

        /// <summary>
        ///     Creates and caches fast property helpers that expose acessors for every public property on the underlying type.
        /// </summary>
        /// <param name="instance">The instance to extract the properties for</param>
        /// <returns>a cached array of property helpers for this object's underlying type</returns>
        public static PropertyHelper[] GetProperties(object instance) => GetProperties(instance, CreateInstance, ReflectionCache);

        /// <summary>
        ///     Internal method to select and cache properties on a type.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="createPropertyHelper"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        protected static PropertyHelper[] GetProperties(
            object instance,
            Func<PropertyInfo, PropertyHelper> createPropertyHelper,
            ConcurrentDictionary<Type, PropertyHelper[]> cache)
        {
            var type = instance.GetType();

            if (!cache.TryGetValue(type, out var helpers))
            {
                var properties = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.GetIndexParameters().Length == 0);

                helpers = properties.Select(createPropertyHelper).Where(x => x.CanRead || x.CanWrite).ToArray();
                cache.TryAdd(type, helpers);
            }

            return helpers;
        }

        private static PropertyHelper CreateInstance(PropertyInfo property) => new PropertyHelper(property);

        private static object CallPropertyGetter<TDeclaringType, TValue>(Func<TDeclaringType, TValue> getter, object @this) => getter((TDeclaringType) @this);

        private static object CallPropertyGetterByReference<TDeclaringType, TValue>(ByRefFunc<TDeclaringType, TValue> getter, object @this)
        {
            var unboxed = (TDeclaringType) @this;
            return getter(ref unboxed);
        }

        private static void CallPropertySetter<TDeclaringType, TValue>(Action<TDeclaringType, TValue> setter, object @this, object value) =>
            setter((TDeclaringType) @this, (TValue) value);

        private static MethodInfo GetMethod(string methodName, BindingFlags bindingFlags = PrivateStaticBindingFlags) => typeof(PropertyHelper).GetMethod(methodName, bindingFlags);

        private delegate TValue ByRefFunc<TDeclaringType, out TValue>(ref TDeclaringType arg);
    }
}
