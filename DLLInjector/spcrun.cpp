#include<iostream>
#include<string>
#include<cstdlib>
#include<cstdio>
#include<windows.h>
#include<direct.h>
#include<algorithm>

#pragma comment( linker, "/subsystem:\"windows\" /entry:\"mainCRTStartup\"" )

using namespace std;

void HideWindowSafe() {
	HWND hwnd = NULL;
	DWORD myPID = GetCurrentProcessId();
	DWORD winPID = 0;

	hwnd = GetConsoleWindow();

	if (hwnd == NULL) {
		hwnd = GetForegroundWindow();
		if (hwnd) {
			GetWindowThreadProcessId(hwnd, &winPID);
			if (winPID != myPID) {
				hwnd = NULL;
			}
			else {
				char title[MAX_PATH];
				if (GetWindowTextA(hwnd, title, MAX_PATH) > 0) {
					string sTitle = title;
					transform(sTitle.begin(), sTitle.end(), sTitle.begin(), ::tolower);
				}
			}
		}
	}

	if (hwnd != NULL) {
		ShowWindow(hwnd, SW_HIDE);
	}
}

int main(int argc, char* args[])
{
	HideWindowSafe();

	string path = "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector";
	string logPath = path + "\\log.txt";
	string logErrPath = path + "\\logerr.txt";

	freopen(logPath.data(), "a+", stdout);
	freopen(logErrPath.data(), "a+", stderr);

	FILE* fp;
	int runModel = 0;
	fp = fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\DLLInjector.ini", "r");
	if (fp != NULL)
	{
		runModel = 1;
		fclose(fp);
	}
	else
	{
		fp = fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\DLLInjector_bak.txt", "r");
		if (fp != NULL)
		{
			runModel = 2;
			fclose(fp);
		}
		else
		{
			fprintf(stderr, "Error: Config loss\n");
			fclose(stdout);
			fclose(stderr);
			return -1;
		}
	}

	system("echo [SpcRunStart]");
	string cmd = "";
	string exeName = "DLLInjector.exe";
	if (runModel == 1)
	{
		system("echo [DLLInjector_Start]");
	}
	if (runModel == 2)
	{
		system("echo [DLLInjector_bak_Start]");
		exeName = "DLLInjector_bak.exe";
	}

	cmd = cmd + "cd /d \"" + path + "\" &dir&echo.&echo.&echo.&echo ---Start---&echo.&echo.&echo.&\".\\" + exeName + "\"";

	system("%SystemRoot%\\system32\\chcp.com 437 2>nul");
	int res = system(cmd.data());

	//ExitCode
	fclose(stdout);
	fclose(stderr);
	fp = fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\ExitCode.txt", "w");
	fprintf(fp, "%d", res);

	//close file
	fclose(fp);
	return res;
}