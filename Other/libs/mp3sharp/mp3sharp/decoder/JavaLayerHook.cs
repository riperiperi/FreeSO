namespace javazoom.jl.decoder
{
	using System;
	/// <summary> The <code>JavaLayerHooks</code> class allows developers to change
	/// the way the JavaLayer library uses Resources. 
	/// </summary>
	
	internal interface JavaLayerHook
		{
			/// <summary> Retrieves the named resource. This allows resources to be
			/// obtained without specifying how they are retrieved. 
			/// </summary>
			System.IO.Stream getResourceAsStream(System.String name);
		}
}