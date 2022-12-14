#include <Windows.h>
#include <detours.h>
#include <iostream>

struct LRProfile
{
	UINT CodePage;
	int HookIME;
	char lfFaceName[LF_FACESIZE] = "None";
};

int WrtieConfigFileMap(LRProfile* profile)
{
	SetEnvironmentVariableW(L"LRCodePage", (LPCWSTR)&profile->CodePage);
	SetEnvironmentVariableW(L"LRHookIME", (LPCWSTR)&profile->HookIME);
	SetEnvironmentVariableA("LRFaceName", (LPCSTR)&profile->lfFaceName);
	return 0;
}

extern "C"
{
	_declspec(dllexport) DWORD __stdcall LRInject(char* application, char* workpath, char* commandline, char* dllpath, UINT CodePage, bool HookIME) {
		LRProfile beta;
		beta.CodePage = CodePage;
		beta.HookIME = HookIME; //win8��������Ҫhook ime

		WrtieConfigFileMap(&beta);

		STARTUPINFOA si;
		PROCESS_INFORMATION pi;
		ZeroMemory(&si, sizeof(STARTUPINFO));
		ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
		si.cb = sizeof(STARTUPINFO);

		if (DetourCreateProcessWithDllExA(application, commandline, NULL, NULL, FALSE, CREATE_DEFAULT_ERROR_MODE, NULL, workpath, &si, &pi, dllpath, NULL))
		{
			return pi.dwProcessId;
		}
		return 0;
	}
}