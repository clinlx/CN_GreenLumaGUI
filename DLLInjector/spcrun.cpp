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
	
	FILE *fp;
	int runModel = 0;
	fp=fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\DLLInjector.ini","r");
	if(fp!=NULL)
	{
		runModel = 1;
		fclose(fp);
	}
	else
	{
		fp=fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\DLLInjector_bak.txt","r");
		if(fp!=NULL)
		{
			runModel = 2;
			fclose(fp);
		}
		else
		{
			fprintf(stderr,"Error: Config loss\n");
			fclose(stdout);
			fclose(stderr);
			return -1;
		} 
	}
	 
	system("echo [SpcRunStart]");
	string cmd="";
	string exeName="DLLInjector.exe";
	if(runModel == 2)
	{
		exeName="DLLInjector_bak.exe";
	}
	//cmd=cmd+"%SystemRoot%\\system32\\cmd.exe /C \"cd /d \""+path+"\"&dir&start .\\"+exeName+"\"";
	//cmd=cmd+"cd /d \""+path+"\" &dir&start /d . %SystemRoot%\\system32\\cmd.exe /C \".\\"+exeName+" > log_bak.txt 2>logerr_bak.txt & echo finish > finish.txt\"&exit";
	cmd=cmd+"cd /d \""+path+"\" &dir&echo.&echo.&echo.&echo ---Start---&echo.&echo.&echo.&\".\\"+exeName+"\"";
	//cmd+=" &pause"; 	
	system("%SystemRoot%\\system32\\chcp.com 437 2>nul");//chcp 65001(utf-8)chcp 437(eng)
	int res=system(cmd.data());
//	//迁移log2内容 
//	FILE *finish;
//	int times=0;
//	do
//	{
//		times++;
//		Sleep(50);
//		finish = fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\finish.txt","r");
//	}while(finish==NULL&&times<100);
//	if(finish!=NULL)
//	{
//		fclose(finish);
//		system("del C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\finish.txt 2>nul");
//		Sleep(100);
//	}
//	char c;
//	FILE *log_bak = fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\log_bak.txt","r");
//	if(log_bak!=NULL)
//	{
//		while(~fscanf(log_bak,"%c",&c))
//			fprintf(stdout,"%c",c);
//		fclose(log_bak);
//		system("del C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\log_bak.txt 2>nul");
//	}
//	FILE *logerr_bak = fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\logerr_bak.txt","r");
//	if(logerr_bak!=NULL)
//	{
//		while(~fscanf(logerr_bak,"%c",&c))
//			fprintf(stderr,"%c",c);
//		fclose(logerr_bak);
//		system("del C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\logerr_bak.txt 2>nul"); 
//	} 
	//ExitCode
	fp=fopen("C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector\\ExitCode.txt","w");
	fprintf(fp,"%d",res);
	//关闭资源 
	fclose(fp);
	fclose(stdout);
	fclose(stderr);
	return res;
}
