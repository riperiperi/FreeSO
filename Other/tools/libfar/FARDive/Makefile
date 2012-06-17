# macros --------------------------------------------------------------------
CC = mingw32-gcc
LD = mingw32-gcc
CFLAGS = -Wall -Wextra -Wabi -march=i686 -O3 -fomit-frame-pointer -ffast-math -maccumulate-outgoing-args -g0 -fno-exceptions -combine -flto
LDFLAGS = -s -combine -flto

WINDRES	= windres
STRIP	= strip

LIBS = -mwindows -L. -lpng15

OBJS = ./obj/readpng.o ./obj/interface.o ./obj/resource.res

# dependencies --------------------------------------------------------------
all: ./FARDive.exe
./FARDive.exe: $(OBJS)
	$(LD) $(LDFLAGS) -L. -o $@ $(OBJS) $(LIBS)
	strip -s -g --strip-unneeded -x -X -v $@

# make rules ----------------------------------------------------------------
./obj/%.o: %.c
	$(CC) -c $(CFLAGS) -o $@ $<

./obj/%.res: %.rc
	windres -O coff -i $< -o $@

# maintenance ---------------------------------------------------------------
clean:
	del /Q /S FARDive.exe obj