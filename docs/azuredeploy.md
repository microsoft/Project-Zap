# Deploy to Azure

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmicrosoft%2FProject-Zap%2Fmaster%2Ftemplates%2Fazuredeploy.json" target="_blank">
<img src="../images/deploytoazure.png"/>
</a>

This project includes Azure Resource Manager (ARM) templates to deploy the application and required infrastructure to Microsoft Azure.

## Prerequisites

To deploy this application from the supplied ARM templates, an AAD B2C tenant is required.  This tenant must be configured with the appropriate attributes, flows and permissions to support the application.  The process to configure AAD B2C for this application can be found [here](./setupb2c.md).

A number of properties are also required from this tenant to use as input to the deployment, these are summarized here:

| Property | Comments |
|----------|----------|
| Org Name | Name of customer or organization for application naming e.g. Contoso |
| B2C Token Endpoint | Token endpoint e.g. https://[domain label].b2clogin.com/tfp/ where [domain label] is the first component of the AAD domain name.  For the domain contoso.onmicrosoft.com this would be https://contoso.b2clogin.com/tfp/ |
|Application ID | App ID for the application registration in AAD B2C |
| B2C Domain Name | AAD B2C domain name |
| Signup Signin Policy ID | Policy ID for signup/signin user flow e.g. B2C_1_susi |
| Reset Password Policy ID | Policy ID for password user flow e.g. B2C_1_password_reset |
| Edit profile Policy ID | Policy ID for edit profile user flow e.g. B2C_1_edit_profile |
| Manager code | Initial registration code string to use for organization manager access, this should be alphanumeric and six characters in length |
| Extension Application ID | App ID for the B2C Extensions app, used to reference custom attributes |
| AAD Tenant ID | Unique identifier for AAD B2C tenant |
| App registration client secret | Auth key for application registration, used to set zaprole custom attribute |


## Components Deployed

* **Cosmos DB** - database to support application functionality including organization setup, stores, shifts and bookings
* **App Service** - application runtime environment, includes deployment of application from source code in this repo