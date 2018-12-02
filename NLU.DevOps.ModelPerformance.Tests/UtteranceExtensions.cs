// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NLU.DevOps.ModelPerformance.Tests
{
    using System.Collections.Generic;

    internal static class UtteranceExtensions
    {
        public static object WithComparer<T>(this T value, IEqualityComparer<T> equalityComparer)
        {
            return new Comparable<T>(value, equalityComparer);
        }

        private class Comparable<T>
        {
            public Comparable(T value, IEqualityComparer<T> equalityComparer)
            {
                this.Value = value;
                this.EqualityComparer = equalityComparer;
            }

            public T Value { get; }

            private IEqualityComparer<T> EqualityComparer { get; }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                else if (obj is Comparable<T> other)
                {
                    return this.Equals(other.Value);
                }
                else if (obj is T otherValue)
                {
                    return this.Equals(otherValue);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return this.EqualityComparer.GetHashCode(this.Value);
            }

            public override string ToString()
            {
                return '"' + this.Value.ToString() + '"';
            }

            private bool Equals(T other)
            {
                return this.EqualityComparer.Equals(this.Value, other);
            }
        }
    }
}
