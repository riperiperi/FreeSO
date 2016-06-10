namespace javazoom.jl.decoder
{
	using System;
	
	/// <summary> Work in progress.
	/// </summary>
	
	internal interface Control
		{
			bool Playing
			{
				get;
				
			}
			bool RandomAccess
			{
				get;
				
			}
			/// <summary> Retrieves the current position.
			/// </summary>
			/// <summary> 
			/// </summary>
			double Position
			{
				get;
				
				set;
				
			}
			/// <summary> Starts playback of the media presented by this control.
			/// </summary>
			void  start();
			/// <summary> Stops playback of the media presented by this control.
			/// </summary>
			void  stop();
			void  pause();
		}
}