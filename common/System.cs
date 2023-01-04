using System.Collections.Generic;

namespace System;

internal struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>, IComparable, IComparable<ValueTuple<T1, T2>>
{
    public T1 Item1;
    public T2 Item2;

    public ValueTuple(T1 item1, T2 item2)
    {
        Item1 = item1;
        Item2 = item2;
    }

    public override bool Equals(object obj)
    {
        return obj is ValueTuple<T1, T2> tuple && Equals(tuple);
    }

    public bool Equals(ValueTuple<T1, T2> other)
    {
        return EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
    }

    int IComparable.CompareTo(object other)
    {
        return other is ValueTuple<T1, T2> tuple ? CompareTo(tuple) : throw new();
    }

    public int CompareTo(ValueTuple<T1, T2> other)
    {
        int c = Comparer<T1>.Default.Compare(Item1, other.Item1);
        return c != 0 ? c : Comparer<T2>.Default.Compare(Item2, other.Item2);
    }

    public override int GetHashCode() => unchecked(((currentKey?.GetHashCode() ?? 0) * (int)0xA5555529) + newKey?.GetHashCode() ?? 0);
    public override string ToString() => $"({Item1}, {Item2})";
}
