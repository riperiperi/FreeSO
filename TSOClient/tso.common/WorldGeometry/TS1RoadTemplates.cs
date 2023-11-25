using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FSO.Common.WorldGeometry
{
    public static class TS1RoadTemplates
    {
        private static ushort ROAD_TILE = 9;
        private static ushort ROAD_LINE_LT_RB = 10;
        private static ushort ROAD_LINE_LB_RT = 11;
        private static ushort PAVEMENT_TILE = 12;

        private static ushort DOWNTOWN_PAVEMENT_LIGHT = 352;
        private static ushort DOWNTOWN_GRATE_LIGHT = 350;
        private static ushort DOWNTOWN_MANHOLE_LIGHT = 351;

        private static ushort DOWNTOWN_PAVEMENT_DARK = 355;
        private static ushort DOWNTOWN_GRATE_DARK = 353;
        private static ushort DOWNTOWN_MANHOLE_DARK = 354;

        private static ushort VACATION_ROAD = 359; //awfully flat

        private static Vector2 Flat(float xOff)
        {
            return new Vector2(xOff, 0);
        }

        public static RoadGeometryTemplate OLD_TOWN = new RoadGeometryTemplate()
        {
            Segments = new RoadGeometryTemplateSegment[]
            {
                //without middle line (3 tiles long)
                new RoadGeometryTemplateSegment()
                {
                    Extent = 3f,
                    Lines = new RoadGeometryTemplateLine[]
                    {
                        new RoadGeometryTemplateLine(Flat(-5.5f), Flat(-4.5f), PAVEMENT_TILE),
                        new RoadGeometryTemplateLine(Flat(-3.5f), Flat(3.5f), ROAD_TILE),
                        new RoadGeometryTemplateLine(Flat(4.5f), Flat(5.5f), PAVEMENT_TILE),
                    }
                },

                //with middle line (3 tiles long)
                new RoadGeometryTemplateSegment()
                {
                    Extent = 3f,
                    Lines = new RoadGeometryTemplateLine[]
                    {
                        new RoadGeometryTemplateLine(Flat(-5.5f), Flat(-4.5f), PAVEMENT_TILE),
                        new RoadGeometryTemplateLine(Flat(-3.5f), Flat(-0.5f), ROAD_TILE),
                        new RoadGeometryTemplateLine(Flat(-0.5f), Flat(0.5f), ROAD_LINE_LT_RB),
                        new RoadGeometryTemplateLine(Flat(0.5f), Flat(3.5f), ROAD_TILE),
                        new RoadGeometryTemplateLine(Flat(4.5f), Flat(5.5f), PAVEMENT_TILE),
                    }
                },
            },
            RepeatLength = 6f,
            EndLines = new RoadGeometryTemplateLine[]
            {
                new RoadGeometryTemplateLine(Flat(-5.5f), Flat(-4.5f), PAVEMENT_TILE),
                new RoadGeometryTemplateLine(Flat(-3.5f), Flat(0f), ROAD_TILE),
            },
            EndRepeats = 17,

            IntersectionSize = 13, //7 wide road, 1 tile gap on each side, 1 tile pavement on each side, 1 tile gap again
            IntersectionFromSize = 13,
            Intersection4Way = new RoadGeometryTemplateRect[]
            {
                //pavement
                new RoadGeometryTemplateRect(new Rectangle(1, 0, 1, 3), PAVEMENT_TILE), //top left cross
                new RoadGeometryTemplateRect(new Rectangle(0, 1, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(2, 1, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 0, 1, 3), PAVEMENT_TILE), //top right cross
                new RoadGeometryTemplateRect(new Rectangle(10, 1, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(12, 1, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(1, 10, 1, 3), PAVEMENT_TILE), //bottom left cross
                new RoadGeometryTemplateRect(new Rectangle(0, 11, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(2, 11, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 10, 1, 3), PAVEMENT_TILE), //bottom right cross
                new RoadGeometryTemplateRect(new Rectangle(10, 11, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(12, 11, 1, 1), PAVEMENT_TILE),

                //road
                new RoadGeometryTemplateRect(new Rectangle(3, 3, 7, 7), ROAD_TILE), //center
                new RoadGeometryTemplateRect(new Rectangle(3, 1, 7, 1), ROAD_TILE), //top
                new RoadGeometryTemplateRect(new Rectangle(3, 11, 7, 1), ROAD_TILE), //bottom
                new RoadGeometryTemplateRect(new Rectangle(1, 3, 1, 7), ROAD_TILE), //left
                new RoadGeometryTemplateRect(new Rectangle(11, 3, 1, 7), ROAD_TILE), //right

                //road lines (vertical)
                new RoadGeometryTemplateRect(new Rectangle(0, 3, 1, 7), ROAD_LINE_LT_RB),
                new RoadGeometryTemplateRect(new Rectangle(2, 3, 1, 7), ROAD_LINE_LT_RB),
                new RoadGeometryTemplateRect(new Rectangle(10, 3, 1, 7), ROAD_LINE_LT_RB),
                new RoadGeometryTemplateRect(new Rectangle(12, 3, 1, 7), ROAD_LINE_LT_RB),

                //road lines (horizontal)
                new RoadGeometryTemplateRect(new Rectangle(3, 0, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 2, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 10, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 12, 7, 1), ROAD_LINE_LB_RT),

            },

            Intersection3Way = new RoadGeometryTemplateRect[]
            {
                //pavement
                new RoadGeometryTemplateRect(new Rectangle(1, 0, 1, 13), PAVEMENT_TILE), //left pavement (with 2 joins)
                new RoadGeometryTemplateRect(new Rectangle(2, 1, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(2, 11, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 0, 1, 3), PAVEMENT_TILE), //top right cross
                new RoadGeometryTemplateRect(new Rectangle(10, 1, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(12, 1, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 10, 1, 3), PAVEMENT_TILE), //bottom right cross
                new RoadGeometryTemplateRect(new Rectangle(10, 11, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(12, 11, 1, 1), PAVEMENT_TILE),

                //road
                new RoadGeometryTemplateRect(new Rectangle(3, 3, 7, 7), ROAD_TILE), //center
                new RoadGeometryTemplateRect(new Rectangle(3, 1, 7, 1), ROAD_TILE), //top
                new RoadGeometryTemplateRect(new Rectangle(3, 11, 7, 1), ROAD_TILE), //bottom
                new RoadGeometryTemplateRect(new Rectangle(11, 3, 1, 7), ROAD_TILE), //right

                //road lines (vertical)
                new RoadGeometryTemplateRect(new Rectangle(10, 3, 1, 7), ROAD_LINE_LT_RB),
                new RoadGeometryTemplateRect(new Rectangle(12, 3, 1, 7), ROAD_LINE_LT_RB),

                //road lines (horizontal)
                new RoadGeometryTemplateRect(new Rectangle(3, 0, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 2, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 10, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 12, 7, 1), ROAD_LINE_LB_RT),

            }
        };

        public static RoadGeometryTemplate OLD_TOWN_DUAL = new RoadGeometryTemplate()
        {
            Segments = new RoadGeometryTemplateSegment[]
            {
                //this road type does not have a middle line.
                new RoadGeometryTemplateSegment()
                {
                    Extent = 3f,
                    Lines = new RoadGeometryTemplateLine[]
                    {
                        new RoadGeometryTemplateLine(Flat(-8.5f), Flat(-7.5f), PAVEMENT_TILE),

                        new RoadGeometryTemplateLine(Flat(-6.5f), Flat(-2.5f), ROAD_TILE),

                        new RoadGeometryTemplateLine(Flat(-2f), Flat(-1f), new Vector2(0.5f, 0), PAVEMENT_TILE),
                        new RoadGeometryTemplateLine(Flat(1f), Flat(2f), new Vector2(0.5f, 0), PAVEMENT_TILE),

                        new RoadGeometryTemplateLine(Flat(2.5f), Flat(6.5f), ROAD_TILE),

                        new RoadGeometryTemplateLine(Flat(7.5f), Flat(8.5f), PAVEMENT_TILE),
                    }
                },
            },
            RepeatLength = 3f,
            EndLines = new RoadGeometryTemplateLine[]
            {
                new RoadGeometryTemplateLine(Flat(-8.5f), Flat(-7.5f), PAVEMENT_TILE),

                new RoadGeometryTemplateLine(Flat(-6.5f), Flat(-2.5f), ROAD_TILE),

                new RoadGeometryTemplateLine(Flat(-2f), Flat(-1f), new Vector2(0.5f, 0), PAVEMENT_TILE),
            },
            EndRepeats = 22,

            IntersectionSize = 19, //7 wide road, 1 tile gap on each side, 1 tile pavement on each side, 1 tile gap again
            IntersectionFromSize = 13, //used for 3 way intersection on special road types. here it is the width of the normal road

            //UNUSED
            Intersection4Way = OLD_TOWN.Intersection4Way,

            Intersection3Way = new RoadGeometryTemplateRect[]
            { //19 tall, 13 wide
                //pavement
                new RoadGeometryTemplateRect(new Rectangle(1, 0, 1, 19), PAVEMENT_TILE), //left pavement (with 2 joins)
                new RoadGeometryTemplateRect(new Rectangle(2, 1, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(2, 17, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 0, 1, 3), PAVEMENT_TILE), //top right cross
                new RoadGeometryTemplateRect(new Rectangle(10, 1, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(12, 1, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 16, 1, 3), PAVEMENT_TILE), //bottom right cross
                new RoadGeometryTemplateRect(new Rectangle(10, 17, 1, 1), PAVEMENT_TILE),
                new RoadGeometryTemplateRect(new Rectangle(12, 17, 1, 1), PAVEMENT_TILE),

                new RoadGeometryTemplateRect(new Rectangle(11, 7, 1, 5), PAVEMENT_TILE), //right path
                new RoadGeometryTemplateRect(new Rectangle(12, 7, 1, 1), PAVEMENT_TILE, new Vector2(0, 0.5f)), //right off1
                new RoadGeometryTemplateRect(new Rectangle(12, 10, 1, 1), PAVEMENT_TILE, new Vector2(0, 0.5f)), //right off2

                //road
                new RoadGeometryTemplateRect(new Rectangle(3, 3, 7, 13), ROAD_TILE), //center
                new RoadGeometryTemplateRect(new Rectangle(3, 1, 7, 1), ROAD_TILE), //top
                new RoadGeometryTemplateRect(new Rectangle(3, 17, 7, 1), ROAD_TILE), //bottom

                new RoadGeometryTemplateRect(new Rectangle(11, 3, 1, 4), ROAD_TILE), //right
                new RoadGeometryTemplateRect(new Rectangle(11, 12, 1, 4), ROAD_TILE), //right

                //road lines (vertical)
                new RoadGeometryTemplateRect(new Rectangle(10, 3, 1, 4), ROAD_LINE_LT_RB),
                new RoadGeometryTemplateRect(new Rectangle(12, 3, 1, 4), ROAD_LINE_LT_RB),

                new RoadGeometryTemplateRect(new Rectangle(10, 12, 1, 4), ROAD_LINE_LT_RB),
                new RoadGeometryTemplateRect(new Rectangle(12, 12, 1, 4), ROAD_LINE_LT_RB),

                //road lines (horizontal)
                new RoadGeometryTemplateRect(new Rectangle(3, 0, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 2, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 16, 7, 1), ROAD_LINE_LB_RT),
                new RoadGeometryTemplateRect(new Rectangle(3, 18, 7, 1), ROAD_LINE_LB_RT),
            }
        };

        public static List<RoadGeometryTemplate> OLD_TOWN_DEFAULT_TEMPLATES = new List<RoadGeometryTemplate>()
        {
            OLD_TOWN,
            OLD_TOWN_DUAL
        };
    }
}
