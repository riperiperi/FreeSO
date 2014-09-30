/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Files.HIT
{
    public enum HITArgs
    {
        kArgsNormal = 0,
        kArgsVolPan = 1,
        kArgsIdVolPan = 2,
        kArgsXYZ = 3
    }

    public enum HITControlGroups
    {
        kGroupSFX = 1,
        kGroupMusic = 2,
        kGroupVox = 3
    }

    public enum HITDuckingPriorities
    {
        duckpri_unknown1 = 32,
        duckpri_unknown2 = 5000,
        duckpri_always = 0x0,
        duckpri_low = 0x1,
        duckpri_normal = 0x14,
        duckpri_high = 0x1e,
        duckpri_higher = 0x28,
        duckpri_evenhigher = 0x32,
        duckpri_never = 0x64
    }

    public enum HITEvents
    {
        kSoundobPlay = 1,
        kSoundobStop = 2,
        kSoundobKill = 3,
        kSoundobUpdate = 4,
        kSoundobSetVolume = 5,
        kSoundobSetPitch = 6,
        kSoundobSetPan = 7,
        kSoundobSetPosition = 8,
        kSoundobSetFxType = 9,
        kSoundobSetFxLevel = 10,
        kSoundobPause = 11,
        kSoundobUnpause = 12
    }

    public enum HITPerson
    {
        Instance = 0x0,
        Gender = 0x1
    }
}
