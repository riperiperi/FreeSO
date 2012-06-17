# makefile for libpng using gcc (generic, static library)
# Copyright (C) 2008 Glenn Randers-Pehrson
# Copyright (C) 2000 Cosmin Truta
# Copyright (C) 1995 Guy Eric Schalnat, Group 42, Inc.
#
# This code is released under the libpng license.
# For conditions of distribution and use, see the disclaimer
# and license in png.h

# Location of the zlib library and include files

# Compiler, linker, lib and other tools
CC = mingw32-gcc
LD = $(CC)
AR_RC = ar rcs
RANLIB = ranlib
RM_F = del /Q /S

CDEBUG = -g -DPNG_DEBUG=5
LDDEBUG =
CRELEASE = -Os -march=i686 -fomit-frame-pointer -ffast-math -funsafe-loop-optimizations -ftree-loop-linear -g0 -fno-exceptions -combine
LDRELEASE = -s -combine
#CFLAGS = -W -Wall $(CDEBUG)
CFLAGS = -W -Wall $(CRELEASE)
#LDFLAGS = $(LDDEBUG)
LDFLAGS = $(LDRELEASE)
LIBS = zlib1.dll

# File extensions
O=.o
A=.a
EXE=

# Variables
OBJS =  png$(O) pngerror$(O) pngget$(O) pngmem$(O) pngpread$(O) \
	pngread$(O) pngrio$(O) pngrtran$(O) pngrutil$(O) pngset$(O) \
	pngtrans$(O) pngwio$(O) pngwrite$(O) pngwtran$(O) pngwutil$(O)

# Targets
all: static shared

# see scripts/pnglibconf.mak for more options
pnglibconf.h: scripts/pnglibconf.h.prebuilt
	copy "scripts\pnglibconf.h.prebuilt" $@

.c$(O):
	$(CC) -c $(CFLAGS) $<

static: libpng$(A) pngtest$(EXE)

shared: libpng15.dll

libpng$(A): $(OBJS)
	$(AR_RC) $@ $(OBJS)
	$(RANLIB) $@
	strip -s -g --strip-unneeded -x -X -v $@

libpng15.dll:
	$(LD) -shared -o $@ $(OBJS) $(LIBS)
	strip -s -g --strip-unneeded -x -X -v $@

test: pngtest$(EXE)
	./pngtest$(EXE)

pngtest$(EXE): pngtest$(O) libpng$(A)
	$(LD) $(LDFLAGS) -o $@ pngtest$(O) libpng$(A) $(LIBS)

clean:
	$(RM_F) *$(O) libpng$(A) pngtest$(EXE) pngout.png pnglibconf.h

png$(O):      png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngerror$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngget$(O):   png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngmem$(O):   png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngpread$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngread$(O):  png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngrio$(O):   png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngrtran$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngrutil$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngset$(O):   png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngtrans$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngwio$(O):   png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngwrite$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngwtran$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h
pngwutil$(O): png.h pngconf.h pnglibconf.h pngpriv.h pngstruct.h pnginfo.h pngdebug.h

pngtest$(O):  png.h pngconf.h pnglibconf.h
