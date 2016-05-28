/*
* 12/12/99 0.0.7	Implementation stores single bits 
*					as ints for better performance. mdm@techie.com.
*
* Java Conversion by E.B, ebsp@iname.com, JavaLayer
*
*---------------------------------------------------
* bit_res.h
*
* 	Declarations for Bit Reservoir for Layer III
*
*  Adapted from the public c code by Jeff Tsay.
*---------------------------------------------------
*/
namespace javazoom.jl.decoder
{
	using System;
	
	/// <summary> Implementation of Bit Reservoir for Layer III.
	/// <p>
	/// The implementation stores single bits as a word in the buffer. If
	/// a bit is set, the corresponding word in the buffer will be non-zero.
	/// If a bit is clear, the corresponding word is zero. Although this
	/// may seem waseful, this can be a factor of two quicker than 
	/// packing 8 bits to a byte and extracting. 
	/// <p> 
	/// </summary>
	
	// REVIEW: there is no range checking, so buffer underflow or overflow
	// can silently occur.
	sealed class BitReserve
	{
		private void  InitBlock()
		{
			buf = new int[BUFSIZE];
		}
		/// <summary> Size of the internal buffer to store the reserved bits.
		/// Must be a power of 2. And x8, as each bit is stored as a single
		/// entry.
		/// </summary>
		private const int BUFSIZE = 4096 * 8;
		
		/// <summary> Mask that can be used to quickly implement the
		/// modulus operation on BUFSIZE.
		/// </summary>
		//UPGRADE_NOTE: Final was removed from the declaration of 'BUFSIZE_MASK '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		private static readonly int BUFSIZE_MASK = BUFSIZE - 1;
		
		private int offset, totbit, buf_byte_idx;
		//UPGRADE_NOTE: Final was removed from the declaration of 'buf '. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1003"'
		//UPGRADE_NOTE: The initialization of  'buf' was moved to method 'InitBlock'. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1005"'
		private int[] buf;
		private int buf_bit_idx;
		
		internal BitReserve()
		{
			InitBlock();
			
			offset = 0;
			totbit = 0;
			buf_byte_idx = 0;
		}
		
		
		/// <summary> Return totbit Field.
		/// </summary>
		public int hsstell()
		{
			return (totbit);
		}
		
		/// <summary> Read a number bits from the bit stream.
		/// </summary>
		/// <param name="N">the number of
		/// 
		/// </param>
		public int hgetbits(int N)
		{
			totbit += N;
			
			int val = 0;
			
			int pos = buf_byte_idx;
			if (pos + N < BUFSIZE)
			{
				while (N-- > 0)
				{
					val <<= 1;
					val |= ((buf[pos++] != 0)?1:0);
				}
			}
			else
			{
				while (N-- > 0)
				{
					val <<= 1;
					val |= ((buf[pos] != 0)?1:0);
					pos = (pos + 1) & BUFSIZE_MASK;
				}
			}
			buf_byte_idx = pos;
			return val;
		}
		
		
		
		/// <summary> Read 1 bit from the bit stream.
		/// </summary>
		/*
		public int hget1bit_old()
		{
		int val;
		totbit++;
		if (buf_bit_idx == 0)
		{
		buf_bit_idx = 8;
		buf_byte_idx++;		 
		}
		// BUFSIZE = 4096 = 2^12, so
		// buf_byte_idx%BUFSIZE == buf_byte_idx & 0xfff
		val = buf[buf_byte_idx & BUFSIZE_MASK] & putmask[buf_bit_idx];
		buf_bit_idx--;
		val = val >>> buf_bit_idx;
		return val;   
		}
		*/
		/// <summary> Returns next bit from reserve.
		/// </summary>
		/// <returns>s 0 if next bit is reset, or 1 if next bit is set.
		/// 
		/// </returns>
		public int hget1bit()
		{
			totbit++;
			int val = buf[buf_byte_idx];
			buf_byte_idx = (buf_byte_idx + 1) & BUFSIZE_MASK;
			return val;
		}
		
		/// <summary> Retrieves bits from the reserve.     
		/// </summary>
		/*   
		public int readBits(int[] out, int len)
		{
		if (buf_bit_idx == 0)
		{
		buf_bit_idx = 8;
		buf_byte_idx++;
		current = buf[buf_byte_idx & BUFSIZE_MASK];
		}      
		
		
		
		// save total number of bits returned
		len = buf_bit_idx;
		buf_bit_idx = 0;
		
		int b = current;
		int count = len-1;
		
		while (count >= 0)
		{
		out[count--] = (b & 0x1);
		b >>>= 1;
		}
		
		totbit += len;
		return len;
		}
		*/
		
		/// <summary> Write 8 bits into the bit stream.
		/// </summary>
		public void  hputbuf(int val)
		{
			int ofs = offset;
			buf[ofs++] = val & 0x80;
			buf[ofs++] = val & 0x40;
			buf[ofs++] = val & 0x20;
			buf[ofs++] = val & 0x10;
			buf[ofs++] = val & 0x08;
			buf[ofs++] = val & 0x04;
			buf[ofs++] = val & 0x02;
			buf[ofs++] = val & 0x01;
			
			if (ofs == BUFSIZE)
				offset = 0;
			else
				offset = ofs;
		}
		
		/// <summary> Rewind N bits in Stream.
		/// </summary>
		public void  rewindNbits(int N)
		{
			totbit -= N;
			buf_byte_idx -= N;
			if (buf_byte_idx < 0)
				buf_byte_idx += BUFSIZE;
		}
		
		/// <summary> Rewind N bytes in Stream.
		/// </summary>
		public void  rewindNbytes(int N)
		{
			int bits = (N << 3);
			totbit -= bits;
			buf_byte_idx -= bits;
			if (buf_byte_idx < 0)
				buf_byte_idx += BUFSIZE;
		}
	}
}