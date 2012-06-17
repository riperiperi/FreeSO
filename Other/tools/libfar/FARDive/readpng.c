#define UNICODE
#define _WIN32_WINNT 0x0500
#include <windows.h>
#include "libpng-1.5.0/png.h"
#include "interface.h"

unsigned int filesize;
char * sourcebuffer, * seekedbuffer;

HINSTANCE hInst;

void user_read_data(png_structp png_ptr, png_bytep data, png_size_t length){
	png_ptr = png_ptr;
	memcpy(data, seekedbuffer, length);
	seekedbuffer += length;
}

HWND FDhWnd, statusbar;
unsigned int statusbarheight;

OPENFILENAME ofn;

wchar_t filename[256], usercustomfilter[256];

MENUITEMINFO mii;

HMENU hMenu;

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow){
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);
	UNREFERENCED_PARAMETER(nCmdShow);
	hInst = hInstance;
	
	{
	WNDCLASS wc;

	wc.style			= CS_HREDRAW | CS_VREDRAW;
	wc.lpfnWndProc		= WndProc;
	wc.cbClsExtra		= 0;
	wc.cbWndExtra		= 0;
	wc.hInstance		= hInst;
	wc.hIcon			= LoadImage(hInst, MAKEINTRESOURCE(IDI_FARDIVE), IMAGE_ICON, 16, 16, LR_DEFAULTCOLOR);
	wc.hCursor			= (HCURSOR) LoadImage(NULL, IDC_ARROW, IMAGE_CURSOR, 0, 0, LR_SHARED | LR_DEFAULTSIZE);
	wc.hbrBackground	= (HBRUSH) (COLOR_MENU+1);
	wc.lpszMenuName		= MAKEINTRESOURCE(IDM_FARDIVE);
	wc.lpszClassName	= L"F";

	RegisterClass(&wc);
	}
	
	{
	FDhWnd = CreateWindowEx(WS_EX_ACCEPTFILES | WS_EX_COMPOSITED, L"F", L"FARDive", WS_OVERLAPPEDWINDOW | WS_VISIBLE, CW_USEDEFAULT, CW_USEDEFAULT, 616, 616, 0, 0, hInst, 0);\
	statusbar = CreateWindowEx(0, L"msctls_statusbar32", NULL, WS_CHILD | WS_VISIBLE, 0, 0, 0, 0, FDhWnd, 0, hInst, 0);
	RECT rect;
	GetWindowRect(statusbar, &rect);
	statusbarheight = rect.bottom - rect.top;
	
	memset(&ofn, sizeof(ofn), 0x00);
	ofn.lStructSize = sizeof(OPENFILENAME);
	ofn.hwndOwner = FDhWnd;
	ofn.lpstrFilter = L"All supported archives (*.far, *.dbpf, *.dat)\0*.far;*.dbpf;*.dat\0FAR (*.far, *.dat)\0*.far;*.dat\0DBPF (*.dbpf, *.dat)\0*.dbpf;*.dat\0All Files\0*.*\0\0";
	ofn.lpstrCustomFilter = usercustomfilter;
	ofn.lpstrCustomFilter[0] = '\0';
	ofn.nMaxCustFilter = 256;
	ofn.nFilterIndex = 1;
	ofn.lpstrFile = filename;
	ofn.lpstrFile[0] = '\0';
	ofn.nMaxFile = 256;
	ofn.lpstrFileTitle = NULL;
	ofn.lpstrInitialDir = NULL;
	ofn.lpstrTitle = NULL;
	ofn.Flags = OFN_DONTADDTORECENT | OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;
	ofn.lpstrDefExt = L"dat";
	ofn.FlagsEx = 0;
	
	DWORD menucolor = GetSysColor(COLOR_MENU);
	
	png_color_16 my_background;
	my_background.red	= GetRValue(menucolor);
	my_background.green	= GetGValue(menucolor);
	my_background.blue	= GetBValue(menucolor);
	
	HRSRC resource;
	
	BITMAPINFO bmi;
	bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	bmi.bmiHeader.biPlanes = 1;
	bmi.bmiHeader.biBitCount = 24;
	bmi.bmiHeader.biCompression = BI_RGB;
	bmi.bmiHeader.biSizeImage = 0;
	bmi.bmiHeader.biXPelsPerMeter = 0;
	bmi.bmiHeader.biYPelsPerMeter = 0;
	bmi.bmiHeader.biClrUsed = 0;
	bmi.bmiHeader.biClrImportant = 0;

	mii.cbSize = sizeof(MENUITEMINFO);
	mii.fMask = MIIM_BITMAP;
	
	HDC hDC = GetDC(FDhWnd);
	hMenu = GetMenu(FDhWnd);
	
	unsigned int iconindex;
	for(iconindex=0; iconindex<sizeof(iconmenulist)/sizeof(WORD); iconindex++){
		resource = FindResource(NULL, MAKEINTRESOURCE(iconmenulist[iconindex]), RT_RCDATA);
		filesize = SizeofResource(NULL, resource);
		sourcebuffer = LockResource(LoadResource(NULL, resource)), seekedbuffer = sourcebuffer;
	
		png_structp png_ptr = png_create_read_struct(PNG_LIBPNG_VER_STRING, NULL, NULL, NULL);
	
		png_set_read_fn(png_ptr, NULL, user_read_data);
	
		png_infop info_ptr = png_create_info_struct(png_ptr);
	
		png_read_info(png_ptr, info_ptr);
	
		png_uint_32 width, height;
		int bit_depth, color_type;
	
		png_get_IHDR(png_ptr, info_ptr, &width, &height, &bit_depth, &color_type, NULL, NULL, NULL);
	
		if(color_type == PNG_COLOR_TYPE_GRAY || color_type == PNG_COLOR_TYPE_GRAY_ALPHA)
			png_set_gray_to_rgb(png_ptr);
		else if(color_type == PNG_COLOR_TYPE_PALETTE || color_type == PNG_COLOR_MASK_PALETTE)
			png_set_palette_to_rgb(png_ptr);
	
		png_set_bgr(png_ptr);
	
		png_set_background(png_ptr, &my_background, PNG_BACKGROUND_GAMMA_SCREEN, 0, 1.0);
	
		png_read_update_info(png_ptr, info_ptr);
	
		png_uint_32 rowbytes = png_get_rowbytes(png_ptr, info_ptr);
	
		png_bytep row_pointers[height];
		int i = height-1;
		row_pointers[i] = malloc(rowbytes*height);
		for(i--; i>=0; i--)
			row_pointers[i] = row_pointers[i+1]+rowbytes;
	
		png_read_image(png_ptr, row_pointers);
	
		png_destroy_read_struct(&png_ptr, &info_ptr, NULL);
	
		bmi.bmiHeader.biWidth = width;
		bmi.bmiHeader.biHeight = height;
		HBITMAP icon = CreateDIBitmap(hDC, &bmi.bmiHeader, CBM_INIT, row_pointers[height-1], &bmi, DIB_RGB_COLORS);
		mii.hbmpItem = icon;
	
		free(row_pointers[height-1]);
	
		SetMenuItemInfo(hMenu, iconmenulist[iconindex]-1000, FALSE, &mii);
	}
	}
	
	MSG msg;
	while(GetMessage(&msg, NULL, 0, 0)){
		DispatchMessage(&msg);
	}
	return 0;
}