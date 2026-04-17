#include <iostream>
#include <Windows.h>
#include <TlHelp32.h>
#include <ctime>
#include <string>
#include <fstream>
#include <cstdio>
#include <cstdlib>
#include <vector>

bool fileExist(const char* path)
{
	FILE* fp;
	for (int i = 0; i < 20; i++)
	{
		errno_t err = fopen_s(&fp, path, "r");
		if (err != 0) {
	        printf("Windows Error Code: 0x%08x\n", GetLastError());
	    }
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
	std::string cmdline = cmdlineStr;
	PROCESS_INFORMATION pi = { 0 };
	STARTUPINFO si = { 0 };
	si.cb = sizeof(si);
	bool retVal = CreateProcess(NULL, &cmdline[0], nullptr, nullptr, false, 0, nullptr, nullptr, &si, &pi);
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
DWORD_PTR GetRemoteModuleHandle(DWORD pid, const char* moduleName)
{
	HANDLE hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, pid);
	if (hSnap == INVALID_HANDLE_VALUE) return 0;
	MODULEENTRY32 mod = { sizeof(MODULEENTRY32) };
	if (Module32First(hSnap, &mod))
	{
		do
		{
			if (_stricmp(mod.szModule, moduleName) == 0)
			{
				CloseHandle(hSnap);
				return (DWORD_PTR)mod.modBaseAddr;
			}
		} while (Module32Next(hSnap, &mod));
	}
	CloseHandle(hSnap);
	return 0;
}

void inject(const std::string& processName, const std::string& dllPath, const std::string& iniPath, DWORD sleep = 0)
{
	std::cout << " --* inject sleep" << std::endl;
	Sleep(sleep);
	std::cout << " --* inject start" << std::endl;
	DWORD processId = getProcessID(processName);
	std::cout << " ----* inject get process id " << processId << std::endl;
	HANDLE process = OpenProcess(PROCESS_ALL_ACCESS, 0, processId);
	std::cout << " ----* inject get process , is Null?: " << (process == 0) << std::endl;
	
	void* address = VirtualAllocEx(process, 0, dllPath.size() + 1, MEM_COMMIT, PAGE_READWRITE);
	std::cout << " ----* inject get addr " << address << std::endl;
	
	WriteProcessMemory(process, address, dllPath.c_str(), dllPath.size() + 1, 0);
	std::cout << " ----* inject write mem" << std::endl;
	
	HANDLE hThread = CreateRemoteThread(process, 0, 0, (LPTHREAD_START_ROUTINE)GetProcAddress(LoadLibrary("KERNEL32"), "LoadLibraryA"), address, 0, 0);
	std::cout << " ----* inject create thread" << std::endl;
	
	if (hThread) {
		WaitForSingleObject(hThread, INFINITE);
		CloseHandle(hThread);
		
		std::string dllName = dllPath;
		size_t lastSlash = dllName.find_last_of("\\/");
		if (lastSlash != std::string::npos) {
			dllName = dllName.substr(lastSlash + 1);
		}
		
		std::cout << " ----* waiting for module to load: " << dllName << std::endl;
		DWORD_PTR remoteModBase = 0;
		for(int i=0; i<20; i++) {
			remoteModBase = GetRemoteModuleHandle(processId, dllName.c_str());
			if(remoteModBase) break;
			Sleep(100);
		}
		
		if (remoteModBase) {
			std::cout << " ----* found remote module base: " << (void*)remoteModBase << std::endl;
			
			HMODULE hLocal = LoadLibraryEx(dllPath.c_str(), NULL, DONT_RESOLVE_DLL_REFERENCES);
			if (hLocal) {
				FARPROC pInit = GetProcAddress(hLocal, "Init");
				if (pInit) {
					DWORD_PTR offset = (DWORD_PTR)pInit - (DWORD_PTR)hLocal;
					DWORD_PTR remoteInit = remoteModBase + offset;
					std::cout << " ----* found Init offset: " << (void*)offset << " calling remote Init at: " << (void*)remoteInit << std::endl;
					
					HANDLE hInitThread = CreateRemoteThread(process, 0, 0, (LPTHREAD_START_ROUTINE)remoteInit, address, 0, 0);
					if (hInitThread) {
						WaitForSingleObject(hInitThread, INFINITE);
						
						// Get exit code to debug
						DWORD exitCode = 0;
						GetExitCodeThread(hInitThread, &exitCode);
						std::cout << " ----* Init called successfully, exit code: " << exitCode << std::endl;
						
						CloseHandle(hInitThread);
					} else {
						std::cout << " ----* Failed to create remote thread for Init: " << GetLastError() << std::endl;
					}
				} else {
					std::cout << " ----* Failed to find Init export" << std::endl;
				}
				FreeLibrary(hLocal);
			} else {
				std::cout << " ----* Failed to load local library: " << GetLastError() << std::endl;
			}
		} else {
			std::cout << " ----* Failed to find remote module base" << std::endl;
		}
	}
	
	// Cleanup
	VirtualFreeEx(process, address, 0, MEM_RELEASE);
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
		std::vector<std::string> dlls;
		std::string dllPathStrCopy = dllPathStr;
		std::string::size_type pos = 0;
		while ((pos = dllPathStrCopy.find(";")) != std::string::npos) {
			std::string dll = dllPathStrCopy.substr(0, pos);
			dlls.push_back(dll);
			dllPathStrCopy.erase(0, pos + 1);
		}
		dlls.push_back(dllPathStrCopy);
		bool start = 1;
		//inFile >> (bool) start;
		inFile.getline(exePathStr, 1024);
		exePath = exePathStr;
		inFile.getline(argStr, 1024);
		arg = argStr;
		DWORD sleep = 0;
		inFile >> sleep;
		char dummy[1024];
		inFile.getline(dummy, 1024); // consume newline
		char iniPathStr[1024];
		inFile.getline(iniPathStr, 1024);
		std::string iniPath = iniPathStr;
		inFile.close();
		std::cout << procName << std::endl;
		std::cout << dllPath << std::endl;
		for (std::vector<std::string>::iterator it = dlls.begin(); it != dlls.end(); it++) {
			std::cout << " --* dlls: " << *it << std::endl;
		}
		std::cout << exePath << std::endl;
		std::cout << arg << std::endl;
		std::cout << sleep << std::endl;
		std::cout << iniPath << std::endl;
		std::string cmdline = exePath + " " + arg;
		//dllPath = "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\GreenLuma.dll\0";
		std::cout << '[' << cmdline << ']' << " procName=" << procName << ",dllPath=" << dllPath << ",iniPath=" << iniPath << std::endl;
		//��֤
		std::cout << "Read Config End && Find DLL/EXE Begin" << std::endl;
		for (std::vector<std::string>::iterator it = dlls.begin(); it != dlls.end(); it++) {
			if (!fileExist((*it).c_str()))return -10030;
		}
		//if (!fileExist(dllPathStr))
		//{
		//	//system("dir C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector");
		//	return -10030;
		//}
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
		//inject(procName, dllPath, sleep);
		for (std::vector<std::string>::iterator it = dlls.begin(); it != dlls.end(); it++) {
			std::cout << " --* inject dll: " << *it << std::endl;
			
			inject(procName, *it, iniPath, sleep);
			std::cout << " --* inject dll end" << std::endl;
		}
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
	time(&nowtime); //��ȡ1900��1��1��0��0��0�뵽���ھ���������
	tm p;
	localtime_s(&p, &nowtime); //������ת��Ϊ����ʱ��,���1900����,��Ҫ+1900,��Ϊ0-11,����Ҫ+1
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
