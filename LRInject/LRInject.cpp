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

bool isWin8OrHight()
{
	std::string vname;
	//先判断是否为win8.1或win10
	typedef void(__stdcall* NTPROC)(DWORD*, DWORD*, DWORD*);
	HINSTANCE hinst = LoadLibrary(L"ntdll.dll");
	DWORD dwMajor, dwMinor, dwBuildNumber;
	NTPROC proc = (NTPROC)GetProcAddress(hinst, "RtlGetNtVersionNumbers");
	proc(&dwMajor, &dwMinor, &dwBuildNumber);
	return dwMajor > 6 || (dwMajor == 6 && dwMinor >= 2);
}

extern "C"
{
	_declspec(dllexport) DWORD __stdcall LRInject(char* application, char* workpath, char* commandline, char* dllpath) {
		LRProfile beta;
		beta.CodePage = 950;//台湾地区codepage
		beta.HookIME = isWin8OrHight(); //win8或以上需要hook ime

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