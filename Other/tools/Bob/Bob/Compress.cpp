#include "stdafx.h"
#include "Compress.h"

/*
 * The following two functions (longest_match and compress) are loosely
 * adapted from zlib 1.2.3's deflate.c, and are probably still covered by
 * the zlib license, which carries this notice:
 */
/* zlib.h -- interface of the 'zlib' general purpose compression library
  version 1.2.3, July 18th, 2005

  Copyright (C) 1995-2005 Jean-loup Gailly and Mark Adler

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jean-loup Gailly        Mark Adler
  jloup@gzip.org          madler@alumni.caltech.edu


  The data format used by the zlib library is described by RFCs (Request for
  Comments) 1950 to 1952 in the files http://www.ietf.org/rfc/rfc1950.txt
  (zlib format), rfc1951.txt (deflate format) and rfc1952.txt (gzip format).
*/


static inline unsigned longest_match(
	int cur_match, 
	const Hash& hash,
    const byte* const src,
    const byte* const srcend,
    unsigned const pos,
    unsigned const remaining,
    unsigned const prev_length,
    unsigned* pmatch_start)
{
    unsigned chain_length = MAX_CHAIN;         /* max hash chain length */
    unsigned int best_len = prev_length;       /* best match length so far */
    unsigned int nice_match = NICE_LENGTH;     /* stop if match long enough */
    int limit = pos > MAX_DIST ? pos - MAX_DIST + 1 : 0;
    /* Stop when cur_match becomes < limit. */

    const byte* const scan = src+pos;

    /* This is important to avoid reading past the end of the memory block */
    if (best_len >= remaining)
        return remaining;

    const unsigned int max_match = (remaining < MAX_MATCH) ? remaining : MAX_MATCH;
    byte scan_end1  = scan[best_len-1];
    byte scan_end   = scan[best_len];

    /* Do not waste too much time if we already have a good match: */
    if (prev_length >= GOOD_LENGTH) {
        chain_length >>= 2;
    }
    /* Do not look for matches beyond the end of the input. This is necessary
     * to make deflate deterministic.
     */
    if ((unsigned)nice_match > remaining) nice_match = remaining;

    do {
        assert(cur_match < pos);
        const byte* match = src + cur_match;

        /* Skip to next match if the match length cannot increase
         * or if the match length is less than 2.
         */
        if (match[best_len]   != scan_end  ||
            match[best_len-1] != scan_end1 ||
            match[0]          != scan[0]   ||
            match[1]          != scan[1])      continue;

        /* It is not necessary to compare scan[2] and match[2] since they
         * are always equal when the other bytes match, given that
         * the hash keys are equal and that HASH_BITS >= 8.
         */
        assert(scan[2] == match[2]);

        unsigned int len = 2;
        do { ++len; } while (len < max_match && scan[len] == match[len]);

        if (len > best_len) {
            *pmatch_start = cur_match;
            best_len = len;
            if (len >= nice_match || scan+len >= srcend) break;
            scan_end1  = scan[best_len-1];
            scan_end   = scan[best_len];
        }
    } while ((cur_match = hash.getprev(cur_match)) >= limit
             && --chain_length > 0);

    return best_len;
}

/* Returns the end of the compressed data if successful, or NULL if we overran the output buffer */

byte *compress(const byte* src, const byte* srcend, byte* dst, byte* dstend, bool pad)
{
    unsigned match_start = 0;
    unsigned match_length = MIN_MATCH-1;           /* length of best match */
    bool match_available = false;         /* set if previous match exists */

    unsigned pos = 0, remaining = srcend - src;

    if (remaining >= 16777216) return 0;

    CompressedOutput compressed_output(src, dst+sizeof(dbpf_compressed_file_header), dstend);

    Hash hash;
    hash.update(src[0]);
    hash.update(src[1]);

    while (remaining) {

        unsigned prev_length = match_length;
        unsigned prev_match = match_start;
        match_length = MIN_MATCH-1;

        int hash_head = -1;

        if (remaining >= MIN_MATCH) {
            hash.update(src[pos + MIN_MATCH-1]);
            hash_head = hash.insert(pos);
        }

        if (hash_head >= 0 && prev_length < MAX_LAZY && pos - hash_head <= MAX_DIST) {

            match_length = longest_match (hash_head, hash, src, srcend, pos, remaining, prev_length, &match_start);

            /* If we can't encode it, drop it. */
            if ((match_length <= 3 && pos - match_start > 1024) || (match_length <= 4 && pos - match_start > 16384))
                match_length = MIN_MATCH-1;
        }
        /* If there was a match at the previous step and the current
         * match is not better, output the previous match:
         */
        if (prev_length >= MIN_MATCH && match_length <= prev_length) {

            if (!compressed_output.emit(prev_match, pos-1, prev_length))
                return 0;

            /* Insert in hash table all strings up to the end of the match.
             * pos-1 and pos are already inserted. If there is not
             * enough lookahead, the last two strings are not inserted in
             * the hash table.
             */
            remaining -= prev_length-1;
            prev_length -= 2;
            do {
                ++pos;
                if (src+pos <= srcend-MIN_MATCH) {
                    hash.update(src[pos + MIN_MATCH-1]);
                    hash.insert(pos);
                }
            } while (--prev_length != 0);
            match_available = false;
            match_length = MIN_MATCH-1;
            ++pos;

        } else  {
            match_available = true;
            ++pos;
            --remaining;
        }
    }
    assert(pos == srcend - src);
    if (!compressed_output.emit(pos, pos, 0))
        return 0;

    byte* dstsize = compressed_output.get_end();
    if (pad && dstsize < dstend) {
        memset(dstsize, 0xFC, dstend-dstsize);
        dstsize = dstend;
    }

    dbpf_compressed_file_header* hdr = (dbpf_compressed_file_header*)dst;
    put(hdr->compressed_size, dstsize - dst);
    put(hdr->compression_id, DBPF_COMPRESSION_QFS);
    put(hdr->uncompressed_size, srcend-src);

    return dstsize;
}