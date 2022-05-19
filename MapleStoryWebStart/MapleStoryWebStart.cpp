// MapleStoryWebStart.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//
#define _CRT_SECURE_NO_WARNINGS
#include <iostream>
#include <Windows.h>
#include <detours.h>
#include"LRCommonLibrary.h"
#include <Shlwapi.h>
#include <direct.h>

/*
安装：
1.判断是否有BakPath项，无就先备份
2.写入游戏目录(GamePath)、本程序目录（Path）
卸载：
1.读取BakPath，写入Path，删除GamePath和BakPath
修复：
拖入游戏主体文件，修改GamePath、BakPath
启动：读取GamePath，然后启动游戏
*/

const LPCWSTR lpSubKey = _T("SOFTWARE\\GAMANIA\\MAPLESTORY");
const LPCWSTR GameName = _T("GamePath");
const wchar_t* wGameExe = L"MapleStory.exe";
const char* dllName = "LRHookx64.dll";

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

LPSTR WideCharToMultiByteInternal(LPCWSTR wstr, UINT CodePage = CP_ACP)
{
	int wsize = lstrlenW(wstr)/* size without '\0' */, n = 0;
	int lsize = (wsize + 1) << 1;
	LPSTR lstr = (LPSTR)HeapAlloc(GetProcessHeap(), 0, lsize);
	if (lstr) {
		n = WideCharToMultiByte(CodePage, 0, wstr, wsize, lstr, lsize, NULL, NULL);
		lstr[n] = '\0'; // make tail ! 
	}
	return lstr;
}

LPWSTR MultiByteToWideCharInternal(LPCSTR lstr, UINT CodePage = CP_ACP)
{
	int lsize = lstrlenA(lstr)/* size without '\0' */, n = 0;
	int wsize = (lsize + 1) << 1;
	LPWSTR wstr = (LPWSTR)HeapAlloc(GetProcessHeap(), 0, wsize);
	if (wstr) {
		n = MultiByteToWideChar(CodePage, 0, lstr, lsize, wstr, wsize);
		wstr[n] = L'\0'; // make tail ! 
	}
	return wstr;
}

LSTATUS readReg(LPCWSTR name, wchar_t* dwValue)//读取操作表,其类型为REG_SZ
{
	HKEY hkey;
	LSTATUS status = ::RegCreateKey(HKEY_LOCAL_MACHINE, lpSubKey, &hkey);

	if (ERROR_SUCCESS == status)
	{
		DWORD dwSzType = REG_SZ;
		DWORD dwSize = MAX_PATH;
		status = ::RegQueryValueEx(hkey, name, 0, &dwSzType, (LPBYTE)dwValue, &dwSize);
	}
	::RegCloseKey(hkey);
	return status;
}

void LRInject(LPCWSTR application, LPCWSTR workpath, LPWSTR commandline, char* dllpath)
{
	LRProfile beta;
	beta.CodePage = 950;//台湾地区codepage
	beta.HookIME = isWin8OrHight(); //win8或以上需要hook ime

	LRConfigFileMap filemap;
	filemap.WrtieConfigFileMap(&beta);

	STARTUPINFOW si;
	PROCESS_INFORMATION pi;
	ZeroMemory(&si, sizeof(STARTUPINFO));
	ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
	si.cb = sizeof(STARTUPINFO);
	//std::cout << beta.CodePage;

	DetourCreateProcessWithDllExW(application, commandline, NULL,
		NULL, FALSE, CREATE_DEFAULT_ERROR_MODE, NULL, workpath,
		&si, &pi, dllpath, NULL);
}

int main(int argc, char* argv[])
{
	wchar_t p[MAX_PATH];
	LSTATUS status = readReg(GameName, p);
	if (ERROR_SUCCESS != status)
	{
		printf("因为读取游戏目录出错，启动游戏失败！\n");
		printf("请检查是否已使用Setting.exe进行设置\n");
		system("pause");
		return 0;
	}
	wchar_t application[MAX_PATH];
	wcscpy(application, p);
	wcscat(application, L"\\");
	wcscat(application, wGameExe);

	wchar_t commandline[MAX_PATH];
	wcscpy(commandline, L"\"");
	wcscat(commandline, p);
	wcscat(commandline, L"\"");
	if (argc > 1)
	{
		for (size_t i = 1; i < argc; i++)
		{
			wcscat(commandline, L" ");
			wcscat(commandline, MultiByteToWideCharInternal(argv[i]));
		}
	}

	char *dp;
	dp = argv[0];
	dp[strlen(dp) - 14] = '\0';
	strcat(dp, dllName);
	LRInject(application, p, commandline, dp);
	return 0;
}