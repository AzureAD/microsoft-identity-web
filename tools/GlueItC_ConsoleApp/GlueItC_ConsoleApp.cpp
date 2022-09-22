// GlueItC_ConsoleApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <stdio.h>
//#pragma comment(lib, "Runtime.ServerGC.lib")

#define USE_DYNAMIC_DLL 1


#if USE_DYNAMIC_DLL == 1
// See https://github.com/dotnet/samples/blob/main/core/nativeaot/NativeLibrary/README.md
#define PATH "C:\\gh\\microsoft-identity-web\\tools\\GlueIt\\bin\\Release\\net7.0\\win-x64\\publish\\GlueIt.dll"
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



int main()
{
#if USE_DYNAMIC_DLL == 1
#ifdef _WIN32
    HINSTANCE handle = LoadLibraryA(PATH);
#else
    void* handle = dlopen(path, RTLD_LAZY);
#endif
    typedef  char * (*validator_t)(const char *, const char*, const char*, const char*);
    validator_t ValidateToken;
    ValidateToken = (validator_t)GetProcAddress(handle, "ValidateToken");
#endif

    const char* instance = "https://login.microsoftonline.com";
    const char* audience = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc";
    const char* tenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
    char* issuer = ValidateToken(instance, tenantId, audience, "");
    printf(issuer);
    printf("\n");
}
