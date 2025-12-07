### AzureAD B2C issuer format

Azure AD B2C policies can be customized to emit the issuer claim in two formats: `https://<domain>/<tenant>/v2.0/` and `https://<domain>/tfp/<tenant>/<userFlow>/v2.0/`. 

Microsoft Identity Web follows the guidance from Azure AD B2C and only supports an issuer in `https://<domain>/<tenant>/v2.0/` format.

### Editing the Azure AD B2C issuer foramt

1. In the menu of the Azure AD B2C tenant overview page, select **User flows**, and then select the desired user flow.
2. On the user flow page, under **Settings**, select **Properties**.
3. Under **Token compatibility settings**, set **Issuer (iss) claim** to `https://<domain>/<tenant>/v2.0/` format.

![image](https://user-images.githubusercontent.com/34331512/89093258-88e7a580-d36d-11ea-8e57-1472c096e834.png)
