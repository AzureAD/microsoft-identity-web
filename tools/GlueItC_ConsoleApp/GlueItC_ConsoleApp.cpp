// GlueItC_ConsoleApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <stdio.h>
//#pragma comment(lib, "NativeLibrary.lib")
//#pragma comment(lib, "bcrypt.lib")
//#pragma comment(lib, "Runtime.ServerGC.lib")

extern char* ValidateToken(char* instance, char* tenant, char* audience, char* token);

int main()
{
    const char* instance = "https://login.microsoftonline.com";
    const char* audience = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc";
    const char* tenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
    char* issuer = ValidateToken((char*)instance, (char*)tenantId, (char*)audience, (char*)"");
    printf(issuer);
    printf("\n");
}
