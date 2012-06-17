#define UNICODE
#define _WIN32_WINNT 0x0500
#include <windows.h>
#include "interface.h"

extern HWND FDhWnd, statusbar;
extern HMENU hMenu;
extern OPENFILENAME ofn;
extern wchar_t filename;
extern MENUITEMINFO mii;

int archivetype;
HWND box = NULL;
extern unsigned int statusbarheight;

LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	switch(message)
	{
	case WM_SIZE:
		SendMessage(statusbar, WM_SIZE, 0, 0);
		
		if(box != NULL)
			SetWindowPos(box, 0, 0, 0, 192, HIWORD(lParam)-statusbarheight-10, SWP_ASYNCWINDOWPOS | SWP_NOMOVE | SWP_NOOWNERZORDER);
		
		break;
	case WM_COMMAND:
		switch(LOWORD(wParam))
		{
		case ID_FILE_NEWFARV1:
		case ID_FILE_NEWFARV3:
		case ID_FILE_NEWDBPF: {
			SetWindowText(hWnd, L"Untitled - FARDive");
			SetWindowText(statusbar, L"0 files");
			
			archivetype = LOWORD(wParam)-ID_FILE_NEWFARV1;
			
			mii.fMask = MIIM_STATE;
			mii.fState = MFS_ENABLED;
			
			SetMenuItemInfo(hMenu, ID_FILE_SAVE, FALSE, &mii);
			SetMenuItemInfo(hMenu, ID_FILE_SAVEAS, FALSE, &mii);
			SetMenuItemInfo(hMenu, ID_FILE_ADD, FALSE, &mii);
			SetMenuItemInfo(hMenu, ID_FILE_CHANGETYPE, FALSE, &mii);
			
			RECT ClientRect;
			GetClientRect(hWnd, &ClientRect);
			box = CreateWindowEx(0, L"LISTBOX", NULL, LBS_NOINTEGRALHEIGHT | WS_CHILD | WS_BORDER | WS_VISIBLE, 5, 5, 192, ClientRect.bottom-statusbarheight-10, hWnd, NULL, NULL, NULL);
			} break;
		case ID_FILE_OPEN:
			GetOpenFileName(&ofn);
			break;
		case ID_HELP_ABOUT:
			MessageBox(FDhWnd, L"FARDive version " FDVERSION
				L"\r\nReleased in to the public domain.\r\n\r\n"
				L"This is an alpha release of FARDive. The About box is not yet complete.\r\n\r\n"
				L"Don't worry, file writing will not be implemented until it is guaranteed stable.\r\n"
				L"File -> Save as won't do anything until we reach that point.", L"About", MB_OK);
			break;
		case ID_FILE_EXIT:
			PostQuitMessage(0);
			break;
		}
		break;
	case WM_DESTROY:
		PostQuitMessage(0);
		break;
	default:
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return 0;
}