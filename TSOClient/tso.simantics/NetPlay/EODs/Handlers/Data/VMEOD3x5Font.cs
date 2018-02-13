// Used under MIT licence. A 3x5 pixel font for the dance floor controller.
// https://hackaday.io/project/6309-vga-graphics-over-spi-and-serial-vgatonic/log/20759-a-tiny-4x6-pixel-font-that-will-fit-on-almost-any-microcontroller-license-mit

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers.Data
{
    public static class VMEOD3x5Font
    {
        // Font Definition
        public static byte[][] font4x6 = new byte[][] {
         new byte[] { 0x00  ,  0x00  },   /*SPACE*/
         new byte[] { 0x49  ,  0x08  },   /*'!'*/
         new byte[] { 0xb4  ,  0x00  },   /*'"'*/
         new byte[] { 0xbe  ,  0xf6  },   /*'#'*/
         new byte[] { 0x7b  ,  0x7a  },   /*'$'*/
         new byte[] { 0xa5  ,  0x94  },   /*'%'*/
         new byte[] { 0x55  ,  0xb8  },   /*'&'*/
         new byte[] { 0x48  ,  0x00  },   /*'''*/
         new byte[] { 0x29  ,  0x44  },   /*'('*/
         new byte[] { 0x44  ,  0x2a  },   /*')'*/
         new byte[] { 0x15  ,  0xa0  },   /*'*'*/
         new byte[] { 0x0b  ,  0x42  },   /*'+'*/
         new byte[] { 0x00  ,  0x50  },   /*','*/
         new byte[] { 0x03  ,  0x02  },   /*'-'*/
         new byte[] { 0x00  ,  0x08  },   /*'.'*/
         new byte[] { 0x25  ,  0x90  },   /*'/'*/
         new byte[] { 0x76  ,  0xba  },   /*'0'*/
         new byte[] { 0x59  ,  0x5c  },   /*'1'*/
         new byte[] { 0xc5  ,  0x9e  },   /*'2'*/
         new byte[] { 0xc5  ,  0x38  },   /*'3'*/
         new byte[] { 0x92  ,  0xe6  },   /*'4'*/
         new byte[] { 0xf3  ,  0x3a  },   /*'5'*/
         new byte[] { 0x73  ,  0xba  },   /*'6'*/
         new byte[] { 0xe5  ,  0x90  },   /*'7'*/
         new byte[] { 0x77  ,  0xba  },   /*'8'*/
         new byte[] { 0x77  ,  0x3a  },   /*'9'*/
         new byte[] { 0x08  ,  0x40  },   /*':'*/
         new byte[] { 0x08  ,  0x50  },   /*';'*/
         new byte[] { 0x2a  ,  0x44  },   /*'<'*/
         new byte[] { 0x1c  ,  0xe0  },   /*'='*/
         new byte[] { 0x88  ,  0x52  },   /*'>'*/
         new byte[] { 0xe5  ,  0x08  },   /*'?'*/
         new byte[] { 0x56  ,  0x8e  },   /*'@'*/
         new byte[] { 0x77  ,  0xb6  },   /*'A'*/
         new byte[] { 0x77  ,  0xb8  },   /*'B'*/
         new byte[] { 0x72  ,  0x8c  },   /*'C'*/
         new byte[] { 0xd6  ,  0xba  },   /*'D'*/
         new byte[] { 0x73  ,  0x9e  },   /*'E'*/
         new byte[] { 0x73  ,  0x92  },   /*'F'*/
         new byte[] { 0x72  ,  0xae  },   /*'G'*/
         new byte[] { 0xb7  ,  0xb6  },   /*'H'*/
         new byte[] { 0xe9  ,  0x5c  },   /*'I'*/
         new byte[] { 0x64  ,  0xaa  },   /*'J'*/
         new byte[] { 0xb7  ,  0xb4  },   /*'K'*/
         new byte[] { 0x92  ,  0x9c  },   /*'L'*/
         new byte[] { 0xbe  ,  0xb6  },   /*'M'*/
         new byte[] { 0xd6  ,  0xb6  },   /*'N'*/
         new byte[] { 0x56  ,  0xaa  },   /*'O'*/
         new byte[] { 0xd7  ,  0x92  },   /*'P'*/
         new byte[] { 0x76  ,  0xee  },   /*'Q'*/
         new byte[] { 0x77  ,  0xb4  },   /*'R'*/
         new byte[] { 0x71  ,  0x38  },   /*'S'*/
         new byte[] { 0xe9  ,  0x48  },   /*'T'*/
         new byte[] { 0xb6  ,  0xae  },   /*'U'*/
         new byte[] { 0xb6  ,  0xaa  },   /*'V'*/
         new byte[] { 0xb6  ,  0xf6  },   /*'W'*/
         new byte[] { 0xb5  ,  0xb4  },   /*'X'*/
         new byte[] { 0xb5  ,  0x48  },   /*'Y'*/
         new byte[] { 0xe5  ,  0x9c  },   /*'Z'*/
         new byte[] { 0x69  ,  0x4c  },   /*'['*/
         new byte[] { 0x91  ,  0x24  },   /*'\'*/
         new byte[] { 0x64  ,  0x2e  },   /*']'*/
         new byte[] { 0x54  ,  0x00  },   /*'^'*/
         new byte[] { 0x00  ,  0x1c  },   /*'_'*/
         new byte[] { 0x44  ,  0x00  },   /*'`'*/
         new byte[] { 0x0e  ,  0xae  },   /*'a'*/
         new byte[] { 0x9a  ,  0xba  },   /*'b'*/
         new byte[] { 0x0e  ,  0x8c  },   /*'c'*/
         new byte[] { 0x2e  ,  0xae  },   /*'d'*/
         new byte[] { 0x0e  ,  0xce  },   /*'e'*/
         new byte[] { 0x56  ,  0xd0  },   /*'f'*/
         new byte[] { 0x55  ,  0x3B  },   /*'g'*/
         new byte[] { 0x93  ,  0xb4  },   /*'h'*/
         new byte[] { 0x41  ,  0x44  },   /*'i'*/
         new byte[] { 0x41  ,  0x51  },   /*'j'*/
         new byte[] { 0x97  ,  0xb4  },   /*'k'*/
         new byte[] { 0x49  ,  0x44  },   /*'l'*/
         new byte[] { 0x17  ,  0xb6  },   /*'m'*/
         new byte[] { 0x1a  ,  0xb6  },   /*'n'*/
         new byte[] { 0x0a  ,  0xaa  },   /*'o'*/
         new byte[] { 0xd6  ,  0xd3  },   /*'p'*/
         new byte[] { 0x76  ,  0x67  },   /*'q'*/
         new byte[] { 0x17  ,  0x90  },   /*'r'*/
         new byte[] { 0x0f  ,  0x38  },   /*'s'*/
         new byte[] { 0x9a  ,  0x8c  },   /*'t'*/
         new byte[] { 0x16  ,  0xae  },   /*'u'*/
         new byte[] { 0x16  ,  0xba  },   /*'v'*/
         new byte[] { 0x16  ,  0xf6  },   /*'w'*/
         new byte[] { 0x15  ,  0xb4  },   /*'x'*/
         new byte[] { 0xb5  ,  0x2b  },   /*'y'*/
         new byte[] { 0x1c  ,  0x5e  },   /*'z'*/
         new byte[] { 0x6b  ,  0x4c  },   /*'{'*/
         new byte[] { 0x49  ,  0x48  },   /*'|'*/
         new byte[] { 0xc9  ,  0x5a  },   /*'}'*/
         new byte[] { 0x54  ,  0x00  },   /*'~'*/
         new byte[] { 0x56  ,  0xe2  }    /*''*/
        };

        // Font retreival function - ugly, but needed.
        public static byte GetFontLine(char data, int line_num)
        {
            var index = (byte)(data - 32);
            if (index < 0 || index >= font4x6.Length) return 0;
            byte pixel = 0;
            if ((font4x6[index][1] & 1) == 1) line_num -= 1;
            if (line_num == 0)
            {
                pixel = (byte)((font4x6[index][0]) >> 4);
            }
            else if (line_num == 1)
            {
                pixel = (byte)((font4x6[index][0]) >> 1);
            }
            else if (line_num == 2)
            {
                // Split over 2 bytes
                return (byte)((((font4x6[index][0]) & 0x03) << 2) | (((font4x6[index][1]) & 0x02)));
            }
            else if (line_num == 3)
            {
                pixel = (byte)((font4x6[index][1]) >> 4);
            }
            else if (line_num == 4)
            {
                pixel = (byte)((font4x6[index][1]) >> 1);
            }
            return (byte)(pixel & 0xE);
        }
    }
}
