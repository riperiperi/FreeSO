/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Iffinator.Flash
{
    public class BHAV : IffChunk
    {
        private int m_ChunkID;

        private List<byte[]> m_Instructions;
        private ushort m_Field1;        //Unknown
        private int m_Field2;           //Unknown
        private ushort m_Signature;     //The header signature.
        private byte m_HeaderLength;    //The length of the header.
        private byte m_NumInstructions;
        private byte m_Type;            //Usually 0, but may be 1, 2, 3, or 22.
        private byte m_Params;          //Number of parameters.
        private ushort m_Locals;        //Number of stack locals.
        private ushort m_Flags;         //Attributes set for this subroutine.    

        /// <summary>
        /// The instructions in this BHAV.
        /// </summary>
        public List<byte[]> Instructions
        {
            get { return m_Instructions; }
        }

        /// <summary>
        /// The length of the header of this BHAV.
        /// </summary>
        public byte HeaderLength
        {
            get { return m_HeaderLength; }
        }

        /// <summary>
        /// The number of instructions in this BHAV.
        /// </summary>
        public byte NumInstructions
        {
            get { return m_NumInstructions; }
        }

        /// <summary>
        /// The number of stack locals in this BHAV.
        /// </summary>
        public ushort Locals
        {
            get { return m_Locals; }
        }

        /// <summary>
        /// The number of parameters in this BHAV.
        /// </summary>
        public byte Params
        {
            get { return m_Params; }
        }

        /// <summary>
        /// The chunk ID of this BHAV chunk.
        /// </summary>
        public int ChunkID
        {
            get { return m_ChunkID; }
        }

        public BHAV(IffChunk Chunk) : base(Chunk)
        {
            m_ChunkID = ChunkID;

            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Instructions = new List<byte[]>();

            byte[] Header = Reader.ReadBytes(12);

            m_Signature = (ushort)((Header[1] << 8) | Header[0]);
            int Count = 0;

            switch (m_Signature)
            {
                case 0x8000: case 0x8001:
                    m_HeaderLength = 12;

                    Count = (Header[3] << 8) | Header[4];
                    m_Locals = 0; //No locals in code; added in 8002?
                    m_Params = 4; //I can't find this in header; always 4?
                    break;
                case 0x8002:
                    m_HeaderLength = 12;

                    Count = (Header[3] << 8) | Header[4];
                    m_Type = Header[5];
                    m_Params = Header[6];
                    m_Locals = (ushort)((Header[7] << 8) | Header[8]);
                    m_Flags = (ushort)((Header[9] << 8) | Header[10]);
                    //Byte 11 and 12 are set to 0 in this version, we have no use for them.
                    break;
                case 0x8003:
                    m_HeaderLength = 13;

                    byte LastHeaderByte = Reader.ReadByte();
                    Count = (((((LastHeaderByte << 8) | Header[11]) << 8) | Header[10]) << 8) | Header[9];

                    m_Type = Header[3];
                    m_Params = Header[4];
                    m_Locals = Header[5];
                    //Byte number 6 and 7 are unknown in this version, we have no use for them.
                    m_Flags = (ushort)((Header[8] << 8) | Header[9]);
                    break;
            }

            m_NumInstructions = (byte)Count;

            if (Count <= 0 || Count > 253)
                return;

            //Read all the instructions...
            for (int i = 0; i < Count; i++)
                m_Instructions.Add(Reader.ReadBytes(12));
        }
    }

    public struct BHAVBlock
    {
        public byte Flow; //Next reordered instruction.
        public byte Loc;  //Location after resequencing.
        public byte Type; //Flow type.
        public byte Pop;  //How much to outdent.
    }

    public enum BHAVFlows
    {
        FlowUnknown,	// Not yet analyzed
        FlowOrphan,		// Unused instruction
        FlowNormal,		// Both branches expressed
        FlowThen,		// Suppress false, next is "then"
        FlowElse,		// Suppress true, next is "then"
        FlowElse_t,		// Suppress true, next is "else"
        FlowElse_f,		// Suppress false, next is "else"
        FlowElse_x,		// Suppress neither, next is "else"
    }

    struct IFFDecode
    {
        public byte[] Instruction;      //Instruction being decoded.
        public StringBuilder OutStream; //Output stream for decoded info.
        //public Iff IFF;                 //Local context for decoding.

        public IFFDecode(byte[] InstructionSet/*, Iff LocalFile*/)
        {
            Instruction = InstructionSet;
            OutStream = new StringBuilder();
            //IFF = LocalFile;
        }

        /// <summary>
        /// Returns the operand starting at the indicated offset.
        /// </summary>
        /// <param name="i">The offset to start at.</param>
        public int Operand(int i)
        {
            return (Instruction[i + 1] << 8) | Instruction[i];
        }

        /// <summary>
        /// Returns the character at the indicated offset.
        /// </summary>
        /// <param name="i">The offset to start at.</param>
        public char this[int i]
        {
            get { return (char)Instruction[i]; }
        }
    }

    class BHAVAnalyzer
    {
        private BHAV m_Instructions;        //Instructions being analyzed.
        private List<BHAVBlock> m_Blocks;   //One for each instruction.
        private Iff m_MyFile;               //IFF file in which BHAV occurs.
        private Iff m_SemiGlobal;           //Related semi-global IFF file.

        public BHAVAnalyzer(Iff IffFile)
        {
            m_MyFile = IffFile;
        }

        #region GlobalSubs

        private string[] m_GlobalSubs = new string[]
        {
	        /* 256 */ "set graphic",
	        /* 257 */ "IncComfort-g",
	        /* 258 */ "random motion",
	        /* 259 */ "old idle",
	        /* 260 */ "set object graphic",
	        /* 261 */ "SetComfort",
	        /* 262 */ "set direction",
	        /* 263 */ "Find Closest Person",
	        /* 264 */ "test for user interrupt",
	        /* 265 */ "hide menu",
	        /* 266 */ "SetEnergy",
	        /* 267 */ "SetStress",
	        /* 268 */ "SetSocial",
	        /* 269 */ "SetEntertained",
	        /* 270 */ "SetEnvironment",
	        /* 271 */ "Init Object",
	        /* 272 */ "Standard entry",
	        /* 273 */ "Standard Exit",
	        /* 274 */ "Move forward n tiles (n)",
	        /* 275 */ "set object lighting",
	        /* 276 */ "Test Behind",
	        /* 277 */ "set multi-object graphic",
	        /* 278 */ "standard multi-tile entry",
	        /* 279 */ "standard multi-tile exit",
	        /* 280 */ "idle",
	        /* 281 */ "Wait For Notify",
	        /* 282 */ "divide tick counts",
	        /* 283 */ "go over and wait around",
	        /* 284 */ "fail food chain object",
	        /* 285 */ "Fun Object Test",
	        /* 286 */ "show skill progress",
	        /* 287 */ "verify stack obj ID",
	        /* 288 */ "In Use?",
	        /* 289 */ "mod8",
	        /* 290 */ "old Wait for Notify",
	        /* 291 */ "try fliiping object",
	        /* 292 */ "create multiple floods in front of object",
	        /* 293 */ "create multiple floods under me",
	        /* 294 */ "privacy - ask Sims to Shoo",
	        /* 295 */ "flip Sim around",
	        /* 296 */ "schedule phone call with neighbor",
	        /* 297 */ "help display - broken object",
	        /* 298 */ "censor",
	        /* 299 */ "SetComfortFromMain",
	        /* 300 */ "SetComfortFromInt",
	        /* 301 */ "is object's room outside",
	        /* 302 */ "Clean",
	        /* 303 */ "Clean test",
	        /* 304 */ "clean inc",
	        /* 305 */ "my dirt inc",
	        /* 306 */ "Check Schedule",
	        /* 307 */ "Schedule",
	        /* 308 */ "set sprite visibility",
	        /* 309 */ "help display - autosnapshot",
	        /* 310 */ "help display - object dirty",
	        /* 311 */ "help display - route failure",
	        /* 312 */ "privacy - should ignore person",
	        /* 313 */ "do the drop",
	        /* 314 */ "Is Stack Object held by person?",
	        /* 315 */ "sfx Hit Person",
	        /* 316 */ "do grim reaper[LL]",
	        /* 317 */ "create reaper[LL]",
	        /* 318 */ "Am I a clone?[LL]",
	        /* 319 */ "Robot Sound?[LL]",
	        /* 320 */ "Is lot a Downtown Lot?[HD]",
	        /* 321 */ "Is lot a Residential Lot?[HD]",
	        /* 322 */ "Trigger Prude Hit[HD]",
	        /* 323 */ "Trigger Prude Finger[HD]",
	        /* 324 */ "Get Stack Object's Autofollow Sim and Place In Temp 0[HD]",
	        /* 325 */ "Allow in Downtown Only[HD]",
	        /* 326 */ "Allow at Home Only[HD]",
	        /* 327 */ "Find Cash Register[HD]",
	        /* 328 */ "Allow Autonomous?[HD]",
	        /* 329 */ "do I love anyone?[HD]",
	        /* 330 */ "does stack object love anyone?[HD]",
	        /* 331 */ "Push Date Response[HD]",
	        /* 332 */ "Push Interaction on My Autofollow[HD]",
	        /* 333 */ "Get My Autofollow Sim in Temp 0[HD]",
	        /* 334 */ "Charge for Downtown[HD]",
	        /* 335 */ "Boost STR[HD]",
	        /* 336 */ "End Autofollow - Bad Behavior[HD]",
	        /* 337 */ "Get Social Availability Scale[HD]",
	        /* 338 */ "Bored Social[HD]",
	        /* 339 */ "dress naked",
	        /* 340 */ "dress normal",
	        /* 341 */ "missing animation tree",
	        /* 342 */ "set object hidden",
	        /* 343 */ "Create Trash Pile By Size",
	        /* 344 */ "dress normal - just dress[HD]",
	        /* 345 */ "wake everyone sleeping in lot[HD]",
	        /* 346 */ "Downtown - Half ad for night[HD]",
	        /* 347 */ "dirty inc",
	        /* 348 */ "set room impact",
	        /* 349 */ "repair inc",
	        /* 350 */ "repair finished",
	        /* 351 */ "repair start",
	        /* 352 */ "relationship",
	        /* 353 */ "Find someone i love",
	        /* 354 */ "Downtown - Double ad for night[HD]",
	        /* 355 */ "verify person in stack obj[HD]",
	        /* 356 */ "Push Browse of Same Type[HD]",
	        /* 357 */ "get my relationship to temp 0",
	        /* 358 */ "Do I Love temp 0?",
	        /* 359 */ "am I friends with temp 0",
	        /* 360 */ "is temp 0 friends with me?",
	        /* 361 */ "Does temp 0 love me?",
	        /* 362 */ "wash hands if neat",
	        /* 363 */ "Am I a Child?",
	        /* 364 */ "Am I an Adult?",
	        /* 365 */ "Is either person a child?",
	        /* 366 */ "idle hours",
	        /* 367 */ "idle minutes",
	        /* 368 */ "Leave",
	        /* 369 */ "Schedule with param",
	        /* 370 */ "animated idle for",
	        /* 371 */ "Start Jealousy",
	        /* 372 */ "End Jealousy",
	        /* 373 */ "find random portal",
	        /* 374 */ "set to leave",
	        /* 375 */ "Is family full?",
	        /* 376 */ "Dress Formal",
	        /* 377 */ "Dress Swimsuit",
	        /* 378 */ "Dress Work",
	        /* 379 */ "enable debug balloons?",
	        /* 380 */ "push slap onto temp0 and temp1",
	        /* 381 */ "Schedule with Hour",
	        /* 382 */ "ensure Sim is standing",
	        /* 383 */ "Create Stool",
	        /* 384 */ "Remove Stool",
	        /* 385 */ "NPC Get Paid",
	        /* 386 */ "privacy - test alone",
	        /* 387 */ "fall on floor - begin",
	        /* 388 */ "fall on floor - end",
	        /* 389 */ "privacy - do shoo",
	        /* 390 */ "group talk cancel[HD]",
	        /* 391 */ "Exit Downtown Object?[HD]",
	        /* 392 */ "do electrocution",
	        /* 393 */ "kill person for good",
	        /* 394 */ "create flood under me",
	        /* 395 */ "create flood in front of me",
	        /* 396 */ "create flood in front of object",
	        /* 397 */ "enable social outcome choice?",
	        /* 398 */ "route failure feedback",
	        /* 399 */ "wake everyone up in stack object's room",
	        /* 400 */ "group talk create",
	        /* 401 */ "group talk do",
	        /* 402 */ "group talk add person",
	        /* 403 */ "group talk exit person",
	        /* 404 */ "schedule phone call",
	        /* 405 */ "Exit fun object",
	        /* 406 */ "drop object from my slot to floor",
	        /* 407 */ "Refuse Skill Object?",
	        /* 408 */ "Refuse Fun Object?",
	        /* 409 */ "repair - Should Tantrum?",
	        /* 410 */ "can join?",
	        /* 411 */ "Is Object in Use Flag Set?",
	        /* 412 */ "Dress for Bed",
	        /* 413 */ "wake everyone in my room with balloon",
	        /* 414 */ "count people in house",
	        /* 415 */ "Exit Skill Object",
	        /* 416 */ "Relationship Impact for Join",
	        /* 417 */ "Exit Repair Function",
	        /* 418 */ "Turn on All TVs in Room[HD]",
	        /* 419 */ "Wander[HD]",
	        /* 420 */ "Autofollow my family member?[HD]",
	        /* 421 */ "Return in Temp 0 & 1 - HIBYTE and LOBYTE[V]",
	        /* 422 */ "Return in Temp 0 - Make WORD[V]",
	        /* 423 */ "Allow on Vacation Only[V]",
	        /* 424 */ "Is lot a Vacation Lot?[V]",
	        /* 425 */ "animated idle for[V]",
	        /* 426 */ "Get Available Family Kid in Temp 0[V]",
	        /* 427 */ "Get Available Family Adult in Temp 0[V]",
	        /* 428 */ "Is lot a Vacation Snow Lot?[V]",
	        /* 429 */ "Turn Off All Dyn Sprites[V]",
	        /* 430 */ "Is Temp 0 a Child?[V]",
	        /* 431 */ "Dress Winter[V]",
	        /* 432 */ "Set Multi-Tile Dyn Sprite[V]",
	        /* 433 */ "Is lot a Vacation Beach Lot?[V]",
	        /* 434 */ "Is lot a Vacation Forest Lot?[V]",
	        /* 435 */ "Prize Token - Add[V]",
	        /* 436 */ "Prize Token - Remove[V]",
	        /* 437 */ "Prize Token - Get Num in Temp[V]",
	        /* 438 */ "Set Multi-Tile Wall Placement Flags[V]",
	        /* 439 */ "Turn Sim to Face Direction[V]",
	        /* 440 */ "Set Multi-Tile Lighting[V]",
	        /* 441 */ "Notify Multi-Tile[V]",
	        /* 442 */ "Temp[0 & 1] := Mutli-Tile Dimensions[base 0][V]",
	        /* 443 */ "count my (non-pet) family members[V]",
	        /* 444 */ "Group Talk - MT - Create[V]",
	        /* 445 */ "Group Talk - MT -  Add Person[V]",
	        /* 446 */ "Am I in Selected Sims Party?[V]",
	        /* 447 */ "Group Talk - MT - Do[V]",
	        /* 448 */ "Group Talk - MT - Exit Person[V]",
	        /* 449 */ "Return in Temp 0 - Tile ID w/ Lowest Subindex[V]",
	        /* 450 */ "can join away from home?[V]",
	        /* 451 */ "Add Deviant Act to Vacation Controller[V]",
	        /* 452 */ "Allow away from home only[V]",
	        /* 453 */ "Find Nearest Rental Shack[V]",
	        /* 454 */ "Find Nearest Rent Shack and Push Interaction[V]",
	        /* 455 */ "Push Interaction by Number[V]",
	        /* 456 */ "Add to Vacation Score[V]",
	        /* 457 */ "Subtract from Vacation Score[V]",
	        /* 458 */ "Get Vacation Score in Temp 0[V]",
	        /* 459 */ "Save Score[V]",
	        /* 460 */ "Set All Wall Placement Flags[V]",
	        /* 461 */ "Exit Vacation Object?[V]",
	        /* 462 */ "Is Stack Object in the Selected Sim's Party[V]",
	        /* 463 */ "Charge for Away from Home[V]",
	        /* 464 */ "Route a away if dating[V]",
	        /* 465 */ "verify stack obj ID with category[V]",
	        /* 466 */ "Remove Time Tokens[U]",
	        /* 467 */ "Am I a Dog?[U]",
	        /* 468 */ "Am I a Cat?[U]",
	        /* 469 */ "Is Lot A Community Lot?[U]",
	        /* 470 */ "Allow On Community Only[U]",
	        /* 471 */ "Allow On Neighborhood Lot Only[U]",
	        /* 472 */ "Dream - Pet[U]",
	        /* 473 */ "Dream - Pet - About Other Sim[U]",
	        /* 474 */ "Dream - Pet - About Object[U]",
	        /* 475 */ "Are there any Community Lots?[U]",
	        /* 476 */ "Allow on Residential Lots only[U]",
	        /* 477 */ "Is Away From Home Family Full[U]",
	        /* 478 */ "Allow Pet Intersection[U]"
        };

        private int m_GlobalSubsLen = 0;

        #endregion

        public void DecodeInstruction(ref IFFDecode P)
        {
            int K = P.Operand(0);

            if (K < 256)
            {
                DecodePrimitive(P);
                return;
            }

            string Name = EntryName("BHAV", K);

            if (m_GlobalSubsLen == 0)
                m_GlobalSubsLen = (m_GlobalSubs.Length / m_GlobalSubs[0].Length) - 1;

            if (Name != "\0")
                P.OutStream.Append(Name);
            else if (K < (uint)(m_GlobalSubsLen + 256))
                P.OutStream.Append(m_GlobalSubs[K - 256]);
            else if (K < 4096)
                P.OutStream.Append("Call global[" + K + "]");
            else if(K < 8192)
                P.OutStream.Append("Call local[" + K + "]");
            else
                P.OutStream.Append("Call semi-global[" + K + "]");

            if (P.Operand(4) == 0xFFFF && P.Operand(6) == 0xFFFF && P.Operand(8) == 0xFFFF && P.Operand(10) == 0xFFFF)
                P.OutStream.Append("(temps)");
            else
            {
                P.OutStream.Append("(" + (short)(P.Operand(4)) +
                    ", " + (short)(P.Operand(6)) +
                    ", " + (short)(P.Operand(8)) +
                    ", " + (short)(P.Operand(10)) +
                    ")");
            }
        }

        private string EntryName(string Type, int ID)
        {
            foreach (IffChunk Chunk in m_MyFile.Chunks)
            {
                if (Chunk.Resource == Type && Chunk.ID == ID)
                    return Chunk.NameString;
            }

            return "";
        }

        private void DecodePrimitive(IFFDecode P)
        {
            int Op = P.Operand(0); //Operation code.
            int U = 0;

            switch (Op)
            {
                default:
                    DefaultPrimitive(P);
                    break;
                case 0:
                    P.OutStream.Append("0: sleep ");
                    DoExpressionOperation(9, P.Operand(4));
                    P.OutStream.Append(" ticks");
                    break;
                case 1: //Generic sim call
                    int BHAVStr220Len = (BHAVStrings.BHAVString220.Length / BHAVStrings.BHAVString220[0].Length) - 1;
                    int T = P.Operand(4);

                    if (T < BHAVStr220Len)
                        P.OutStream.Append(BHAVStrings.BHAVString220[T]);
                    else
                        P.OutStream.Append("1: generic sim call " + T);
                    break;
                case 2:
                    ExpressionPrimitive(P.Instruction);
                    break;
                case 3:
                    P.OutStream.Append("3: find best interaction");
                    break;
                case 4:
                    P.OutStream.Append("4: grab stack object");
                    break;
                case 5:
                    P.OutStream.Append("5: drop stack object");
                    break;
                case 6:
                    ChangeSuitOrAccessoryPrimitive(P.Instruction);
                    break;
                case 7: //Update
                    P.OutStream.Append("7: refresh ");

                    int BHAVStr153Len = (BHAVStrings.BHAVString153.Length / BHAVStrings.BHAVString153[0].Length) - 1;

                    if (P[4] < BHAVStr153Len)
                        P.OutStream.Append(BHAVStrings.BHAVString153[P[4]]);
                    else
                        P.OutStream.Append("WHO=" + (int)(P[4]));

                    P.OutStream.Append(" ");

                    int BHAVStr212Len = (BHAVStrings.BHAVString212.Length / BHAVStrings.BHAVString212[0].Length) - 1;

                    if (P[6] < BHAVStr212Len)
                        P.OutStream.Append(BHAVStrings.BHAVString212[P[6]]);
                    else
                        P.OutStream.Append("WHAT=" + (int)(P[6]));
                    break;
                case 8: //Random
                    DoExpressionOperation(P[6], P.Operand(4));
                    P.OutStream.Append(" := random ");
                    DoExpressionOperation(P[10], P.Operand(8));
                    break;
                case 9: //Burn
                    P.OutStream.Append("9: burn ");

                    int BHAVStr231Len = (BHAVStrings.BHAVString231.Length / BHAVStrings.BHAVString231[0].Length) - 1;

                    if (P[4] < BHAVStr231Len)
                        P.OutStream.Append(BHAVStrings.BHAVString231[P[4]]);
                    else
                        P.OutStream.Append("WHAT [" + (int)(P[4]) + "]");
                    break;
                case 10: //Tutorial
                    // STR#238 - situation action descriptions
                    P.OutStream.Append("10: tutorial " + ((P[4] == 0) ? "begin" : "end"));
                    break;
                case 11: //Distance
                    DoExpressionOperation(8, P.Operand(4));
                    P.OutStream.Append(" := distance from stack object to ");

                    U = P.Operand(8);

                    if (P[6] == 0)
                        P.OutStream.Append("me");
                    else if (P[9] == 3 && U == 11)
                        P.OutStream.Append("me");
                    else
                        DoExpressionOperation(P[9], U);
                    break;
                case 12: //Direction to
                    DoExpressionOperation(P[6], P.Operand(4));
                    P.OutStream.Append(" := direction from stack object to ");

                    U = P.Operand(10);

                    if (P[8] == 0)
                        P.OutStream.Append("me");
                    else if (P[9] == 3 && U == 11)
                        P.OutStream.Append("me");
                    else
                        DoExpressionOperation(P[9], U);
                    break;
                case 13: //Push interaction
                    P.OutStream.Append("13: queue ");
                    P.OutStream.Append((int)(P.OutStream[4]) + " of ");
                    DoExpressionOperation(((P[7] & 2) != 0) ? 25 : 9, P[5]);

                    int BHAVString224Len = (BHAVStrings.BHAVString224.Length / BHAVStrings.BHAVString224[0].Length) - 1;

                    if (P[6] < BHAVString224Len)
                    {
                        if (P[6] > 0)
                        {
                            //cout << " pri=" << BhavStr224[p[6]];
                        }
                    }
                    else
                        P.OutStream.Append(" PRI=" + (int)(P[6]));

                    if ((P[7] & 1) != 0)
                    {
                        P.OutStream.Append(" icon=");
                        DoExpressionOperation(25, P.Operand(8));
                    }

                    if ((P[7] & 0x04) != 0) P.OutStream.Append(", continue as current ");
                    if ((P[7] & 0x08) != 0) P.OutStream.Append(", use name ");
                    
                    break;
                case 14: //Find the best object for...
                    U = P.Operand(4);
                    P.OutStream.Append("14: find best object for ");

                    int BHAVString201Len = (BHAVStrings.BHAVString201Obj.Length / BHAVStrings.BHAVString201Obj[0].Length) - 1;

                    if (U < BHAVString201Len)
                        P.OutStream.Append(BHAVStrings.BHAVString201Obj[U]);
                    else
                        P.OutStream.Append("function " + (int)(U));

                    int X = P.Operand(6),
                        Y = P.Operand(8),
                        Z = P.Operand(10);

                    if (X == 0 && Y == 0 && Z == 0)
                        break;

                    P.OutStream.Append("(" + (int)(X) + ", " + (int)(Y) + ", " + (int)(Z) + ")");

                    break;
                case 15: //Tree breakpoint
                    P.OutStream.Append("15: tree breakpoint");
                    U = P.Operand(4);

                    if (P[6] != 7 || U == 0)
                    {
                        P.OutStream.Append(" if ");
                        DoExpressionOperation(P[6], U);
                        P.OutStream.Append(" != 0");
                    }
                    break;
                case 16: //Find location for
                    P.OutStream.Append("16: find: ");

                    if((P[6] & 2) != 0) P.OutStream.Append("empty ");

                    int BHAVStr239Len = (BHAVStrings.BHAVString239.Length / BHAVStrings.BHAVString239[0].Length) - 1;

                    if (P[4] < BHAVStr239Len)
                        P.OutStream.Append(BHAVStrings.BHAVString239[P[4]]);
                    else
                        P.OutStream.Append("LOC=" + (int)(P[4]));

                    P.OutStream.Append(" loc");

                    if ((P[6] & 1) != 0)
                    {
                        P.OutStream.Append(" start at ");
                        DoExpressionOperation(25, P[5]);
                    }

                    if ((P[6] & 4) != 0) P.OutStream.Append(" user-editable");
                    break;
                case 17: //Idle for input
                    P.OutStream.Append("17: idle for input ");
                    DoExpressionOperation(9, P.Operand(4));

                    U = P.Operand(6);
                    P.OutStream.Append(" ticks, " + ((U == 0) ? "forbid" : "allow") + " push");
                    break;
                case 18: //Remove object instance
                    P.OutStream.Append("18: remove ");
                    int Who = P.Operand(4);
                    BHAVStr153Len = (BHAVStrings.BHAVString153.Length / BHAVStrings.BHAVString153[0].Length) - 1;

                    if (Who < BHAVStr153Len)
                        P.OutStream.Append(BHAVStrings.BHAVString153[Who]);
                    else
                        P.OutStream.Append("object[" + Who + "]'s");

                    P.OutStream.Append(" instance");

                    if ((P[6] & 1) != 0) P.OutStream.Append(", return immediately");
                    if ((P[6] & 2) != 0) P.OutStream.Append(", clean up all");
                    break;
                case 19: //Make new character
                    P.OutStream.Append("19: make new character(");
                    DoExpressionOperation(25, P[4]);
                    P.OutStream.Append(",");
                    DoExpressionOperation(25, P[5]);
                    P.OutStream.Append(",");
                    DoExpressionOperation(25, P[6]);
                    P.OutStream.Append(")");
                    break;
                case 20: //Run functional tree
                    T = P.Operand(4);
                    P.OutStream.Append("20: run ");
                    BHAVString201Len = (BHAVStrings.BHAVString201Run.Length / BHAVStrings.BHAVString201Run[0].Length) - 1;

                    if (T < BHAVString201Len)
                        P.OutStream.Append("\"" + BHAVStrings.BHAVString201Run[T] + "\"");
                    else
                        P.OutStream.Append("function " + (int)T);

                    T = (short)P.Operand(6);
                    if (T != 0) P.OutStream.Append(" with new icon");
                    break;
            }
        }

        private void ChangeSuitOrAccessoryPrimitive(byte[] Instruction)
        {
            //throw new NotImplementedException();
        }

        private void ExpressionPrimitive(byte[] p)
        {
            //throw new NotImplementedException();
        }

        private void DoExpressionOperation(int Code, int Value)
        {
            //throw new NotImplementedException();
        }

        private void DefaultPrimitive(IFFDecode P)
        {
            int Op = P.Operand(0); //Operation code

            //Primitives
            int BHAVString139Len = (BHAVStrings.BHAVString139.Length / BHAVStrings.BHAVString139[0].Length) - 1;

            if (Op < BHAVString139Len)
                P.OutStream.Append(BHAVStrings.BHAVString139[Op]);
            else
                P.OutStream.Append("PRIM_" + Op);

            P.OutStream.Append("(" + P.Operand(4) + ", " + P.Operand(6) + ", " +
                P.Operand(8) + ", " + P.Operand(10) + ")");
        }
    }
}
