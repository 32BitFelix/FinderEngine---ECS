using MemorySystems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Utility.InputHandling;

// IF YOU STRUGGLE WITH USING THIS, MAKE SURE TO LOOK AT THESE EXAMPLES BELOW.
// MAYBE THEY COULD HELP YOU UNDERSTAND IT:

/* Create input
    // forward = 0
    // back = 1
    // left = 2
    // right = 3

    // shoot = 4

    InputSystem.AddInput((int)Keys.W, InputPressType.continous, 0);

    InputSystem.AddInput((int)Keys.S, InputPressType.continous, 1);

    InputSystem.AddInput((int)Keys.A, InputPressType.continous, 2);

    InputSystem.AddInput((int)Keys.D, InputPressType.continous, 3);   


    InputSystem.AddInput((int)MouseButton.Button1, InputPressType.continous, 4);     
*/

/* Change bound key/mousebutton on input
    InputSystem.SetInputIndex(4, (int)MouseButton.Button2);

    or

    InputSystem.SetInputIndex(4, (int)Keys.Space);  
*/

/* Get bound key/mousebutton on input
    int i = InputSystem.GetInutIndex(2); // returns Keys.A as int
*/

/* Read Input
    bool b = InputSystem.GetInput(0);

    if(b) Console.WriteLine("Walking forward!");
*/

/* Change pressType on input
    InputSystem.SetPressType(0, InputPressType.direct); // input 0 set to direct read
*/

/* Get pressType on input
    InputPressType pressType2 = InputSystem.GetPressType(2); // returns InputPressType.continous
*/

internal unsafe class InputHandler
{
    /// <summary>
    ///     Manual initalisation of the
    ///     input system
    /// </summary>
    /// <param name="mainWindow"></param>
    public static void InitInputSystem(Window* mainWindow)
    {
        // Set the keyboard key callback
        GLFW.SetKeyCallback(mainWindow, KeyCallback);

        // Set the mouse button callback
        GLFW.SetMouseButtonCallback(mainWindow, MouseButtonCallback);

        // Set the cursor position callback
        //GLFW.SetCursorPosCallback(mainWindow, CursorPosCallback);

        // Set the mousewheel callback
        GLFW.SetScrollCallback(mainWindow, MouseWheelCallback);


        GLFW.GetCursorPos(mainWindow, out double x, out double y);

        lastCursorPos = new Vector2((float)x, (float)y);


        window = mainWindow;


        inputIndexes = new CNArrayImprov<int>(0);

        pressTypes = new CNArrayImprov<int>(0);

        areInput = new CNArrayImprov<bool>(0);


        CursorSensitivity = 0.05f;
    }

    private static Window* window;

    /// <summary>
    ///     Disposes of the inputSystem,
    ///     as it has umanaged elements.
    ///     Please call upon closing the
    ///     application
    /// </summary>
    public static void FinalizeInputSystem()
    {
        inputIndexes.Dispose();

        pressTypes.Dispose();

        areInput.Dispose();
    }

    // The array to hold the inputIndexes
    private static CNArrayImprov<int> inputIndexes;

    /// <summary>
    ///     Enables you to change inputIndex
    ///     of an input with the given index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="inputIndex"></param>
    public static void SetInputIndex(int index, int inputIndex)
    {
        if(index > inputIndexes.Size) throw new IndexOutOfRangeException();

        inputIndexes[index] = inputIndex;
    }

    /// <summary>
    ///     Enables you to get the inputIndex
    ///     of an input with the given index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static int GetInputIndex(int index)
    {
        return inputIndexes[index];
    }

    // The array to hold the pressTypes
    private static CNArrayImprov<int> pressTypes;

    /// <summary>
    ///     Enables you to change the presstype
    ///     of an input with the given index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="pressType"></param>
    public static void SetPressType(int index, InputPressType pressType)
    {
        if(index > pressTypes.Size) throw new IndexOutOfRangeException();

        pressTypes[index] = (int)pressType; 
    }

    /// <summary>
    ///     Enables you to get the pressType
    ///     of an input with the given index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static InputPressType GetPressType(int index)
    {
        return (InputPressType)pressTypes[index];
    }

    // The array to hold the input indicators
    private static CNArrayImprov<bool> areInput;

    // The delta of the mousewheel in the y dimension
    public static float MouseWheelDelta { get; private set; }

    // The delta of the mouse position
    public static Vector2 CursorPositionDelta
    {
        get
        {
            GLFW.GetCursorPos(window, out double x, out double y);

            Vector2 cursorPos = new Vector2((float)x, -(float)y);


            Vector2 delta = cursorPos - lastCursorPos;


            lastCursorPos = cursorPos;

            return delta * CursorSensitivity;
        }
    }

    private static Vector2 lastCursorPos;

    // The sensitivity of the cursor
    public static float CursorSensitivity { get; set; }

    /// <summary>
    ///     Returns the current position
    ///     of the cursor
    /// </summary>
    public static Vector2 CursorPos 
    {
        get
        {
            GLFW.GetCursorPos(window, out double x, out double y);

            return new Vector2((float)x, (float)y);
        }
    } 

    /// <summary>
    ///     Adds an input to the input handler. Both mousebutton and
    ///     key input of choice can be handled (Don't forget to convert
    ///     the enums to ints)
    /// </summary>
    /// <param name="inputIndex"></param>
    /// <param name="pressType"></param>
    /// <param name="index"></param>
    public static void AddInput(int inputIndex, InputPressType pressType, int index)
    {
        inputIndexes[index] = inputIndex;

        pressTypes[index] = (int)pressType;

        areInput[index] = false;
    }

    /// <summary>
    ///     Returns a boolean representing if the input
    ///     with the given index has been identified or not.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static bool GetInput(int index)
    {
        return areInput[index];
    }

    private static void MouseWheelCallback(Window* window, double offsetX, double offsetY)
    {
        MouseWheelDelta = (float)offsetY;
    }

    private static void MouseButtonCallback(Window* window, MouseButton button, InputAction action, KeyModifiers mods)
    {
        switch(action)
        {
            case InputAction.Release:
                for(int i = 0; i < areInput.Size; i++)
                {
                    if((int)button != inputIndexes[i])
                        continue;

                    switch(pressTypes[i])
                    {

                        case (int)InputPressType.continous:
                            areInput[i] = false;
                        return;

                        case (int)InputPressType.direct:

                        return;

                        case (int)InputPressType.toggle:

                        return;

                        case (int)InputPressType.directRelease:
                            areInput[i] = true;
                        return;
                    }
                }
            return;

            case InputAction.Press:
                for(int i = 0; i < areInput.Size; i++)
                {   
                    if((int)button != inputIndexes[i])
                        continue;

                    switch(pressTypes[i])
                    {

                        case (int)InputPressType.continous:
                            if(!areInput[i])
                                areInput[i] = true;
                        return;

                        case (int)InputPressType.direct:
                            if(!areInput[i])
                                areInput[i] = true;    
                        return;

                        case (int)InputPressType.toggle:
                            areInput[i] = !areInput[i];
                        return;

                        case (int)InputPressType.directRelease:

                        return;
                    }
                }
            return;

            /*case InputAction.Repeat:
                // Don't know what to do with this. Keeping it
                // if demand needs it.
            return;*/
        }
    }

    private static void KeyCallback(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mods)
    {
        switch(action)
        {
            case InputAction.Release:
                for(int i = 0; i < areInput.Size; i++)
                {
                    if((int)key != inputIndexes[i])
                        continue;

                    switch(pressTypes[i])
                    {

                        case (int)InputPressType.continous:
                            areInput[i] = false;
                        return;

                        case (int)InputPressType.direct:

                        return;

                        case (int)InputPressType.toggle:

                        return;

                        case (int)InputPressType.directRelease:
                            areInput[i] = true;
                        return;
                    }
                }
            return;

            case InputAction.Press:
                for(int i = 0; i < areInput.Size; i++)
                {   
                    if((int)key != inputIndexes[i])
                        continue;

                    switch(pressTypes[i])
                    {

                        case (int)InputPressType.continous:
                            if(!areInput[i])
                                areInput[i] = true;
                        return;

                        case (int)InputPressType.direct:
                            if(!areInput[i])
                                areInput[i] = true;    
                        return;

                        case (int)InputPressType.toggle:
                            areInput[i] = !areInput[i];
                        return;

                        case (int)InputPressType.directRelease:

                        return;
                    }
                }
            return;

            /*case InputAction.Repeat:
                // Don't know what to do with this. Keeping it
                // if demand needs it.
            return;*/
        }
    }

    /// <summary>
    ///     Resets the input of direct inputs.
    ///     Only use if you intend on using the
    ///     direct input type.
    ///     (Please call at the end of update loop
    ///     for functioning direct input)
    /// </summary>
    public static void ResetDirect()
    {
        for(int i = 0; i < areInput.Size; i++)
        {
            if(pressTypes[i] == (int)InputPressType.direct ||
                pressTypes[i] == (int)InputPressType.directRelease)

                areInput[i] = false;
        }

        MouseWheelDelta = 0;
    }   
}

/// <summary>
///     The types of ways input
///     can and will be handled
/// </summary>
public enum InputPressType
{

    /// <summary>
    ///     Represents a continous detection of the
    ///     input regardless of if the button was
    ///     held in the previous frame
    /// </summary>
    continous = 0,

    /// <summary>
    ///     Represents a detection that signals an
    ///     input only once and never again if the
    ///     bound key was held in the previous frame
    ///     
    /// </summary>
    direct = 1,

    /// <summary>
    ///     Represents a detection that inverts
    ///     it's signal if the bound key has been
    ///     pressed
    /// </summary>
    toggle = 2,


    directRelease = 3,
}