MP3Sharp: JavaLayer C# Port
Robert Burke, 25 Feb 04
rob@mle.ie
 
Right now it's lacking polish, but here's a C# port of JavaLayer, 
an MP3 decoder for Java written by the JavaZoom team.  Hopefully 
it will be useful to other people who want to decode Mp3s in 
native C#.  I've tested it with a variety of MP3s (Constant and 
Variable Bitrate, Stereo and Mono, etc. etc.) and - props to the 
JavaZoom team! - it seems to do the trick. 

There's some sample code in the enclosed (VS.NET2003) Solution that 
should hopefully set you on the right path.

I used Beta2 of the Java Language Conversion Assistant as a starting 
point for this project, and spent the rest of the day cleaning up 
after it.  There were some bizarre bit-shifting bugs introduced by 
the JLCA that I corrected.  I also removed JavaZoom's dependency on 
serialized files.  

Honestly, this was a half-day project, so please forgive me for the 
state of the code.  I came back to this a year on and spent another
half-day writing the System.IO.Stream-derived interface to it,
and an example of streaming MP3 audio using Managed DirectSound.

But I welcome comments, requests, suggestions and contributions: 
rob@mle.ie

--------

Update 1 Sep 04
rob@mle.ie

Version 1.4 released.  With kind thanks to tedHedd (tekhedd@byteheaven.net)
the code is now significantly optimized.  I cleaned up the interface a
little and so now if you just use the Mp3Sharp DLL it should get the job done.


--------


Quickstart:

See Sample.cs.  Altough this assembly exposes a bunch of classes,
the only one you really want to use is Mp3Sharp.Mp3Stream.

 
