## If you have simple scenarios

If you have simple scenarios, such as signing-in users in my organization and/or calling an API, you can use App Services authentication. In Microsoft.Identity.Web versions 1.2.0 and later, for these simple scenarios, the same code for your web app will work seamlessly with or without EasyAuth. Your web app can sign-in users and possibly call web APIs or Microsoft Graph. Indeed, Microsoft.Identity.Web now detects that the app is hosted in App Services, and uses that authentication. You can still sign-in users, and you can call web APIs provided you enabled them in App Services. For details on how to do that, see this tutorial: [Configure App Service to return a usable access token](https://docs.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-user?tabs=command-line#configure-app-service-to-return-a-usable-access-token). 

Normally your app should not need to know if it's hosted in App Services with Authentication or not, but if you want to propose a different UI, it can call `AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled` to detect it.

Note that when Microsoft.Identity.Web detects EasyAuth, it automatically overrides the default authentication scheme to be `AppServicesAuthenticationDefaults.AppServicesAuthenticationDefaults`, and uses this scheme instead of the `OpenIdConnect` scheme. If you configure the OpenID Connect scheme, you might want to guard this configuration code with tests using `AppServicesAuthenticationInformation.IsAppServicesAadAuthenticationEnabled`. But if you do so, you might have an advanced scenario, and therefore we recommend you disable App Services authentication (see below).

## If you have advanced scenarios

When you deploy your web app or web API to Azure web services and have advanced code, like configuring OpenID Connect or cookies, make sure that you **turn off** the App Services authentication, even if the message in App Services tells you that anonymous access is enabled. Indeed it is enabled at that level, but your web app or web API is protected by Microsoft.identity.Web.

![image](https://user-images.githubusercontent.com/13203188/89042701-a6097d80-d347-11ea-88e7-b2c12ec767c8.png)

If you turned on App Services authentication and also customized OpenID Connect settings, the authentication will not work.

## Deploying web apps to App Services as Linux containers

If you choose to deploy your web app in Linux containers (without enabling App Services authentication), you'll also need to use forwarded headers. For details see [Deploying web apps to App Services as Linux containers](Deploying-Web-apps-to-App-services-as-Linux-containers).