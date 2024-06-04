using MemorySystems;
using OpenTK.Mathematics;

namespace Transforming;

// The transform system is a
// Data oriented, ECS style
// system made with the purpose
// of offering high-end
// spacial hierarchy features
// while still making them easy to use
//
// TODOS: ChildContainer
public unsafe struct TransformSystem
{
    // Constructor
    public TransformSystem()
    {
        XScales = new CNArray<float>(0);

        YScales = new CNArray<float>(0);

        ZScales = new CNArray<float>(0);


        XRotations = new CNArray<float>(0);

        YRotations = new CNArray<float>(0);

        ZRotations = new CNArray<float>(0);


        XPositions = new CNArray<float>(0);

        YPositions = new CNArray<float>(0);

        ZPositions = new CNArray<float>(0);


        Parents = new CNArray<int>(0);

        Children = new CNArray<ChildContainer>(0);


        States = new CNArray<bool>(0);
    }

    // Hold the scales of
    // every transform
    private CNArray<float> XScales,
                            YScales,
                            ZScales;    

    // Hold the rotations of
    // every transform
    // (As radians)
    private CNArray<float> XRotations,
                            YRotations,
                            ZRotations;                            

    // Hold the translations
    // of every transform
    private CNArray<float> XPositions,
                            YPositions,
                            ZPositions;

    // Holds the parents of
    // every transform
    private CNArray<int> Parents;

    // Small data structure to
    // ease the storing of children
    // of a transform
    //
    // Sadly, this forces the data
    // to be stored vertically and
    // less predictably, which can
    // take a hit on the potential
    // performance.
    //
    // TODO: Replace data structure
    // with contigous array that
    // differentiates individual collections
    // with offsets (Limit children count?...)
    private struct ChildContainer
    {
        // Constructor
        public ChildContainer()
        {
            childrenID = new CNArray<int>(0);
        }

        // The actual container
        // of the children
        public CNArray<int> childrenID;
    }

    // Holds the children
    // of all transforms
    private CNArray<ChildContainer> Children;

    // Holds the states of the
    // transforms
    private CNArray<bool> States;

    // Creates a transform with
    // the given values
    public void CreateTransform(int identifier, Vector3 scale,
        Vector3 rotation, Vector3 translation)
    {
        XScales[identifier] = scale.X;

        YScales[identifier] = scale.Y;

        ZScales[identifier] = scale.Z;


        XRotations[identifier] = MathHelper.DegreesToRadians(rotation.X);

        YRotations[identifier] = MathHelper.DegreesToRadians(rotation.Y);

        ZRotations[identifier] = MathHelper.DegreesToRadians(rotation.Z);


        XPositions[identifier] = translation.X;

        YPositions[identifier] = translation.Y;

        ZPositions[identifier] = translation.Z;


        Parents[identifier] = 0;

        Children[identifier] = new ChildContainer();

        States[identifier] = true;
    }

    // Removes the specified transform
    // along with it's children.
    //
    // It will also unbind the transform
    // from it's parent
    public void RemoveTransform(int identifier)
    {
        if(Parents[identifier] != 0)
            UnbindChild(Parents[identifier], identifier);

        if(Children[identifier].childrenID.Size > 0)
        {
            for(int i = 0; i < Children[identifier].childrenID.Size; i++)
            {
                if(Children[identifier].childrenID[i] == 0) continue;

                RemoveTransform(Children[identifier].childrenID[i]);
            }   
        }

        MultiClearance(identifier);
    }

        // Helper method for clearing
        // a transform
        private void MultiClearance(int identifier)
        {
            XScales[identifier] = 0;

            YScales[identifier] = 0;

            ZScales[identifier] = 0;


            XRotations[identifier] = 0;

            YRotations[identifier] = 0;

            ZRotations[identifier] = 0;


            XPositions[identifier] = 0;

            YPositions[identifier] = 0;

            ZPositions[identifier] = 0;


            Parents[identifier] = 0;

            Children[identifier].childrenID.Dispose();
            Children[identifier] = new ChildContainer();

            States[identifier] = false;
        }

    // Returns the up vector
    // of a transform with the
    // given identifier 
    public Vector3 GetUp(int identifier)
    { 
        Vector3 rot = GetRotation(identifier);

        return Vector3.NormalizeFast(Vector3.UnitY * Matrix3.CreateRotationX(rot.X)
                                    * Matrix3.CreateRotationY(rot.Y)
                                    * Matrix3.CreateRotationZ(rot.Z));
    }

    // Returns the front vector
    // of a transform with the
    // given identifier 
    public Vector3 GetFront(int identifier)
    {
        Vector3 rot = GetRotation(identifier);

        return Vector3.NormalizeFast(-Vector3.UnitZ * Matrix3.CreateRotationX(rot.X)
                                    * Matrix3.CreateRotationY(rot.Y)
                                    * Matrix3.CreateRotationZ(rot.Z));
    }

    // Returns the right vector
    // of a transform with the
    // given identifier 
    public Vector3 GetRight(int identifier)
    {


        return Vector3.NormalizeFast(Vector3.UnitX * CreateRotationXYZ_M3(GetRotation(identifier)));
    }

    // Returns the scale of a transform
    // relative to it's parent or the scene
    public Vector3 GetScale(int identifier)
    {

        if(Parents[identifier] == 0)
        {
            return new Vector3(XScales[identifier],
                                YScales[identifier],
                                ZScales[identifier]);            
        }
        else
        {
            return new Vector3(XScales[identifier],
                                YScales[identifier],
                                ZScales[identifier])
                                        
            * GetScale(Parents[identifier]);            
        }            
    }     

    // Returns the scale of a transform
    // relative to itself
    public Vector3 GetLocalScale(int identifier)
    {
        return new Vector3(XScales[identifier],
                            YScales[identifier],
                            ZScales[identifier]);           
    }

    // Sets the scale of a transform
    // with the given new value and id
    public void SetScale(int identifier, Vector3 value)
    {
        XScales[identifier] = value.X;

        YScales[identifier] = value.Y;

        ZScales[identifier] = value.Z;
    }

    // Returns the rotation of a transform.
    // May return one relative to it's parent
    public Vector3 GetRotation(int identifier)
    {
        if(Parents[identifier] == 0)
        {
            Vector3 rot = new Vector3(XRotations[identifier],
                                        YRotations[identifier],
                                        ZRotations[identifier]);

            return RadToDegVector(rot);
        }
        else
        {
            Vector3 rot = new Vector3(XRotations[identifier],
                                        YRotations[identifier],
                                        ZRotations[identifier]);

            return RadToDegVector(rot) +
                    GetRotation(Parents[identifier]);     
        }
    }

    // Returns the rotation of a transform.
    // May return one relative to it's parent (in 2D)
    public float GetRotation2D(int identifier)
    {
        if(Parents[identifier] == 0)
        {   
            return MathHelper.RadiansToDegrees(ZRotations[identifier]);
        }
        else
        {
            return MathHelper.RadiansToDegrees(ZRotations[identifier]) +
                    GetRotation2D(Parents[identifier]);
        }
    }

    // Returns the rotation relative to itself
    public Vector3 GetLocalRotation(int identifier)
    {
        Vector3 rot = new Vector3(XRotations[identifier],
                                    YRotations[identifier],
                                    ZRotations[identifier]);

        return RadToDegVector(rot);
    }

    // Returns the rotation relative to itself (in 2D)
    public float GetLocalRotation2D(int identifier)
    {
        return MathHelper.RadiansToDegrees(ZRotations[identifier]);
    }        

    // Sets a rotation with the
    // given value and index
    public void SetRotation(int identifier, Vector3 value)
    {
       XRotations[identifier] = MathHelper.DegreesToRadians(value.X);

       YRotations[identifier] = MathHelper.DegreesToRadians(value.Y);

       ZRotations[identifier] = MathHelper.DegreesToRadians(value.Z);      
    }

    // Sets a rotation with the
    // given value and index (in 2D)
    public void SetRotation2D(int identifier, float value)
    {
        ZRotations[identifier] = MathHelper.DegreesToRadians(value);
    }

    // Changes the rotation relative
    // to the parent
    public void SetLocalRotation(int identifier, Vector3 value)
    {
        if(Parents[identifier] == 0)
        {
            XRotations[identifier] = MathHelper.DegreesToRadians(value.X);

            YRotations[identifier] = MathHelper.DegreesToRadians(value.Y);

            ZRotations[identifier] = MathHelper.DegreesToRadians(value.Z);       
        }
        else
        {
            Vector3 parentRot = DegToRadVector(GetRotation(Parents[identifier]));

            XRotations[identifier] = MathHelper.DegreesToRadians(value.X) - parentRot.X;

            YRotations[identifier] = MathHelper.DegreesToRadians(value.Y) - parentRot.Y;

            ZRotations[identifier] = MathHelper.DegreesToRadians(value.Z) - parentRot.Z;        
        }
    }

    // Changes the rotation relative
    // to the parent (in 2D)
    public void SetLocalRotation2D(int identifier, float value)
    {
        if(Parents[identifier] == 0)
        {
            ZRotations[identifier] = MathHelper.DegreesToRadians(value);   
        }
        else
        {
            float parentZ = ZRotations[Parents[identifier]];

            ZRotations[identifier] = MathHelper.DegreesToRadians(value) - parentZ; 
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
    public Vector3 GetTranslation(int identifier)
    {
        if(Parents[identifier] == 0)
        {
            Vector3 translation = new Vector3(XPositions[identifier],
                                                YPositions[identifier],
                                                ZPositions[identifier]);

            return translation;
        }
        else
        {
            Vector3 translation = new Vector3(XPositions[identifier],
                                                YPositions[identifier],
                                                ZPositions[identifier]);

            return translation * GetScale(Parents[identifier]) *
            CreateRotationXYZ_M3(GetRotation(Parents[identifier])) +
            GetTranslation(Parents[identifier]);
        }
    }

    // Returns the translation
    // of the transform.
    // May return it relative to it's parent (in 2D)
    public Vector2 GetTranslation2D(int identifier)
    {
        if(Parents[identifier] == 0)
        {
            return new Vector2(XPositions[identifier],
                                YPositions[identifier]);
        }
        else
        {
            Vector3 translation = new Vector3(XPositions[identifier],
                                                YPositions[identifier],
                                                ZPositions[identifier]);

            return (translation * GetScale(Parents[identifier]) *
            CreateRotationXYZ_M3(GetRotation(Parents[identifier])) +
            GetTranslation(Parents[identifier])).Xy;
        }
    }        

    // Returns the translation
    // of a transform relative to itself
    public Vector3 GetLocalTranslation(int identifier)
    {
        return new Vector3(XPositions[identifier],
                            YPositions[identifier],
                            ZPositions[identifier]);
    }

    // Returns the translation
    // of a transform relative to itself (in 2D)
    public Vector2 GetLocalTranslation2D(int identifier)
    {
        return new Vector2(XPositions[identifier],
                            YPositions[identifier]);
    }

    // Sets the translation of a
    // transform reltaive to itself
    public void SetTranslation(int identifier, Vector3 value)
    {
        value = Vector3.Clamp(value, new Vector3(-9999), new Vector3(9999));


        XPositions[identifier] = value.X;

        YPositions[identifier] = value.Y;

        ZPositions[identifier] = value.Z;
    }

    // Sets the translation of a
    // transform reltaive to it's parent
    public void SetLocalTranslation(int identifier, Vector3 value)
    {
        value = Vector3.Clamp(value, new Vector3(-9999), new Vector3(9999));


        XPositions[identifier] = value.X;

        YPositions[identifier] = value.Y;

        ZPositions[identifier] = value.Z;
    }     

    // Sets the translation of a
    // transform reltaive to itself (in 2D)
    public void SetTranslation2D(int identifier, Vector2 value)
    {
        int parent = Parents[identifier];


        Vector2 pMulti = parent != 0 ? GetTranslation2D(parent) : Vector2.Zero;


        value = Vector2.Clamp(value - pMulti, new Vector2(-9999), new Vector2(9999));


        XPositions[identifier] = value.X;

        YPositions[identifier] = value.Y;
    }

    // Sets the translation of a
    // transform reltaive to it's parent (in 2D)
    public void SetLocalTranslation2D(int identifier, Vector2 value)
    {
        value = Vector2.Clamp(value, new Vector2(-9999), new Vector2(9999));


        XPositions[identifier] = value.X;

        YPositions[identifier] = value.Y;
    }        

    // returns the model Matrix
    // of a transform
    public Matrix4 GetModelMatrix(int identifier)
    {
        Matrix4 nMatrix = Matrix4.CreateScale(GetScale(identifier));

        nMatrix *= CreateRotationXYZ_M4(GetRotation(identifier));

        nMatrix *= Matrix4.CreateTranslation(GetTranslation(identifier));

        return nMatrix;
    }

    // Objects that lie lower in
    // the y axis will have a higher
    // position in the Z axis
    public Matrix4 GetModelMatrix2D(int identifier)
    {
        Matrix4 nMatrix = Matrix4.CreateScale(GetScale(identifier));

        nMatrix *= CreateRotationXYZ_M4(GetRotation(identifier));

        nMatrix *= Matrix4.CreateTranslation(GetTranslation(identifier) - new Vector3(0, 0, GetTranslation2D(identifier).Y));

        return nMatrix;
    }

    // Creates a 4x4 dimensional rotation matrix
    private Matrix4 CreateRotationXYZ_M4(Vector3 value)
    {
        return Matrix4.CreateRotationX(MathHelper.DegreesToRadians(value.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(value.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(value.Z));
    }

    // Creates a 3x3 dimensional rotation matrix
    private Matrix3 CreateRotationXYZ_M3(Vector3 value)
    {
        return Matrix3.CreateRotationX(MathHelper.DegreesToRadians(value.X)) *
            Matrix3.CreateRotationY(MathHelper.DegreesToRadians(value.Y)) *
            Matrix3.CreateRotationZ(MathHelper.DegreesToRadians(value.Z));
    }

    // Binds a specified child transform
    // to it's specified parent transform
    public void BindChild(int parent, int child)
    {
        // Bind parent to child

        Parents[child] = parent;

        // Bind child to parent

        ChildContainer* cc = Children.GetPtr(parent);

        // First try to see if
        // there is any empty slot
        for(int i = 0; i < cc->childrenID.Size; i++)
        {
            if(cc->childrenID[i] != 0) continue;

            cc->childrenID[i] = child;

            return;
        }

        // If that hasn't worked

        // Then try to allocate
        // a new slot for the child
        cc->childrenID[cc->childrenID.Size] = child;
    }

    // Unbinds a specified child transform
    // from it's specified parent transform
    public void UnbindChild(int parent, int child)
    {
        // Unbind parent from child

        Parents[child] = 0;

        // Unbind child from parent

        ChildContainer* cc = Children.GetPtr(parent);

        for(int i = 0; i < cc->childrenID.Size; i++)
        {
            if(cc->childrenID[i] != child) continue;

            cc->childrenID[i] = 0;
        }
    }

    // Returns the parent's id of
    // the specified transform
    public int ShowParent(int identifier)
        => Parents[identifier];

    // Returns the children's id
    // of the specified transform
    public int[] ShowChildren(int identifier)
    {
        ChildContainer* cc = Children.GetPtr(identifier);

        Span<int> nArray = new Span<int>(cc->childrenID.Values, cc->childrenID.Size);

        return nArray.ToArray();
    }

    // Sets the state of the specified
    // transform with the given value.
    // entity can be skipped by some
    // systems if state is set to false
    public void SetState(int identifier, bool value)
        => States[identifier] = value;

    // Returns the state of the
    // specified transform relative
    // to it's parent
    public bool GetState(int identifier)
    {
        if(Parents[identifier] != 0)
            return States[identifier] && States[Parents[identifier]];
        else
            return States[identifier];
    }

    // Returns the local state of the
    // specified transform
    public bool GetStateLocal(int identifier)
        => States[identifier];
}