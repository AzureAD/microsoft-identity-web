// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CrossPlatformValidation;

Console.WriteLine("Hello, World!");

RequestValidator requestValidator = new();

requestValidator.Initialize("https://login.microsoftonline.com/7f58f645-c190-4ce5-9de4-e2b7acd2a6ab", "a4c2469b-cf84-4145-8f5f-cb7bacf814bc");

string token = "Bearer ";
IDictionary<string, object> claims = requestValidator.Validate(token);
foreach (var claim in claims)
{
    Console.WriteLine(claim);
}
Console.ReadLine();
