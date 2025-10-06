The appsettings.json file offers a convenient way to configure Microsoft.Identity.Web authentication. To improve this experience, we've created a JSON schema you can import into your appsettings.json file. 

Adding the schema to your appsettings.json file makes configuration easier by enabling you to see required and optional fields when editing in an IDE as well as valid values when choosing from a pre-defined set of options as seen below.

## How to implement

Add this line at the top of your appsettings.json in the main block, to enable the functionality:

    "$schema": "https://raw.githubusercontent.com/AzureAD/microsoft-identity-web/refs/heads/master/JsonSchemas/microsoft-identity-web.json",

![image](https://github.com/user-attachments/assets/2a9a9686-d1ec-4d22-9544-fd5cecc3699e)



