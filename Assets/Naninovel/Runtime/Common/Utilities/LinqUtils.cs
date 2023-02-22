// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    public static class LinqUtils
    {
        /// <summary>
        /// Removes last item in the list.
        /// </summary>
        public static void RemoveLastItem<T> (this List<T> list, Predicate<T> predicate = null)
        {
            if (list == null || list.Count == 0) return;

            var elementIndex = predicate == null ? list.Count - 1 : list.FindLastIndex(predicate);
            if (elementIndex >= 0)
                list.RemoveAt(elementIndex);
        }

        /// <summary>
        /// Returns last <paramref name="count"/> elements in the collection.
        /// In case collection length is less then <paramref name="count"/>, will return less elements.
        /// </summary>
        public static IEnumerable<T> TakeLast<T> (this IReadOnlyCollection<T> source, int count)
        {
            var skipCount = Mathf.Max(0, source.Count - count);
            return source.Skip(skipCount);
        }

        public static int GetArrayHashCode<T> (this T[] array)
        {
            return ArrayEqualityComparer<T>.GetHashCode(array);
        }

        public static bool IsIndexValid<T> (this T[] array, int index)
        {
            return array.Length > 0 && index >= 0 && index < array.Length;
        }

        public static bool IsIndexValid<T> (this List<T> list, int index)
        {
            return list.Count > 0 && index >= 0 && index < list.Count;
        }

        public static bool IsIndexValid<T> (this IReadOnlyCollection<T> list, int index)
        {
            return list.Count > 0 && index >= 0 && index < list.Count;
        }

        public static int IndexOf<T> (this IReadOnlyList<T> list, T itemToFind)
        {
            var i = 0;
            foreach (var item in list)
            {
                if (Equals(item, itemToFind)) return i;
                i++;
            }
            return -1;
        }

        public static int IndexOf<T> (this IList<T> list, Predicate<T> predicate)
        {
            var i = 0;
            foreach (var item in list)
            {
                if (predicate(item)) return i;
                i++;
            }
            return -1;
        }

        public static int IndexOf<T> (this IReadOnlyList<T> list, Predicate<T> predicate)
        {
            var i = 0;
            foreach (var item in list)
            {
                if (predicate(item)) return i;
                i++;
            }
            return -1;
        }

        public static T Random<T> (this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            var randomIndex = UnityEngine.Random.Range(0, list.Count);
            return list[randomIndex];
        }

        public static T Random<T> (this T[] array)
        {
            if (array == null || array.Length == 0) return default;
            var randomIndex = UnityEngine.Random.Range(0, array.Length);
            return array[randomIndex];
        }

        public static IEnumerable<T> DistinctBy<T, TKey> (this IEnumerable<T> items, Func<T, TKey> property, IEqualityComparer<TKey> propertyComparer = null)
        {
            var comparer = new GeneralPropertyComparer<T, TKey>(property, propertyComparer);
            return items.Distinct(comparer);
        }

        public static float ProgressOf<T> (this IList<T> list, T currentItem)
        {
            return list.IndexOf(currentItem) / (float)list.Count;
        }

        public static IList<T> Swap<T> (this IList<T> list, int indexA, int indexB)
        {
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        public static int RemoveAll<T> (this LinkedList<T> list, Predicate<T> match)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (match == null) throw new ArgumentNullException(nameof(match));

            var count = 0;
            var node = list.First;
            while (node != null)
            {
                var next = node.Next;
                if (match(node.Value))
                {
                    list.Remove(node);
                    count++;
                }
                node = next;
            }
            return count;
        }

        /// <summary>
        /// Orders the elements of <paramref name="source"/> collection in a way that no element depends on any previous element.
        /// </summary>
        /// <param name="source">The collection to order.</param>
        /// <param name="getDependencies">Function used to retrieve element's dependencies.</param>
        ///  <param name="warnCyclic">Whether to warn about cyclic dependencies.</param>
        /// <remarks>Based on: https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp </remarks>
        public static IList<T> TopologicalOrder<T> (this IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies, bool warnCyclic = true)
        {
            var sorted = new List<T>();
            var visited = new Dictionary<T, bool>();

            foreach (var item in source)
                Visit(item);

            return sorted;

            void Visit (T item)
            {
                var alreadyVisited = visited.TryGetValue(item, out var inProcess);

                if (alreadyVisited)
                {
                    if (inProcess && warnCyclic)
                        Debug.LogWarning($"Cyclic dependency found while performing topological ordering of {typeof(T).Name}.");
                }
                else
                {
                    visited[item] = true;

                    var dependencies = getDependencies(item);
                    if (dependencies != null)
                    {
                        foreach (var dependency in dependencies)
                            Visit(dependency);
                    }

                    visited[item] = false;
                    sorted.Add(item);
                }
            }
        }
    }

    public class GeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
    {
        private readonly Func<T, TKey> property;
        private readonly IEqualityComparer<TKey> propertyComparer;

        public GeneralPropertyComparer (Func<T, TKey> property, IEqualityComparer<TKey> propertyComparer = null)
        {
            this.property = property;
            this.propertyComparer = propertyComparer;
        }

        public bool Equals (T first, T second)
        {
            var firstProperty = property.Invoke(first);
            var secondProperty = property.Invoke(second);
            if (propertyComparer != null) return propertyComparer.Equals(firstProperty, secondProperty);
            if (firstProperty == null && secondProperty == null) return true;
            if (firstProperty == null ^ secondProperty == null) return false;
            return firstProperty.Equals(secondProperty);
        }

        public int GetHashCode (T obj)
        {
            var prop = property.Invoke(obj);
            if (propertyComparer != null) return propertyComparer.GetHashCode(prop);
            return prop == null ? 0 : prop.GetHashCode();
        }
    }

    /// <summary>
    /// Allows comparing arrays using equality comparer of the array items.
    /// Type of the array items should provide a valid comparer for this to work.
    /// Implementation based on: https://stackoverflow.com/a/7244729/1202251
    /// </summary>
    /// <typeparam name="T">Type of the items contained in the array.</typeparam>
    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private static readonly EqualityComparer<T> ITEMS_COMPARER = EqualityComparer<T>.Default;

        public static bool Equals (T[] first, T[] second)
        {
            if (first == second) return true;
            if (first == null || second == null) return false;
            if (first.Length != second.Length) return false;
            for (int i = 0; i < first.Length; i++)
                if (!ITEMS_COMPARER.Equals(first[i], second[i]))
                    return false;
            return true;
        }

        public static int GetHashCode (T[] array)
        {
            unchecked
            {
                if (array == null) return 0;
                var hash = 17;
                foreach (var item in array)
                    hash = hash * 31 + ITEMS_COMPARER.GetHashCode(item);
                return hash;
            }
        }

        bool IEqualityComparer<T[]>.Equals (T[] first, T[] second)
        {
            return Equals(first, second);
        }

        int IEqualityComparer<T[]>.GetHashCode (T[] array)
        {
            return GetHashCode(array);
        }
    }
}
