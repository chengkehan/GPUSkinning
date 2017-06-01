using UnityEngine;

public class GPUSkinningBetterList<T>
{
    public T[] buffer;

    public int size = 0;

    private int bufferIncrement = 0;

    public T this[int i]
    {
        get { return buffer[i]; }
        set { buffer[i] = value; }
    }

    public GPUSkinningBetterList(int bufferIncrement)
    {
        this.bufferIncrement = Mathf.Max(1, bufferIncrement);
    }

    void AllocateMore()
    {
        T[] newList = (buffer != null) ? new T[buffer.Length + bufferIncrement] : new T[bufferIncrement];
        if (buffer != null && size > 0) buffer.CopyTo(newList, 0);
        buffer = newList;
    }

    public void Clear() { size = 0; }

    public void Release() { size = 0; buffer = null; }

    public void Add(T item)
    {
        if (buffer == null || size == buffer.Length) AllocateMore();
        buffer[size++] = item;
    }

    public void AddRange(T[] items)
    {
        if (items == null)
        {
            return;
        }
        int length = items.Length;
        if (length == 0)
        {
            return;
        }

        if (buffer == null)
        {
            buffer = new T[Mathf.Max(bufferIncrement, length)];
            items.CopyTo(buffer, 0);
            size = length;
        }
        else
        {
            if (size + length > buffer.Length)
            {
                T[] newList = new T[Mathf.Max(buffer.Length + bufferIncrement, size + length)];
                buffer.CopyTo(newList, 0);
                items.CopyTo(newList, size);
                buffer = newList;
            }
            else
            {
                items.CopyTo(buffer, size);
            }
            size += length;
        }
    }

    public void RemoveAt(int index)
    {
        if (buffer != null && index > -1 && index < size)
        {
            --size;
            buffer[index] = default(T);
            for (int b = index; b < size; ++b) buffer[b] = buffer[b + 1];
            buffer[size] = default(T);
        }
    }

    public T Pop()
    {
        if(buffer == null || size == 0)
        {
            return default(T);
        }
        --size;
        T t = buffer[size];
        buffer[size] = default(T);
        return t;
    }

    public T Peek()
    {
        if (buffer == null || size == 0)
        {
            return default(T);
        }
        return buffer[size - 1];
    }
}
