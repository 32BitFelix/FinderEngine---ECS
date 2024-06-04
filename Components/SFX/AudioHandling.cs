

using FinderEngine.Scenes;
using MemorySystems;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Transforming;

namespace Components.SFX.AudioHandling;

public unsafe struct AudioScene
{
    public AudioScene(int i_listener,
        TransformSystem* i_transformSystem,
            float* i_sceneTimeScale)
    {
        listener = i_listener;

        transformSystem = i_transformSystem;

        sceneTimeScale = i_sceneTimeScale;


        sources = new CNArray<int>(0);

        volumeGroupBelonging = new CNArray<int>(0);

        volumeGroups = new CNArray<float>(0);

        volumes = new CNArray<float>(0);

        pitches = new CNArray<float>(0);


        volumeGroups[0] = 1;
    }

    private int listener;

    // Creates a transformation matrix
    // for the listener
    private Matrix4 CreateListenerMatrix()
    {
        Vector3 position = transformSystem->GetTranslation(listener);

        Vector3 front = transformSystem->GetFront(listener);

        Vector3 up = transformSystem->GetUp(listener);

        return Matrix4.LookAt(position, position + front, up);
    }

    private TransformSystem* transformSystem;

    private float* sceneTimeScale;

    private CNArray<int> sources;

    public void BindToGroup(int sourceIdentifier, int groupIndex)
        => volumeGroupBelonging[sourceIdentifier] = groupIndex;

    private CNArray<int> volumeGroupBelonging;

    private CNArray<float> volumeGroups;

    public void CreateVolumeGroup(int index, float value)
        => volumeGroups[index] = value; 

    public void SetGroupVolume(int index, float value)
        => volumeGroups[index] = value;

    public float GetGroupVolume(int index)
        => volumeGroups[index];

    private CNArray<float> volumes;

    public void SetVolume(int identifier, float value)
        => volumes[identifier] = value;

    public float GetVolume(int identifier)
        => volumes[identifier];

    private CNArray<float> pitches;

    public void SetPitch(int identifier, float value)
        => pitches[identifier] = value;

    public float GetPitch(int identifier)
        => pitches[identifier];

    public void CreateAudioSource(int identifier, int buffer)
    {
        int s = AL.GenSource();

        AL.Source(s, ALSourcef.Pitch, 1);

        pitches[identifier] = 1;

        // Set the volume of the source (0.0 'till 1.0)
        AL.Source(s, ALSourcef.Gain, 1);

        AL.Source(s, ALSourcef.MinGain, 0);

        AL.Source(s, ALSourcef.MaxGain, 1);

        volumes[identifier] = 1;

        volumeGroupBelonging[identifier] = 0;


        Vector3 position = transformSystem->GetTranslation(identifier);
        // Set the position of the source
        AL.Source(s, ALSource3f.Position, 0, 0, 0);

        // Set the velocity of the source
        AL.Source(s, ALSource3f.Velocity, 0, 0, 0);


        // Set if the audio should loop
        AL.Source(s, ALSourceb.Looping, true);


        AL.Source(s, ALSourcei.Buffer, buffer);
        
        sources[identifier] = s;
    }

    public void Run()
    {
        Matrix4 listenerView = CreateListenerMatrix();

        for(int i = 0; i < sources.Size; i++)
        {
            float currentPitch = AL.GetSource(sources[i], ALSourcef.Pitch);      

            if(currentPitch != pitches[i] * 1)        
                AL.Source(sources[i], ALSourcef.Pitch, pitches[i] * 1);


            float currentVolume = AL.GetSource(sources[i], ALSourcef.Gain);

            if(currentVolume != volumes[i] * volumeGroups[volumeGroupBelonging[i]])
                AL.Source(sources[i], ALSourcef.Gain, volumes[i] * volumeGroups[volumeGroupBelonging[i]]);


            if(sources[i] == 0) continue;

            Vector4 differencePos = -new Vector4(transformSystem->GetTranslation(i), 1) * listenerView;

            AL.Source(sources[i], ALSource3f.Position, differencePos.X, differencePos.Y, differencePos.Z);    
        }
    }

    public void PlaySource(int identifier)
        => AL.SourcePlay(sources[identifier]);

    public void PauseSource(int identifier)
        => AL.SourcePause(sources[identifier]);

    public void StopSource(int identifier)
        => AL.SourceStop(sources[identifier]);

    public void RewindSource(int identifier)
        => AL.SourceRewind(sources[identifier]);
}