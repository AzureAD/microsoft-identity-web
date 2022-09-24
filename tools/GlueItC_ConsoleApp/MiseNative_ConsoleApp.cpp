// GlueItC_ConsoleApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <stdio.h>
//#pragma comment(lib, "Runtime.ServerGC.lib")

#define USE_DYNAMIC_DLL 1


#if USE_DYNAMIC_DLL == 1
// See https://github.com/dotnet/samples/blob/main/core/nativeaot/NativeLibrary/README.md
#define PATH "C:\\gh\\Mise\\mise-lastest\\src\\Prototypes\\NativeAOT\\bin\\Debug\\net7.0\\win-x64\\publish\\Microsoft.Identity.ServiceEssentials.NativeAOT.dll"
#ifdef _WIN32
#include "windows.h"
#define symLoad GetProcAddress GetProcAddress
#else
#include "dlfcn.h"
#define symLoad dlsym
#endif
#else
// Static library
extern char* ValidateToken(const char* instance, const char* tenant, const char* audience, const char* token);
#endif



int main2()
{
#if USE_DYNAMIC_DLL == 1
#ifdef _WIN32
    HINSTANCE handle = LoadLibraryA(PATH);
#else
    void* handle = dlopen(path, RTLD_LAZY);
#endif
    typedef  int (*configure_t)(const char*);
    typedef  char* (*validate_t)(const char*);
    configure_t Configure;
    Configure = (configure_t)GetProcAddress(handle, "Configure");
    validate_t ValidateToken;
    ValidateToken = (validate_t)GetProcAddress(handle, "Validate");
#endif


    printf(Configure("C:\\gh\\microsoft-identity-web\\tools\\GlueItC_ConsoleApp\\appsettings.json"));
    printf("\n");
}
