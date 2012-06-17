#ifndef __COMPRESSEDOUTPUT_H__
#define __COMPRESSEDOUTPUT_H__

#include <string.h>  // for memcpy and memset
#include <stdlib.h>
#include <assert.h>

typedef unsigned char byte;
struct word { byte lo,hi; };
struct dword { word lo,hi; };

class CompressedOutput
{
private:

    byte* dstpos;
    byte* dstend;
    const byte* src;
    unsigned srcpos;

public:

    CompressedOutput(const byte* src_, byte* dst, byte* dstend_)
	{
        dstpos = dst; dstend = dstend_; src = src_;
        srcpos = 0;
    }

    byte* get_end() { return dstpos; }

    bool emit(unsigned from_pos, unsigned to_pos, unsigned count)
    {
        if (count)
            assert(memcmp(src + from_pos, src + to_pos, count) == 0);

        unsigned lit = to_pos - srcpos;

        while (lit >= 4) {
            unsigned amt = lit>>2;
            if (amt > 28) amt = 28;
            if (dstpos + amt*4 >= dstend) return false;
            *dstpos++ = 0xE0 + amt - 1;
            memcpy(dstpos, src + srcpos, amt*4);
            dstpos += amt*4;
            srcpos += amt*4;
            lit -= amt*4;
        }

        unsigned offset = to_pos - from_pos - 1;

        if (count == 0) {
            if (dstpos+1+lit > dstend) return false;
            *dstpos++ = 0xFC + lit;
        } else if (offset < 1024 && 3 <= count && count <= 10) {
            if (dstpos+2+lit > dstend) return false;
            *dstpos++ = ((offset >> 3) & 0x60) + ((count-3) * 4) + lit;
            *dstpos++ = offset;
        } else if (offset < 16384 && 4 <= count && count <= 67) {
            if (dstpos+3+lit > dstend) return false;
            *dstpos++ = 0x80 + (count-4);
            *dstpos++ = lit * 0x40 + (offset >> 8);
            *dstpos++ = offset;
        } else /* if (offset < 131072 && 5 <= count && count <= 1028) */ {
            if (dstpos+4+lit > dstend) return false;
            *dstpos++ = 0xC0 + ((offset >> 12) & 0x10) + (((count-5) >> 6) & 0x0C) + lit;
            *dstpos++ = offset >> 8;
            *dstpos++ = offset;
            *dstpos++ = (count-5);
        }

        for (; lit; --lit) *dstpos++ = src[srcpos++];
        srcpos += count;

        return true;
    }
};

#endif