#include <iostream>
#include <Windows.h>
#include <TlHelp32.h>
#include <ctime>
#include <string>
#include <fstream>
#include <cstdio>
#include <cstdlib>
#include <atlstr.h>

bool fileExist(const char* path)
{
	FILE* fp;
	for (int i = 0; i < 5; i++)
	{
		fopen_s(&fp, path, "r");
		if (fp != NULL)
		{
			fclose(fp);
			return true;
		}
		Sleep(10);
	}
	return false;
}
bool startProcess(const char* exePath, const char* cmdlineStr)
{
	CString cmdline;
	cmdline.Format("%s", cmdlineStr);
	PROCESS_INFORMATION pi = { 0 };
	STARTUPINFO si = { 0 };
	si.cb = sizeof(si);
	bool retVal = CreateProcess(NULL, cmdline.GetBuffer(), nullptr, nullptr, false, 0, nullptr, nullptr, &si, &pi);
	CloseHandle(pi.hProcess);
	CloseHandle(pi.hThread);
	return retVal;
}

DWORD getProcessID(const std::string& procName)
{
	DWORD id = 0;
	PROCESSENTRY32 pe = { sizeof(PROCESSENTRY32) };
	HANDLE processes = 0;
	while (!id)
	{
		processes = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
		if (Process32First(processes, &pe))
		{
			do
			{
				if (!strcmp(pe.szExeFile, procName.c_str()))
				{
					id = pe.th32ProcessID;
					break;
				}
			} while (Process32Next(processes, &pe));
		}
	}
	CloseHandle(processes);
	return id;
}
void inject(const std::string& processName, const std::string& dllPath, DWORD sleep = 0)
{
	std::cout << " --* inject sleep" << std::endl;
	Sleep(sleep);
	std::cout << " --* inject start" << std::endl;
	DWORD processId = getProcessID(processName);
	std::cout << " ----* inject get process id " << processId << std::endl;
	HANDLE process = OpenProcess(PROCESS_ALL_ACCESS, 0, processId);
	std::cout << " ----* inject get process , is Null?: " << (process == 0) << std::endl;
	void* address = VirtualAllocEx(process, 0, dllPath.size(), MEM_COMMIT, PAGE_READWRITE);
	std::cout << " ----* inject get addr " << address << std::endl;
	WriteProcessMemory(process, address, dllPath.c_str(), dllPath.size(), 0);
	std::cout << " ----* inject write mem" << std::endl;
	CreateRemoteThread(process, 0, 0, (LPTHREAD_START_ROUTINE)GetProcAddress(LoadLibrary("KERNEL32"), "LoadLibraryA"), address, 0, 0);
	std::cout << " ----* inject create thread" << std::endl;
	CloseHandle(process);
	std::cout << " --* inject end" << std::endl;
}
int ret = -10010;
int work()
{
	Sleep(100);
	std::ifstream inFile("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\DLLInjector_bak.txt");
	if (inFile.is_open() && inFile.good()) {
		std::cout << "Get DLLInjector_bak.txt && Read Config Begin" << std::endl;
		std::string procName = "", dllPath = "", exePath = "", arg = "";
		char procNameStr[1024], dllPathStr[1024], exePathStr[1024], argStr[1024];
		inFile.getline(procNameStr, 1024);
		procName = procNameStr;
		inFile.getline(dllPathStr, 1024);
		dllPath = dllPathStr;
		bool start = 1;
		//inFile >> (bool) start;
		inFile.getline(exePathStr, 1024);
		exePath = exePathStr;
		inFile.getline(argStr, 1024);
		arg = argStr;
		DWORD sleep = 0;
		inFile >> sleep;
		inFile.close();
		std::cout << procName << std::endl;
		std::cout << dllPath << std::endl;
		std::cout << exePath << std::endl;
		std::cout << arg << std::endl;
		std::cout << sleep << std::endl;
		std::string cmdline = exePath + " " + arg;
		//dllPath = "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\GreenLuma.dll\0";
		std::cout << '[' << cmdline << ']' << " procName=" << procName << ",dllPath=" << dllPath << std::endl;
		//验证
		std::cout << "Read Config End && Find DLL/EXE Begin" << std::endl;
		if (!fileExist(dllPathStr))
		{
			//system("dir C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector");
			return -10030;
		}
		std::cout << " --* dll exist" << std::endl;
		if (!fileExist(exePathStr))
		{
			//system("dir C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector");
			return -10040;
		}
		std::cout << " --* steam exist" << std::endl;
		//Start
		std::cout << "Find DLL/EXE End && StartProcess Begin" << std::endl;
		if (start)
			startProcess(exePath.c_str(), cmdline.c_str());
		std::cout << "StartProcess End && Inject Begin" << std::endl;
		inject(procName, dllPath, sleep);
		std::cout << "Inject End && Program End" << std::endl;
		return 0;
	}
	else {
		return -10050;
	}
	return 0;
}
int main(int argc, char** argv)
{
	time_t nowtime;
	time(&nowtime); //获取1900年1月1日0点0分0秒到现在经过的秒数
	tm p;
	localtime_s(&p, &nowtime); //将秒数转换为本地时间,年从1900算起,需要+1900,月为0-11,所以要+1
	printf("\nStart at %04d:%02d:%02d %02d:%02d:%02d\n", p.tm_year + 1900, p.tm_mon + 1, p.tm_mday, p.tm_hour, p.tm_min, p.tm_sec);
	FILE* fpstart;
	fopen_s(&fpstart, "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\bak_start.txt", "w");
	if (fpstart == NULL)
		ret = -10020;
	else
		fclose(fpstart);
	if (ret == -10010) ret = work();
	FILE* fpend;
	fopen_s(&fpend, "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\bak_end.txt", "w");
	if (fpend == NULL)
		return -10100;
	fprintf(fpend, "%d", ret);
	fclose(fpend);
	return ret;
}