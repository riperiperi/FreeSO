Streaming MP3 Demo using Mp3Sharp and Managed DirectSound
Robert Burke, 25 Feb 04
rob@mle.ie

Here is a sample, admittedly a little rough around the edges, of how to 
do streaming MP3 audio using the Mp3Stream decoder.

Main.cs is a modified version of the "Playsound" Managed DirectX sample.  
It creates an instance of the StreamedMp3Sound class:
  ApplicationStreamedSound = new StreamedMp3Sound(ApplicationDevice, new Mp3Stream(name));
  
StreamedMp3Sound derives from StreamedSound, which uses an EventRaisingSoundBuffer to 
implement streaming PCM audio.  The only thing StreamedMp3Sound does differently from 
StreamedSound that makes it MP3-specific is that it looks at the first header in the 
MP3 file to see what the frequency and channel count of the bytestream is.  The 
frequency of the secondary buffer used by the EventRaisingSoundBuffer is set accordingly.

If you have your own streaming classes, you can toss all of this and just use Mp3Stream 
to provide you with a PCM audio stream.

Let me know how this works for you!  I tested it a little, but not extensively.  
This is one of those weekend projects that is starting to take on a life of its own!

