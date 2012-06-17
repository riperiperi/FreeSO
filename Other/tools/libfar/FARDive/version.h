#define FD_VERSION_A	0
#define FD_VERSION_B	0
#define FD_VERSION_C	1
#define FD_REVISION		1

//You don't have to touch the following
#define xstr(x) str(x)
#define str(x) #x //Yes, double levels is required. See <http://gcc.gnu.org/onlinedocs/cpp/Stringification.html>
#define FDVERSION L"" \
	xstr(FD_VERSION_A) \
	L"." \
	xstr(FD_VERSION_B) \
	L"." \
	xstr(FD_VERSION_C) \
	L" (rev. " \
	xstr(FD_REVISION) \
	L")"
