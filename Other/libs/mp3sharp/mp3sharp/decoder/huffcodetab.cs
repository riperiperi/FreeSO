using Support;
/*
* 16/11/99 Renamed class, added javadoc, and changed table
*			name from String to 3 chars. mdm@techie.com
* 02/15/99 Java Conversion by E.B, ebsp@iname.com, JavaLayer
*
*---------------------------------------------------------------------------
* huffman.h
*
*	Adapted from the ISO MPEG Audio Subgroup Software Simulation
*  Group's public c source for its MPEG audio decoder. Miscellaneous
*  changes by Jeff Tsay (ctsay@pasteur.eecs.berkeley.edu).
*
*  Last modified : 04/19/97
*
**********************************************************************
Copyright (c) 1991 MPEG/audio software simulation group, All Rights Reserved
huffman.h
**********************************************************************
**********************************************************************
* MPEG/audio coding/decoding software, work in progress              *
*   NOT for public distribution until verified and approved by the   *
*   MPEG/audio committee.  For further information, please contact   *
*   Davis Pan, 508-493-2241, e-mail: pan@3d.enet.dec.com             *
*                                                                    *
* VERSION 4.1                                                        *
*   changes made since last update:                                  *
*   date   programmers                comment                        *
*  27.2.92 F.O.Witte (ITT Intermetall)				                  *
*  8/24/93 M. Iwadare          Changed for 1 pass decoding.          *
*  7/14/94 J. Koller		useless 'typedef' before huffcodetab  	  *
*				removed				      			                  *
**********************************************************************
*----------------------------------------------------------------------------
*/
namespace javazoom.jl.decoder
{
	using System;
	
	/// <summary> Class to implements Huffman decoder.
	/// </summary>
	sealed class huffcodetab
	{
		private const int MXOFF = 250;
		private const int HTN = 34;
		
		private char tablename0 = ' '; /* string, containing table_description   */
		private char tablename1 = ' '; /* string, containing table_description   */
		private char tablename2 = ' '; /* string, containing table_description   */
		
		private int xlen; /* max. x-index+                          */
		private int ylen; /* max. y-index+				          */
		private int linbits; /* number of linbits   	                  */
		private int linmax; /* max number to be stored in linbits	  */
		private int ref_Renamed; /* a positive value indicates a reference */
		private int[] table = null; /* pointer to array[xlen][ylen]		      */
		private int[] hlen = null; /* pointer to array[xlen][ylen]		      */
		private int[][] val = null; /* decoder tree		    	              */
		private int treelen; /* length of decoder tree  	              */
		
		private static int[][] ValTab0 = {new int[]{0, 0}};
		
		private static int[][] ValTab1 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{2, 1}, new int[]{0, 16}, new int[]{2, 1}, new int[]{0, 1}, new int[]{0, 17}};
		
		private static int[][] ValTab2 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 33}, new int[]{2, 1}, new int[]{0, 18}, new int[]{2, 1}, new int[]{0, 2}, new int[]{0, 34}};
		
		private static int[][] ValTab3 = {new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{2, 1}, new int[]{0, 16}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 33}, new int[]{2, 1}, new int[]{0, 18}, new int[]{2, 1}, new int[]{0, 2}, new int[]{0, 34}};
		
		private static int[][] ValTab4 = {new int[]{0, 0}}; // dummy
		
		private static int[][] ValTab5 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 34}, new int[]{0, 48}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 19}, new int[]{2, 1}, new int[]{0, 49}, new int[]{2, 1}, new int[]{0, 50}, new int[]{2, 1}, new int[]{0, 35}, new int[]{0, 51}};
		
		private static int[][] ValTab6 = {new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{0, 16}, new int[]{0, 17}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 33}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 18}, new int[]{2, 1}, new int[]{0, 2}, new int[]{0, 34}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 50}, new int[]{2, 1}, new int[]{0, 35}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 51}};
		
		private static int[][] ValTab7 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{0, 33}, new int[]{18, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 18}, new int[]{2, 1}, new int[]{0, 34}, new int[]{0, 48}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 50}, new int[]{2, 1}, new int[]{0, 35}, new int[]{0, 4}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 65}, new int[]{2, 1}, new int[]{0, 20}, new int[]{2, 1}, new int[]{0, 66}, new int[]{0, 36}, new int[]{12, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 51}, new int[]{0, 67}, new int[]{0, 80}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 52}, new int[]{0, 5}, new int[]{0, 81}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 21}, new int[]{2, 1}, new int[]{0, 82}, new int[]{0, 37}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 53}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 83}, new int[]{0, 84}, new int[]{2, 1}, new int[]{0, 69}, new int[]{0, 85}};
		
		private static int[][] ValTab8 = {new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{14, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{2, 1}, new int[]{0, 34}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 3}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 4}, new int[]{2, 1}, new int[]{0, 65}, new int[]{2, 1}, new int[]{0, 20}, new int[]{0, 66}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 36}, new int[]{2, 1}, new int[]{0, 51}, new int[]{0, 80}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 67}, new int[]{0, 52}, new int[]{0, 81}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 21}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 82}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 37}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 53}, new int[]{2, 1}, new int[]{0, 83}, new int[]{2, 1}, new int[]{0, 69}, new int[]{2, 1}, new int[]{0, 84}, new int[]{0, 85}};
		
		private static int[][] ValTab9 = {new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{0, 16}, new int[]{2, 1}, new int[]{0, 1}, new int[]{0, 17}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 33}, new int[]{2, 1}, new int[]{0, 18}, new int[]{2, 1}, new int[]{0, 2}, new int[]{0, 34}, new int[]{12, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 3}, new int[]{0, 49}, new int[]{2, 1}, new int[]{0, 19}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 65}, new int[]{0, 20}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 51}, new int[]{2, 1}, new int[]{0, 66}, new int[]{0, 36}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 4}, new int[]{0, 80}, new int[]{0, 67}, new int[]{2, 1}, new int[]{0, 52}, new int[]{0, 81}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 21}, new int[]{0, 82}, new int[]{2, 1}, new int[]{0, 37}, new int[]{0, 68}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 84}, new int[]{0, 83}, new int[]{2, 1}, new int[]{0, 53}, new int[]{2, 1}, new int[]{0, 69}, new int[]{0, 85}};
		
		private static int[][] ValTab10 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{10, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{28, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 34}, new int[]{0, 48}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 50}, new int[]{2, 1}, new int[]{0, 35}, new int[]{0, 64}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 65}, new int[]{0, 20}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 4}, new int[]{0, 51}, new int[]{2, 1}, new int[]{0, 66}, new int[]{0, 36}, new int[]{28, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 80}, new int[]{0, 5}, new int[]{0, 96}, new int[]{2, 1}, new int[]{0, 97}, new int[]{0, 22}, new int[]{12, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 67}, new int[]{0, 52}, new int[]{0, 81}, new int[]{2, 1}, new int[]{0, 21}, new int[]{2, 1}, new int[]{0, 82}, new int[]{0, 37}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 38}, new int[]{0, 54}, new int[]{0, 113}, new int[]{20, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 23}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 83}, new int[]{0, 6}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 53}, new int[]{0, 69}, new int[]{0, 98}, new int[]{2, 1}, new int[]{0, 112}, new int[]{2, 1}, new int[]{0, 7}, new int[]{0, 100}, new int[]{14, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 114}, new int[]{0, 39}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 99}, new int[]{2, 1}, new int[]{0, 84}, new int[]{0, 85}, new int[]{2, 1}, new int[]{0, 70}, new int[]{0, 115}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 55}, new int[]{0, 101}, new int[]{2, 1}, new int[]{0, 86}, new int[]{0, 116}, 
			new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 71}, new int[]{2, 1}, new int[]{0, 102}, new int[]{0, 117}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 87}, new int[]{0, 118}, new int[]{2, 1}, new int[]{0, 103}, new int[]{0, 119}};
		
		private static int[][] ValTab11 = {new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{0, 18}, new int[]{24, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 33}, new int[]{2, 1}, new int[]{0, 34}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 3}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 4}, new int[]{2, 1}, new int[]{0, 65}, new int[]{0, 20}, new int[]{30, 1}, new int[]{16, 1}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 66}, new int[]{0, 36}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 51}, new int[]{0, 67}, new int[]{0, 80}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 52}, new int[]{0, 81}, new int[]{0, 97}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 22}, new int[]{2, 1}, new int[]{0, 6}, new int[]{0, 38}, new int[]{2, 1}, new int[]{0, 98}, new int[]{2, 1}, new int[]{0, 21}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 82}, new int[]{16, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 37}, new int[]{0, 68}, new int[]{0, 96}, new int[]{2, 1}, new int[]{0, 99}, new int[]{0, 54}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 112}, new int[]{0, 23}, new int[]{0, 113}, new int[]{16, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 7}, new int[]{0, 100}, new int[]{0, 114}, new int[]{2, 1}, new int[]{0, 39}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 83}, new int[]{0, 53}, new int[]{2, 1}, new int[]{0, 84}, new int[]{0, 69}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 70}, new int[]{0, 115}, new int[]{2, 1}, new int[]{0, 55}, new int[]{2, 1}, new int[]{0, 101}, new int[]{0, 86}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, 
			new int[]{0, 85}, new int[]{0, 87}, new int[]{0, 116}, new int[]{2, 1}, new int[]{0, 71}, new int[]{0, 102}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 117}, new int[]{0, 118}, new int[]{2, 1}, new int[]{0, 103}, new int[]{0, 119}};
		
		private static int[][] ValTab12 = {new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{2, 1}, new int[]{0, 0}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{16, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 34}, new int[]{0, 49}, new int[]{2, 1}, new int[]{0, 19}, new int[]{2, 1}, new int[]{0, 48}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 64}, new int[]{26, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{2, 1}, new int[]{0, 65}, new int[]{0, 51}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 20}, new int[]{0, 66}, new int[]{2, 1}, new int[]{0, 36}, new int[]{2, 1}, new int[]{0, 4}, new int[]{0, 80}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 67}, new int[]{0, 52}, new int[]{2, 1}, new int[]{0, 81}, new int[]{0, 21}, new int[]{28, 1}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 82}, new int[]{0, 37}, new int[]{2, 1}, new int[]{0, 83}, new int[]{0, 53}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 96}, new int[]{0, 22}, new int[]{0, 97}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 98}, new int[]{0, 38}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 6}, new int[]{0, 68}, new int[]{2, 1}, new int[]{0, 84}, new int[]{0, 69}, new int[]{18, 1}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 99}, new int[]{0, 54}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 112}, new int[]{0, 7}, new int[]{0, 113}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 23}, new int[]{0, 100}, new int[]{2, 1}, new int[]{0, 70}, new int[]{0, 114}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 39}, new int[]{2, 1}, new int[]{0, 85}, new int[]{0, 115}, new int[]{2, 1}, new int[]{0, 55}, new int[]{0, 86}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 101}, 
			new int[]{0, 116}, new int[]{2, 1}, new int[]{0, 71}, new int[]{0, 102}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 117}, new int[]{0, 87}, new int[]{2, 1}, new int[]{0, 118}, new int[]{2, 1}, new int[]{0, 103}, new int[]{0, 119}};
		
		private static int[][] ValTab13 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{2, 1}, new int[]{0, 1}, new int[]{0, 17}, new int[]{28, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 34}, new int[]{0, 48}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 49}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 19}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 4}, new int[]{0, 65}, new int[]{70, 1}, new int[]{28, 1}, new int[]{14, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 20}, new int[]{2, 1}, new int[]{0, 51}, new int[]{0, 66}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 36}, new int[]{0, 80}, new int[]{2, 1}, new int[]{0, 67}, new int[]{0, 52}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 81}, new int[]{0, 21}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 82}, new int[]{2, 1}, new int[]{0, 37}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 83}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 96}, new int[]{0, 6}, new int[]{2, 1}, new int[]{0, 97}, new int[]{0, 22}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 128}, new int[]{0, 8}, new int[]{0, 129}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 53}, new int[]{0, 98}, new int[]{2, 1}, new int[]{0, 38}, new int[]{0, 84}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 69}, new int[]{0, 99}, new int[]{2, 1}, new int[]{0, 54}, new int[]{0, 112}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 7}, new int[]{0, 85}, new int[]{0, 113}, new int[]{2, 1}, new int[]{0, 23}, new int[]{2, 1}, new int[]{0, 39}, new int[]{0, 55}, new int[]{72, 1}, new int[]{24, 1}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 24}, new int[]{0, 130}, new int[]{2, 1}, 
			new int[]{0, 40}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 100}, new int[]{0, 70}, new int[]{0, 114}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 132}, new int[]{0, 72}, new int[]{2, 1}, new int[]{0, 144}, new int[]{0, 9}, new int[]{2, 1}, new int[]{0, 145}, new int[]{0, 25}, new int[]{24, 1}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 115}, new int[]{0, 101}, new int[]{2, 1}, new int[]{0, 86}, new int[]{0, 116}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 71}, new int[]{0, 102}, new int[]{0, 131}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 56}, new int[]{2, 1}, new int[]{0, 117}, new int[]{0, 87}, new int[]{2, 1}, new int[]{0, 146}, new int[]{0, 41}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 103}, new int[]{0, 133}, new int[]{2, 1}, new int[]{0, 88}, new int[]{0, 57}, new int[]{2, 1}, new int[]{0, 147}, new int[]{2, 1}, new int[]{0, 73}, new int[]{0, 134}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 160}, new int[]{2, 1}, new int[]{0, 104}, new int[]{0, 10}, new int[]{2, 1}, new int[]{0, 161}, new int[]{0, 26}, new int[]{68, 1}, new int[]{24, 1}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 162}, new int[]{0, 42}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 149}, new int[]{0, 89}, new int[]{2, 1}, new int[]{0, 163}, new int[]{0, 58}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 74}, new int[]{0, 150}, new int[]{2, 1}, new int[]{0, 176}, new int[]{0, 11}, new int[]{2, 1}, new int[]{0, 177}, new int[]{0, 27}, new int[]{20, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 178}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 118}, new int[]{0, 119}, new int[]{0, 148}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 135}, new int[]{0, 120}, new int[]{0, 164}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 105}, new int[]{0, 165}, new int[]{0, 43}, new int[]{12, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 
			90}, new int[]{0, 136}, new int[]{0, 179}, new int[]{2, 1}, new int[]{0, 59}, new int[]{2, 1}, new int[]{0, 121}, new int[]{0, 166}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 106}, new int[]{0, 180}, new int[]{0, 192}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 12}, new int[]{0, 152}, new int[]{0, 193}, new int[]{60, 1}, new int[]{22, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 28}, new int[]{2, 1}, new int[]{0, 137}, new int[]{0, 181}, new int[]{2, 1}, new int[]{0, 91}, new int[]{0, 194}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 44}, new int[]{0, 60}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 182}, new int[]{0, 107}, new int[]{2, 1}, new int[]{0, 196}, new int[]{0, 76}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 168}, new int[]{0, 138}, new int[]{2, 1}, new int[]{0, 208}, new int[]{0, 13}, new int[]{2, 1}, new int[]{0, 209}, new int[]{2, 1}, new int[]{0, 75}, new int[]{2, 1}, new int[]{0, 151}, new int[]{0, 167}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 195}, new int[]{2, 1}, new int[]{0, 122}, new int[]{0, 153}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 197}, new int[]{0, 92}, new int[]{0, 183}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 29}, new int[]{0, 210}, new int[]{2, 1}, new int[]{0, 45}, new int[]{2, 1}, new int[]{0, 123}, new int[]{0, 211}, new int[]{52, 1}, new int[]{28, 1}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 61}, new int[]{0, 198}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 108}, new int[]{0, 169}, new int[]{2, 1}, new int[]{0, 154}, new int[]{0, 212}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 184}, new int[]{0, 139}, new int[]{2, 1}, new int[]{0, 77}, new int[]{0, 199}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 124}, new int[]{0, 213}, new int[]{2, 1}, new int[]{0, 93}, new int[]{0, 224}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 225}, new int[]{0, 30}, new int[]{4, 1}
			, new int[]{2, 1}, new int[]{0, 14}, new int[]{0, 46}, new int[]{0, 226}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 227}, new int[]{0, 109}, new int[]{2, 1}, new int[]{0, 140}, new int[]{0, 228}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 229}, new int[]{0, 186}, new int[]{0, 240}, new int[]{38, 1}, new int[]{16, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 241}, new int[]{0, 31}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 170}, new int[]{0, 155}, new int[]{0, 185}, new int[]{2, 1}, new int[]{0, 62}, new int[]{2, 1}, new int[]{0, 214}, new int[]{0, 200}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 78}, new int[]{2, 1}, new int[]{0, 215}, new int[]{0, 125}, new int[]{2, 1}, new int[]{0, 171}, new int[]{2, 1}, new int[]{0, 94}, new int[]{0, 201}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 15}, new int[]{2, 1}, new int[]{0, 156}, new int[]{0, 110}, new int[]{2, 1}, new int[]{0, 242}, new int[]{0, 47}, new int[]{32, 1}, new int[]{16, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 216}, new int[]{0, 141}, new int[]{0, 63}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 243}, new int[]{2, 1}, new int[]{0, 230}, new int[]{0, 202}, new int[]{2, 1}, new int[]{0, 244}, new int[]{0, 79}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 187}, new int[]{0, 172}, new int[]{2, 1}, new int[]{0, 231}, new int[]{0, 245}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 217}, new int[]{0, 157}, new int[]{2, 1}, new int[]{0, 95}, new int[]{0, 232}, new int[]{30, 1}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 111}, new int[]{2, 1}, new int[]{0, 246}, new int[]{0, 203}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 188}, new int[]{0, 173}, new int[]{0, 218}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 247}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 126}, new int[]{0, 127}, new int[]{0, 142}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 158}, new int[]{0, 174}
			, new int[]{0, 204}, new int[]{2, 1}, new int[]{0, 248}, new int[]{0, 143}, new int[]{18, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 219}, new int[]{0, 189}, new int[]{2, 1}, new int[]{0, 234}, new int[]{0, 249}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 159}, new int[]{0, 235}, new int[]{2, 1}, new int[]{0, 190}, new int[]{2, 1}, new int[]{0, 205}, new int[]{0, 250}, new int[]{14, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 221}, new int[]{0, 236}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 233}, new int[]{0, 175}, new int[]{0, 220}, new int[]{2, 1}, new int[]{0, 206}, new int[]{0, 251}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 191}, new int[]{0, 222}, new int[]{2, 1}, new int[]{0, 207}, new int[]{0, 238}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 223}, new int[]{0, 239}, new int[]{2, 1}, new int[]{0, 255}, new int[]{2, 1}, new int[]{0, 237}, new int[]{2, 1}, new int[]{0, 253}, new int[]{2, 1}, new int[]{0, 252}, new int[]{0, 254}};
		
		private static int[][] ValTab14 = {new int[]{0, 0}};
		
		private static int[][] ValTab15 = {new int[]{16, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{2, 1}, new int[]{0, 16}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 17}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{50, 1}, new int[]{16, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 34}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 49}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 19}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 64}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{14, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 4}, new int[]{0, 20}, new int[]{0, 65}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 51}, new int[]{0, 66}, new int[]{2, 1}, new int[]{0, 36}, new int[]{0, 67}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 52}, new int[]{2, 1}, new int[]{0, 80}, new int[]{0, 5}, new int[]{2, 1}, new int[]{0, 81}, new int[]{0, 21}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 82}, new int[]{0, 37}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 83}, new int[]{0, 97}, new int[]{90, 1}, new int[]{36, 1}, new int[]{18, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 53}, new int[]{2, 1}, new int[]{0, 96}, new int[]{0, 6}, new int[]{2, 1}, new int[]{0, 22}, new int[]{0, 98}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 38}, new int[]{0, 84}, new int[]{2, 1}, new int[]{0, 69}, new int[]{0, 99}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 54}, new int[]{2, 1}, new int[]{0, 112}, new int[]{0, 7}, new int[]{2, 1}, new int[]{0, 113}, new int[]{0, 85}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 23}, new int[]{0, 100}, new int[]{2, 1}, new int[]{0, 114}, new int[]{0, 39}, new int[]{24, 1}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 70}, new int[]{0, 115}, new int[]{2, 1}, new int[]{0, 55}, new int[]{0, 101}, new int[]{4, 1}
			, new int[]{2, 1}, new int[]{0, 86}, new int[]{0, 128}, new int[]{2, 1}, new int[]{0, 8}, new int[]{0, 116}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 129}, new int[]{0, 24}, new int[]{2, 1}, new int[]{0, 130}, new int[]{0, 40}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 71}, new int[]{0, 102}, new int[]{2, 1}, new int[]{0, 131}, new int[]{0, 56}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 117}, new int[]{0, 87}, new int[]{2, 1}, new int[]{0, 132}, new int[]{0, 72}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 144}, new int[]{0, 25}, new int[]{0, 145}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 146}, new int[]{0, 118}, new int[]{2, 1}, new int[]{0, 103}, new int[]{0, 41}, new int[]{92, 1}, new int[]{36, 1}, new int[]{18, 1}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 133}, new int[]{0, 88}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 9}, new int[]{0, 119}, new int[]{0, 147}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 57}, new int[]{0, 148}, new int[]{2, 1}, new int[]{0, 73}, new int[]{0, 134}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 104}, new int[]{2, 1}, new int[]{0, 160}, new int[]{0, 10}, new int[]{2, 1}, new int[]{0, 161}, new int[]{0, 26}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 162}, new int[]{0, 42}, new int[]{2, 1}, new int[]{0, 149}, new int[]{0, 89}, new int[]{26, 1}, new int[]{14, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 163}, new int[]{2, 1}, new int[]{0, 58}, new int[]{0, 135}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 120}, new int[]{0, 164}, new int[]{2, 1}, new int[]{0, 74}, new int[]{0, 150}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 105}, new int[]{0, 176}, new int[]{0, 177}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 27}, new int[]{0, 165}, new int[]{0, 178}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 90}, new int[]{0, 43}, new int[]{2, 1}, new int[]{0, 136}, new int[]{
			0, 151}, new int[]{2, 1}, new int[]{0, 179}, new int[]{2, 1}, new int[]{0, 121}, new int[]{0, 59}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 106}, new int[]{0, 180}, new int[]{2, 1}, new int[]{0, 75}, new int[]{0, 193}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 152}, new int[]{0, 137}, new int[]{2, 1}, new int[]{0, 28}, new int[]{0, 181}, new int[]{80, 1}, new int[]{34, 1}, new int[]{16, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 91}, new int[]{0, 44}, new int[]{0, 194}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 11}, new int[]{0, 192}, new int[]{0, 166}, new int[]{2, 1}, new int[]{0, 167}, new int[]{0, 122}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 195}, new int[]{0, 60}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 12}, new int[]{0, 153}, new int[]{0, 182}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 107}, new int[]{0, 196}, new int[]{2, 1}, new int[]{0, 76}, new int[]{0, 168}, new int[]{20, 1}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 138}, new int[]{0, 197}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 208}, new int[]{0, 92}, new int[]{0, 209}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 183}, new int[]{0, 123}, new int[]{2, 1}, new int[]{0, 29}, new int[]{2, 1}, new int[]{0, 13}, new int[]{0, 45}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 210}, new int[]{0, 211}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 61}, new int[]{0, 198}, new int[]{2, 1}, new int[]{0, 108}, new int[]{0, 169}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 154}, new int[]{0, 184}, new int[]{0, 212}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 139}, new int[]{0, 77}, new int[]{2, 1}, new int[]{0, 199}, new int[]{0, 124}, new int[]{68, 1}, new int[]{34, 1}, new int[]{18, 1}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 213}, new int[]{0, 93}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 224}, new int[]{0, 14}, new int[]{0, 
			225}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 30}, new int[]{0, 226}, new int[]{2, 1}, new int[]{0, 170}, new int[]{0, 46}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 185}, new int[]{0, 155}, new int[]{2, 1}, new int[]{0, 227}, new int[]{0, 214}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 109}, new int[]{0, 62}, new int[]{2, 1}, new int[]{0, 200}, new int[]{0, 140}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 228}, new int[]{0, 78}, new int[]{2, 1}, new int[]{0, 215}, new int[]{0, 125}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 229}, new int[]{0, 186}, new int[]{2, 1}, new int[]{0, 171}, new int[]{0, 94}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 201}, new int[]{0, 156}, new int[]{2, 1}, new int[]{0, 241}, new int[]{0, 31}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 240}, new int[]{0, 110}, new int[]{0, 242}, new int[]{2, 1}, new int[]{0, 47}, new int[]{0, 230}, new int[]{38, 1}, new int[]{18, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 216}, new int[]{0, 243}, new int[]{2, 1}, new int[]{0, 63}, new int[]{0, 244}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 79}, new int[]{2, 1}, new int[]{0, 141}, new int[]{0, 217}, new int[]{2, 1}, new int[]{0, 187}, new int[]{0, 202}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 172}, new int[]{0, 231}, new int[]{2, 1}, new int[]{0, 126}, new int[]{0, 245}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 157}, new int[]{0, 95}, new int[]{2, 1}, new int[]{0, 232}, new int[]{0, 142}, new int[]{2, 1}, new int[]{0, 246}, new int[]{0, 203}, new int[]{34, 1}, new int[]{18, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 15}, new int[]{0, 174}, new int[]{0, 111}, new int[]{2, 1}, new int[]{0, 188}, new int[]{0, 218}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 173}, new int[]{0, 247}, new int[]{2, 1}, new int[]{0, 127}, new int[]{0, 233}, new int[]{8
			, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 158}, new int[]{0, 204}, new int[]{2, 1}, new int[]{0, 248}, new int[]{0, 143}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 219}, new int[]{0, 189}, new int[]{2, 1}, new int[]{0, 234}, new int[]{0, 249}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 159}, new int[]{0, 220}, new int[]{2, 1}, new int[]{0, 205}, new int[]{0, 235}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 190}, new int[]{0, 250}, new int[]{2, 1}, new int[]{0, 175}, new int[]{0, 221}, new int[]{14, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 236}, new int[]{0, 206}, new int[]{0, 251}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 191}, new int[]{0, 237}, new int[]{2, 1}, new int[]{0, 222}, new int[]{0, 252}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 207}, new int[]{0, 253}, new int[]{0, 238}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 223}, new int[]{0, 254}, new int[]{2, 1}, new int[]{0, 239}, new int[]{0, 255}};
		
		private static int[][] ValTab16 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 16}, new int[]{2, 1}, new int[]{0, 1}, new int[]{0, 17}, new int[]{42, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{2, 1}, new int[]{0, 33}, new int[]{0, 18}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 34}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 3}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 4}, new int[]{0, 65}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 20}, new int[]{2, 1}, new int[]{0, 51}, new int[]{0, 66}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 36}, new int[]{0, 80}, new int[]{2, 1}, new int[]{0, 67}, new int[]{0, 52}, new int[]{138, 1}, new int[]{40, 1}, new int[]{16, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 21}, new int[]{0, 81}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 82}, new int[]{0, 37}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 53}, new int[]{0, 83}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 96}, new int[]{0, 6}, new int[]{0, 97}, new int[]{2, 1}, new int[]{0, 22}, new int[]{0, 98}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 38}, new int[]{0, 84}, new int[]{2, 1}, new int[]{0, 69}, new int[]{0, 99}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 54}, new int[]{0, 112}, new int[]{0, 113}, new int[]{40, 1}, new int[]{18, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 23}, new int[]{2, 1}, new int[]{0, 7}, new int[]{2, 1}, new int[]{0, 85}, new int[]{0, 100}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 114}, new int[]{0, 39}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 70}, new int[]{0, 101}, new int[]{0, 115}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 55}
			, new int[]{2, 1}, new int[]{0, 86}, new int[]{0, 8}, new int[]{2, 1}, new int[]{0, 128}, new int[]{0, 129}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 24}, new int[]{2, 1}, new int[]{0, 116}, new int[]{0, 71}, new int[]{2, 1}, new int[]{0, 130}, new int[]{2, 1}, new int[]{0, 40}, new int[]{0, 102}, new int[]{24, 1}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 131}, new int[]{0, 56}, new int[]{2, 1}, new int[]{0, 117}, new int[]{0, 132}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 72}, new int[]{0, 144}, new int[]{0, 145}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 25}, new int[]{2, 1}, new int[]{0, 9}, new int[]{0, 118}, new int[]{2, 1}, new int[]{0, 146}, new int[]{0, 41}, new int[]{14, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 133}, new int[]{0, 88}, new int[]{2, 1}, new int[]{0, 147}, new int[]{0, 57}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 160}, new int[]{0, 10}, new int[]{0, 26}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 162}, new int[]{2, 1}, new int[]{0, 103}, new int[]{2, 1}, new int[]{0, 87}, new int[]{0, 73}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 148}, new int[]{2, 1}, new int[]{0, 119}, new int[]{0, 134}, new int[]{2, 1}, new int[]{0, 161}, new int[]{2, 1}, new int[]{0, 104}, new int[]{0, 149}, new int[]{220, 1}, new int[]{126, 1}, new int[]{50, 1}, new int[]{26, 1}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 42}, new int[]{2, 1}, new int[]{0, 89}, new int[]{0, 58}, new int[]{2, 1}, new int[]{0, 163}, new int[]{2, 1}, new int[]{0, 135}, new int[]{0, 120}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 164}, new int[]{0, 74}, new int[]{2, 1}, new int[]{0, 150}, new int[]{0, 105}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 176}, new int[]{0, 11}, new int[]{0, 177}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 27}, new int[]{0, 178}, new int[]{2, 1}, new int[]{0, 43}, new int[]{2, 1}, new int[]{0, 165}, new int[]{0, 90}, new int[]
			{6, 1}, new int[]{2, 1}, new int[]{0, 179}, new int[]{2, 1}, new int[]{0, 166}, new int[]{0, 106}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 180}, new int[]{0, 75}, new int[]{2, 1}, new int[]{0, 12}, new int[]{0, 193}, new int[]{30, 1}, new int[]{14, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 181}, new int[]{0, 194}, new int[]{0, 44}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 167}, new int[]{0, 195}, new int[]{2, 1}, new int[]{0, 107}, new int[]{0, 196}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 29}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 136}, new int[]{0, 151}, new int[]{0, 59}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 209}, new int[]{0, 210}, new int[]{2, 1}, new int[]{0, 45}, new int[]{0, 211}, new int[]{18, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 30}, new int[]{0, 46}, new int[]{0, 226}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 121}, new int[]{0, 152}, new int[]{0, 192}, new int[]{2, 1}, new int[]{0, 28}, new int[]{2, 1}, new int[]{0, 137}, new int[]{0, 91}, new int[]{14, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 60}, new int[]{2, 1}, new int[]{0, 122}, new int[]{0, 182}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 76}, new int[]{0, 153}, new int[]{2, 1}, new int[]{0, 168}, new int[]{0, 138}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 13}, new int[]{2, 1}, new int[]{0, 197}, new int[]{0, 92}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 61}, new int[]{0, 198}, new int[]{2, 1}, new int[]{0, 108}, new int[]{0, 154}, new int[]{88, 1}, new int[]{86, 1}, new int[]{36, 1}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 139}, new int[]{0, 77}, new int[]{2, 1}, new int[]{0, 199}, new int[]{0, 124}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 213}, new int[]{0, 93}, new int[]{2, 1}, new int[]{0, 224}, new int[]{0, 14}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 227}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 208}, new int[]{0, 183}, 
			new int[]{0, 123}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 169}, new int[]{0, 184}, new int[]{0, 212}, new int[]{2, 1}, new int[]{0, 225}, new int[]{2, 1}, new int[]{0, 170}, new int[]{0, 185}, new int[]{24, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 155}, new int[]{0, 214}, new int[]{0, 109}, new int[]{2, 1}, new int[]{0, 62}, new int[]{0, 200}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 140}, new int[]{0, 228}, new int[]{0, 78}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 215}, new int[]{0, 229}, new int[]{2, 1}, new int[]{0, 186}, new int[]{0, 171}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 156}, new int[]{0, 230}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 110}, new int[]{0, 216}, new int[]{2, 1}, new int[]{0, 141}, new int[]{0, 187}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 231}, new int[]{0, 157}, new int[]{2, 1}, new int[]{0, 232}, new int[]{0, 142}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 203}, new int[]{0, 188}, new int[]{0, 158}, new int[]{0, 241}, new int[]{2, 1}, new int[]{0, 31}, new int[]{2, 1}, new int[]{0, 15}, new int[]{0, 47}, new int[]{66, 1}, new int[]{56, 1}, new int[]{2, 1}, new int[]{0, 242}, new int[]{52, 1}, new int[]{50, 1}, new int[]{20, 1}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 189}, new int[]{2, 1}, new int[]{0, 94}, new int[]{2, 1}, new int[]{0, 125}, new int[]{0, 201}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 202}, new int[]{2, 1}, new int[]{0, 172}, new int[]{0, 126}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 218}, new int[]{0, 173}, new int[]{0, 204}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 174}, new int[]{2, 1}, new int[]{0, 219}, new int[]{0, 220}, new int[]{2, 1}, new int[]{0, 205}, new int[]{0, 190}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 235}, new int[]{0, 237}, new int[]{0, 238}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 
			217}, new int[]{0, 234}, new int[]{0, 233}, new int[]{2, 1}, new int[]{0, 222}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 221}, new int[]{0, 236}, new int[]{0, 206}, new int[]{0, 63}, new int[]{0, 240}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 243}, new int[]{0, 244}, new int[]{2, 1}, new int[]{0, 79}, new int[]{2, 1}, new int[]{0, 245}, new int[]{0, 95}, new int[]{10, 1}, new int[]{2, 1}, new int[]{0, 255}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 246}, new int[]{0, 111}, new int[]{2, 1}, new int[]{0, 247}, new int[]{0, 127}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 143}, new int[]{2, 1}, new int[]{0, 248}, new int[]{0, 249}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 159}, new int[]{0, 250}, new int[]{0, 175}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 251}, new int[]{0, 191}, new int[]{2, 1}, new int[]{0, 252}, new int[]{0, 207}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 253}, new int[]{0, 223}, new int[]{2, 1}, new int[]{0, 254}, new int[]{0, 239}};
		
		private static int[][] ValTab24 = {new int[]{60, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{0, 16}, new int[]{2, 1}, new int[]{0, 1}, new int[]{0, 17}, new int[]{14, 1}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 32}, new int[]{0, 2}, new int[]{0, 33}, new int[]{2, 1}, new int[]{0, 18}, new int[]{2, 1}, new int[]{0, 34}, new int[]{2, 1}, new int[]{0, 48}, new int[]{0, 3}, new int[]{14, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 49}, new int[]{0, 19}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 50}, new int[]{0, 35}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 64}, new int[]{0, 4}, new int[]{0, 65}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 20}, new int[]{0, 51}, new int[]{2, 1}, new int[]{0, 66}, new int[]{0, 36}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 67}, new int[]{0, 52}, new int[]{0, 81}, new int[]{6, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 80}, new int[]{0, 5}, new int[]{0, 21}, new int[]{2, 1}, new int[]{0, 82}, new int[]{0, 37}, new int[]{250, 1}, new int[]{98, 1}, new int[]{34, 1}, new int[]{18, 1}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 68}, new int[]{0, 83}, new int[]{2, 1}, new int[]{0, 53}, new int[]{2, 1}, new int[]{0, 96}, new int[]{0, 6}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 97}, new int[]{0, 22}, new int[]{2, 1}, new int[]{0, 98}, new int[]{0, 38}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 84}, new int[]{0, 69}, new int[]{2, 1}, new int[]{0, 99}, new int[]{0, 54}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 113}, new int[]{0, 85}, new int[]{2, 1}, new int[]{0, 100}, new int[]{0, 70}, new int[]{32, 1}, new int[]{14, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 114}, new int[]{2, 1}, new int[]{0, 39}, new int[]{0, 55}, new int[]{2, 1}, new int[]{0, 115}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 112}, new int[]{0, 7}, new int[]{0, 23}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, 
			new int[]{0, 101}, new int[]{0, 86}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 128}, new int[]{0, 8}, new int[]{0, 129}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 116}, new int[]{0, 71}, new int[]{2, 1}, new int[]{0, 24}, new int[]{0, 130}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 40}, new int[]{0, 102}, new int[]{2, 1}, new int[]{0, 131}, new int[]{0, 56}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 117}, new int[]{0, 87}, new int[]{2, 1}, new int[]{0, 132}, new int[]{0, 72}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 145}, new int[]{0, 25}, new int[]{2, 1}, new int[]{0, 146}, new int[]{0, 118}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 103}, new int[]{0, 41}, new int[]{2, 1}, new int[]{0, 133}, new int[]{0, 88}, new int[]{92, 1}, new int[]{34, 1}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 147}, new int[]{0, 57}, new int[]{2, 1}, new int[]{0, 148}, new int[]{0, 73}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 119}, new int[]{0, 134}, new int[]{2, 1}, new int[]{0, 104}, new int[]{0, 161}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 162}, new int[]{0, 42}, new int[]{2, 1}, new int[]{0, 149}, new int[]{0, 89}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 163}, new int[]{0, 58}, new int[]{2, 1}, new int[]{0, 135}, new int[]{2, 1}, new int[]{0, 120}, new int[]{0, 74}, new int[]{22, 1}, new int[]{12, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 164}, new int[]{0, 150}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 105}, new int[]{0, 177}, new int[]{2, 1}, new int[]{0, 27}, new int[]{0, 165}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 178}, new int[]{2, 1}, new int[]{0, 90}, new int[]{0, 43}, new int[]{2, 1}, new int[]{0, 136}, new int[]{0, 179}, new int[]{16, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 144}, new int[]{2, 1}, new int[]{0, 9}, new int[]{0, 160}, new int[]{2, 1}, new int[]{0, 151}, new int[]{0, 121}, new int[]
			{4, 1}, new int[]{2, 1}, new int[]{0, 166}, new int[]{0, 106}, new int[]{0, 180}, new int[]{12, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 26}, new int[]{2, 1}, new int[]{0, 10}, new int[]{0, 176}, new int[]{2, 1}, new int[]{0, 59}, new int[]{2, 1}, new int[]{0, 11}, new int[]{0, 192}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 75}, new int[]{0, 193}, new int[]{2, 1}, new int[]{0, 152}, new int[]{0, 137}, new int[]{67, 1}, new int[]{34, 1}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 28}, new int[]{0, 181}, new int[]{2, 1}, new int[]{0, 91}, new int[]{0, 194}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 44}, new int[]{0, 167}, new int[]{2, 1}, new int[]{0, 122}, new int[]{0, 195}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 60}, new int[]{2, 1}, new int[]{0, 12}, new int[]{0, 208}, new int[]{2, 1}, new int[]{0, 182}, new int[]{0, 107}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 196}, new int[]{0, 76}, new int[]{2, 1}, new int[]{0, 153}, new int[]{0, 168}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 138}, new int[]{0, 197}, new int[]{2, 1}, new int[]{0, 92}, new int[]{0, 209}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 183}, new int[]{0, 123}, new int[]{2, 1}, new int[]{0, 29}, new int[]{0, 210}, new int[]{9, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 45}, new int[]{0, 211}, new int[]{2, 1}, new int[]{0, 61}, new int[]{0, 198}, new int[]{85, 250}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 108}, new int[]{0, 169}, new int[]{2, 1}, new int[]{0, 154}, new int[]{0, 212}, new int[]{32, 1}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 184}, new int[]{0, 139}, new int[]{2, 1}, new int[]{0, 77}, new int[]{0, 199}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 124}, new int[]{0, 213}, new int[]{2, 1}, new int[]{0, 93}, new int[]{0, 225}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 30}, new int[]{0, 226}, new int[]{2, 1
			}, new int[]{0, 170}, new int[]{0, 185}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 155}, new int[]{0, 227}, new int[]{2, 1}, new int[]{0, 214}, new int[]{0, 109}, new int[]{20, 1}, new int[]{10, 1}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 62}, new int[]{2, 1}, new int[]{0, 46}, new int[]{0, 78}, new int[]{2, 1}, new int[]{0, 200}, new int[]{0, 140}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 228}, new int[]{0, 215}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 125}, new int[]{0, 171}, new int[]{0, 229}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 186}, new int[]{0, 94}, new int[]{2, 1}, new int[]{0, 201}, new int[]{2, 1}, new int[]{0, 156}, new int[]{0, 110}, new int[]{8, 1}, new int[]{2, 1}, new int[]{0, 230}, new int[]{2, 1}, new int[]{0, 13}, new int[]{2, 1}, new int[]{0, 224}, new int[]{0, 14}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 216}, new int[]{0, 141}, new int[]{2, 1}, new int[]{0, 187}, new int[]{0, 202}, new int[]{74, 1}, new int[]{2, 1}, new int[]{0, 255}, new int[]{64, 1}, new int[]{58, 1}, new int[]{32, 1}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 172}, new int[]{0, 231}, new int[]{2, 1}, new int[]{0, 126}, new int[]{0, 217}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 157}, new int[]{0, 232}, new int[]{2, 1}, new int[]{0, 142}, new int[]{0, 203}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 188}, new int[]{0, 218}, new int[]{2, 1}, new int[]{0, 173}, new int[]{0, 233}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 158}, new int[]{0, 204}, new int[]{2, 1}, new int[]{0, 219}, new int[]{0, 189}, new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 234}, new int[]{0, 174}, new int[]{2, 1}, new int[]{0, 220}, new int[]{0, 205}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 235}, new int[]{0, 190}, new int[]{2, 1}, new int[]{0, 221}, new int[]{0, 236}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 206}, new int[]{0, 237}, new int[]
			{2, 1}, new int[]{0, 222}, new int[]{0, 238}, new int[]{0, 15}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 240}, new int[]{0, 31}, new int[]{0, 241}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 242}, new int[]{0, 47}, new int[]{2, 1}, new int[]{0, 243}, new int[]{0, 63}, new int[]{18, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 244}, new int[]{0, 79}, new int[]{2, 1}, new int[]{0, 245}, new int[]{0, 95}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 246}, new int[]{0, 111}, new int[]{2, 1}, new int[]{0, 247}, new int[]{2, 1}, new int[]{0, 127}, new int[]{0, 143}, new int[]{10, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 248}, new int[]{0, 249}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 159}, new int[]{0, 175}, new int[]{0, 250}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 251}, new int[]{0, 191}, new int[]{2, 1}, new int[]{0, 252}, new int[]{0, 207}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 253}, new int[]{0, 223}, new int[]{2, 1}, new int[]{0, 254}, new int[]{0, 239}};
		
		private static int[][] ValTab32 = {new int[]{2, 1}, new int[]{0, 0}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 8}, new int[]{0, 4}, new int[]{2, 1}, new int[]{0, 1}, new int[]{0, 2}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 12}, new int[]{0, 10}, new int[]{2, 1}, new int[]{0, 3}, new int[]{0, 6}, new int[]{6, 1}, new int[]{2, 1}, new int[]{0, 9}, new int[]{2, 1}, new int[]{0, 5}, new int[]{0, 7}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 14}, new int[]{0, 13}, new int[]{2, 1}, new int[]{0, 15}, new int[]{0, 11}};
		
		private static int[][] ValTab33 = {new int[]{16, 1}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 0}, new int[]{0, 1}, new int[]{2, 1}, new int[]{0, 2}, new int[]{0, 3}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 4}, new int[]{0, 5}, new int[]{2, 1}, new int[]{0, 6}, new int[]{0, 7}, new int[]{8, 1}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 8}, new int[]{0, 9}, new int[]{2, 1}, new int[]{0, 10}, new int[]{0, 11}, new int[]{4, 1}, new int[]{2, 1}, new int[]{0, 12}, new int[]{0, 13}, new int[]{2, 1}, new int[]{0, 14}, new int[]{0, 15}};
		
		
		public static huffcodetab[] ht = null; /* Simulate extern struct                 */
		
		private static int[] bitbuf;
		
		/// <summary> Big Constructor : Computes all Huffman Tables.
		/// </summary>
		private huffcodetab(System.String S, int XLEN, int YLEN, int LINBITS, int LINMAX, int REF, int[] TABLE, int[] HLEN, int[][] VAL, int TREELEN)
		{
			tablename0 = S[0];
			tablename1 = S[1];
			tablename2 = S[2];
			xlen = XLEN;
			ylen = YLEN;
			linbits = LINBITS;
			linmax = LINMAX;
			ref_Renamed = REF;
			table = TABLE;
			hlen = HLEN;
			val = VAL;
			treelen = TREELEN;
		}
		
		
		
		/// <summary> Do the huffman-decoding.
		/// note! for counta,countb -the 4 bit value is returned in y,
		/// discard x.
		/// </summary>
		public static int huffman_decoder(huffcodetab h, int[] x, int[] y, int[] v, int[] w, BitReserve br)
		{
			// array of all huffcodtable headers
			// 0..31 Huffman code table 0..31
			// 32,33 count1-tables
			
			int dmask = 1 << ((4 * 8) - 1);
			int hs = 4 * 8;
			int level;
			int point = 0;
			int error = 1;
			level = dmask;
			
			if (h.val == null)
				return 2;
			
			/* table 0 needs no bits */
			if (h.treelen == 0)
			{
				x[0] = y[0] = 0;
				return 0;
			}
			
			/* Lookup in Huffman table. */
			
			/*int bitsAvailable = 0;	 
			int bitIndex = 0;
			
			int bits[] = bitbuf;*/
			do 
			{
				if (h.val[point][0] == 0)
				{
					/*end of tree*/
					x[0] = SupportClass.URShift(h.val[point][1], 4);
					y[0] = h.val[point][1] & 0xf;
					error = 0;
					break;
				}
				
				// hget1bit() is called thousands of times, and so needs to be
				// ultra fast. 
				/*
				if (bitIndex==bitsAvailable)
				{
				bitsAvailable = br.readBits(bits, 32);			
				bitIndex = 0;
				}
				*/
				//if (bits[bitIndex++]!=0)
				if (br.hget1bit() != 0)
				{
					while (h.val[point][1] >= MXOFF)
						point += h.val[point][1];
					point += h.val[point][1];
				}
				else
				{
					while (h.val[point][0] >= MXOFF)
						point += h.val[point][0];
					point += h.val[point][0];
				}
				level = SupportClass.URShift(level, 1);
				// MDM: ht[0] is always 0;
			}
			while ((level != 0) || (point < 0));
			
			// put back any bits not consumed
			/*	
			int unread = (bitsAvailable-bitIndex);
			if (unread>0)
			br.rewindNbits(unread);
			*/
			/* Process sign encodings for quadruples tables. */
			// System.out.println(h.tablename);
			if (h.tablename0 == '3' && (h.tablename1 == '2' || h.tablename1 == '3'))
			{
				v[0] = (y[0] >> 3) & 1;
				w[0] = (y[0] >> 2) & 1;
				x[0] = (y[0] >> 1) & 1;
				y[0] = y[0] & 1;
				
				/* v, w, x and y are reversed in the bitstream.
				switch them around to make test bistream work. */
				
				if (v[0] != 0)
					if (br.hget1bit() != 0)
						v[0] = - v[0];
				if (w[0] != 0)
					if (br.hget1bit() != 0)
						w[0] = - w[0];
				if (x[0] != 0)
					if (br.hget1bit() != 0)
						x[0] = - x[0];
				if (y[0] != 0)
					if (br.hget1bit() != 0)
						y[0] = - y[0];
			}
			else
			{
				// Process sign and escape encodings for dual tables.
				// x and y are reversed in the test bitstream.
				// Reverse x and y here to make test bitstream work.
				
				if (h.linbits != 0)
					if ((h.xlen - 1) == x[0])
						x[0] += br.hgetbits(h.linbits);
				if (x[0] != 0)
					if (br.hget1bit() != 0)
						x[0] = - x[0];
				if (h.linbits != 0)
					if ((h.ylen - 1) == y[0])
						y[0] += br.hgetbits(h.linbits);
				if (y[0] != 0)
					if (br.hget1bit() != 0)
						y[0] = - y[0];
			}
			return error;
		}
		
		public static void  inithuff()
		{
			
			if (ht != null)
				return ;
			
			ht = new huffcodetab[HTN];
			ht[0] = new huffcodetab("0  ", 0, 0, 0, 0, - 1, null, null, ValTab0, 0);
			ht[1] = new huffcodetab("1  ", 2, 2, 0, 0, - 1, null, null, ValTab1, 7);
			ht[2] = new huffcodetab("2  ", 3, 3, 0, 0, - 1, null, null, ValTab2, 17);
			ht[3] = new huffcodetab("3  ", 3, 3, 0, 0, - 1, null, null, ValTab3, 17);
			ht[4] = new huffcodetab("4  ", 0, 0, 0, 0, - 1, null, null, ValTab4, 0);
			ht[5] = new huffcodetab("5  ", 4, 4, 0, 0, - 1, null, null, ValTab5, 31);
			ht[6] = new huffcodetab("6  ", 4, 4, 0, 0, - 1, null, null, ValTab6, 31);
			ht[7] = new huffcodetab("7  ", 6, 6, 0, 0, - 1, null, null, ValTab7, 71);
			ht[8] = new huffcodetab("8  ", 6, 6, 0, 0, - 1, null, null, ValTab8, 71);
			ht[9] = new huffcodetab("9  ", 6, 6, 0, 0, - 1, null, null, ValTab9, 71);
			ht[10] = new huffcodetab("10 ", 8, 8, 0, 0, - 1, null, null, ValTab10, 127);
			ht[11] = new huffcodetab("11 ", 8, 8, 0, 0, - 1, null, null, ValTab11, 127);
			ht[12] = new huffcodetab("12 ", 8, 8, 0, 0, - 1, null, null, ValTab12, 127);
			ht[13] = new huffcodetab("13 ", 16, 16, 0, 0, - 1, null, null, ValTab13, 511);
			ht[14] = new huffcodetab("14 ", 0, 0, 0, 0, - 1, null, null, ValTab14, 0);
			ht[15] = new huffcodetab("15 ", 16, 16, 0, 0, - 1, null, null, ValTab15, 511);
			ht[16] = new huffcodetab("16 ", 16, 16, 1, 1, - 1, null, null, ValTab16, 511);
			ht[17] = new huffcodetab("17 ", 16, 16, 2, 3, 16, null, null, ValTab16, 511);
			ht[18] = new huffcodetab("18 ", 16, 16, 3, 7, 16, null, null, ValTab16, 511);
			ht[19] = new huffcodetab("19 ", 16, 16, 4, 15, 16, null, null, ValTab16, 511);
			ht[20] = new huffcodetab("20 ", 16, 16, 6, 63, 16, null, null, ValTab16, 511);
			ht[21] = new huffcodetab("21 ", 16, 16, 8, 255, 16, null, null, ValTab16, 511);
			ht[22] = new huffcodetab("22 ", 16, 16, 10, 1023, 16, null, null, ValTab16, 511);
			ht[23] = new huffcodetab("23 ", 16, 16, 13, 8191, 16, null, null, ValTab16, 511);
			ht[24] = new huffcodetab("24 ", 16, 16, 4, 15, - 1, null, null, ValTab24, 512);
			ht[25] = new huffcodetab("25 ", 16, 16, 5, 31, 24, null, null, ValTab24, 512);
			ht[26] = new huffcodetab("26 ", 16, 16, 6, 63, 24, null, null, ValTab24, 512);
			ht[27] = new huffcodetab("27 ", 16, 16, 7, 127, 24, null, null, ValTab24, 512);
			ht[28] = new huffcodetab("28 ", 16, 16, 8, 255, 24, null, null, ValTab24, 512);
			ht[29] = new huffcodetab("29 ", 16, 16, 9, 511, 24, null, null, ValTab24, 512);
			ht[30] = new huffcodetab("30 ", 16, 16, 11, 2047, 24, null, null, ValTab24, 512);
			ht[31] = new huffcodetab("31 ", 16, 16, 13, 8191, 24, null, null, ValTab24, 512);
			ht[32] = new huffcodetab("32 ", 1, 16, 0, 0, - 1, null, null, ValTab32, 31);
			ht[33] = new huffcodetab("33 ", 1, 16, 0, 0, - 1, null, null, ValTab33, 31);
		}
		static huffcodetab()
		{
			bitbuf = new int[32];
		}
	}
}