# Introduction 
The purpose of this project is to extract SSW Rules content from sharepoint and into markdown files in git.
We also preserve history for each rule.

# Why full framework?
To get everything we need out of sharepoint, we need to used more than one api/sdk.
We're using .net 4.7.2 so we can use the CSOM libray for sharepoint 2016.

# Configuration
To read from sharepoint you need to confgure your credentials!
create an appsettings.local.json file to do this:

```
{
  "SharePointUrl": "https://rules.ssw.com.au",
  "Username": "SP_SVC",
  "Domain": "SSW2000",
  "Password": "password_here"
}
```
Ask Kiki, Jean or Brendan for the password.

# Usage
Getting all the data out of SharePoint takes a lot of time - 60-90 minutes!
To make development more bearable
- we read from sharepoint and parse into an in-memory object model.
- we can then write our output process against this easier object model.
- to save time developing, we can save this full model to a json file

```
SSW.Rules.SharepointExtractor.exe WriteFile=data.json
```
This will run the slow import from sharepoint and save the results to data.json

```
SSW.Rules.SharepointExtractor.exe ReadFile=data.json
```
Once you have saved a json file you can skip the tedious import-from-sharepoint step. This takles 1 second instead of > 1hr

A copy of the data.json has been checked in - this should speed up development - but we should get the latest data when we run this for our Prod migration.
