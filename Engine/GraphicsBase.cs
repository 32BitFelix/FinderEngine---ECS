using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

using OpenTK.Mathematics;

using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GraphicsBase;

public sealed class GraphicsSystem
{
    public static void Init(Vector2i viewportSize)
    {
        // Load a glfw bindings context
        glfwContext = new GLFWBindingsContext();

        // Load the assembly of the
        // opentk graphics namespace
        Assembly graphicsAssembly = Assembly.Load("OpenTK.Graphics");

        // Check if the loading failed
        if(graphicsAssembly == null)
            throw new Exception("User, your pc is literally too bad to run graphics.");

        // Check if library bindings exist
        void LoadBindings(string typeNamespace)
        {
            // Check if the specified library exists
            Type? type = graphicsAssembly.GetType($"OpenTK.Graphics.{typeNamespace}.GL");

            // Throw an error
            // if the library does not exist
            if(type == null)
            {
                throw new Exception($"Couldn't load namespace {typeNamespace}.");
            }

            // Check if the given
            // library can be loaded
            MethodInfo? load = type.GetMethod("LoadBindings");

            // Throw an error
            // if the library was found,
            // but couldn't be loaded
            if(load == null)
            {
                throw new Exception($"OpenTK.Graphics.{typeNamespace}.GL was found, but couldn't be loaded.");
            }
        }


        // Load the OpenGL4 library
        LoadBindings("OpenGL4");


        GL.LoadBindings(glfwContext);

        GL.ClearColor(1, 1, 1, 1);   


        GL.Enable(EnableCap.DepthTest);

        GL.DepthFunc(DepthFunction.Always);


        GL.Enable(EnableCap.Multisample);


        GL.Viewport(0, 0, viewportSize.X, viewportSize.Y);


        ErrorCode ec = GL.GetError();

        if(ec != ErrorCode.NoError)
            throw new Exception("GL Initialisation error!" + ec.ToString());
    }


#pragma warning disable CS8618


    public static GLFWBindingsContext glfwContext;
}