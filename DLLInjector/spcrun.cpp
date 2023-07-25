#include<iostream>
#include<string>
#include<cstdlib>
#include<cstdio>
#include<windows.h>
#include<direct.h>
#pragma comment( linker, "/subsystem:\"windows\" /entry:\"mainCRTStartup\"" )
using namespace std;
int main(int argc,char* args[])
{
//	HWND hwnd=GetForegroundWindow();
//	if(hwnd)
//	{
//		ShowWindow(hwnd,SW_HIDE);
//	}
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
	string logPath = path+"\\log.txt";
	string logErrPath = path+"\\logerr.txt";
	freopen(logPath.data(),"a+",stdout);
	freopen(logErrPath.data(),"a+",stderr);
	system("echo [SpcRunStart]");
	string cmd="";
	cmd=cmd+"cmd /C \"cd /d \""+path+"\"&dir&start .\\DLLInjector.exe\"";
	//cmd+=" &pause"; 
	system("chcp 437");//chcp 65001(utf-8)chcp 437(eng)
	int res=system(cmd.data());
	FILE *fp=fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\ExitCode.txt","w");
	fprintf(fp,"%d",res);
	fclose(fp);
	fclose(stdout);
	fclose(stderr);
	return 0;
}
