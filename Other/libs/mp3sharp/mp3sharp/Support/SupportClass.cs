using System;

namespace Support
{

	internal interface IThreadRunnable
	{
		void Run();
	}

	internal class SupportClass
	{
		/// <summary>
		/// Creates an instance of a received Type
		/// </summary>
		/// <param name="classType">The Type of the new class instance to return</param>
		/// <returns>An Object containing the new instance</returns>
		public static System.Object CreateNewInstance(System.Type classType)
		{
			System.Reflection.ConstructorInfo[] constructors = classType.GetConstructors();

			if (constructors.Length == 0)
				return null;

			System.Reflection.ParameterInfo[] firstConstructor = constructors[0].GetParameters();
			int countParams = firstConstructor.Length;

			System.Type[] constructor = new System.Type[countParams];
			for( int i = 0; i < countParams; i++)
				constructor[i] = firstConstructor[i].ParameterType;

			return classType.GetConstructor(constructor).Invoke(new System.Object[]{});
		}

		/*******************************/
		public static System.Object PutElement(System.Collections.Hashtable hashTable, System.Object key, System.Object newValue)
		{
			System.Object element = hashTable[key];
			hashTable[key] = newValue;
			return element;
		}

		/*******************************/
		/// <summary>
		/// Removes the element with the specified key from a Hashtable instance.
		/// </summary>
		/// <param name="hashtable">The Hashtable instance</param>
		/// <param name="key">The key of the element to remove</param>
		/// <returns>The element removed</returns>  
		public static System.Object HashtableRemove(System.Collections.Hashtable hashtable, System.Object key)
		{
			System.Object element = hashtable[key];
			hashtable.Remove(key);
			return element;
		}

		/*******************************/
		public static int URShift(int number, int bits)
		{
			if ( number >= 0)
				return number >> bits;
			else
				return (number >> bits) + (2 << ~bits);
		}

		public static int URShift(int number, long bits)
		{
			return URShift(number, (int)bits);
		}

		public static long URShift(long number, int bits)
		{
			if ( number >= 0)
				return number >> bits;
			else
				return (number >> bits) + (2L << ~bits);
		}

		public static long URShift(long number, long bits)
		{
			return URShift(number, (int)bits);
		}

		/*******************************/
		public static void WriteStackTrace(System.Exception throwable, System.IO.TextWriter stream)
		{
			stream.Write(throwable.StackTrace);
			stream.Flush();
		}

		/*******************************/
		internal class ThreadClass:IThreadRunnable
		{
			private System.Threading.Thread threadField;

			public ThreadClass()
			{
				threadField = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
			}

			public ThreadClass(System.Threading.ThreadStart p1)
			{
				threadField = new System.Threading.Thread(p1);
			}

			public virtual void Run()
			{
			}

			public virtual void Start()
			{
				threadField.Start();
			}

			public System.Threading.Thread Instance
			{
				get
				{
					return threadField;
				}
				set
				{
					threadField	= value;
				}
			}

			public System.String Name
			{
				get
				{
					return threadField.Name;
				}
				set
				{
					if (threadField.Name == null)
						threadField.Name = value; 
				}
			}

			public System.Threading.ThreadPriority Priority
			{
				get
				{
					return threadField.Priority;
				}
				set
				{
					threadField.Priority = value;
				}
			}

			public bool IsAlive
			{
				get
				{
					return threadField.IsAlive;
				}
			}

			public bool IsBackground
			{
				get
				{
					return threadField.IsBackground;
				} 
				set
				{
					threadField.IsBackground = value;
				}
			}

			public void Join()
			{
				threadField.Join();
			}

			public void Join(long p1)
			{
				lock(this)
				{
					threadField.Join(new System.TimeSpan(p1 * 10000));
				}
			}

			public void Join(long p1, int p2)
			{
				lock(this)
				{
					threadField.Join(new System.TimeSpan(p1 * 10000 + p2 * 100));
				}
			}

			public void Resume()
			{
				threadField.Resume();
			}

			public void Abort()
			{
				threadField.Abort();
			}

			public void Abort(System.Object stateInfo)
			{
				lock(this)
				{
					threadField.Abort(stateInfo);
				}
			}

			public void Suspend()
			{
				threadField.Suspend();
			}

			public override System.String ToString()
			{
				return "Thread[" + Name + "," + Priority.ToString() + "," + "" + "]";
			}

			public static ThreadClass Current()
			{
				ThreadClass CurrentThread = new ThreadClass();
				CurrentThread.Instance = System.Threading.Thread.CurrentThread;
				return CurrentThread;
			}
		}

		/*******************************/
		/// <summary>
		/// This method is used as a dummy method to simulate VJ++ behavior
		/// </summary>
		/// <param name="literal">The literal to return</param>
		/// <returns>The received value</returns>
		public static long Identity(long literal)
		{
			return literal;
		}

		/// <summary>
		/// This method is used as a dummy method to simulate VJ++ behavior
		/// </summary>
		/// <param name="literal">The literal to return</param>
		/// <returns>The received value</returns>
		public static ulong Identity(ulong literal)
		{
			return literal;
		}

		/// <summary>
		/// This method is used as a dummy method to simulate VJ++ behavior
		/// </summary>
		/// <param name="literal">The literal to return</param>
		/// <returns>The received value</returns>
		public static float Identity(float literal)
		{
			return literal;
		}

		/// <summary>
		/// This method is used as a dummy method to simulate VJ++ behavior
		/// </summary>
		/// <param name="literal">The literal to return</param>
		/// <returns>The received value</returns>
		public static double Identity(double literal)
		{
			return literal;
		}

		/*******************************/
		/// <summary>Reads a number of characters from the current source Stream and writes the data to the target array at the specified index.</summary>
		/// <param name="sourceStream">The source Stream to read from</param>
		/// <param name="target">Contains the array of characteres read from the source Stream.</param>
		/// <param name="start">The starting index of the target array.</param>
		/// <param name="count">The maximum number of characters to read from the source Stream.</param>
		/// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source Stream.</returns>
		public static System.Int32 ReadInput(System.IO.Stream sourceStream, ref sbyte[] target, int start, int count)
		{
			byte[] receiver = new byte[target.Length];
			int bytesRead   = sourceStream.Read(receiver, start, count);
			
			for(int i = start; i < start + bytesRead; i++)
				target[i] = (sbyte)receiver[i];
			
			return bytesRead;
		}

		/// <summary>Reads a number of characters from the current source TextReader and writes the data to the target array at the specified index.</summary>
		/// <param name="sourceTextReader">The source TextReader to read from</param>
		/// <param name="target">Contains the array of characteres read from the source TextReader.</param>
		/// <param name="start">The starting index of the target array.</param>
		/// <param name="count">The maximum number of characters to read from the source TextReader.</param>
		/// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source TextReader.</returns>
		public static System.Int32 ReadInput(System.IO.TextReader sourceTextReader, ref sbyte[] target, int start, int count)
		{
			char[] charArray = new char[target.Length];
			int bytesRead = sourceTextReader.Read(charArray, start, count);

			for(int index=start; index<start+bytesRead; index++)
				target[index] = (sbyte)charArray[index];

			return bytesRead;
		}

		/*******************************/
		public static System.Object Deserialize(System.IO.BinaryReader binaryReader)
		{
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			return formatter.Deserialize(binaryReader.BaseStream);
		}

		/*******************************/
		/// <summary>
		/// Writes an object to the specified Stream
		/// </summary>
		/// <param name="stream">The target Stream</param>
		/// <param name="objectToSend">The object to be sent</param>
		public static void Serialize(System.IO.Stream stream, System.Object objectToSend)
		{
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			formatter.Serialize(stream, objectToSend);
		}

		/// <summary>
		/// Writes an object to the specified BinaryWriter
		/// </summary>
		/// <param name="stream">The target BinaryWriter</param>
		/// <param name="objectToSend">The object to be sent</param>
		public static void Serialize(System.IO.BinaryWriter binaryWriter, System.Object objectToSend)
		{
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
			formatter.Serialize(binaryWriter.BaseStream, objectToSend);
		}

		/*******************************/


		internal class BackInputStream : System.IO.BinaryReader
		{
			protected byte[] buffer;
			protected int position = 1;

			public BackInputStream(System.IO.Stream streamReader, System.Int32 size) : base(streamReader)
			{
				this.buffer = new byte[size];
				//this.position = size;
				this.position = 0; // why would you not do that?
			}

			public BackInputStream(System.IO.Stream streamReader) : base(streamReader)
			{
				this.buffer = new byte[position];
			}

			public bool MarkSupported()
			{	
				return false;
			}

			public override int Read()
			{
				if (position >= 0 && position < buffer.Length)
					return (int)this.buffer[position++];
				return base.Read();
			}

			public override int Read(byte[] array, int index, int count)
			{
				int byteCount = 0;
				int readLimit = count + index;

				for(byteCount = 0;index <= buffer.Length  && index < readLimit;byteCount++)
					array[index++] = buffer[position++];


				if (index < readLimit)
					byteCount += base.Read(array,index, readLimit - index);

				return byteCount;
			}

			public void UnRead(int i)
			{
				this.position--;
				this.buffer[position] = (byte)i;
			}

			public void UnRead(byte[] array, int index, int count)
			{			
				this.Move(array,index,count);
			}

			public void UnRead(byte[] array)
			{
				this.Move(array, 0,array.Length-1);
			}

			public void Move(byte[] array, int index, int count)
			{
				for(int arrayPosition= index + count;  arrayPosition >= index;  arrayPosition--)			
					this.UnRead(array[ arrayPosition]);
			}
		}

		/*******************************/
		/// <summary>
		/// Converts an array of sbytes to an array of bytes
		/// </summary>
		/// <param name="sbyteArray">The array of sbytes to be converted</param>
		/// <returns>The new array of bytes</returns>
		public static byte[] ToByteArray(sbyte[] sbyteArray)
		{
			byte[] byteArray = new byte[sbyteArray.Length];
			for(int index=0; index < sbyteArray.Length; index++)
				byteArray[index] = (byte) sbyteArray[index];
			return byteArray;
		}

		/// <summary>
		/// Converts a string to an array of bytes
		/// </summary>
		/// <param name="sourceString">The string to be converted</param>
		/// <returns>The new array of bytes</returns>
		public static byte[] ToByteArray(string sourceString)
		{
			byte[] byteArray = new byte[sourceString.Length];
			for (int index=0; index < sourceString.Length; index++)
				byteArray[index] = (byte) sourceString[index];
			return byteArray;
		}

		/*******************************/
		internal class RandomAccessFileSupport
		{
			public static System.IO.FileStream CreateRandomAccessFile(string fileName, string mode) 
			{
				System.IO.FileStream newFile = null;

				if (mode.CompareTo("rw") == 0)
					newFile =  new System.IO.FileStream(fileName, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite); 
				else if (mode.CompareTo("r") == 0 )
					newFile =  new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read); 
				else
					throw new System.ArgumentException();

				return newFile;
			}

			public static System.IO.FileStream CreateRandomAccessFile(System.IO.FileInfo fileName, string mode)
			{
				return CreateRandomAccessFile(fileName.FullName, mode);
			}

			public static void WriteBytes(string data,System.IO.FileStream fileStream)
			{
				int index = 0;
				int length = data.Length;

				while(index < length)
					fileStream.WriteByte((byte)data[index++]);	
			}

			public static void WriteChars(string data,System.IO.FileStream fileStream)
			{
				WriteBytes(data, fileStream);	
			}

			public static void WriteRandomFile(sbyte[] sByteArray,System.IO.FileStream fileStream)
			{
				byte[] byteArray = ToByteArray(sByteArray);
				fileStream.Write(byteArray, 0, byteArray.Length);
			}
		}

		/*******************************/
		/// <summary>
		/// Method that copies an array of sbytes from a String to a received array .
		/// </summary>
		/// <param name="sourceString">The String to get the sbytes.</param>
		/// <param name="sourceStart">Position in the String to start getting sbytes.</param>
		/// <param name="sourceEnd">Position in the String to end getting sbytes.</param>
		/// <param name="destinationArray">Array to store the bytes.</param>
		/// <param name="destinationStart">Position in the destination array to start storing the sbytes.</param>
		/// <returns>An array of sbytes</returns>
		public static void GetSBytesFromString(string sourceString, int sourceStart, int sourceEnd, ref sbyte[] destinationArray, int destinationStart)
		{	
			int sourceCounter;
			int destinationCounter;
			sourceCounter = sourceStart;
			destinationCounter = destinationStart;
			while (sourceCounter < sourceEnd)
			{
				destinationArray[destinationCounter] = (sbyte) sourceString[sourceCounter];
				sourceCounter++;
				destinationCounter++;
			}
		}

	}
}