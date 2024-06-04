using AudioBase;
using GraphicsBase;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Monitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;
using Utility.InputHandling;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.Common;
using FinderEngine.Scenes;
using OpenTK.Graphics.OpenGL4;
using System.Reflection;
using FreeTypeSharp;
using Components.ECS;
using Components.SFX.AudioHandling;
using MemorySystems;
using Components.SpacialHierarchy;

namespace Engine;

public unsafe struct FinderEngine
{
    static FinderEngine()
    {
        WindowSize = new (800, 600);

        // Initialise GLFW
        if(!GLFW.Init())
            // Throw an exception if it failed
            throw new Exception("Failed to initialise GLFW.");


        // Set the major version
        GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);

        // Set the minor version
        GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);

        // Hinting that the client's api is OpenGL
        GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);

        // Hinting that the GL to use is the core version
        GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);


        GLFW.WindowHint(WindowHintInt.Samples, 4);


        GLFW.WindowHint(WindowHintBool.Focused, true);


        // Create a window
        window = GLFW.CreateWindow(WindowSize.X, WindowSize.Y, "SAMPLE", null, null);


        GLFW.SetWindowSize(window, WindowSize.X, WindowSize.Y);


        // Check if the making of the window failed
        if(window == (Window*)null)
        {
            GLFW.Terminate();

            throw new Exception("Failed to create window during initialisation.");
        }


        monitor = GLFW.GetPrimaryMonitor();


        GLFW.SetWindowSizeCallback(window, OnResize);


        // Set the window
        // as current context
        GLFW.MakeContextCurrent(window);


        // Set the vertical synchronisation mode
        GLFW.SwapInterval(1); 


        InputHandler.InitInputSystem(window);


        GraphicsSystem.Init(WindowSize);

        AudioSystem.Init();


        // GL debug stuffs
        GL.DebugMessageCallback(OnGFXDebugMessage, IntPtr.Zero);
        GL.Enable(EnableCap.DebugOutput);

        GL.Enable(EnableCap.DebugOutputSynchronous);


        // Set time scale
        TimeScale = 1;


        // Init internal systems

        new EntityHandler();

        new SystemHandler();

        new TransformSystem();

        new Components.UISystem.UIHandler();

        new Components.Shimshek.Renderer();

        new Components.SFX.Tonklang.AudioRenderer();


        scenes = new CNArrayImprov<VirtualScene>(0);

        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            Attribute? sceneAttrib = type.GetCustomAttribute(typeof(SceneAttribute));

            if(sceneAttrib == null)
                continue;

            Attribute? starterAttrib = type.GetCustomAttribute(typeof(StarterAttribute));

            if(starterAttrib == null)
                continue;

            //Start
            delegate* <void> start = (delegate* <void>)null;

                if(type.GetMethod("Start") != null)
                    start =
                        (delegate* <void>)type.GetMethod("Start").MethodHandle.GetFunctionPointer();

            // Update
            delegate* <void> update =
                (delegate* <void>)null;

                if(type.GetMethod("Update") != null)
                    update =
                        (delegate* <void>)type.GetMethod("Update").MethodHandle.GetFunctionPointer();

            // Render
            delegate* <void> render =
                (delegate* <void>)null;

                if(type.GetMethod("Render") != null)
                    render =
                        (delegate* <void>)type.GetMethod("Render").MethodHandle.GetFunctionPointer();
            // Resize
            delegate* <int, int, void> resize =
                (delegate* <int, int, void>)null;

                if(type.GetMethod("Resize") != null)
                    resize =
                        (delegate* <int, int, void>)type.GetMethod("Resize").MethodHandle.GetFunctionPointer();

            // End
            delegate* <void> end =
                (delegate* <void>)null;

                if(type.GetMethod("End") != null)
                    end =
                        (delegate* <void>)type.GetMethod("End").MethodHandle.GetFunctionPointer();

            scenes[scenes.Size] = new VirtualScene(start, update, render, resize, end);
        }

        for(int i = 0; i < scenes.Size; i++)
        {
            scenes[i].StartCall();
        }
    }

    private static CNArrayImprov<VirtualScene> scenes;

    private struct VirtualScene
    {
        public VirtualScene(delegate* <void> i_StartCall,
            delegate* <void> i_UpdateCall,
            delegate* <void> i_RenderCall,
            delegate* <int, int, void> i_ResizeCall,
            delegate* <void> i_EndCall)
        {
            StartCall = i_StartCall;

            UpdateCall = i_UpdateCall;

            RenderCall = i_RenderCall;

            ResizeCall = i_ResizeCall;

            EndCall = i_EndCall;


            DeltaTime =
                (float*)NativeMemory.AllocZeroed(sizeof(float));


            TimeScale =
                (float*)NativeMemory.AllocZeroed(sizeof(float));

            *TimeScale = 1;
        }


        public int ID;


        public readonly delegate* <void> StartCall;

        public readonly delegate* <void> UpdateCall;

        public readonly delegate* <void> RenderCall;

        public readonly delegate* <int, int, void> ResizeCall;

        public readonly delegate* <void> EndCall;


        public readonly float* DeltaTime;


        public readonly float* TimeScale;
    }


    // Current window
    private static Window* window;

    // Current monitor
    private static Monitor* monitor;


    // Sets the title of the window
    public static string WindowTitle
    {
        set => GLFW.SetWindowTitle(window, value);
    }

    // Cache for previous window state.
    // Important for fullscreen stuff
    private static WindowState previousWindowState;

    // Enables to change the format of
    // the window. Fullscreen, minimized,
    // maximized... it's all possible with this
    public static WindowState CurrentWindowState
    {
        set
        {
            if(previousWindowState == WindowState.Fullscreen && value != WindowState.Fullscreen)
            {
                GLFW.SetWindowMonitor(window, null, 0, 0, WindowSize.X, WindowSize.Y, 0);
            }

            switch(value)
            {

                case WindowState.Normal:
                    GLFW.RestoreWindow(window);
                break;

                case WindowState.Minimized:
                    GLFW.IconifyWindow(window);
                break;

                case WindowState.Maximized:
                    GLFW.MaximizeWindow(window);
                break;

                case WindowState.Fullscreen:
                    VideoMode* mode = GLFW.GetVideoMode(monitor);
                    GLFW.SetWindowMonitor(window, monitor, 0, 0, mode->Width, mode->Height, mode->RefreshRate);
                break;
            }

            previousWindowState = value;
        }

        get => previousWindowState;
    }

    // Cache for VSync.
    // Can't get the swapinterval
    // for the life of me
    private static VSyncMode previousVSyncMode;

    // Returns or sets
    // the vsyncmode of
    // the window
    public static VSyncMode CurrentVSyncMode
    {
        get => previousVSyncMode;

        set
        {
            switch(value)
            {

                case VSyncMode.Off:
                    GLFW.SwapInterval(0);
                break;

                case VSyncMode.On:
                    GLFW.SwapInterval(1);
                break;
            }

            previousVSyncMode = value;
        }
    }

    // Returns or sets the refreshrate
    // of thw window
    public static int RefreshRate
    {
        get => GLFW.GetVideoMode(monitor)->RefreshRate;

        set => GLFW.WindowHint(WindowHintInt.RefreshRate, value);
    }

    // Returns or sets the current cursormode.
    public static CursorModeValue CurrentCursorState
    {
        get => GLFW.GetInputMode(window, CursorStateAttribute.Cursor);

        set => GLFW.SetInputMode(window, CursorStateAttribute.Cursor, value);
    }

    // Returns true if the window is
    // focused, false if it isn't
    public static bool IsWindowFocused
    {
        get => GLFW.GetWindowAttrib(window, WindowAttributeGetBool.Focused);
    }

    // Ends the application
    // upon call
    public static void EndEngine() => GLFW.SetWindowShouldClose(window, true);

    // The size of the window in pixels
    public static Vector2i WindowSize {get; private set;}

    // holds the time that passed
    // between the last and current frame
    public static float DeltaTime;

    // The multiplier for
    // in engine time.
    // 1: Normal time behaviour
    // 0.5f: Time passes half as fast
    // 2: Time passes as double as fast
    public static float TimeScale;


    public static void Start()
    {
        while(!GLFW.WindowShouldClose(window))
        {
            GLFW.PollEvents();

            Update();

            Render();   

            InputHandler.ResetDirect();
        }

        End();
    }

    private static void Update()
    {
        DeltaTime = (float)GLFW.GetTime() * TimeScale;

        GLFW.SetTime(0);

        SystemHandler.__RUNSYSTEMS();        

        for(int i = 0; i < scenes.Size; i++)
        {
            if(scenes[i].UpdateCall == (delegate* <void>)null)
                continue;

            *scenes[i].DeltaTime = DeltaTime * *scenes[i].TimeScale;

            scenes[i].UpdateCall();
        }
    }

    private static void Render()
    {
        Components.Shimshek.Renderer.__RENDER();

        Components.SFX.Tonklang.AudioRenderer.__RENDER();

        for(int i = 0; i < scenes.Size; i++)
        {
            if(scenes[i].RenderCall == (delegate* <void>)null)
                continue;

            scenes[i].RenderCall();
        }

        GLFW.SwapBuffers(window);   
    }

    private static void OnResize(Window* window, int width, int height)
    {
        WindowSize = (width, height);

        GL.Viewport(0, 0, width, height);

        Components.Shimshek.Renderer.__RESIZE(width, height);

        for(int i = 0; i < scenes.Size; i++)
        {
            if(scenes[i].ResizeCall == (delegate* <int, int, void>)null)
                continue;

            scenes[i].ResizeCall(width, height);
        }
    }

    // Debug callback for OpenGL
    private static void OnGFXDebugMessage(
        DebugSource source,     // Source of the debugging message.
        DebugType type,         // Type of the debugging message.
        int id,                 // ID associated with the message.
        DebugSeverity severity, // Severity of the message.
        int length,             // Length of the string in pMessage.
        IntPtr pMessage,        // Pointer to message string.
        IntPtr pUserParam)  
    {
        // In order to access the string pointed to by pMessage, you can use Marshal
        // class to copy its contents to a C# string without unsafe code. You can
        // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
        string message = Marshal.PtrToStringAnsi(pMessage, length);

        // The rest of the function is up to you to implement, however a debug output
        // is always useful.
        Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);
    }

    private static void End()
    {
        for(int i = 0; i < scenes.Size; i++)
        {
            if(scenes[i].EndCall == (delegate* <void>)null)
                continue;

            scenes[i].EndCall();
        }


    }
}