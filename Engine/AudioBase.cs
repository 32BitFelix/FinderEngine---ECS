using OpenTK.Audio.OpenAL;

namespace AudioBase;

public unsafe sealed class AudioSystem
{
    // Constructor
    public static void Init()
    {

        void TryAssignDevice(string[] names)
        {
            foreach(string s in names)
            {
                device = ALC.OpenDevice(s);

                if(device != ALDevice.Null)
                    return;
            }
        }


        string[] deviceNames = {"OpenAL Soft", "Generic Software", "Generic Hardware"};

        TryAssignDevice(deviceNames);
        

        context = ALC.CreateContext(device, (int*)null);

        if(!ALC.MakeContextCurrent(context))
            return;


        //Logging.WriteLine("EFFECTS SUPPORT IS: " + ALC.IsExtensionPresent(device, "ALC_EXT_EFX"));
    
        //Logging.WriteLine("ENUMERATION SUPPORT IS: " + ALC.IsExtensionPresent(device, "ALC_ENUMERATION_EXT")); 

        //Logging.WriteLine("ENUMERATE ALL SUPPORT IS: " + ALC.IsExtensionPresent(device, "ALC_ENUMERATE_ALL")); 
        

        // Set the listener's position
        AL.Listener(ALListener3f.Position, 0, 0, 0);

        // Set the listener's velocity
        AL.Listener(ALListener3f.Velocity, 0, 0, 0);

        // Set the listener's orientation
        AL.Listener(ALListenerfv.Orientation, new float[]{0, 0, 1, 0, 1, 0});

        // Set the listener's distance model.
        // In this case it's LinearDistanceClamped,
        // which pretty much mutes the source
        // as soon as it's outside the hearable distance
        AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);
    }

    // The current audio context
    public static ALContext context;

    // The current audio device
    public static ALDevice device;
}