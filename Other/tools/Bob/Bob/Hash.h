#ifndef __HASH_H__

#define __HASH_H__

#define MAX_MATCH 1028
#define MIN_MATCH 3

#define MIN_LOOKAHEAD (MAX_MATCH+MIN_MATCH+1)

// corresponds to zlib compression level 9
#define GOOD_LENGTH 32
#define MAX_LAZY    258
#define NICE_LENGTH 258
#define MAX_CHAIN   4096

#define HASH_BITS 16
#define HASH_SIZE 65536
#define HASH_MASK 65535
#define HASH_SHIFT 6

#define W_SIZE 131072
#define MAX_DIST W_SIZE
#define W_MASK (W_SIZE-1)

// Note that mynew and mydelete don't call the constructor or destructor (we don't have any)
template<class T> 
static inline T* mynew(int n)
{
    int size = n * sizeof(T);
    size += !size;  // don't depend on behavior of malloc(0)
    return (T*)malloc(size);
}

inline void mydelete(void* p) { if (p) free(p); }

class Hash
{
private:
    unsigned hash;
    int *head, *prev;
public:
    Hash() {
        hash = 0;
        head = mynew<int>(HASH_SIZE);
        for (int i=0; i<HASH_SIZE; ++i)
            head[i] = -1;
        prev = mynew<int>(W_SIZE);
    }
    ~Hash() {
        mydelete(head);
        mydelete(prev);
    }

    int getprev(unsigned pos) const { return prev[pos & W_MASK]; }

    void update(unsigned c) {
        hash = ((hash << HASH_SHIFT) ^ c) & HASH_MASK;
    }

    int insert(unsigned pos) {
        int match_head = prev[pos & W_MASK] = head[hash];
        head[hash] = pos;
        return match_head;
    }
};

#endif