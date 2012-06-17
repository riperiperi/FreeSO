#ifndef __COMPRESS_H__
#define __COMPRESS_H__

#include "CompressedOutput.h"
#include "Hash.h"

#define _X86_

#ifdef _X86_   // little-endian and no alignment restrictions
  static inline unsigned get(const word& w)     { return *(unsigned short*)&w; }
  static inline unsigned get(const dword& dw)   { return *(unsigned*)&dw; }
  static inline void put(word& w, unsigned x)   { *(unsigned short*)&w = x; }
  static inline void put(dword& dw, unsigned x) { *(unsigned*)&dw = x; }
#else
  static inline unsigned get(const word& w)     { return w.lo + w.hi * 256; }
  static inline unsigned get(const dword& dw)   { return get(dw.lo) + get(dw.hi) * 65536; }
  static inline void put(word& w, unsigned x)   { w.lo = x; w.hi = x >> 8; }
  static inline void put(dword& dw, unsigned x) { put(dw.lo, x); put(dw.hi, x >> 16); }
#endif

struct word3be { byte hi,mid,lo; };

static inline unsigned get(const word3be& w3)   { return w3.hi * 65536 + w3.mid * 256 + w3.lo; }
static inline void put(word3be& w3, unsigned x) { w3.hi = x >> 16; w3.mid = x >> 8; w3.lo = x; }

struct dbpf_compressed_file_header  // 9 bytes
{
    dword compressed_size;
    word compression_id;       // DBPF_COMPRESSION_QFS
    word3be uncompressed_size;
};

#define DBPF_COMPRESSION_QFS (0xFB10)

static inline unsigned longest_match(
	int cur_match, 
	const Hash& hash,
    const byte* const src,
    const byte* const srcend,
    unsigned const pos,
    unsigned const remaining,
    unsigned const prev_length,
    unsigned* pmatch_start);
/*static*/ byte* compress(const byte* src, const byte* srcend, byte* dst, byte* dstend, bool pad);

#endif