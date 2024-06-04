using Components.ECS;
using MemorySystems;
using OpenTK.Mathematics;

namespace Components.SpacialHierarchy;

[InterestedIn(EntityInterest.Remove)]
public unsafe class TransformSystem
{
    static TransformSystem()
    {
        EntityHandler.AddComponentType<Scale>();

        EntityHandler.AddComponentType<Rotation>();

        EntityHandler.AddComponentType<Position>();

        EntityHandler.AddComponentType<Parenting>();

        EntityHandler.AddComponentType<EnableState>();        
    }

    public static void CreateTransform(int entityID, Vector3 i_scale,
        Vector3 i_rotation, Vector3 i_position)
    {
        EntityHandler.AddComponent<Scale>(entityID);

        EntityHandler.SetComponent(entityID, new Scale()
        {
            scale = i_scale,
        });


        EntityHandler.AddComponent<Rotation>(entityID);

        EntityHandler.SetComponent(entityID, new Rotation()
        {
            rotation = i_rotation,
        });


        EntityHandler.AddComponent<Position>(entityID);

        EntityHandler.SetComponent(entityID, new Position()
        {
            position = i_position,
        });        


        EntityHandler.AddComponent<Parenting>(entityID);

        EntityHandler.SetComponent(entityID, new Parenting()
        {
            parent = 0,

            children = new CNArrayImprov<int>(0),
        });   


        EntityHandler.AddComponent<EnableState>(entityID);

        EntityHandler.SetComponent(entityID, new EnableState()
        {
            enabled = true,
        });    
    }

    // Removes the specified transform
    // along with it's children.
    //
    // It will also unbind the transform
    // from it's parent
    public static void OnRemoveEntity(int identifier)
    {
        if(!EntityHandler.HasComponent<Scale>(identifier))
            return;

        Parenting relationship = EntityHandler.GetComponent<Parenting>(identifier);

        if(relationship.parent != 0)
            UnbindChild(relationship.parent, identifier);

        if(relationship.children.Size > 0)
        {
            for(int i = 0; i < relationship.children.Size; i++)
            {
                if(relationship.children[i] == 0) continue;

                OnRemoveEntity(relationship.children[i]);
            }   
        }

        MultiClearance(identifier);
    }

        // Helper method for clearing
        // a transform
        private static void MultiClearance(int identifier)
        {
            EntityHandler.SetComponent(identifier, new Scale()
            {
                scale = Vector3.Zero
            });

            EntityHandler.SetComponent(identifier, new Rotation()
            {
                rotation = Vector3.Zero
            });

            EntityHandler.SetComponent(identifier, new Position()
            {
                position = Vector3.Zero
            });


            Parenting pr = EntityHandler.GetComponent<Parenting>(identifier);


            pr.parent = 0;

            pr.children.Dispose();

            pr.children = new CNArrayImprov<int>(0);


            EntityHandler.SetComponent(identifier, pr);


            EntityHandler.SetComponent(identifier, new EnableState());
        }

    // Returns the up vector
    // of a transform with the
    // given identifier 
    public static Vector3 GetUp(int identifier)
        => Vector3.NormalizeFast(Vector3.UnitY * CreateRotationXYZ_M3(GetRotation(identifier)));

    // Returns the front vector
    // of a transform with the
    // given identifier 
    public static Vector3 GetFront(int identifier)
        => Vector3.NormalizeFast(-Vector3.UnitZ * CreateRotationXYZ_M3(GetRotation(identifier)));

    // Returns the right vector
    // of a transform with the
    // given identifier 
    public static Vector3 GetRight(int identifier)
        => Vector3.NormalizeFast(Vector3.UnitX * CreateRotationXYZ_M3(GetRotation(identifier)));

    // Returns the scale of a transform
    // relative to it's parent or the scene
    public static Vector3 GetScale(int identifier)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Scale scale =
            EntityHandler.GetComponent<Scale>(identifier);

        if(relation.parent == 0)
        {
            return scale.scale;            
        }
        else
        {
            return scale.scale * GetScale(relation.parent);            
        }            
    }     

    // Returns the rotation of a transform.
    // May return one relative to it's parent
    public static Vector3 GetRotation(int identifier)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Rotation rotation =
            EntityHandler.GetComponent<Rotation>(identifier);


        if(relation.parent == 0)
            return rotation.rotation;
        else
            return rotation.rotation + GetRotation(relation.parent);     
    }

    // Returns the rotation of a transform.
    // May return one relative to it's parent (in 2D)
    public static float GetRotation2D(int identifier)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Rotation rotation =
            EntityHandler.GetComponent<Rotation>(identifier);


        if(relation.parent == 0)  
            return rotation.rotation.Z;
        else
            return rotation.rotation.Z + GetRotation2D(relation.parent);
    }

    // Changes the rotation relative
    // to the parent
    public static void SetLocalRotation(int identifier, Vector3 value)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);


        if(relation.parent == 0)
            EntityHandler.SetComponent(identifier, new Rotation()
            {
                rotation = value
            });
        else
        {
            Vector3 parentRot = GetRotation(relation.parent);

            EntityHandler.SetComponent(identifier, new Rotation()
            {
                rotation = new Vector3(value.X - parentRot.X,
                                        value.Y - parentRot.Y,
                                        value.Z - parentRot.Z)
            });    
        }
    }

    // Changes the rotation relative
    // to the parent (in 2D)
    public static void SetLocalRotation2D(int identifier, float value)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Rotation rotation =
            EntityHandler.GetComponent<Rotation>(identifier);  


        if(relation.parent == 0)
            EntityHandler.SetComponent(identifier, new Rotation()
            {
                rotation = new Vector3(rotation.rotation.X,
                                        rotation.rotation.Y,
                                        value)
            });
        else
        {
            Vector3 parentRot = GetRotation(relation.parent);

            EntityHandler.SetComponent(identifier, new Rotation()
            {
                rotation = new Vector3(rotation.rotation.X,
                                        rotation.rotation.Y,
                                        value - parentRot.Z)
            });    
        }
    }   

    // Turns a Vector3's
    // radians into degrees
    private static Vector3 RadToDegVector(Vector3 value)
    {
        return (MathHelper.RadiansToDegrees(value.X),
                MathHelper.RadiansToDegrees(value.Y),
                MathHelper.RadiansToDegrees(value.Z));
    }

    // Turns a Vector3's
    // degrees into radians        
    private static Vector3 DegToRadVector(Vector3 value)
    {
        return (MathHelper.DegreesToRadians(value.X),
                MathHelper.DegreesToRadians(value.Y),
                MathHelper.DegreesToRadians(value.Z));
    }      

    // Returns the translation
    // of the transform.
    // May return it relative to it's parent
    public static Vector3 GetTranslation(int identifier)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Position position =
            EntityHandler.GetComponent<Position>(identifier);  


        if(relation.parent == 0)
            return position.position;
        else
        {


            return position.position * GetScale(relation.parent) *
            CreateRotationXYZ_M3(GetRotation(relation.parent)) +
            GetTranslation(relation.parent);
        }
    }

    // Returns the translation
    // of the transform.
    // May return it relative to it's parent (in 2D)
    public static Vector2 GetTranslation2D(int identifier)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Position position =
            EntityHandler.GetComponent<Position>(identifier);  

        if(relation.parent == 0)
            return position.position.Xy;
        else
        {
            return (position.position * GetScale(relation.parent) *
            CreateRotationXYZ_M3(GetRotation(relation.parent)) +
            GetTranslation(relation.parent)).Xy;
        }
    }        

    // Sets the translation of a
    // transform reltaive to itself
    public static void SetTranslation(int identifier, Vector3 value)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Position position =
            EntityHandler.GetComponent<Position>(identifier);  




        position.position = value;
    }

    // Sets the translation of a
    // transform reltaive to itself (in 2D)
    public static void SetTranslation2D(int identifier, Vector2 value)
    {
        Parenting relation =
            EntityHandler.GetComponent<Parenting>(identifier);

        Position position =
            EntityHandler.GetComponent<Position>(identifier);  


        Vector2 pMulti = relation.parent != 0 ? GetTranslation2D(relation.parent) : Vector2.Zero;


        position.position.Xy = value - pMulti;


        EntityHandler.SetComponent(identifier, position);
    }

    // returns the model Matrix
    // of a transform
    public static Matrix4 GetModelMatrix(int identifier)
    {
        Matrix4 nMatrix = Matrix4.CreateScale(GetScale(identifier));

        nMatrix *= CreateRotationXYZ_M4(GetRotation(identifier));

        nMatrix *= Matrix4.CreateTranslation(GetTranslation(identifier));

        return nMatrix;
    }

    // Objects that lie lower in
    // the y axis will have a higher
    // position in the Z axis
    public static Matrix4 GetModelMatrix2D(int identifier)
    {
        Matrix4 nMatrix = Matrix4.CreateScale(GetScale(identifier));

        nMatrix *= CreateRotationXYZ_M4(GetRotation(identifier));

        nMatrix *= Matrix4.CreateTranslation(GetTranslation(identifier) - new Vector3(0, 0, GetTranslation2D(identifier).Y));

        return nMatrix;
    }

    // Creates a 4x4 dimensional rotation matrix
    private static Matrix4 CreateRotationXYZ_M4(Vector3 value)
    {
        return Matrix4.CreateRotationX(MathHelper.DegreesToRadians(value.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(value.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(value.Z));
    }

    // Creates a 3x3 dimensional rotation matrix
    private static Matrix3 CreateRotationXYZ_M3(Vector3 value)
    {
        return Matrix3.CreateRotationX(MathHelper.DegreesToRadians(value.X)) *
            Matrix3.CreateRotationY(MathHelper.DegreesToRadians(value.Y)) *
            Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(value.Z));
    }

    // Binds a specified child transform
    // to it's specified parent transform
    public static void BindChild(int parent, int child)
    {
        Parenting pRelation = EntityHandler.GetComponent<Parenting>(parent),
            cRelation = EntityHandler.GetComponent<Parenting>(child);

        // Bind parent to child

        cRelation.parent = parent;

        // Bind child to parent

        // First try to see if
        // there is any empty slot
        for(int i = 0; i < pRelation.children.Size; i++)
        {
            if(pRelation.children[i] != 0) continue;

            pRelation.children[i] = child;

            return;
        }

        // If that hasn't worked

        // Then try to allocate
        // a new slot for the child
        pRelation.children[pRelation.children.Size] = child;

        // Save changes
        EntityHandler.SetComponent(parent, pRelation);

        EntityHandler.SetComponent(child, cRelation);
    }

    // Unbinds a specified child transform
    // from it's specified parent transform
    public static void UnbindChild(int parent, int child)
    {
        Parenting pRelation = EntityHandler.GetComponent<Parenting>(parent),
            cRelation = EntityHandler.GetComponent<Parenting>(child);

        // Unbind parent from child

        cRelation.parent = 0;

        // Unbind child from parent

        for(int i = 0; i < pRelation.children.Size; i++)
        {
            if(pRelation.children[i] != child) continue;

            pRelation.children[i] = 0;
        }

        // Save changes
        EntityHandler.SetComponent(parent, pRelation);

        EntityHandler.SetComponent(child, pRelation);
    }

    public static bool GetEnable(int entityID)
    {
        Parenting relation = EntityHandler.GetComponent<Parenting>(entityID);

        EnableState enableState = EntityHandler.GetComponent<EnableState>(entityID);

        if(relation.parent == 0)
            return enableState.enabled;
        else
            return enableState.enabled && GetEnable(relation.parent);
    }
}

[Component]
public struct Scale
{
    public Vector3 scale;
}

[Component]
public struct Rotation
{
    public Vector3 rotation;
}

[Component]
public struct Position
{
    public Vector3 position;
}

[Component]
public struct Parenting
{
    public int parent;

    public CNArrayImprov<int> children;
}

[Component]
public struct EnableState
{
    public bool enabled;
}