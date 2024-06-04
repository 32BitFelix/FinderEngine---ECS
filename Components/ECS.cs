
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FinderEngine.Scenes;
using MemorySystems;

namespace Components.ECS;

// The attribute to indicate 
// which object is a component
[AttributeUsage(AttributeTargets.Struct)]
// IMPORTANT: Try to use unmanaged types
public sealed class ComponentAttribute : Attribute;

// The attribute to indicate
// which object is a system
public sealed class SystemAttribute : Attribute;

// The attribute to indicate
// what event in the entityhandler
// a system is interested in
public sealed class InterestedInAttribute : Attribute
{
    public InterestedInAttribute(EntityInterest i_interest)
        => Interest = i_interest;

    public readonly EntityInterest Interest;
}

[Flags]
public enum EntityInterest : byte
{
    // None
    None = 0b0000,

    // Interested about
    // the removal of an entity
    Remove = 0b0001,

    // Interested about
    // the creation of an entity
    Create = 0b0010,
}

// A handler for all classes that have
// the System attribute
public sealed unsafe class SystemHandler
{
    // Static constructor.
    // Gets all "static" objects
    // holding the System attribute
    // and add them to the system accordingly
    static SystemHandler()
    {
        systemStates = new CNArrayImprov<SystemState>(0);

        // TODO: Improve code below

        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute(typeof(SystemAttribute)) == null)
                continue;

            delegate* <void> runAdress = (delegate* <void>)null;

            MethodInfo? method = type.GetMethod("Run");

            if(method != null)
                runAdress =
                    (delegate*<void>)method.MethodHandle.GetFunctionPointer();

            systemStates[systemStates.Size] =
                new SystemState(type.GetHashCode(), runAdress);

            if(type.GetCustomAttribute(typeof(StarterAttribute)) == null)
                continue;

            SystemState* ptr = systemStates.GetPtr(systemStates.Size - 1);

            ptr->Active = true;
        }
    }

    // Holder of all Systems
    private static CNArrayImprov<SystemState> systemStates;

    // Returns the shouldrun
    // property of a system
    public static bool GetShouldRun<T>()
    {
        for(int i = 0; i < systemStates.Size; i++)
        {
            if(systemStates[i].typeID != typeof(T).GetHashCode())
                continue;

            return systemStates[i].Active;
        }

        return false;
    }

    // Sets the shouldrun
    // property of a system
    public static void SetShouldRun<T>(bool value)
    {
        for(int i = 0; i < systemStates.Size; i++)
        {
            if(systemStates[i].typeID != typeof(T).GetHashCode())
                continue;

            SystemState* ptr = systemStates.GetPtr(i);

            ptr->Active = value;          
        }
    }

    // Gave it a <emmintrin.h> style
    // name to scare off unnecessary calls
    // from the user
    //
    // Calls the "Run" method of all
    // active Systems
    public static void __RUNSYSTEMS()
    {
        for(int i = 0; i < systemStates.Size; i++)
        {
            if(!systemStates[i].Active)
                continue;

            systemStates[i].RunCall();
        }
    }

    // Container of parameters
    // for systems
    private struct SystemState
    {
        public SystemState(int i_typeID, delegate* <void> i_RunCall)
        {
            typeID = i_typeID;

            RunCall = i_RunCall;
        }

        public readonly int typeID;

        public readonly delegate* <void> RunCall;

        public bool Active;
    }
}

// Flags for systems.
// Not enough flags planned
// to justify using it (now)
[Flags]
public enum SystemStateFlags : short
{
    None = 0b_0000_0000,
    ShouldRun = 0b_0000_0001,
    
    // The rest
}

// Handles the entities
// in terms of creation
public sealed unsafe class EntityHandler
{
    // static constructor
    static EntityHandler()
    {
        entities = new CNArrayImprov<Entity>(0);

        columns = new CNArrayImprov<nuint>(0);

        componentIndex = new CNMap<int, int>();


        createCalls = new CNArrayImprov<nint>(0);

        removeCalls = new CNArrayImprov<nint>(0);


        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute(typeof(InterestedInAttribute)) == null)
                continue;

            InterestedInAttribute? a = type.GetCustomAttribute<InterestedInAttribute>();
        
            if((a?.Interest & EntityInterest.Remove) == EntityInterest.Remove)
            {
                MethodInfo? mR = type.GetMethod("OnRemoveEntity");

                if(mR == null) break;

                nint mtdRem =
                    mR.MethodHandle.GetFunctionPointer();

                removeCalls[removeCalls.Size] = mtdRem;
            }

            if((a?.Interest & EntityInterest.Create) == EntityInterest.Create)
            {
                MethodInfo? mC = type.GetMethod("OnCreateEntity");

                if(mC == null) break;

                nint mtdCr =
                    mC.MethodHandle.GetFunctionPointer();

                removeCalls[createCalls.Size] = mtdCr;
            }
        }
    }

    // Array to hold the entities
    private static CNArrayImprov<Entity> entities;

    // Array to hold columns of
    // component-types
    private static CNArrayImprov<nuint> columns;

    // A map that holds the index
    // of a component-type's column
    private static CNMap<int, int> componentIndex;

    // Pointer references to all OnRemoveEntity
    // methods in static classes that are interested in
    // events such as creating an entity
    private static CNArrayImprov<nint> createCalls;

    // Pointer references to all OnRemoveEntity
    // methods in static classes that are interested in
    // events such as removing an entity
    private static CNArrayImprov<nint> removeCalls;

    // A counter for convenience sake
    private static int count;

    // Returns a freshly created
    // entity with optinal custom name
    public static Entity CreateEntity(string Name = "NAME")
    {
        CNArrayImprov<char> nName = new CNArrayImprov<char>(Name.Length);

        for(int i = 0; i < nName.Size; i++)
        {
            nName[i] = Name[i];
        }

        // Add entity to 
        // entity list
        entities[++count] = new Entity(count, nName);

        return entities[count];
    }

    // Defaults the
    // specified entity
    public static void RemoveEntity(int entityID)
    {
        for(int i = 0; i < removeCalls.Size; i++)
        {
            delegate* <int, void> m = (delegate* <int, void>)removeCalls[i];

            m(entityID);
        }

        entities[entityID] = default;
    } 

    // Adds a component type to
    // the column collection
    public static void AddComponentType<T>()
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        componentIndex.Add(componentID, columns.Size);

        columns[columns.Size] =
            (nuint)NativeMemory.AllocZeroed((nuint)Unsafe.SizeOf<Column<T>>());

        *(Column<T>*)columns[columns.Size - 1] = new Column<T>(0);
    }

    public static void AddComponent<T>(int entityID)
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        int columnIndex = componentIndex[componentID];

        int componentColumnIndex = ((Column<T>*)columns[columnIndex])[0].Size;

        entities[entityID].components->Add(componentID, componentColumnIndex);

        ((Column<T>*)columns[columnIndex])[0][componentColumnIndex] = default;

        ((Column<T>*)columns[columnIndex])->entityIDs[componentColumnIndex] = entityID;
    }

    public static void RemoveComponent<T>(int entityID)
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        if(!componentIndex.ContainsKey(componentID))
            throw new Exception("Component Type " + componentID + " does not exist in column registry. Try adding it with AddComponentType<T>()");

        int columnIndex = componentIndex[componentID];

        if(!entities[entityID].components->ContainsKey(componentID))
            throw new Exception("Component " + componentID + "(componentID) does not exist in specified Entity.");

        int componentColumnIndex = entities[entityID].components[0][componentID];

        for(int i = 0; i < entities[entityID].components->Keys.Size; i++)
        {
            if(entities[entityID].components->Keys[i] == componentID)
            {
                entities[entityID].components->Keys[i] = 0;

                entities[entityID].components->Values[i] = 0;

                ((Column<T>*)columns[columnIndex])[0][componentColumnIndex] = default;

                // Signalise column that there is a free space now

                break;
            }
        }
    }

    public static void SetComponent<T>(int entityID, T value)
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        if(!componentIndex.ContainsKey(componentID))
            throw new Exception("Component Type " + componentID + " does not exist in column registry. Try adding it with AddComponentType<T>()");

        int columnIndex = componentIndex[componentID];

        if(!entities[entityID].components->ContainsKey(componentID))
            throw new Exception("Component " + componentID + "(componentID) does not exist in specified Entity.");

        int componentColumnIndex = entities[entityID].components[0][componentID];

        ((Column<T>*)columns[columnIndex])[0][componentColumnIndex] = value;
    }

    public static T GetComponent<T>(int entityID)
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        if(!componentIndex.ContainsKey(componentID))
            throw new Exception("Component Type " + componentID + " does not exist in column registry. Try adding it with AddComponentType<T>()");

        int columnIndex = componentIndex[componentID];

        if(!entities[entityID].components->ContainsKey(componentID))
            throw new Exception("Component " + componentID + "(componentID) does not exist in specified Entity.");

        int componentColumnIndex = entities[entityID].components[0][componentID];

        return ((Column<T>*)columns[columnIndex])[0][componentColumnIndex];  
    }

    public static T[] GetColumn<T>()
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        if(!componentIndex.ContainsKey(componentID))
            throw new Exception("Component Type " + componentID + " does not exist in column registry. Try adding it with AddComponentType<T>()");

        int columnIndex = componentIndex[componentID];

        return ((Column<T>*)columns[columnIndex])->ToSpan().ToArray();
    }

    public static int GetIDFromCCI<T>(int index)
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        int columnIndex = componentIndex[componentID];

        return ((Column<T>*)columns[columnIndex])->entityIDs[index];
    }

    public static bool HasComponent<T>(int entityID)
        where T : unmanaged
    {
        int componentID = typeof(T).GetHashCode();

        return entities[entityID].components->ContainsKey(componentID);
    }
}

// The Entity object.
// It represents a
// collection of components
// to easily define unique
// behaviours
public unsafe struct Entity
{
    // Constructor
    public Entity(int i_ID, CNArrayImprov<char> i_Name)
    {
        ID = i_ID;

        Name = i_Name;

        components =
            (CNMap<int, int>*)NativeMemory.AllocZeroed((nuint)Unsafe.SizeOf<CNMap<int, int>>());

        *components = new CNMap<int, int>();
    }

    // Unique id of the entity
    public readonly int ID = 0;

    // The name of the entity
    public readonly CNArrayImprov<char> Name;

    // Holds the components and
    // their indexes in their columns

    // CNMap<componentType, index>
    public CNMap<int, int>* components;
}


public unsafe struct Column<T>
    where T : unmanaged
{


    public Column(int size)
    {
        Values =
            (T*)NativeMemory.AllocZeroed((nuint)(Unsafe.SizeOf<T>() * size));

        Size = size;

        entityIDs = new CNArray<int>(0);
    }


    public T* Values;

    public CNArray<int> entityIDs;


    public int Size {get; private set;}


    public T this[int index]
    {
        get
        {
            return Values[index];
        }

        set
        {
            if(index > Size - 1)
            {
                nuint oldSize = (nuint)Size;

                Size = index + 1;

                Values =
                    (T*)NativeMemory.Realloc(Values, (nuint)(Unsafe.SizeOf<T>() * Size));

                for(nuint i = oldSize; i < (nuint)Size; i++)
                {
                    Values[i] = default;
                }
            }

            Values[index] = value;
        }
    } 


    public T* GetPtr(int index)
    {
        return &Values[index];
    }


    public Span<T> ToSpan()
        => new Span<T>(Values, Size);


    public void Dispose()
        => NativeMemory.Free(Values);
}