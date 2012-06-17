/*
    read_utk.c - Copyright (c) 2011-2012 Fatbag <X-Fi6@phppoll.org>

    Permission to use, copy, modify, and/or distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

#include "stdafx.h"
#include "readutk.h"

#ifndef read_int32
 #define read_uint32(x) (unsigned)(((x)[0]<<(8*0)) | ((x)[1]<<(8*1)) | ((x)[2]<<(8*2)) | ((x)[3]<<(8*3)))
 #define read_uint16(x) (unsigned)(((x)[0]<<(8*0)) | ((x)[1]<<(8*1)))
#endif

#ifndef round
 #define round(x) ((x) >= 0 ? (x)+0.5 : (x)-0.5)
#endif

#ifndef __inline
#define __inline
#endif
#ifndef __restrict
#define __restrict
#endif

float         UTKTable1[64];
uint8_t       UTKTable2[512];
const uint8_t UTKTable3[29] = {8,7,8,7,2,2,2,3,3,4,4,3,3,5,5,4,4,6,6,5,5,7,7,6,6,8,8,7,7};
float         UTKTable4[29];

int utk_read_header(utkheader_t * UTKHeader, const uint8_t * Buffer, unsigned FileSize)
{
    if(FileSize < 28) return 0;
    memcpy(&UTKHeader->sID, Buffer, 4);
    UTKHeader->dwOutSize = read_uint32(Buffer+4);
    UTKHeader->dwWfxSize = read_uint32(Buffer+8);
    UTKHeader->wFormatTag = read_uint16(Buffer+12);
    UTKHeader->nChannels = read_uint16(Buffer+14);
    UTKHeader->nSamplesPerSec = read_uint32(Buffer+16);
    UTKHeader->nAvgBytesPerSec = read_uint32(Buffer+20);
    UTKHeader->nBlockAlign = read_uint16(Buffer+24);
    UTKHeader->wBitsPerSample = read_uint16(Buffer+26);
    UTKHeader->cbSize = read_uint32(Buffer+28);

    if(memcmp(UTKHeader->sID, "UTM0", 4) ||
        UTKHeader->wFormatTag != 1 ||
        UTKHeader->nChannels != 1 ||
        UTKHeader->nSamplesPerSec < 8000 || UTKHeader->nSamplesPerSec > 192000 ||
            !(UTKHeader->nSamplesPerSec%8000==0 || UTKHeader->nSamplesPerSec%11025==0) ||
        UTKHeader->wBitsPerSample != 16 ||
        UTKHeader->nBlockAlign != UTKHeader->nChannels*(UTKHeader->wBitsPerSample>>3) ||
        UTKHeader->nAvgBytesPerSec != UTKHeader->nSamplesPerSec*UTKHeader->nBlockAlign ||
        UTKHeader->dwOutSize%UTKHeader->nBlockAlign != 0 ||
        UTKHeader->cbSize != 0
    )   return 0;

    UTKHeader->Frames = UTKHeader->dwOutSize/UTKHeader->nBlockAlign;
    UTKHeader->UTKDataSize = FileSize - 32;

    return 1;
}

int utk_decode(const uint8_t *__restrict InBuffer, uint8_t *__restrict OutBuffer, unsigned Frames){
    utkparams_t p;
    p.InData = InBuffer;
    SetUTKParameters(&p);

    while(Frames){
        int i, BlockSize = (Frames > 432) ? 432 : Frames;
        DecompressBlock(&p);

        for(i=0; i<BlockSize; i++){
            int value = round(p.DecompressedBlock[i]);

            if(value < -32767)
                value = 32767;
            else if(value > 32768)
                value = 32768;

            *(OutBuffer++) = (value&0x00FF)>>(8*0);
            *(OutBuffer++) = (value&0xFF00)>>(8*1);
        }
        Frames -= BlockSize;
    }
    return 1;
}

void UTKGenerateTables(void){
    /* Call once per runtime */
    int i;

    /* UTKTable1 */
    UTKTable1[0] = 0;
    for(i=-31; i<32; i++){
        int s = (i>=0) ? 1 : -1;
        if     (s*i<14) UTKTable1[i+32] = i*.051587f;
        else if(s*i<25) UTKTable1[i+32] = i*.051587f/2 + s*.337503f;
        else            UTKTable1[i+32] = i*.051587f/8 + s*.796876f;
    }

    /* UTKTable2 */
    for(i=0; i<512; i++){
        switch(i%4){
        case 0: UTKTable2[i] = 4; break;
        case 1: UTKTable2[i] = (i<256) ? 6 : (11 + (i%8 > 4)); break;
        case 2: UTKTable2[i] = (i<256) ? 5 : (7 + (i%8 > 4)); break;
        case 3: {
            uint8_t l1[] = {9,15,13,19,10,16};
            uint8_t l2[] = {17,21,18,25,17,22,18,00,17,21,18,26,17,22,18,02,
                            23,27,24,01,23,28,24,03,23,27,24,01,23,28,24,03};
            if(i%16 < 4)       UTKTable2[i] = l1[0 + (i>256)];
            else if(i%16 < 8)  UTKTable2[i] = l1[2 + (i>256)] + (i%32 > 16);
            else if(i%16 < 12) UTKTable2[i] = l1[4 + (i>256)];
            else UTKTable2[i] = l2[i/16];
        } break;
        }
    }

    /* UTKTable4 */
    UTKTable4[0] = 0;
    for(i=0; i<7; i++){
        UTKTable4[4*i+1] = -i;
        UTKTable4[4*i+2] = +i;
        UTKTable4[4*i+3] = -i;
        UTKTable4[4*i+4] = +i;
    }
}

uint8_t ReadBits(utkparams_t *p, uint8_t bits){
    unsigned value = p->UnreadBitsValue & (255>>(8-bits));
    p->UnreadBitsValue >>= bits;
    p->UnreadBitsCount -= bits;

    if(p->UnreadBitsCount < 8){
        p->UnreadBitsValue |= *(p->InData++) << p->UnreadBitsCount;
        p->UnreadBitsCount += 8;
    }
    return value;
}

void SetUTKParameters(utkparams_t *p){
    /* Call once per file */
    int i;
    float s;
    p->UnreadBitsValue = *(p->InData++);
    p->UnreadBitsCount = 8;
    p->UseLattice = (int)ReadBits(p, 1);
    p->NoiseFloor = 32 - ReadBits(p, 4);
    p->FixedCodebook[0] = (ReadBits(p, 4)+1)*8;

    s = (float)ReadBits(p, 6)/1000 + 1.04;
    for(i=1; i<64; i++)
        p->FixedCodebook[i] = p->FixedCodebook[i-1]*s;

    memset(p->ImpulseTrain, 0, 12*sizeof(float));
    memset(p->R, 0, 12*sizeof(float));
    memset(p->Delay, 0, 324*sizeof(float));
}

void DecompressBlock(utkparams_t *p){
    int i,j;
    float Window[118];
    float Matrix[12];
    int Voiced = 0;

    memset(&Window[0], 0, 5*sizeof(float));
    memset(&Window[113], 0, 5*sizeof(float));

    for(i=0; i<12; i++){
        unsigned result = ReadBits(p, (i<4) ? 6 : 5);
        if(i==0 && p->NoiseFloor > result) Voiced++;
        Matrix[i] = (UTKTable1[result + ((i<4)?0:16)] - p->ImpulseTrain[i])/4;
    }

    for(i=0; i<4; i++){
        float PitchGain, InnovationGain;
        int Phase = (int)ReadBits(p, 8);
        PitchGain = (float)ReadBits(p, 4)/15;
        InnovationGain = p->FixedCodebook[ReadBits(p, 6)];

        if(!p->UseLattice){
            LatticeFilter(p, Voiced, &Window[5], 1);
        }else{
            int o = ReadBits(p, 1); /* Order */
            int y = ReadBits(p, 1);
            LatticeFilter(p, Voiced, &Window[5+o], 2);

            if(y){
                for(j=0; j<108; j+=2)
                    Window[6-o + j] = 0;
            }else{
                /* Vector quantization */
                float *z = &Window[6-o];
                for(j=0; j<54; j++, z+=2)
                    *z =
                          (z[-5]+z[+5]) * .0180326793f
                        - (z[-3]+z[+3]) * .1145915613f
                        + (z[-1]+z[+1]) * .5973859429f;

                InnovationGain /= 2;
            }
        }

        /* Excitation */
        for(j=0; j<108; j++)
            p->DecompressedBlock[108*i + j] = InnovationGain*Window[5+j] + PitchGain*p->Delay[216 - Phase + 108*i + j];
    }

    memcpy(p->Delay, &p->DecompressedBlock[108], 324*sizeof(float));

    for(i=0; i<4; i++){
        for(j=0; j<12; j++)
            p->ImpulseTrain[j] += Matrix[j];

        Synthesize(p, i*12, (i!=3) ? 1 : 33);
    }
}

void LatticeFilter(utkparams_t *p, int Voiced, float * Window, int Interval){
    if(Voiced){
        int t = 0;
        int i = 0;
        while(i<108){
            unsigned code = UTKTable2[(t<<8) | (p->UnreadBitsValue&0xFF)];
            t = (code<2 || code>8);
            ReadBits(p, UTKTable3[code]);

            if(code >= 4){
                Window[i] = UTKTable4[code];
                i += Interval;
            }else{
                if(code > 1){
                    int x = (int)ReadBits(p, 6)+7;
                    if(x > (108 - i)/Interval)
                        x = (108 - i)/Interval;

                    while(x--){
                        Window[i] = 0;
                        i += Interval;
                    }
                }else{
                    Window[i] = 7;
                    while(ReadBits(p, 1))
                        Window[i]++;

                    if(!ReadBits(p, 1))
                        Window[i] *= -1;

                    i += Interval;
                }
            }
        }
    }else{
        /* Unvoiced signal; load noise */
        int i;
        for(i=0; i<108; i+=Interval){
            uint8_t b;
            switch(p->UnreadBitsValue & 3){
            case 3:
                Window[i] = 2.0;
                b = 2;
                break;
            case 1:
                Window[i] = -2.0;
                b = 2;
                break;
            default:
                Window[i] = 0.0;
                b = 1;
            }

            ReadBits(p, b);
        }
    }
}

void Synthesize(utkparams_t *p, unsigned Sample, unsigned Blocks){
    float Residual[12];
    unsigned Samples = Blocks*12;
    int offset = -1;
    PredictionFilter(p->ImpulseTrain, Residual);

    while(Samples--){
        int i;
        float x = p->DecompressedBlock[Sample];
        for(i=0; i<12; i++){
            if(++offset == 12) offset = 0;
            x += p->R[offset] * Residual[i];
        }
        p->R[offset--] = x;
        p->DecompressedBlock[Sample++] = x;
    }
}

void PredictionFilter(const float *__restrict c2, float *__restrict Residual){
    int i,j;
    float M1[12];
    float M2[12];
    M2[0] = 1;
    memcpy(&M2[1], c2, 11*sizeof(float));

    for(i=0; i<12; i++){
        float x = 0;
        for(j=11; j>=0; j--){
            x -= c2[j] * M2[j];
            if(j != 11)
                M2[j+1] = x*c2[j] + M2[j];
        }
        M2[0] = x;
        M1[i] = x;

        for(j=0; j<i; j++)
            x -= M1[i-j-1] * Residual[j];

        Residual[i] = x;
    }
}