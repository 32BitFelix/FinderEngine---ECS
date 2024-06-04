using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MemorySystems;

#pragma warning disable CS8500

public unsafe struct CNArray<T> : IDisposable
{
    public T* Values;

    public int Size;

    public CNArray(int Size = 0)
    {
        Values =
            (T*)NativeMemory.AllocZeroed((nuint)(Unsafe.SizeOf<T>() * Size));

        this.Size = Size;
    }


    public T this[int index]
    {
        get => Values[index];

        set
        {
            if(index > Size - 1)
            {
                Size = index + 1;

                Values =
                    (T*)NativeMemory.Realloc(Values, (nuint)(Unsafe.SizeOf<T>() * Size));
            }

            Values[index] = value;
        }
    }


    public T* GetPtr(int index)
        => &Values[index];

    public void Dispose()
    {
        NativeMemory.Free(Values);
    }
}

public unsafe struct CNArrayImprov<T> : IDisposable where T : unmanaged
{

    public T* Values;


    public int Size;


    public CNArrayImprov(int Size = 0)
    {
        Values =
            (T*)NativeMemory.AllocZeroed((nuint)(Unsafe.SizeOf<T>() * Size));

        this.Size = Size;
    }


    public T this[int index]
    {
        get => Values[index];

        set
        {
            if(index > Size - 1)
            {
                Size = index + 1;

                Values =
                    (T*)NativeMemory.Realloc(Values, (nuint)(Unsafe.SizeOf<T>() * Size));
            }

            Values[index] = value;
        }
    }


    public bool Contains(T value)
    {
        for(int i = 0; i < Size; i++)
            if(Values[i].Equals(value)) return true;

        return false;
    }


    public T* GetPtr(int index)
        => &Values[index];


    public void Dispose()
        => NativeMemory.Free(Values);


    public Span<T> ToSpan()
        => new Span<T>(Values, Size);
}

public unsafe struct CNMap<TOne, TTwo> : IDisposable
    where TOne : unmanaged
    where TTwo : unmanaged
{
    public CNMap()
    {
        Keys = new CNArrayImprov<TOne>(0);

        Values = new CNArrayImprov<TTwo>(0);
    }

    public void Dispose()
    {
        Keys.Dispose();

        Values.Dispose();
    }    

    public CNArrayImprov<TOne> Keys;

    public CNArrayImprov<TTwo> Values; 

    public void Add(TOne key, TTwo value)
    {
        Keys[Keys.Size] = key;

        Values[Values.Size] = value;
    }

    public TTwo this[TOne key]
    {
        set
        {
            for(int i = 0; i < Keys.Size; i++)
            {
                if(Keys[i].Equals(key))
                    Values[i] = value;
            }
        }

        get
        {
            for(int i = 0; i < Keys.Size; i++)
            {
                if(Keys[i].Equals(key))
                    return Values[i];
            }

            return default;
        }
    }

    public bool ContainsKey(TOne key)
    {
        for(int i = 0; i < Keys.Size; i++)
            if(Keys[i].Equals(key)) return true;

        return false;
    }

    public bool ContainsValue(TTwo value)
    {
        for(int i = 0; i < Values.Size; i++)
            if(Values[i].Equals(value)) return true;

        return false;
    }    

    public TTwo* GetPtrFromKey(TOne key)
    {
        for(int i = 0; i < Keys.Size; i++)
        {
            if(Keys[i].Equals(key))
                return &Values.Values[i];
        }

        return (TTwo*)null;
    }
}

public unsafe class MemoryHelper
{
    public static unsafe int GetTypeSize<T>(T _)
        => Unsafe.SizeOf<T>();  

}

