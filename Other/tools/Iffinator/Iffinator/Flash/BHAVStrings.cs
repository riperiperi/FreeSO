/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Iffinator.Flash
{
    public class BHAVStrings
    {
        public static string[] BHAVString139 = new string[]
        {
            	/*  0 */ "sleep",
	            /*  1 */ "generic sims call",
	            /*  2 */ "expression",
	            /*  3 */ "find best interaction",
	            /*  4 */ "grab",
	            /*  5 */ "drop",
	            /*  6 */ "change suit/accessory",
	            /*  7 */ "refresh",
	            /*  8 */ "random number",
	            /*  9 */ "burn",
	            /* 10 */ "tutorial",
	            /* 11 */ "get distance to",
	            /* 12 */ "get direction to",
	            /* 13 */ "push interaction",
	            /* 14 */ "find best object for function",
	            /* 15 */ "tree break point",
	            /* 16 */ "find location for",
	            /* 17 */ "idle for input",
	            /* 18 */ "remove object instance",
	            /* 19 */ "make new character",
	            /* 20 */ "run functional tree",
	            /* 21 */ "show string (UNUSED)",
	            /* 22 */ "look towards",
	            /* 23 */ "play sound event",
	            /* 24 */ "old relationship (DEPRECATED)",
	            /* 25 */ "alter budget",
	            /* 26 */ "relationship",
	            /* 27 */ "go to relative position",
	            /* 28 */ "run tree by name",
	            /* 29 */ "set motive change",
	            /* 30 */ "gosub found action",
	            /* 31 */ "set to next",
	            /* 32 */ "test object type",
	            /* 33 */ "find five worst motives",
	            /* 34 */ "UI effect",
	            /* 35 */ "special effect",
	            /* 36 */ "dialog",
	            /* 37 */ "test sim interacting with",
	            /* 38 */ "unused",
	            /* 39 */ "unused",
	            /* 40 */ "unused",
	            /* 41 */ "set balloon/headline",
	            /* 42 */ "create new object instance",
	            /* 43 */ "drop onto",
	            /* 44 */ "animate sim",
	            /* 45 */ "go to routing slot",
	            /* 46 */ "snap",
	            /* 47 */ "reach",
	            /* 48 */ "stop ALL sounds",
	            /* 49 */ "notify the stack object out of idle",
	            /* 50 */ "add/change the action string",
	            /* 51 */ "manage inventory[HD, animate object in TSO]",
	            /* 52 */ "change light color[TSO]",
	            /* 53 */ "change sun color[TSO]",
	            /* 54 */ "point light at object[TSO]",
	            /* 55 */ "sync field[TSO]",
	            /* 56 */ "ownership[TSO]",
	            /* 57 */ "start persistant dialog[TSO]",
	            /* 58 */ "end persistant dialog[TSO]",
	            /* 59 */ "update persistant dialog[TSO]",
	            /* 60 */ "poll persistant dialog[TSO]",
	            /* 61 */ "~unused[TSO]",
	            /* 62 */ "invoke plugin[TSO]",
	            /* 63 */ "get terrain info[TSO]"
        };

        public static string[] BHAVString153 = new string[]
        {
            // update who
        	/*  0 */ "my",
	        /*  1 */ "stack obj's",
	        /*  2 */ "target's [OBSOLETE]"
        };

        public static string[] BHAVString201Obj = new string[]
        {
            //Functions (in objects, that can be run by Sims).
            /*  0 */ "preparing food",
	        /*  1 */ "cooking food",
	        /*  2 */ "flat surface",
	        /*  3 */ "disposing",
	        /*  4 */ "eating",
	        /*  5 */ "picking up from slot",
	        /*  6 */ "washing dish",
	        /*  7 */ "eating surface",
	        /*  8 */ "siting",
	        /*  9 */ "standing",
	        /* 10 */ "serving surface",
	        /* 11 */ "cleaning",
	        /* 12 */ "gardening",
	        /* 13 */ "washing hands",
	        /* 14 */ "repairing",
	        /* 15 */ "sleeping[V]"
        };

        public static string[] BHAVString201Run = new string[]
        {
                //Functions (in Sims, that can be run by objects).
            	/*  0 */ "prepare food",
	            /*  1 */ "cook food",
	            /*  2 */ "put on flat surface",
	            /*  3 */ "dispose",
	            /*  4 */ "eat",
	            /*  5 */ "pick up from slot",
	            /*  6 */ "wash dish",
	            /*  7 */ "put on eating surface",
	            /*  8 */ "sit",
	            /*  9 */ "stand",
	            /* 10 */ "put on serving surface",
	            /* 11 */ "clean",
	            /* 12 */ "garden",
	            /* 13 */ "wash hands",
	            /* 14 */ "repair",
	            /* 15 */ "sleep[V]"
        };

        public static string[] BHAVString212 = new string[]
        {
            // update what
	        /*  0 */ "graphic",
	        /*  1 */ "lighting contribution",
	        /*  2 */ "room score contribution"
        };

        public static string[] BHAVString220 = new string[] 
        {	// generic sim calls
	        /*  0 */ "house tutorial complete",
	        // /*  1 */ "UNUSED[LL] - center view on stack obj",
	        /*  1 */ "swap my and stack obj's slots[HD]",
	        /*  2 */ "set action icon to stack obj",
	        // /*  3 */ "UNUSED[LL] - uncenter view",
	        /*  3 */ "pull down taxi dialog[HD]",
	        /*  4 */ "add stack obj to family",
	        /*  5 */ "take assets of family in temp 0",
	        /*  6 */ "remove stack obj from family",
	        /*  7 */ "DEPRECATED - make new neighbor",
	        /*  8 */ "family tutorial complete",
	        /*  9 */ "architecture tutorial complete",
	        /* 10 */ "disable build and buy",
	        /* 11 */ "enable build and buy",
	        /* 12 */ "temp 0 := distance to camera",
	        /* 13 */ "abort interactions with stack obj",
	        /* 14 */ "house radio station := temp 0",
	        /* 15 */ "my footprint extension := temp 0",
	        /* 16 */ "change normal outfit to next available",
        // Added in Hot Date
	        /* 17 */ "change to lot in temp 0[HD]",
	        /* 18 */ "build the downtown Sim and place obj ID in temp 0[HD]",
	        /* 19 */ "spawn downtown date of person in temp 0; place spawned autofollow Sim in temp 0[HD]",
	        /* 20 */ "spawn take back home date of person in temp 0; place spawned autofollow Sim in temp 0[HD]",
	        /* 21 */ "spawn inventory SimData effects[HD]",
	        /* 22 */ "select downtown lot[HD]",
	        /* 23 */ "get downtown time from SO's inventory(Hours in T0, Minutes in T1)[HD]",
	        /* 24 */ "change suits permanently[HD]",
	        /* 25 */ "save this Sim's persistent data[HD]",
        // Added in Vacation
	        /* 26 */ "build vacation family; temp 0 := family number[V]",
	        /* 27 */ "temp 0 :=  number of available vacation lots[V]",
        // Added in Unleashed
	        /* 28 */ "temp 0 := temp[0]'s lot's zoning type[U]",
	        /* 29 */ "set stack obj's suit: type := temp[0], index := temp[1]; temp[1] := old index[U]",
	        /* 30 */ "get stack obj's suit: temp[0] := type. temp[1] := index[U]",
	        /* 31 */ "temp[1] := count stack obj's suits of type temp[0][U]",
	        /* 32 */ "create all purchased pets near owner[U]",
	        /* 33 */ "add to family in temp 0[U]"
        };

        public static string[] BHAVString221 = new string[] 
        {
            // neighbor data labels
	        /*  0 */ "person instance ID",
	        /*  1 */ "belongs in house",
	        /*  2 */ "person age",
	        /*  3 */ "relationship raw score",
	        /*  4 */ "relationship score",
	        /*  5 */ "friend count",
	        /*  6 */ "house number",
	        /*  7 */ "has telephone",
	        /*  8 */ "has baby",
	        /*  9 */ "family friend count"
        };

        public static string[] BHAVString222 = new string[]
        {	
            // how to call named tree
	        /*  0 */ "run in my stack",
	        /*  1 */ "run in stack obj's stack",
	        /*  2 */ "push onto my stack"
        };

        public static string[] BHAVString223 = new string[]
        {
            // priorities
	        // person data labels:33 - priority
	        /*  0 */ "inherited",
	        /*  1 */ "max",
	        /*  2 */ "autonomous",
	        /*  3 */ "user"
        };

        public static string[] BHAVString224 = new string[]
        {
        	// person data labels:33 - priority
	        /*  0 */ "inherited",
	        /*  1 */ "max",
	        /*  2 */ "autonomous",
	        /*  3 */ "user"
        };

        public static string[] BHAVString231 = new string[]
        {
            // what to burn
        	/*  0 */ "stack obj",
	        /*  1 */ "tile in front of stack obj",
	        /*  2 */ "floor under stack obj"
        };

        public static string[] BHAVString239 = new string[]
        {
            // find good location behaviors
	        /*  0 */ "normal",
	        /*  1 */ "out-of-world",
	        /*  2 */ "smoke",
	        /*  3 */ "object vector",
	        /*  4 */ "lateral",
	        // added in Hot Date
	        /*  5 */ "random[HD]"
        };
    }
}
