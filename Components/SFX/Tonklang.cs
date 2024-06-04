using System.Data.Common;
using System.Runtime.InteropServices;
using Components.ECS;
using Components.Shimshek;
using Components.SpacialHierarchy;
using MemorySystems;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Components.SFX.Tonklang;

[InterestedIn(EntityInterest.Remove)]
public class AudioRenderer
{

    static AudioRenderer()
    {
        EntityHandler.AddComponentType<AudioObject>();


        volumeGroup = new CNArray<float>(1);

        pitchGroup = new CNArray<float>(1);


        volumeGroup[0] = 1;

        pitchGroup[0] = 1;
    }


    private static CNArray<float> volumeGroup;


    public static void CreateGroupVolume(int groupID, float volume)
        => volumeGroup[groupID] = volume;


    public static void SetGroupVolume(int groupID, float volume)
        => volumeGroup[groupID] = volume;


    public static float GetGroupVolume(int groupID)
        => volumeGroup[groupID];


    private static CNArray<float> pitchGroup;


    public static void CreateGroupPitch(int groupID, float pitch)
        => pitchGroup[groupID] = pitch;


    public static void SetGroupPitch(int groupID, float pitch)
        => pitchGroup[groupID] = pitch;


    public static float GetGroupPitch(int groupID)
        => pitchGroup[groupID];


    public static void CreateAudioObject(int identifier, int buffer = 0,
        float volume = 1, float pitch = 1,
            bool looping = false, float referenceDistance = 0,
                float maxDistance = 20, float rolloffFactor = 1)
    {
        // Add component to entity
        EntityHandler.AddComponent<AudioObject>(identifier);

        // Get the component
        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);


        // Set the pitch
        audioObject.pitch = pitch;


        // Set the volume
        audioObject.volume = volume;


        // Set the group belonging
        // of the object
        audioObject.groupBelonging = 0;


        // Set looping
        audioObject.looping = looping;


        // default play
        audioObject.play = false;


        // default pause
        audioObject.pause = false;


        // default stop
        audioObject.stop = false;


        // default remove
        audioObject.remove = false;


        // Set maximum distance the
        // source can be heard in
        // full volume
        audioObject.referenceDistance = referenceDistance;


        // Set maximum distance the
        // source can be heard in
        // lowest volume
        audioObject.maxDistance = maxDistance;


        // Set how much the volume
        // falls off throughout the
        // distance
        audioObject.rolloffFactor = rolloffFactor;


        // Set the buffer
        audioObject.soundBuffer = buffer;


        // Save changes
        EntityHandler.SetComponent(identifier, audioObject);
    }


    private static void CreateAudioObjectBackend(int identifier)
    {
        // Add component to entity
        EntityHandler.AddComponent<AudioObject>(identifier);

        // Get the component
        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);


        // Create source
        audioObject.source = AL.GenSource();


        // Set the pitch
        AL.Source(audioObject.source, ALSourcef.Pitch, audioObject.pitch);


        // Set the volume
        AL.Source(audioObject.source, ALSourcef.Gain, audioObject.volume);

        AL.Source(audioObject.source, ALSourcef.MinGain, 0);

        AL.Source(audioObject.source, ALSourcef.MaxGain, 1);


        // Set the position of the source
        AL.Source(audioObject.source, ALSource3f.Position, 0, 0, 0);

        // Set the velocity of the source
        AL.Source(audioObject.source, ALSource3f.Velocity, 0, 0, 0);


        // Set if the audio should loop
        AL.Source(audioObject.source, ALSourceb.Looping, audioObject.looping);


        // Set the buffer of the audio
        AL.Source(audioObject.source, ALSourcei.Buffer, audioObject.soundBuffer);


        // Set the distance of how far the listener
        // can hear the sound in full volume before
        // dropping down
        AL.Source(audioObject.source, ALSourcef.ReferenceDistance, audioObject.referenceDistance);

        // Set the maximum distance the listener can hear
        // the sound from
        AL.Source(audioObject.source, ALSourcef.MaxDistance, audioObject.maxDistance);

        // Set how much the volume drops
        // in volume by distance
        AL.Source(audioObject.source, ALSourcef.RolloffFactor, audioObject.rolloffFactor);


        // Save changes
        EntityHandler.SetComponent(identifier, audioObject);
    }


    public static void ChangeBuffer(int identifier, int buffer)
    {
        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);

        audioObject.soundBuffer = buffer;

        EntityHandler.SetComponent(identifier, audioObject);
    }


    public static void RemoveAudioObject(int identifier)
        => OnRemoveEntity(identifier);


    public static void __RENDER()
    {
        Camera[] cameras = EntityHandler.GetColumn<Camera>();

        for(int c = 0; c < cameras.Length; c++)
        {
            AudioObject[] audioObjects = EntityHandler.GetColumn<AudioObject>();

            for(int i = 0; i < audioObjects.Length; i++)
            {
                // Create
                if(audioObjects[i].source == 0)
                {
                    if(audioObjects[i].soundBuffer == 0)
                        continue;
                    else
                        CreateAudioObjectBackend(EntityHandler.GetIDFromCCI<AudioObject>(i));
                }

                // Delete
                if(audioObjects[i].remove)
                {
                    AL.DeleteSource(audioObjects[i].source);

                    EntityHandler.SetComponent<AudioObject>(EntityHandler.GetIDFromCCI<AudioObject>(i), default);

                    return;
                }

                // Play
                switch(AL.GetSource(audioObjects[i].source, ALGetSourcei.SourceState))
                {
                    // Initial
                    case 4113:
                        if(!audioObjects[i].play)
                            break;

                        AL.SourcePlay(audioObjects[i].source);

                        audioObjects[i].play = false;
                    break;
                    // Playing
                    case 4114:
                        if(!audioObjects[i].stop)
                            break;

                        AL.SourceStop(audioObjects[i].source);

                        audioObjects[i].stop = false;
                    break;
                    // Paused
                    case 4115:
                        if(!audioObjects[i].play)
                            break;

                        AL.SourcePlay(audioObjects[i].source);

                        audioObjects[i].play = false;
                    break;
                    // Stopped
                    case 4116:
                        if(!audioObjects[i].play)
                            break;

                        AL.SourcePlay(audioObjects[i].source);

                        audioObjects[i].play = false;
                    break;
                }

                // Volume
                float desiredVolume = audioObjects[i].volume * volumeGroup[audioObjects[i].groupBelonging];

                if(AL.GetSource(audioObjects[i].source, ALSourcef.Gain) != desiredVolume)
                    AL.Source(audioObjects[i].source, ALSourcef.Gain, desiredVolume);


                // Pitch
                float desiredPitch = audioObjects[i].pitch * pitchGroup[audioObjects[i].groupBelonging] *
                    Engine.FinderEngine.TimeScale;

                if(AL.GetSource(audioObjects[i].source, ALSourcef.Pitch) != desiredPitch)
                    AL.Source(audioObjects[i].source, ALSourcef.Pitch, desiredPitch);

                // Loop
                if(AL.GetSource(audioObjects[i].source, ALSourceb.Looping) != audioObjects[i].looping)
                    AL.Source(audioObjects[i].source, ALSourceb.Looping, audioObjects[i].looping);


                // Position
                int cameraIndex = EntityHandler.GetIDFromCCI<Camera>(c);

                Vector4 differencePos =
                    -new Vector4(TransformSystem.GetTranslation(EntityHandler.GetIDFromCCI<AudioObject>(i)), 1) *
                        Renderer.MakeViewMatrix(cameraIndex);

                AL.Source(audioObjects[i].source, ALSource3f.Position, differencePos.X, differencePos.Y, differencePos.Z);      
            
                // Reference distance
                if(AL.GetSource(audioObjects[i].source, ALSourcef.ReferenceDistance) != audioObjects[i].referenceDistance)
                    AL.Source(audioObjects[i].source, ALSourcef.ReferenceDistance, audioObjects[i].referenceDistance);

                // Max distance
                if(AL.GetSource(audioObjects[i].source, ALSourcef.MaxDistance) != audioObjects[i].maxDistance)
                    AL.Source(audioObjects[i].source, ALSourcef.MaxDistance, audioObjects[i].maxDistance);

                // Rolloff factor
                if(AL.GetSource(audioObjects[i].source, ALSourcef.RolloffFactor) != audioObjects[i].rolloffFactor)
                    AL.Source(audioObjects[i].source, ALSourcef.RolloffFactor, audioObjects[i].rolloffFactor);
            }
        }
    }


    public static void PlaySound(int identifier)
    {
        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);

        audioObject.play = true;

        audioObject.stop = !audioObject.play;

        audioObject.pause = !audioObject.play;

        EntityHandler.SetComponent(identifier, audioObject);
    }


    public static void StopSound(int identifier)
    {
        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);

        audioObject.stop = true;

        audioObject.play = !audioObject.stop;

        audioObject.pause = !audioObject.stop;

        EntityHandler.SetComponent(identifier, audioObject);
    }


    public static void PauseSound(int identifier)
    {
        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);

        audioObject.pause = true;

        audioObject.play = !audioObject.pause;
        
        audioObject.stop = !audioObject.pause;

        EntityHandler.SetComponent(identifier, audioObject);
    }


    public static void OnRemoveEntity(int identifier)
    {
        if(!EntityHandler.HasComponent<AudioObject>(identifier))
            return;

        AudioObject audioObject = EntityHandler.GetComponent<AudioObject>(identifier);

        audioObject.remove = true;

        EntityHandler.SetComponent(identifier, audioObject);
    }
}

// TODO: Add bitflag that indicates
// what has been changed in this object
// so that the system doesn't have to
// update everything

// TODO: Replace bools with bitflag
// to optimize data usage

[Component]
public struct AudioObject
{
    public int groupBelonging;

    public float pitch;

    public float volume;

    public bool looping;

    public bool play;

    public bool stop;

    public bool pause;

    public bool remove;

    public int soundBuffer;

    public int source;

    public float referenceDistance;
    
    public float maxDistance;
    
    public float rolloffFactor; 
}