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

    public override int GetHashCode()
    {
        return Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0);
    }

    public override string ToString()
    {
        return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ")";
    }
}