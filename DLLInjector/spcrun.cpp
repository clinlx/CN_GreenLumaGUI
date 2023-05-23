#include<iostream>
#include<string>
#include<cstdlib>
#include<windows.h>
using namespace std;
int main(int argc,char* args[])
{
	HWND hwnd=GetForegroundWindow();
	if(hwnd)
	{
		ShowWindow(hwnd,SW_HIDE);
	}
	string path = args[0];
	while(path[path.length()-1]!='\\')
	{
		path.pop_back();
	}
	path.pop_back();
	path="cmd /C \"cd /d \""+path+"\"&start .\\DLLInjector.exe\"";
	system(path.data());
	return 0;
}
