using OpenTK.Audio.OpenAL;

namespace Components.SFX.TonklangHelper;

// Struct that handles reading
// audio data from files
public static unsafe class AudioReader
{
    public static void DeleteData(int bufferID)
        => AL.DeleteBuffer(bufferID);

        // In order to begin the reading
        // of an audio file, this method
        // should be called
        public static int ReadData(string path)
        {
            string magicN;

            // Open a binaryreader (To read the "magic number" of the file)
            using(BinaryReader b = new BinaryReader(File.Open("./Resources/" + path, FileMode.Open)))
            {
                magicN = new string(b.ReadChars(4));
            }

            // Check what type the given file is
            switch(magicN)
            {
                // If file is Ogg
                case "OggS":
                    // Read the file with the
                    // ogg reading algorythm
                    // and return the read data
                    return ReadOgg("./Resources/" + path);

                // If file is Wav
                case "RIFF":
                    // Read the file with the
                    // wav reading algorythm
                    // and return the read data
                    return ReadWav("./Resources/" + path);

                // If file is none of the above
                default:
                    // Throw an excpetion
                    throw new IOException("file type not supported");
            }                   
        }

        // This is unfinished and probably will stay like
        // this as long as i don't find out where the
        // god blessed channels and samplerates are stored at

        // The method that contains the ogg
        // reading algorythm
        private static int ReadOgg(string p)
        {
            /*using(BinaryReader reader = new BinaryReader(File.Open(p, FileMode.Open)))
            {
                List<byte> samples = new List<byte>(0);

                long totalAmount = reader.BaseStream.Length;

                try
                {
                    for(long l = 0; l < totalAmount; l += 8)
                    {
                        if((reader.ReadChar() == 'O') && (reader.ReadChar() == 'g') &&
                            (reader.ReadChar() == 'g') && (reader.ReadChar() == 'S'))
                        {
                            Console.WriteLine("FOUND");
                        }

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }

                for(int i = 0; i < reader.BaseStream.Length; i++)
                {
                    try
                    {
                        if((reader.ReadChar() == 'O') && (reader.ReadChar() == 'g') &&
                            (reader.ReadChar() == 'g') && (reader.ReadChar() == 'S'))
                        {
                            Console.WriteLine("FOUND");

                            // Version
                            Console.WriteLine(reader.ReadByte() + " version");

                            // headerType
                            Console.WriteLine(reader.ReadByte() + " header type");

                            // granule Position
                            Console.WriteLine(reader.ReadUInt64() + " granule position");

                            // serial number
                            Console.WriteLine(reader.ReadUInt32() + " serial number");

                            // sequence Number
                            // 29161 = AUDIO
                            Console.WriteLine(reader.ReadUInt32() + " sequence number");

                            // checksum
                            Console.WriteLine(reader.ReadUInt32() + " checksum");

                            // page segments
                            byte segments = reader.ReadByte();

                            Console.WriteLine(segments + " page segments");

                            for(byte b = 0; b < segments; b++)
                            {
                                byte current = reader.ReadByte();

                                samples.Add(current);

                                Console.WriteLine(current);
                            }
                        }
                    }
                    catch
                    {
                        reader.ReadByte();
                    }
                }

                string capturePattern = new string(reader.ReadChars(4));

                Console.WriteLine(capturePattern);

                byte version = reader.ReadByte();

                Console.WriteLine(version);

                byte headerType = reader.ReadByte();

                Console.WriteLine(headerType);

                ulong granulePosition = reader.ReadUInt64();

                Console.WriteLine(granulePosition);

                uint serialNumber = reader.ReadUInt32();

                Console.WriteLine(serialNumber);

                uint sequenceNumber = reader.ReadUInt32();

                Console.WriteLine(sequenceNumber);

                uint checksum = reader.ReadUInt32();

                Console.WriteLine(checksum);

                byte pageSegments = reader.ReadByte();

                Console.WriteLine(pageSegments);

                for(byte b = 0; b < pageSegments; b++)
                {
                    Console.WriteLine(reader.ReadByte());
                }
            }

            return 0;*/

            // Open a file stream
            using(NVorbis.VorbisReader vorbis = new NVorbis.VorbisReader(p))
            {
                // Prepare the format parameter
                ALFormat format;

                // Prepare the frequency parameter
                int freq;

                // Take the amount of channels
                // from the audio data
                int channels = vorbis.Channels;

                // Set the format depending
                // on the data's nominal bitrate
                // and amount of channels
                switch(channels)
                {
                    // If one channel...
                    case 1:
                        // Set the format to Mono8, if the bitrate equals 8
                        // or Mono16 if it doesn't
                        format = vorbis.NominalBitrate == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                    break;

                    // If two channels...
                    case 2:
                        // Set the format to Stereo8, if the bitrate equals 8
                        // or Stereo16 if it doesn't                    
                        format = vorbis.NominalBitrate == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                    break;

                    // If edge case...
                    default:
                        // Set the format to the default
                        format = default;
                    break;
                }

                // Get the samplerate
                int sampleRate = freq = vorbis.SampleRate;

                // Create a buffer to read from the bitstream
                float[] readBuffer = new float[channels * sampleRate / 5];

                // Prepare a list for the resulting samples
                List<byte> result = new List<byte>();

                // Some counter
                int cnt;

                // I don't know what happens here...
                while((cnt = vorbis.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    for(int i = 0; i < cnt; i++)
                    {
                        short temp = (short)(short.MaxValue * readBuffer[i]);

                        result.Add((byte)temp);
                        result.Add((byte)(temp >> 8));
                    }
                }


                int b = AL.GenBuffer();

                byte[] final = result.ToArray();

                fixed(byte* ptr = final)
                {
                    AL.BufferData(b, format, ptr, final.Length, freq);
                }

                return b;
            }
        }

        // The method that contains the wav
        // reading algorythm
        private static int ReadWav(string p)
        {
            using(BinaryReader reader = new BinaryReader(File.Open(p, FileMode.Open)))
            {
                string signatue = new string(reader.ReadChars(4));

                if(signatue != "RIFF")
                    throw new IOException("The given file is not wav file");


                int riff_chunk_size = reader.ReadInt32();


                string format = new string(reader.ReadChars(4));

                if(format != "WAVE")
                    throw new IOException("The given file is not wav file");


                string format_signature = new string(reader.ReadChars(4));

                if(format_signature != "fmt ")
                    throw new IOException("The given file is not a supported wav file"); 


                int format_chunk_size = reader.ReadInt32();
                int audio_format = reader.ReadInt16();
                int num_channels = reader.ReadInt16();
                int sample_rate = reader.ReadInt32();
                int byte_rate = reader.ReadInt32();
                int block_align = reader.ReadInt16();
                int bits_per_sample = reader.ReadInt16();


                string data_signature = new string(reader.ReadChars(4));

                if (data_signature != "data")
                    throw new IOException("The given file is not a supported wav file"); 

                int data_chunk_size = reader.ReadInt32();    


                ALFormat GetFormat()
                {
                    switch(num_channels)
                    {

                        case 1:
                            return bits_per_sample == 8 ? ALFormat.Mono8 : ALFormat.Mono16;

                        case 2:
                            return bits_per_sample == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
                    }

                    throw new Exception("Reverb bruh");
                }

                ALFormat alFormat = GetFormat();


                int b = AL.GenBuffer();

                byte[] final = reader.ReadBytes((int)reader.BaseStream.Length);

                fixed(byte* ptr = final)
                {
                    AL.BufferData(b, alFormat, ptr, final.Length, sample_rate);
                }

                return b;
            }
        }
}