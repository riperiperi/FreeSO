/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

namespace FSO.Client.UI.Model
{
    /// <summary>
    /// Names of HIT subroutines that are related to the UI. When the HIT system is implemented you will be able to easily call these whenever.
    /// </summary>
    public sealed class UISounds //hey guys, it's totally an enum!
    {
        //Generic

        public static readonly string Error = "ui_error";
        public static readonly string Click = "ui_click";
        public static readonly string Whoosh = "ui_whoosh"; //plays when hovering over certain ui elements

        public static readonly string BulldozeDemolish = "ui_nhood_bdoze_demolish"; //these are leftovers from ts1
        public static readonly string BulldozeLoop = "ui_nhood_bdoze_loop";
        public static readonly string BulldozeEnd = "ui_nhood_bdoze_end";
        public static readonly string BulldozeEvict = "ui_nhood_bdoze_evict";
        public static readonly string NeighborhoodClick = "ui_nhood_click";
        public static readonly string NeighborhoodError = "ui_nhood_error";
        public static readonly string NeighborhoodRollover = "ui_nhood_rollover";

        public static readonly string QueueDelete = "ui_queue_delete";
        public static readonly string QueueAdd = "ui_queued";

        public static readonly string BuildDragToolPlace = "ui_bld_dragtool_place";
        public static readonly string BuildDragToolDown = "ui_bld_dragtool_mousedown";
        public static readonly string BuildDragToolUp = "ui_bld_dragtool_mouseup";

        public static readonly string BuyPlace = "ui_buy_place"; //ca-ching!
        public static readonly string MoneyBack = "ui_moneyback";
        public static readonly string Remove = "ui_remove"; //idk; i think moneyback is used for buy mode

        public static readonly string CreateCharacterCycleBody = "ui_cac_cycleparts";
        public static readonly string CreateCharacterPersonality = " ui_cac_personpts"; //unused in tso?
        public static readonly string CreateCharacterCycleHead = "ui_cac_cyclehead"; //actually caps in the file; but our system makes all names lowercase anyways.

        public static readonly string ObjectMoneyBack = "ui_object_moneyback";
        public static readonly string ObjectMovePlace = "ui_object_move_place";
        public static readonly string ObjectPlace = "ui_object_place";
        public static readonly string ObjectRotate = "ui_object_rotate";

        public static readonly string PieMenuAppear = "ui_piemenu_appear";
        public static readonly string PieMenuHighlight = "ui_piemenu_highlight";
        public static readonly string PieMenuSelect = "ui_piemenu_select";

        //Speed Changes

        public static readonly string Speed1To2 = "ui_speed_1to2";
        public static readonly string Speed1To3 = "ui_speed_1to3";
        public static readonly string Speed1ToP = "ui_speed_1top";
        public static readonly string Speed2To1 = "ui_speed_2to1";
        public static readonly string Speed2To3 = "ui_speed_2to3";
        public static readonly string Speed2ToP = "ui_speed_2top";
        public static readonly string Speed3To1 = "ui_speed_3to1";
        public static readonly string Speed3To2 = "ui_speed_3to2";
        public static readonly string Speed3ToP = "ui_speed_3top";
        public static readonly string SpeedPTo1 = "ui_speed_pto1";
        public static readonly string SpeedPTo2 = "ui_speed_pto2";
        public static readonly string SpeedPTo3 = "ui_speed_pto3";

        //PictureInPicture

        public static readonly string CameraPhoto = "ui_camera_photo";

        //Tutorial
        
        public static readonly string Help = "ui_help";

        //Terrain

        public static readonly string TerrainLevelMouseUp = "ui_terrain_level_mouseup"; //these are not assigned sound ids...
        public static readonly string TerrainLevelMouseDown = "ui_terrain_level_mousedown";
        public static readonly string TerrainLower = "ui_terrain_lower";
        public static readonly string TerrainRaise = "ui_terrain_raise";

        //Messaging System

        public static readonly string CallRecieveFirst = "ui_call_rec_first";
        public static readonly string CallRecieve = "ui_call_rec"; //alternate between these two
        public static readonly string CallRecieveNext = "ui_call_rec_next";
        public static readonly string CallSend = "ui_call_send";
        public static readonly string CallQueueFull = "ui_call_q_full";

        public static readonly string LetterSend = "ui_letter_send";
        public static readonly string LetterRecieve = "ui_letter_rec";
        public static readonly string LetterQueueFull = "ui_letter_q_full";

    }
}
