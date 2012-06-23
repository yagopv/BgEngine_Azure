# Add **BgEngine **to an existing Azure website

Creating a blog is a great way to gain SEO authority for your site. In order to gain SEO for your domain , instead of WordPress or Blogger, the ideal situation is to create your blog under your existing site (ex: www.YourSite.com/Blog). This doc will guide you through that process.

## Task 1: Create your database

1. Using SSMS, connect to your Azure database server (master db) and create a new database:

    CREATE DATABASE BgEngine
    GO

2. Open a query window to your new **BgEngine **db and run the Initialize BgEngine.sql script (add url here)

## Task 2: Configure Email, Google, Twitter

1. Add your site domain in http://www.google.com/recaptcha. Once you have this done, you will be provided with a pair of keys (private and public). Then you have to add this keys in the blog admin panel (Admin Home page).

2. Login as admin/admin and add your site-specific info on the Administration area page, Configuration tab 

> not filling these values will prevent the create new account process from completing

## Task 3: Configure your Azure project

1. Add these keys to the  <ConfigurationSettings> of all 3 off these files. 
(If you right-click your role and choose settings there is a GUI for this that should populate your Azure account name and key automatically)

    ServiceDefinition.csdef:
         <Setting name="StorageConnectionString" />
    ServiceConfiguration.Local.csfg:
         <Setting name="StorageConnectionString" value="UseDevelopmentStorage=true" /> 
    ServiceConfiguration.Cloud.csfg:
         <Setting name="StorageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=YourAccountName;AccountKey=ThatEncryptedTopSecretNumber" />

2. Configure BgEngine Web.Config

**Update the existing BlogUnitOfWork web.config**

     <add name="BlogUnitOfWork" connectionString="data source=.;Initial Catalog=BgEngine;User Id=YourUserId;Password=YourPassword providerName="System.Data.SqlClient" />

## Task 4: Add the BgEngine platform

**Add the BgEngine platform**

Create a folder named BgEngine in the parent directory where your existing Azure solution directory is. (ex: If your existing solution is in “C:\Projects\MyAzureProject” then create a directory named “C:\Projects\BgEngine” and download the latest BgEngine.Azure code from Codeplex (add url here)

Right-click your solution and choose Add… Existing project… Navigate to the BgEngine folder on your local machine and add the projects:

    BgEngine.Application
    BgEngine.DependencyResolution
    BgEngine.Domain
    BgEngine.Infrestructure
    BgEngine.Resources
    BgEngine.Security
    BgEngine.Web


To configure BgEngine under your exisitng domain () open the ServiceDefinition.csdef and add a virtual application tag under your site name tag:

    <WebRole name="NameOfYourRole" vmsize="Small">
        <Sites>
            <Site name="Web">
            <VirtualApplication name="Blog" physicalDirectory="../../BgEngine/BgEngine.Web" />

> Even though the tag name is “physicalDirectory” it is actually a relative path from your Azure solution to the BgEngine.Web project. If you put BgEngine somewhere else than described in the step before this you will have to adjust this path.

## Known Issues

1. Ports may get reassigned when using Visual Studio Compute Emulator (IN DEV ONLY) this does not happen when you deploy to Azure. 

Ex: 
The Confrim account page points to:  
http://127.0.0.1:82/Blog/Account/ConfirmAccount?id=BKkMAuVkRQwQOJjdTJhrg%3d%3d&user=test                             
This page can be navigated to manually at port 81        
On compile you will see this warning:  
> Windows Azure Tools: Warning: Remapping private port 81 in 82 role YourRoleName to avoid conflict during emulation.

Enjoy!!



















