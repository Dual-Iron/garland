using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if !NET7_0_OR_GREATER
namespace System
{
    /// <summary>
    /// Represents a 2-tuple, or pair, as a value type.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    public struct ValueTuple<T1, T2>
        : IEquatable<ValueTuple<T1, T2>>, IComparable, IComparable<ValueTuple<T1, T2>>
    {
        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2}"/> instance's first component.
        /// </summary>
        public T1 Item1;

        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2}"/> instance's first component.
        /// </summary>
        public T2 Item2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTuple{T1, T2}"/> value type.
        /// </summary>
        /// <param name="item1">The value of the tuple's first component.</param>
        /// <param name="item2">The value of the tuple's second component.</param>
        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="ValueTuple{T1, T2}"/> instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current instance is equal to the specified object; otherwise, <see langword="false"/>.</returns>
        ///
        /// <remarks>
        /// The <paramref name="obj"/> parameter is considered to be equal to the current instance under the following conditions:
        /// <list type="bullet">
        ///     <item><description>It is a <see cref="ValueTuple{T1, T2}"/> value type.</description></item>
        ///     <item><description>Its components are of the same types as those of the current instance.</description></item>
        ///     <item><description>Its components are equal to those of the current instance. Equality is determined by the default object equality comparer for each component.</description></item>
        /// </list>
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj is ValueTuple<T1, T2> tuple && Equals(tuple);
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="ValueTuple{T1, T2}"/> instance is equal to a specified <see cref="ValueTuple{T1, T2}"/>.
        /// </summary>
        /// <param name="other">The tuple to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The <paramref name="other"/> parameter is considered to be equal to the current instance if each of its fields
        /// are equal to that of the current instance, using the default comparer for that field's type.
        /// </remarks>
        public bool Equals(ValueTuple<T1, T2> other)
        {
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
        }

        int IComparable.CompareTo(object other)
        {
            if (other == null) return 1;

            if (other is not ValueTuple<T1, T2>) {
                throw new();
            }

            return CompareTo((ValueTuple<T1, T2>)other);
        }

        /// <summary>Compares this instance to a specified instance and returns an indication of their relative values.</summary>
        /// <param name="other">An instance to compare.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and <paramref name="other"/>.
        /// Returns less than zero if this instance is less than <paramref name="other"/>, zero if this
        /// instance is equal to <paramref name="other"/>, and greater than zero if this instance is greater 
        /// than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(ValueTuple<T1, T2> other)
        {
            int c = Comparer<T1>.Default.Compare(Item1, other.Item1);
            if (c != 0) return c;

            return Comparer<T2>.Default.Compare(Item2, other.Item2);
        }

        static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }

        /// <summary>
        /// Returns the hash code for the current <see cref="ValueTuple{T1, T2}"/> instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0);
        }

        private int GetHashCodeCore(IEqualityComparer comparer)
        {
            return Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2));
        }

        /// <summary>
        /// Returns a string that represents the value of this <see cref="ValueTuple{T1, T2}"/> instance.
        /// </summary>
        /// <returns>The string representation of this <see cref="ValueTuple{T1, T2}"/> instance.</returns>
        /// <remarks>
        /// The string returned by this method takes the form <c>(Item1, Item2)</c>,
        /// where <c>Item1</c> and <c>Item2</c> represent the values of the <see cref="Item1"/>
        /// and <see cref="Item2"/> fields. If either field value is <see langword="null"/>,
        /// it is represented as <see cref="String.Empty"/>.
        /// </remarks>
        public override string ToString()
        {
            return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ")";
        }
    }

    public unsafe readonly ref struct Span<T>
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        internal Span(T* ptr, int len)
        {
            this.ptr = ptr;
            this.len = len;
        }

        // This is equivalent to `ref T` on Mono because Mono's garbage collector treats all values on the stack as GC pointers.
        // Hence, just having this pointer to an object on the GC will prevent it from being moved or collected.
        // Pointer to the stack is ok too, stack values aren't moved around.
        readonly T* ptr;
        readonly int len;

        public int Length => len;
        public T* Pointer => ptr;

        public T this[int index] {
            get {
                if (index < 0) throw new IndexOutOfRangeException($"index is {index}");
                if (index >= len) throw new IndexOutOfRangeException($"index is {index} but the len is {len}");
                return ptr[index];
            }
            set {
                if (index < 0) throw new IndexOutOfRangeException($"index is {index}");
                if (index >= len) throw new IndexOutOfRangeException($"index is {index} but the len is {len}");
                ptr[index] = value;
            }
        }

        public Span<T> Slice(int start)
        {
            if (start >= len) throw new IndexOutOfRangeException($"start is {start} but the len is {len}");
            return new(ptr + start, len - start);
        }
        public Span<T> Slice(int start, int length)
        {
            if (start + length >= len) throw new IndexOutOfRangeException($"start + length is {start} but the len is {len}");
            return new(ptr + start, length);
        }

        public void CopyTo(Span<T> destination)
        {
            if (destination.Length < len) throw new ArgumentException($"destination length is {destination.Length} but source length is {len}");

            for (int i = 0; i < len; i++) {
                destination[i] = ptr[i];
            }
        }

        public void Fill(T value)
        {
            for (int i = 0; i < len; i++) {
                ptr[i] = value;
            }
        }
    }

    public unsafe readonly ref struct ReadOnlySpan<T>
    {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
        internal ReadOnlySpan(T* ptr, int len)
        {
            this.ptr = ptr;
            this.len = len;
        }

        // This is equivalent to `ref T` on Mono because Mono's garbage collector treats all values on the stack as GC pointers.
        // Hence, just having this pointer to an object on the GC will prevent it from being moved or collected.
        // Pointer to the stack is ok too, stack values aren't moved around.
        readonly T* ptr;
        readonly int len;

        public int Length => len;
        public T* Pointer => ptr;

        public T this[int index] {
            get {
                if (index < 0) throw new IndexOutOfRangeException($"index is {index}");
                if (index >= len) throw new IndexOutOfRangeException($"index is {index} but the length is {len}");
                return ptr[index];
            }
        }

        public ReadOnlySpan<T> Slice(int start)
        {
            if (start >= len) throw new ArgumentOutOfRangeException("start", $"start is {start} but the length is {len}");
            return new(ptr + start, len - start);
        }
        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if (start + length >= len) throw new ArgumentOutOfRangeException("start, length", $"start + length is {start} but the length is {len}");
            return new(ptr + start, length);
        }

        public void CopyTo(Span<T> destination)
        {
            if (destination.Length < len) throw new ArgumentException($"destination length is {destination.Length} but source length is {len}");

            for (int i = 0; i < len; i++) {
                destination[i] = ptr[i];
            }
        }

        public static implicit operator ReadOnlySpan<T>(Span<T> span) => new(span.Pointer, span.Length);
        public static implicit operator ReadOnlySpan<T>(T[] array)
        {
            unsafe {
                fixed (T* ptr = array) {
                    return new(ptr, array.Length);
                }
            }
        }
    }

    public static class SpanExt
    {
        public static Span<T> AsSpan<T>(this T[] array)
        {
            unsafe {
                fixed (T* ptr = array) {
                    return new(ptr, array.Length);
                }
            }
        }
        public static Span<T> AsSpan<T>(this T[] array, int start) => array.AsSpan().Slice(start);
        public static Span<T> AsSpan<T>(this T[] array, int start, int length) => array.AsSpan().Slice(start, length);
    }
}
#endif
