# Create AAD B2C Tenant

* Follow the steps [here](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-tenant) to create an Azure Active Directory B2C tenant to be used by the application

# Register and configure the application in AAD B2C

1. Sign in to the [Azure Portal](https://portal.azure.com/)
1. Select the **Directory + Subscription** icon in the portal toolbar (top right), then select the directory containing the AAD B2C tenant created in the previous section
1. In the Azure Portal, search for and select **Azure AD B2C** 
1. Select **App registrations (Preview)** from the left menu, and then select **New registration**
1. Enter a simple name for the application e.g. *contosozap*
1. Select **Accounts in any organizational directory or any identity provider**
1. Under Redirect URI, select Web, and then enter the app redirect URL in the URL text box.  If the application is deployed with the supplied ARM templates this will be **https://[company name]-zap.azurewebsites.net/signin-oidc** where [company name] is the Org Name parameter passed to the ARM deployment 
1. Under Permissions, select the *Grant admin consent to openid and offline_access permissions* check box then click **Register**
1. Make a note of the **Application ID** and **Directory ID** - you will need these for the application deployment
1. Click **Authentication** in the left menu
1. Check the **Access tokens** box under **Implicit grant** and click save
1. Click **Certificates & secrets** in the left menu
1. Click **New client secret**, enter a description e.g. "zapsecret" and click **Add**. ***Note the value of the generated secret, you will need this for the application deployment and it cannot be viewed after you leave this page.***
1. Select **API permissions** from the left menu, then **Add a permission**
1. Choose **Microsoft Graph** from the options then select **Application permissions**
1. Select the **User.ReadWrite.All** permission and click **Add permissions**
1. Click the **Grant admin consent for [directory name]** button then **Yes**

# Add custom attribute
* Follow the steps [here](https://docs.microsoft.com/en-us/azure/active-directory-b2c/user-flow-custom-attributes) to add one custom **string** attribute with the name **zaprole**

# Extensions Application ID

1. In Azure AD B2C click **App registrations (Preview)** then the **All applications** header
1. Select the **b2c-extensions-app** item
1. Copy the Application ID, you will need this for the application deployment.

# Create User Flows

* Follow the steps [here](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-user-flows) to create user flows for sign-up/sign-in, password reset and profile editing.
  * Make a note of the flow names to use for application deployment parameters

Configure the attributes and claims as below:

## Edit profile

* User attributes
  * Given Name
  * Surname

* Application claims
  * Given Name
  * Identity Provider
  * Identity Provider Access Token
  * Surname
  * User's Object ID

## Password reset

* Application claims
  * Email Addresses
  * Given Name
  * Surname
  * User's Object ID

## Sign-up/sign-in

* User attributes
  * Email Address
  * Given Name
  * Surname

* Application claims
  * Email Addresses
  * Given Name
  * Identity Provider
  * Identity Provider Access Token
  * Surname
  * User is new
  * User's Object ID
  * zaprole


