using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;
using System.IO;

using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Buffer = Microsoft.DirectX.DirectSound.Buffer;
namespace Mp3Sharp
{


	/// <summary>
	/// Represents the method that handles a buffer notification event.  
	/// To properly handle the event, the NewSoundByte field should be set to an array of bytes less than or equal to the
	/// NumBytesRequired property.  If less than the required number of bytes are provided, the stream will fill the remainder
	/// with silence.  SoundFinished defaults to false, and should be set to indicate
	/// if the bytes contained in NewSoundByte represent the end of the sound.
	/// </summary>
	public delegate void BufferNotificationEventHandler(object sender, BufferNotificationEventArgs e);

	/// <summary>
	/// Describes a buffer notification event.  
	/// To properly handle the event, the NewSoundByte field should be set to an array of bytes less than or equal to the
	/// NumBytesRequired property.  If less than the required number of bytes are provided, the stream will fill the remainder
	/// with silence.  SoundFinished defaults to false, and should be set to indicate
	/// if the bytes contained in NewSoundByte represent the end of the sound.
	/// </summary>
	public class BufferNotificationEventArgs : EventArgs
	{
		public BufferNotificationEventArgs (int numBytesRequired)
		{
			NumBytesRequiredRep = numBytesRequired;
		}

		/// <summary>
		/// Gets or sets whether these represent the final bytes in the sound.
		/// </summary>
		public bool SoundFinished { get { return SoundFinishedRep; } set { SoundFinishedRep = value; } }
		private bool SoundFinishedRep = false;

		/// <summary>
		/// Gets the number of bytes required for this event.
		/// </summary>
		public int NumBytesRequired { get { return NumBytesRequiredRep; } }
		internal int NumBytesRequiredRep;

		/// <summary>
		/// Set this field to the new bytes provided for the sound.
		/// </summary>
		public byte[] NewSoundByte;
	}

	/// <summary>
	/// Component representing a secondary buffer that can be used for streaming.  
	/// The buffer raises events when it reaches its half-way mark, as well as its end.  
	/// </summary>
	public class EventRaisingSoundBuffer
	{
		public EventRaisingSoundBuffer(Device device, WaveFormat waveFormat, TimeSpan bufferLength)
		{
			Device = device; WaveFormat = waveFormat; BufferLength = bufferLength;
		}


		/// <summary>
		/// Gets or sets the format of the buffer.  
		/// The format can be set only if the buffer is not playing.  
		/// Defaults to 22050Hz, 16-bit stereo sound.  Hint: Use SoundUtil.CreateWaveFormat to quickly build WaveFormat objects.
		/// </summary>
		public WaveFormat WaveFormat
		{
			set
			{
				if (Playing) throw new ApplicationException("Can't change the format of the event-raising sound buffer while the buffer is playing.");
				bool hasChanged = WaveFormat.BitsPerSample != value.BitsPerSample 
					|| WaveFormat.Channels != value.Channels
					|| WaveFormat.SamplesPerSecond != value.SamplesPerSecond;
				WaveFormatRep = value;
				if (SB != null && hasChanged) { SB.Dispose(); SB = null; }
			}
			get { return WaveFormatRep; } 
		}
		private WaveFormat WaveFormatRep = SoundUtil.CreateWaveFormat(22050, 16, 2);

		/// <summary>
		/// Gets or sets the length of the buffer.  
		/// The buffer length can be set only if the buffer is not playing.
		/// Defaults to 1.0 seconds.
		/// </summary>
		public TimeSpan BufferLength
		{
			set
			{
				if (BufferLengthRep == value) return;
				if (Playing) throw new ApplicationException("Can't change the buffer length of the event-raising sound buffer while the buffer is playing.");
				BufferLengthRep = value;
				if (SB != null) { SB.Dispose(); SB = null; }
			}
			get { return BufferLengthRep; }
		}
		private TimeSpan BufferLengthRep = TimeSpan.FromSeconds(1.0);

		/// <summary>
		/// Gets the span of time between events raised by the buffer.  Equal to half the buffer's length.
		/// </summary>
		public TimeSpan EventInterval
		{
			get { return TimeSpan.FromSeconds(BufferLengthRep.TotalSeconds / 2); }
		}

		public SecondaryBuffer SecondaryBuffer { get { return SB; } }

		SecondaryBuffer SB;
		Notify Notify;

		protected Device Device;

		protected AutoResetEvent NotificationEvent = new AutoResetEvent(false);


		/// <summary>
		/// Initialize the SecondaryBuffer and Notify instances.
		/// </summary>
		protected void InitSecondaryBuffer()	
		{
			if (SB != null) SB.Dispose();
			if (Notify != null) Notify.Dispose();

			BufferDescription description = new BufferDescription(WaveFormat);
			description.ControlPositionNotify = true;
			description.BufferBytes = (int)Math.Round(((double)WaveFormat.AverageBytesPerSecond * this.BufferLength.TotalSeconds));
			description.ControlVolume = true;
			description.ControlEffects = false;
			description.Control3D = false;
			description.StickyFocus = true;

			SB = new SecondaryBuffer(description, Device);
			int length = SB.Caps.BufferBytes;
			byte[] bytes = new byte[length];
			Random r = new Random();
			r.NextBytes(bytes);

			Notify = new Notify(SB);
			BufferPositionNotify []bpn = new BufferPositionNotify[3];
			bpn[0] = new BufferPositionNotify();
			bpn[0].Offset = length/2-1;
			bpn[0].EventNotifyHandle = NotificationEvent.Handle;

			bpn[1] = new BufferPositionNotify();
			bpn[1].Offset = length-1;
			bpn[1].EventNotifyHandle = NotificationEvent.Handle;

			bpn[2] = new BufferPositionNotify();
			bpn[2].Offset = (int)PositionNotifyFlag.OffsetStop;
			bpn[2].EventNotifyHandle = NotificationEvent.Handle;

			Notify.SetNotificationPositions(bpn, 3);

			if (Initialized != null) Initialized(this, new EventArgs());
		}

		/// <summary>
		/// Event that is raised after the secondary buffer is initialized.
		/// </summary>
		public event EventHandler Initialized;


		public void Play() 
		{
			if (SB == null) InitSecondaryBuffer();

			Thread t = new Thread(new ThreadStart(StreamControlThread));
			t.Name = "Event-Raising Sound Buffer Control Thread";
			t.IsBackground = true;
			t.Start();

			BufferPlayFlags flags = BufferPlayFlags.Looping; 
			//ApplyEffectsInfo();

			SB.Play(0, flags);
		}

		private void GetBytesByRaisingEvent(int locationInSecondaryBuffer, int numBytesToAcquire, BufferNotificationEventArgs e)
		{
			e.NumBytesRequiredRep = numBytesToAcquire;
			if (BufferNotification != null) BufferNotification(this, e);

			if (e.NewSoundByte == null) e.NewSoundByte = new byte[0];

			//Console.WriteLine("Request issued for " + numBytesToAcquire + " bytes; " + e.NewSoundByte.Length + " obtained.");
		}

		private enum NextNotificationTask
		{
			FillSectionWithNewSound,
			FillSectionWithSilence,
			StopSecondaryBufferAndThread
		}

		private NextNotificationTask HandleNewBytesInControlThread(int nextPlaceForBytes, int byteWindowSize, BufferNotificationEventArgs ea)
		{
			LockFlag lockFlag = LockFlag.None;
			int bytesObtained = ea.NewSoundByte.Length;
			if (bytesObtained > byteWindowSize) 
			{
				SB.Stop();
				throw new ApplicationException("An event handler provided the streaming buffer with " + bytesObtained + " bytes of sound, but it only requested " + byteWindowSize + " bytes.");
			}
			else if (bytesObtained == byteWindowSize)
			{
				SB.Write(nextPlaceForBytes, ea.NewSoundByte, lockFlag);
			}
			else
			{
				// Fill the remainder of the segment with silence.
				if (ea.NewSoundByte.Length > 0) SB.Write(nextPlaceForBytes, ea.NewSoundByte, lockFlag);
				SB.Write(nextPlaceForBytes+ea.NewSoundByte.Length, new byte[byteWindowSize-ea.NewSoundByte.Length], lockFlag);

				if (ea.SoundFinished) return NextNotificationTask.FillSectionWithSilence;
			}
			return NextNotificationTask.FillSectionWithNewSound;
		}

		/// <summary>
		/// The stream control thread raises events every half the stream.  
		/// When the BufferNotificationEventArgs contains a SoundFinished property set to true, the
		/// current buffer segment is padded with silence.  At the next notification, the next
		/// buffer segment is filled with silence, and no event is raised.  
		/// At the next notification, which will come when the padded segment 
		/// (not the completely silent segment) is finished, the SecondaryBuffer is stopped and 
		/// the thread terminated.
		/// </summary>
		private void StreamControlThread()
		{
			int nextPlaceForBytes = 0;
			int wholeBufferSize = SB.Caps.BufferBytes;
			int byteWindowSize = wholeBufferSize / 2;
			NextNotificationTask task = NextNotificationTask.FillSectionWithNewSound;

			//BufferNotificationEventArgs ssea = new BufferNotificationEventArgs(SB.Caps.BufferBytes);
			BufferNotificationEventArgs firstNotificationEventArgs = new BufferNotificationEventArgs(wholeBufferSize);
			GetBytesByRaisingEvent(0, wholeBufferSize, firstNotificationEventArgs);
			task = HandleNewBytesInControlThread(nextPlaceForBytes, wholeBufferSize, firstNotificationEventArgs);


			bool terminate = false;

			while (!terminate)
			{
				NotificationEvent.Reset();
				NotificationEvent.WaitOne();

				if (SB.Disposed || (!Playing)) break;

				/// Very strange behavior from DirectSound!!
				/// SB.PlayPosition returns a value slightly less than the actual position.  Either that or the event is raised
				/// So you can use that to determine which section to fill.  Fill the half that you're currently "playing" 
				/// according to the PlayPosition.
				/// If anyone knows how to do this properly, please e-mail me, rob@mle.ie.
				int playPosition = SB.PlayPosition;
				int distToBegin = Math.Abs(playPosition - 0);
				int distToEnd = Math.Abs(playPosition - wholeBufferSize);
				int distToMid = Math.Abs(playPosition - byteWindowSize);

				if (distToMid < distToEnd && distToMid < distToBegin)
					nextPlaceForBytes = 0;
				else
					nextPlaceForBytes = byteWindowSize;
				//Console.WriteLine(DateTime.Now + ": Received request for bytes at " + nextPlaceForBytes + " and I'm now at " + SB.PlayPosition);
				switch(task)
				{
					case NextNotificationTask.FillSectionWithNewSound:
						BufferNotificationEventArgs nextNotificationEventArgs = new BufferNotificationEventArgs(byteWindowSize);
						GetBytesByRaisingEvent(nextPlaceForBytes, byteWindowSize, nextNotificationEventArgs);
						task = HandleNewBytesInControlThread(nextPlaceForBytes, byteWindowSize, nextNotificationEventArgs);
						break;
					case NextNotificationTask.FillSectionWithSilence:
						task = NextNotificationTask.StopSecondaryBufferAndThread;
						//Console.WriteLine("Filling section with silence at " + nextPlaceForBytes);
						int currentPosition = 0; int writePos = 0;
						SB.GetCurrentPosition(out currentPosition, out writePos);
						//Console.WriteLine("Current pos " + currentPosition + " and writing " + byteWindowSize + " at " + nextPlaceForBytes);
						SB.Write(nextPlaceForBytes, new byte[byteWindowSize], LockFlag.None);
						break;
					default: // NextNotificationTask.StopSecondaryBufferAndThread
						SB.Stop();
						//Console.WriteLine("stream control thread dies.");
						return;
				}
				//nextPlaceForBytes += byteWindowSize; if (nextPlaceForBytes >= SB.Caps.BufferBytes) nextPlaceForBytes = 0;
			}
			//Console.WriteLine("stream control thread dies.");
		}

		/// <summary>
		/// Event that is raised when the buffer notification event occurs.
		/// </summary>
		public event BufferNotificationEventHandler BufferNotification;



		/// <summary>
		/// Gets or sets whether the sound buffer is playing.  On a set, plays (but does not loop) the sound.
		/// </summary>
		public bool Playing
		{
			get { return SB != null && (SB.Status.Looping || SB.Status.Playing); }
			set
			{
				if (value == false) SB.Stop();
				else if (!Playing) Play();
			}
		}


		public void Stop()
		{
			if (Playing) 
			{ 
				SB.Stop(); 
				//NotificationEvent.Set();
				//if (RewindBufferOnStop) Rewind();
			}
		}



	}

}