using System;

namespace Mp3Sharp
{
	/// <summary>
	/// Some samples that show the use of the Mp3Stream class.
	/// </summary>
	internal class Sample
	{
		public static readonly string Mp3FilePath = @"c:\sample.mp3";

		/// <summary>
		/// Sample showing how to read through an MP3 file and obtain its contents as a PCM byte stream.
		/// </summary>
		public static void ReadAllTheWayThroughMp3File()
		{
			Mp3Stream stream = new Mp3Stream(Mp3FilePath);

			// Create the buffer
			int numberOfPcmBytesToReadPerChunk = 512;
			byte[] buffer = new byte[numberOfPcmBytesToReadPerChunk];

			int bytesReturned = -1;
			int totalBytes = 0;
			while (bytesReturned != 0)
			{
				bytesReturned = stream.Read(buffer, 0, buffer.Length);
				totalBytes += bytesReturned;
			}
			Console.WriteLine("Read a total of " + totalBytes + " bytes.");
		}

	}
}
