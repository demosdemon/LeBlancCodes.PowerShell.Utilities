using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LeBlancCodes.PowerShell.Utilities.Properties;

namespace LeBlancCodes.PowerShell.Utilities.Internal
{
    internal static class EnumerableExtensions
    {
        public static void ForEach<T>([CanBeNull] this IEnumerable<T> source, [NotNull] Action<T> action) =>
            InternalForEach(source, (item, idx) => action(item), false);

        public static void ForEach<TSource, TReturn>([CanBeNull] this IEnumerable<TSource> source, [NotNull] Func<TSource, TReturn> action) =>
            InternalForEach(source, (item, idx) => action(item), false);

        public static void ForEach<T>([CanBeNull] this IEnumerable<T> source, [NotNull] Action<T, int> action) =>
            InternalForEach(source, action, false);

        public static void SafeForEach<T>([CanBeNull] this IEnumerable<T> source, [NotNull] Action<T> action) =>
            InternalForEach(source, (item, idx) => action(item), true);

        public static void SafeForEach<T>([CanBeNull] this IEnumerable<T> source, [NotNull] Action<T, int> action) =>
            InternalForEach(source, action, true);

        [NotNull]
        public static IEnumerable<KeyValuePair<string, object>> AsEnumerable([CanBeNull] this IDictionary dictionary)
        {
            if (dictionary == null) yield break;

            var enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var key = enumerator.Key?.ToString();
                yield return new KeyValuePair<string, object>(key, enumerator.Value);
            }
        }

        [NotNull]
        public static IEnumerable<TReturn> Map<TKey, TValue, TReturn>([CanBeNull] this IEnumerable<KeyValuePair<TKey, TValue>> source,
            [NotNull] Func<TKey, TValue, TReturn> selector)
        {
            Error.ArgumentNull(selector, nameof(selector));
            return (source ?? Enumerable.Empty<KeyValuePair<TKey, TValue>>()).Select(kvp => kvp.Apply(selector));
        }

        public static TReturn Apply<TKey, TValue, TReturn>(this KeyValuePair<TKey, TValue> kvp, [NotNull] Func<TKey, TValue, TReturn> action)
        {
            Error.ArgumentNull(action, nameof(action));
            return action(kvp.Key, kvp.Value);
        }

        private static void InternalForEach<T>([CanBeNull] this IEnumerable<T> source, [NotNull] Action<T, int> action, bool aggregateExceptions)
        {
            source = source ?? Enumerable.Empty<T>();
            Error.ArgumentNull(action, nameof(action));

            var count = 0;
            var exceptions = new LinkedList<Exception>();
            using (var enumerator = source.GetEnumerator())
            {
                try
                {
                    while (enumerator.MoveNext())
                    {
                        try
                        {
                            action(enumerator.Current, count++);
                        }
                        catch (Exception exp) when (aggregateExceptions)
                        {
                            exceptions.AddLast(exp);
                        }
                    }
                }
                // an exception occured in the enumerator itself, not the action
                catch (Exception exp) when (aggregateExceptions)
                {
                    exceptions.AddLast(exp);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(Strings.ExceptionDuringIteration, exceptions);
        }
    }
}
