using System;
using System.Collections;
using System.Collections.Generic;

namespace Quack
{
    public static class Extension
    {
        public static T As<T>(this object value) where T : class
        {
            if (value is T t0)
                return t0;

            if (value is IValue implementationValue)
            {
                value = implementationValue.Value;

                if (value is T t1)
                    return t1;
            }

            return Implementation.Implement<T>(value);
        }

        internal static IEnumerable<IndexValuePair<T>> Enumerate<T>(this IEnumerable<T> source, int start = 0)
        {
            foreach (var item in source)
                yield return new IndexValuePair<T>(start++, item);
        }
    }

    struct IndexValuePair<T>
    {
        public readonly int Index;
        public readonly T Value;

        internal IndexValuePair(int index, T value)
        {
            Index = index;
            Value = value;
        }

        public void Deconstruct(out int index, out T value)
        {
            index = Index;
            value = Value;
        }
    }
}
