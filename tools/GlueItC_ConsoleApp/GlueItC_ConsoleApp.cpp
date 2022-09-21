// GlueItC_ConsoleApp.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#pragma comment(lib, "NativeLibrary.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "Runtime.ServerGC.lib")

extern "C" char* ValidateToken(char* instance, char* tenant, char* audience, char* token);

int main()
{
    const char* instance = "https://login.microsoftonline.com";
    const char* audience = "a4c2469b-cf84-4145-8f5f-cb7bacf814bc";
    const char* tenantId = "7f58f645-c190-4ce5-9de4-e2b7acd2a6ab";
    char* issuer = ValidateToken((char*)instance, (char*)tenantId, (char*)audience, (char*)"");
    std::cout << issuer<<"\n";
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
