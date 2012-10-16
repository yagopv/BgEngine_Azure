# What is BgEngine Azure

BgEngine Azure is a blog engine build using ASP MVC 3, jQuery and Entity Framework Code First.

This engine allow users to create blog posts, image galleries and video galleries.

No database server needed because the application is using a Compact Framework database.

BgEngine Azure is a BgEngine version ready to work in the Azure Cloud

# Introduction

BgEngine Azure is an open source application licensed under GPLv3 that builds an engine for personal blogging purposes.

### Scope

The blog engine developed allow users publishing articles,  building photographic galleries, creating a video gallery and share all the posts in the social networks more highlighted. The main scope is using the application for blogging, mainly like personal website and provide the functionality of this type of web sites.

# Technologies and  features

Main list of technologies used in this project are:

    ASP MVC 3  (Web)
    jquery (client side scripting)
    jquery ui (user interface)
    Entity Framework 4.3.1  'Code First' (ORM for data access)
    Structure Map  (dependency injection)
    Combres (combining, compressing and minifying script and css files) 

The default database is a .sdf Compact Framework database, but is easy to change to SQL Server database simply changing the web.config file and uncomment the database initializer lines in Global.asax. 

The Web site built in this project have two main parts:

### FrontEnd

* Home page listing the latest articles and videos being published
* Image Gallery listing albums in the site
* Video Gallery listing videos linked from the site
* About me page for include information about the site
* Creating new user accounts is possible in the frontend web site. Confirmation mail will be sent when users creates an account.
* Email button in footer opening popup for sending messages to the  email account configured in the backend

* Sidebar widgets including.  

    * Social bar. Could share pages in Facebook, Twitter and Google+ and subscribe to the rss feed showing latest articles
    
    * Category widget showing categories in the site with number of related articles in each category. Each category links to another page showing articles in the category
    
    * Archive widget showing articles published by month. Each month links to another page showing articles published in that month - year
    
    * Stats Widget showing information about articles like, more visits, more comments and top rated articles
    
    * Tag cloud showing the tags and number of articles tagged with it. Each tag links to another page showing articles published marked with that tag
	
	* Subscription to newsletter
    
    * Twitter Profile widget. Shows Twitter profile if configured in the backend
    
    * Twitter Search widget. Shows tweets following the search previously configured in the backend. Typically you are configuring it with a search word like @myusername for watching who is talking about you
    
    * Displays posts lists
    
    * Displays individual posts and allows rating them or writing comments in a anonymous or authenticated way (Admin users can select in the backend  this behaviour)

### BackEnd

* Management of the information displayed in frontend
* Management image galleries
* Management video galleries
* Management of subscriptions and newsletters
* Upload files to the server
* Display stats from the site
* Configuration of the site (Mail account, paging, Twitter, Google analytics ...)

# Add **BgEngine Azure** to an existing Azure website

Creating a blog is a great way to gain SEO authority for your site. In order to gain SEO for your domain , instead of WordPress or Blogger, the ideal situation is to create your blog under your existing site (ex: www.YourSite.com/Blog). This doc will guide you through that process.

### Task 1: Create your database server in Azure

1. Create your database server in Azure and change your connection string in Web.config (Release mode)

	<add name="BlogUnitOfWork" connectionString="data source=myserver.database.windows.net;Initial Catalog=BgEngine;User ID=myuser@myserver;Password=mypassword;Encrypt=true;Trusted_Connection=false;MultipleActiveResultSets=True" providerName="System.Data.SqlClient" xdt:Transform="SetAttributes" xdt:Locator="Match(name)" />

2. Entity Framework will create your database in your server first time you start the installed application
   
   
### Task 2: Configure Email, Google, Twitter

1. Add your site domain in http://www.google.com/recaptcha. Once you have this done, you will be provided with a pair of keys (private and public). Then you have to add this keys in the blog admin panel (Admin Home page).

2. Login as admin/admin and add your site-specific info on the Administration area page, Configuration tab 

> not filling these values will prevent the create new account process from completing

### Task 3: Configure your Azure project

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

### Task 4: Add the BgEngine platform

**Add the BgEngine platform**

Create a folder named BgEngine in the parent directory where your existing Azure solution directory is. (ex: If your existing solution is in `C:\Projects\MyAzureProject` then create a directory named `C:\Projects\BgEngine` and download the latest BgEngine.Azure code from Codeplex (add url here)

Right-click your solution and choose `Add Existing project` Navigate to the BgEngine folder on your local machine and add the projects:

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

> Even though the tag name is `physicalDirectory` it is actually a relative path from your Azure solution to the BgEngine.Web project. If you put BgEngine somewhere else than described in the step before this you will have to adjust this path.

### Known Issues

1. Ports may get reassigned when using Visual Studio Compute Emulator (IN DEV ONLY) this does not happen when you deploy to Azure. 

Ex: 
The Confrim account page points to:  
http://127.0.0.1:82/Blog/Account/ConfirmAccount?id=BKkMAuVkRQwQOJjdTJhrg%3d%3d&user=test                             
This page can be navigated to manually at port 81         
On compile you will see this warning:  
> Windows Azure Tools: Warning: Remapping private port 81 in 82 role YourRoleName to avoid conflict during emulation.

Enjoy!!



















