@echo off
call "{}\VC\Auxiliary\Build\vcvars64.bat"
cl /EHsc /MT "{}\CN_GreenLumaGUI\DLLInjector\DLLInjector_bak.cpp" /Fe"{}\CN_GreenLumaGUI\DLLInjector\DLLInjector_bak.exe"
