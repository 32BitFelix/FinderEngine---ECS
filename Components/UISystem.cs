

using Components.ECS;
using Components.Shimshek;
using Components.SpacialHierarchy;
using FinderEngine.Scenes;
using MemorySystems;
using OpenTK.Mathematics;
using Utility.InputHandling;

namespace Components.UISystem;


// A system that handles UI related things
[System]
public unsafe class UIHandler
{
    // Static constructor
    static UIHandler()
    {
        EntityHandler.AddComponentType<VirtualMouse>();

        EntityHandler.AddComponentType<Button>();

        EntityHandler.AddComponentType<Slider>();

        EntityHandler.AddComponentType<InputBox>();

        EntityHandler.AddComponentType<Toggle>();
    }


    public static void Run()
    {
        VirtualMouse[] virtualMice = EntityHandler.GetColumn<VirtualMouse>();

        for(int v = 0; v < virtualMice.Length; v++)
        {
            int vIndex = EntityHandler.GetIDFromCCI<VirtualMouse>(v);

            Vector2 pPos = TransformSystem.GetTranslation2D(vIndex);

            pPos += InputHandler.CursorPositionDelta;


            Camera[] cams = EntityHandler.GetColumn<Camera>();

            for(int c = 0; c < cams.Length; c++)
            {
                int cIndex = EntityHandler.GetIDFromCCI<Camera>(c);

                Matrix4 inverseViewProj = (Renderer.MakeViewMatrix(cIndex) * Renderer.MakeProjectionMatrix(cIndex)).Inverted();

                Vector3 PosBound = Vector3.Unproject(new Vector3(1, 1, 1), -1, -1, 2, 2, -1, 1, inverseViewProj);
                Vector3 NegBound = Vector3.Unproject(new Vector3(-1, -1, -1), -1, -1, 2, 2, -1, 1, inverseViewProj);

                pPos = Vector2.Clamp(pPos, NegBound.Xy, PosBound.Xy);
            }

            TransformSystem.SetTranslation2D(vIndex, pPos);


            Button[] buttons = EntityHandler.GetColumn<Button>();

            for(int b = 0; b < buttons.Length; b++)
            {
                int bIndex = EntityHandler.GetIDFromCCI<Button>(b);

                Matrix4 model = TransformSystem.GetModelMatrix(bIndex);

                Matrix4 vMModel = TransformSystem.GetModelMatrix(vIndex);


                if(!TransformSystem.GetEnable(bIndex))
                {
                    buttons[b].pressed = false;

                    EntityHandler.SetComponent(bIndex, buttons[b]);

                    continue;
                }


                if(IsPointerInInteractible(model, vMModel.ExtractTranslation().Xy, vMModel))
                {
                    Sprite s = EntityHandler.GetComponent<Sprite>(bIndex);

                    Sprite vMS = EntityHandler.GetComponent<Sprite>(vIndex);


                    if(InputHandler.GetInput(pressBind))
                    {
                        s.Texture = buttons[b].PressTexture;

                        EntityHandler.SetComponent(bIndex, s);

                        buttons[b].pressed = true;

                        EntityHandler.SetComponent(bIndex, buttons[b]);


                        vMS.Texture = virtualMice[v].PressTexture;

                        EntityHandler.SetComponent(vIndex, vMS);


                        return;
                    }
                    else
                    {
                        s.Texture = buttons[b].HoverTexture;

                        EntityHandler.SetComponent(bIndex, s);

                        buttons[b].pressed = false; 

                        EntityHandler.SetComponent(bIndex, buttons[b]);


                        vMS.Texture = virtualMice[v].HoverTexture;

                        EntityHandler.SetComponent(vIndex, vMS);


                        return;
                    }
                }
                else
                {
                    Sprite s = EntityHandler.GetComponent<Sprite>(bIndex);

                    if(s.Texture != buttons[b].IdleTexture)
                    {
                        s.Texture = buttons[b].IdleTexture;

                        EntityHandler.SetComponent(bIndex, s);
                    }


                    Sprite vMS = EntityHandler.GetComponent<Sprite>(vIndex);

                    if(vMS.Texture != virtualMice[v].IdleTexture)
                    {
                        vMS.Texture = virtualMice[v].IdleTexture;

                        EntityHandler.SetComponent(vIndex, vMS);
                    }


                    buttons[b].pressed = false; 

                    EntityHandler.SetComponent(bIndex, buttons[b]);
                }
            }


            /*Slider[] sliders = EntityHandler.GetColumn<Slider>();

            for(int s = 0; s < sliders.Length; s++)
            {

            }


            InputBox[] inputBoxes = EntityHandler.GetColumn<InputBox>();

            for(int i = 0; i < inputBoxes.Length; i++)
            {

            }


            Toggle[] toggles = EntityHandler.GetColumn<Toggle>();

            for(int t = 0; t < toggles.Length; t++)
            {

            }*/
        }
    }

        // Checks the collision
        // between two polygons
        private static bool IsPointerInInteractible(Matrix4 interactible, Vector2 aPos,
            Matrix4 aMat)
        {
            Vector2[] aVerts =
                TransformVertices(aMat, [(-1, 1), (-0.9f, 1)]);


            Vector2 bPos = interactible.ExtractTranslation().Xy;

            Vector2[] bVerts =
                TransformVertices(interactible, boxVerts);


            Vector2 normal = Vector2.Zero;

            float depth = float.MaxValue;


            for (int i = 0; i < aVerts.Length; i++)
            {
                Vector2 va = aVerts[i];
                Vector2 vb = aVerts[(i + 1) % aVerts.Length];

                Vector2 edge = vb - va;
                Vector2 axis = new Vector2(-edge.Y, edge.X);
                axis.Normalize();

                ProjectVertices(aVerts, axis, out float minA, out float maxA);
                ProjectVertices(bVerts, axis, out float minB, out float maxB);

                if (minA >= maxB || minB >= maxA)
                {
                    return false;
                }

                float axisDepth = MathF.Min(maxB - minA, maxA - minB);

                if (axisDepth < depth)
                {
                    depth = axisDepth;
                    normal = axis;
                }
            }

            for (int i = 0; i < bVerts.Length; i++)
            {
                Vector2 va = bVerts[i];
                Vector2 vb = bVerts[(i + 1) % bVerts.Length];

                Vector2 edge = vb - va;
                Vector2 axis = new Vector2(-edge.Y, edge.X);
                axis.Normalize();

                ProjectVertices(aVerts, axis, out float minA, out float maxA);
                ProjectVertices(bVerts, axis, out float minB, out float maxB);

                if (minA >= maxB || minB >= maxA)
                {
                    return false;
                }

                float axisDepth = MathF.Min(maxB - minA, maxA - minB);

                if (axisDepth < depth)
                {
                    depth = axisDepth;
                    normal = axis;
                }
            }

            Vector2 direction = bPos - aPos;

            if (Vector2.Dot(direction, normal) < 0f)
            {
                normal = -normal;
            }  

            return true;
        }

        private static void ProjectVertices(Vector2[] vertices, Vector2 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for(int i = 0; i < vertices.Length; i++)
            {
                Vector2 v = vertices[i];
                float proj = Vector2.Dot(v, axis);

                if(proj < min) { min = proj; }
                if(proj > max) { max = proj; }
            }
        }

        // Transforms vertices with
        // the given transform
        private static Vector2[] TransformVertices(Matrix4 transform, Vector2[] verts)
        {
            Vector2[] nVerts = new Vector2[verts.Length];

            for(int i = 0; i < nVerts.Length; i++)
            {
                nVerts[i] = (new Vector4(verts[i].X, verts[i].Y, 0, 1) * transform).Xy;
            }

            return nVerts;
        }

        // The vertices of triangles are
        // set up counter clock wise
        private static Vector2[] boxVerts = 
        {
            (-1, 1), (1, 1),
            (1, -1), (-1, -1)
        };


    public static void CreateVirtualMouse(int identifier, int idleTexture,
        int hoverTexture, int pressTexture, int lockedTexture)
    {
        VirtualMouse vM = new VirtualMouse()
        {
            IdleTexture = idleTexture,

            HoverTexture = hoverTexture,

            PressTexture = pressTexture,

            LockedTexture = lockedTexture,
        };

        EntityHandler.AddComponent<VirtualMouse>(identifier);

        EntityHandler.SetComponent(identifier, vM);
    }


    public static void RemoveVirtualMouse(int identifier)
        => EntityHandler.RemoveComponent<VirtualMouse>(identifier);


    public static void CreateButton(int identifier, int idleTexture,
        int hoverTexture, int pressTexture, int lockedTexture)
    {
        Button b = new Button()
        {
            IdleTexture = idleTexture,

            HoverTexture = hoverTexture,

            PressTexture = pressTexture,

            LockedTexture = lockedTexture,
        };

        EntityHandler.AddComponent<Button>(identifier);

        EntityHandler.SetComponent(identifier, b);
    }


    public static void RemoveButton(int identifier)
        => EntityHandler.RemoveComponent<Button>(identifier);


    private static int pressBind;


    public static void SetPressBind(int bindIndex)
        => pressBind = bindIndex;
}

[Component]
public struct VirtualMouse
{
    public int IdleTexture;

    public int HoverTexture;

    public int PressTexture;

    public int LockedTexture;
}

[Component]
public struct Button
{
    public int IdleTexture;

    public int HoverTexture;

    public int PressTexture;

    public int LockedTexture;

    public bool pressed;
}

[Component]
public struct Slider
{

}

[Component]
public struct InputBox
{

}

[Component]
public struct Toggle
{

}