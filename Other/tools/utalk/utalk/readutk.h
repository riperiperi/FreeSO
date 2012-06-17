/*
    read_utk.h - Copyright (c) 2011-2012 Fatbag <X-Fi6@phppoll.org>

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

#include <stdlib.h>
#include <string.h>
#include "stdint.h"
#include <windows.h>

typedef struct
{
    char  sID[4];
    DWORD dwOutSize;
    DWORD dwWfxSize;
    /* WAVEFORMATEX */
    WORD  wFormatTag;
    WORD  nChannels;
    DWORD nSamplesPerSec;
    DWORD nAvgBytesPerSec;
    WORD  nBlockAlign;
    WORD  wBitsPerSample;
    DWORD cbSize;

    unsigned Frames;
    unsigned UTKDataSize;
} utkheader_t;

typedef struct
{
    const uint8_t *InData;
    unsigned UnreadBitsValue, UnreadBitsCount;
    int UseLattice;
    unsigned NoiseFloor;
    float FixedCodebook[64]; /* Fixed codebook gain matrix */
    float ImpulseTrain[12];  /* Impulse train matrix */
    float R[12];             /* Autocorrelation coefficient matrix */
    float Delay[324];
    float DecompressedBlock[432];
} utkparams_t;

#ifdef __cplusplus
extern "C" {
#endif

extern "C" __declspec(dllexport) int utk_read_header(utkheader_t * UTKHeader, const uint8_t * Buffer, unsigned FileSize);
extern "C" __declspec(dllexport) int utk_decode(const uint8_t *__restrict InBuffer, uint8_t *__restrict OutBuffer, unsigned Frames);
extern "C" __declspec(dllexport) void UTKGenerateTables(void);
/*uint8_t ReadCC(utkparams_t *p, uint8_t bits);*/
void SetUTKParameters(utkparams_t *p);
void DecompressBlock(utkparams_t *p);
void LatticeFilter(utkparams_t *p, int Voiced, float * Window, int Interval);
void Synthesize(utkparams_t *p, unsigned Sample, unsigned Blocks);
void PredictionFilter(const float *__restrict c2, float *__restrict Residual);

#ifdef __cplusplus
}
#endif