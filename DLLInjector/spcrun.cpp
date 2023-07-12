#include<iostream>
#include<string>
#include<cstdlib>
#include<windows.h>
#include <direct.h>
using namespace std;
int main(int argc,char* args[])
{
	HWND hwnd=GetForegroundWindow();
	if(hwnd)
	{
		ShowWindow(hwnd,SW_HIDE);
	}
//	cout<<_getcwd(NULL, 0)<<endl; 
//	cout<<argc<<endl;
//	string path = args[0];
//	cout<<path<<endl;
//	while(path.length()>0&&path.back()!='\\')
//	{
//		if(path.length()>0)
//			path.pop_back();
//	}
//	if(path.length()>0)
//		path.pop_back();
//	if(path.length()==0)
//	{
//		path=".";
//	}
//	cout<<path<<endl;
	string path = "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector";
	string cmd="";
	cmd=cmd+"cmd /C \"cd /d \""+path+"\"&start .\\DLLInjector.exe\"";
	//cmd+=" &pause"; 
	system(cmd.data());
	return 0;
}
