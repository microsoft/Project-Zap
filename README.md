# Deploy to Azure

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmicrosoft%2FProject-Zap%2Fmaster%2Ftemplates%2Fazuredeploy.json" target="_blank">
<img src="/images/deploytoazure.png"/>
</a>

This project includes Azure Resource Manager (ARM) templates to deploy the solution to Microsoft Azure.

## Prerequisites

To deploy this application from the supplied ARM templates, an AAD B2C tenant is required.  This tenant must be configured with the appropriate attributes, flows and permissions to support the application.  The process to configure AAD B2C for this application can be found [here](./docs/setupb2c.md).

A number of properties are also required from this tenant to use as input to the deployment, these are summarized here:

| Property | Deployment parameter | Comments |
|----------|----------------------|----------|
| Registered application ID | B2C Client ID | App ID for the application registration in AAD B2C |
| App registration secret | App registration client secret | Auth key for application registration, used to set zaprole custom attribute |
| Extension App ID | Extension Client ID | App ID for the B2C Extensions app, used to reference custom attributes |
| Tenant ID | Tenant ID | Unique identifier for AAD B2C tenant |
| Domain Name | B2C domain | AAD B2C domain name |
| Token endpoint | B2C Instance | Token endpoint e.g. https://[domain label].b2clogin.com/tfp/ where [domain label] is the first component of the AAD domain name.  For the domain contoso.onmicrosoft.com this would be https://contoso.b2clogin.com/tfp/ |
| Manager code | Manager code | Initial registration code string to use for organization manager access |
| Signup/Signin policy ID | Signup Signin Policy ID | Policy ID for signup/signin user flow e.g. B2C_1_susi |
| Edit profile policy ID | Edit profile Policy ID | Policy ID for edit profile user flow e.g. B2C_1_edit_profile |
| Reset password policy ID | Reset Password Policy ID | Policy ID for password user flow e.g. B2C_1_password_reset |
| Customer/Organization Name | Org Name | Name of customer or organization for application naming e.g. Contoso |


## Components Deployed

* **Cosmos DB** - database to support application functionality including organization setup, stores, shifts and bookings
* **App Service** - application runtime environment, includes deployment of application from source code in this repo


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
