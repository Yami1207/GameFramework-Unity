using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[] left, byte[] right)
    {
        if (left == null || right == null)
            return left == right;
        if (left.Length != right.Length)
            return false;
        for (int i = 0; i < left.Length; ++i)
        {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    public int GetHashCode(byte[] key)
    {
        if (key == null)
            throw new ArgumentNullException("key");

        if (key.Length <= 8)
        {
            ulong value = 0;
            for (int i = 0; i < key.Length; ++i)
                value |= ((ulong)key[i] & 0xff) << (8 * i);
            return value.GetHashCode();
        }
        else
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < key.Length; i++)
                    hash = (hash ^ key[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }
}
