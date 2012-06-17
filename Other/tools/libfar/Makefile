# macros --------------------------------------------------------------------
CC = mingw32-gcc
LD = mingw32-gcc
CFLAGS = -Wall -Wextra -Wabi -march=i686 -O3 -fomit-frame-pointer -ffast-math -maccumulate-outgoing-args -g0 -fno-exceptions -combine -flto
LDFLAGS = -s -combine -flto

AR = ar

LIBS = -mconsole -lfar-1

LIBFAR_OBJS = ./obj/src/libfar.o
EXAMPLE_OBJS = ./obj/libfar_example.o

# dependencies --------------------------------------------------------------
all: ./libfar.exe

./libfar.exe: ./libfar.a ./libfar-1.dll $(EXAMPLE_OBJS)
	$(LD) $(LDFLAGS) -L. -o $@ $(EXAMPLE_OBJS) $(LIBS)

./obj/libs/libfar.a: $(LIBFAR_OBJS)
	$(AR) -cvq $@ $<
./libfar.a: ./obj/libs/libfar.a
	copy /B /Y $(subst /,\,$<) $@

./obj/libs/libfar-1.dll: $(LIBFAR_OBJS)
	$(CC) $(LDFLAGS) -shared -o $@ $<
./libfar-1.dll: ./obj/libs/libfar-1.dll
	copy /B /Y $(subst /,\,$<) $@

# make rules ----------------------------------------------------------------
./obj/%.o: %.c
	$(CC) -ansi -pedantic -c $(CFLAGS) -o $@ $< 

./obj/%.o: %.cpp
	$(CC) -c $(CFLAGS) -o $@ $<

# maintenance ---------------------------------------------------------------
clean:
	del /Q /S libfar.exe obj libfar.a libfar-1.dll