using Components.ECS;
using Components.SpacialHierarchy;
using Components.ShimshekHelper;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Components.Shimshek;

public unsafe class Renderer
{
    static Renderer()
    {
        // Initialise Components
        EntityHandler.AddComponentType<Camera>();

        EntityHandler.AddComponentType<Sprite>();


        // Initialise global shader
        globalShader = ShaderHandler.CreateShader("Sprite2DShader.vert", "Sprite2DShader.frag");

        ShaderHandler.UseShader(globalShader);

        ShaderHandler.SetInt("texture0", 0, globalShader);

        ShaderHandler.SetInt("depthTex", 1, globalShader);

        ShaderHandler.SetInt("colorTex", 2, globalShader);

        ShaderHandler.SetVec2("viewportSize", Engine.FinderEngine.WindowSize, globalShader);


        // Initialise global element-buffer
        globalElementBuffer = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, globalElementBuffer);

        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(uint),
            Indices, BufferUsageHint.DynamicDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


        // Initialise Framebuffer
        frameBuffer = GL.GenFramebuffer();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);


            // Initialise depth-texture-buffer
            depthTextureBuffer = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, depthTextureBuffer);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                Engine.FinderEngine.WindowSize.X, Engine.FinderEngine.WindowSize.Y,
                    0, PixelFormat.DepthComponent, PixelType.Float, 0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);

            GL.BindTexture(TextureTarget.Texture2D, 0);


            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, depthTextureBuffer, 0);


            // Initialise color-texture-buffer
            colorTextureBuffer = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, colorTextureBuffer);


            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                Engine.FinderEngine.WindowSize.X, Engine.FinderEngine.WindowSize.Y,
                    0, PixelFormat.Rgba, PixelType.UnsignedByte, 0);


            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);


            GL.BindTexture(TextureTarget.Texture2D, 0);


            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, colorTextureBuffer, 0);


        Console.WriteLine(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer));


        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    // A buffer to hold the depth values
    // of the current frame
    private static int depthTextureBuffer,
        // A buffer to hold the color values
        // of the current frame
        colorTextureBuffer;

    // The framebuffer used to sample
    // in shaders
    private static int frameBuffer;

    // The shader used by all sprites
    private static int globalShader;

    // The element-buffer used by all sprites
    private static int globalElementBuffer;

    // Creates the view matrix of the camera
    public static Matrix4 MakeViewMatrix(int camID)
    {
        // Get the positon of the camera
        Vector3 position = TransformSystem.GetTranslation(camID);

        // Get the up vector of the camera
        Vector3 up = TransformSystem.GetUp(camID);

        // Get the front vector of the camera
        Vector3 front = TransformSystem.GetFront(camID);

        // Finally, use the gotten values for the lookAt function
        return Matrix4.LookAt(position, position + front, up);
    }

    // Creates the projection matrix of the camera
    public static Matrix4 MakeProjectionMatrix(int camID)
    {
        Camera camera = EntityHandler.GetComponent<Camera>(camID);

        // Returns either a perspective-
        // or projection matrix depending
        // on the projection mode
        return camera.IsOrtho ? Matrix4.CreateOrthographic(camera.ProjectionSize * camera.AspectRatio,
                                                            camera.ProjectionSize,
                                                            camera.NearClip,
                                                            camera.FarClip)
            : Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FOV),
                                                    camera.AspectRatio,
                                                    camera.NearClip,
                                                    camera.FarClip);
    }

    // Begins with the render loop
    // when called
    public static void __RENDER()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);  

        Span<Camera> cameras = EntityHandler.GetColumn<Camera>();      

        for(int i = 0; i < cameras.Length; i++)
        {   
            if(cameras[i].FOV == 0f || cameras[i].NearClip <= 0 || cameras[i].FarClip <= 0 ||
                (cameras[i].NearClip >= cameras[i].FarClip)) continue;

            int index = EntityHandler.GetIDFromCCI<Camera>(i);

            Matrix4 viewM = MakeViewMatrix(index),
                projM = MakeProjectionMatrix(index);

            ShaderHandler.SetMat4("aProjection", projM, globalShader);

            ShaderHandler.SetMat4("aView", viewM, globalShader);     

            renderInstanced(viewM, projM, i);
        }
    }

    // Hidden backend of
    // the rendering
    private static void renderInstanced(Matrix4 view, Matrix4 proj, int camID)
    {
        // The container of currently
        // existing sprites
        Sprite[] sprites = EntityHandler.GetColumn<Sprite>();


        // Stackallocate Vector4* for
        // BufferSubData operation
        Vector4* ptr = stackalloc Vector4[1];    


        // Go through all sprites
        for(int i = 0; i < sprites.Length; i++)
        {

            // Check if vertex buffer
            // of the sprite exists
            if(!GL.IsBuffer(sprites[i].vertexBuffer))
            {
                // Check if the sprite has a texture
                if(!GL.IsTexture(sprites[i].Texture))
                    // If not, skip
                    continue;
                else
                    // If is, create the
                    // necessary opengl
                    // stuff of the sprite
                    CreateSpriteBackend(EntityHandler.GetIDFromCCI<Sprite>(i));
            }


            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frameBuffer);

            Vector2i size = Engine.FinderEngine.WindowSize;

            GL.BlitFramebuffer(0, 0, size.X, size.Y,
                0, 0, size.X, size.Y,
                    ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit,
                        BlitFramebufferFilter.Nearest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);


            // Get the entity Id of
            // the component's entity
            int index = EntityHandler.GetIDFromCCI<Sprite>(i);


            // See if the transform of
            // the entity is active.
            // If not, skip
            if(!TransformSystem.GetEnable(index))
                continue;


            // See if the sprite is
            // inside the camera's frustum.
            // If not, skip
            if(!IsSpriteInCamera(index, view, proj))
                continue;


            // Set the value of the allocated memory
            *ptr = sprites[i].color;

                // Bind to sprite's array buffer
                GL.BindBuffer(BufferTarget.ArrayBuffer, sprites[i].vertexBuffer);

                // Set the color value for all
                // vertices of the sprite
                for(int v = 0; v < 4; v++)
                {
                    int offset = (9 * sizeof(float) * v) + sizeof(float) * 5;

                    GL.BufferSubData(BufferTarget.ArrayBuffer, offset, sizeof(float) * 4, (nint)ptr);
                }
                
                // Unbind from sprite's array buffer
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            // Get the model matrix of the sprite's transform
            Matrix4 model = TransformSystem.GetModelMatrix(index);

            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            GL.BindVertexArray(sprites[i].vertexArray);

                ShaderHandler.SetMat4("aModel", model, globalShader);

                TextureHandler.UseTexture(TextureUnit.Texture0, sprites[i].Texture);
                    
                TextureHandler.UseTexture(TextureUnit.Texture1, depthTextureBuffer);

                TextureHandler.UseTexture(TextureUnit.Texture2, colorTextureBuffer);

            GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
        }
    }

        private static void CreateSpriteBackend(int id)
        {
            EntityHandler.AddComponent<Sprite>(id);

            Sprite sprite = EntityHandler.GetComponent<Sprite>(id);


            // Create vertex array
            sprite.vertexArray = GL.GenVertexArray();

            // Bind to vertex array
            GL.BindVertexArray(sprite.vertexArray);

            // Create vertex buffer
            sprite.vertexBuffer = GL.GenBuffer();

            // Bind to vertex buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, sprite.vertexBuffer);

            // Allocate memory to vertex buffer
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Length * sizeof(float),
                Vertices, BufferUsageHint.DynamicDraw);

            
            // Bind element buffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, globalElementBuffer);


                // Define aPosition field of shader
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);

                // Enable the field
                GL.EnableVertexAttribArray(0);


                // Define aTexCoord field of shader
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));

                // Enable the field
                GL.EnableVertexAttribArray(1);


                // Define aColor field of shader
                GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));

                // Enable the field
                GL.EnableVertexAttribArray(2);


            // Unbind from vertex buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Unbind from vertex array
            GL.BindVertexArray(0);

            // Unbind from elementbuffer
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);


            // Save changes
            EntityHandler.SetComponent(id, sprite);
        }

    public static void CreateSprite(int id, int textureID)
    {
        EntityHandler.AddComponent<Sprite>(id);

        Sprite sprite = EntityHandler.GetComponent<Sprite>(id);


        sprite.Texture = textureID;


        sprite.color = (1, 1, 1, 1);


        EntityHandler.SetComponent(id, sprite);
    }

    public static void RemoveSprite(int id)
    {
        Sprite sprite = EntityHandler.GetComponent<Sprite>(id);

        fixed(int* ptr = stackalloc int[2])
        {
            ptr[0] = sprite.vertexBuffer;
            ptr[1] = sprite.vertexArray;

            GL.DeleteBuffers(2, ptr);
        }

        sprite.vertexBuffer = 0;
        sprite.vertexArray = 0;

        EntityHandler.SetComponent(id, sprite);
    }

    public static void CreateCamera(int id, bool isOrtho = true,
        float fov = 90, float nearClip = 0.1f,
            float farClip = 100, float projectionSize = 20)
    {
        EntityHandler.AddComponent<Camera>(id);

        Camera cam = new Camera()
        {
            IsOrtho = isOrtho,

            FOV = fov,

            NearClip = nearClip, FarClip = farClip,

            ProjectionSize = projectionSize,

            AspectRatio = Engine.FinderEngine.WindowSize.X /
                (float)Engine.FinderEngine.WindowSize.Y,
        };

        EntityHandler.SetComponent(id, cam);
    }

    // Resizes the aspect ratios of
    // the cameras upon call
    public static void __RESIZE(int x, int y)
    {
        GL.BindTexture(TextureTarget.Texture2D, depthTextureBuffer);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
            x, y,
                0, PixelFormat.DepthComponent, PixelType.Float, 0);


        GL.BindTexture(TextureTarget.Texture2D, colorTextureBuffer);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb,
            x, y,
                0, PixelFormat.Rgb, PixelType.UnsignedByte, 0);


        GL.BindTexture(TextureTarget.Texture2D, 0);


        ShaderHandler.SetVec2("viewportSize", Engine.FinderEngine.WindowSize, globalShader);


        Span<Camera> cameras = EntityHandler.GetColumn<Camera>();

        for(int i = 0; i < cameras.Length; i++)
        {
            int camID = EntityHandler.GetIDFromCCI<Camera>(i);

            cameras[i].AspectRatio = x / (float)y;

            EntityHandler.SetComponent(camID, cameras[i]);
        }
    }

    // The vertices of
    // the 2D graphic
    private static readonly float[] Vertices = 
    {
        // v pos        t pos      r  g  b  a
        -1, -1, 0,      0, 0,      1, 1, 1, 1,
         1, -1, 0,      1, 0,      1, 1, 1, 1,
         1,  1, 0,      1, 1,      1, 1, 1, 1,
        -1,  1, 0,      0, 1,      1, 1, 1, 1,
    };

    // The indices of
    // the 2D graphic
    private static readonly uint[] Indices = 
    {
        0, 1, 2,   
        0, 2, 3,
    };

        // Checks the collision
        // between two polygons
        private static bool IsSpriteInCamera(int aID, Matrix4 view, Matrix4 proj)
        {
            Vector2 aPos = TransformSystem.GetTranslation2D(aID);

            Vector2[] aVerts =
                TransformVertices(TransformSystem.GetModelMatrix(aID), boxVerts);


            Matrix4 inverseViewProj = (view * proj).Inverted();
            

            Vector2 bPos = inverseViewProj.ExtractTranslation().Xy;

            Vector2[] bVerts =
                TransformVertices(inverseViewProj, boxVerts);


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
}

[Component]
public struct Camera
{
    public bool IsOrtho;

    public float FOV;

    public float NearClip, FarClip;

    public float ProjectionSize;

    public float AspectRatio;
}

[Component]
public struct Sprite
{
    public int Texture;

    public int vertexBuffer;

    public int vertexArray;

    public Vector4 color;
}