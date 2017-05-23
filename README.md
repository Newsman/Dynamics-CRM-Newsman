# Dynamics-CRM-Newsman
Newsman integration module for Microsoft Dymamics CRM (365 / on premise)

# Installing, configuring and using the connector

## Download
Download the zip file from [NewsmanSync_1_0_0_2_managed.zip](https://github.com/Newsman/Dynamics-CRM-Newsman/raw/master/NewsmanSync_1_0_0_2_managed.zip).

## Installation
Access Solutions from the Settings module, in the navigation area:

  ![Solutions](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/solutions.png)

Import solution:

  ![Import](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/import.png)

Select the zip file:

  ![Browse](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/choosefile.png)
  
Keep the check for the SDK messages:

  ![SDK](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/enablesdk.png)

At the end of the import, you'll see this dialog:

  ![Imported](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/imported.png)

## Configuration

On the Marketing tab, marketing lists section, you will be able to access the Newsman API button which can be used to set up the API key and the user ID:

  ![mkt](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/marketinglists.png)

Click the Newsman API button:

  ![setup](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/configure.png)

The same configuration page can be found on the managed solution:

  ![managed](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/solution_configpage.png)

On this page you need to first insert the apikey and userid from the Newsman account and then click submit. After refreshing the page, you'll be ableto select the list which will be used for the synchronization.

## Usage

To enable the synchronization, once the solution was imported, every time you create a marketing list will trigger the creation of a segment with the same name within the selected list from the Newsman account.

After the creation of a list there are two ways of adding members to that list and, thus, to the Newsman segment.

### 1. Lookup

Use the '+' button to manage members on the newly created marketing lists and select the lookup option:

  ![lookup](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/manage_members_lookup.png)

And select the member you wish to add:

  ![lookupadd](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/add_members_lookup.png)
  
### 2. Advanced find

Select the advanced find option to manage members:

  ![advfind](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/manage_members_advfind.png)
  
You can apply a filter on the records set:

  ![filter](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/filter_members_advfind.png)
  
And select the wanted records:
  
  ![advfindadd](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/NewsmanLib/Pics/add_members_advfind.png)
  
# License

This code is released under [MIT license](https://github.com/Newsman/Dynamics-CRM-Newsman/blob/master/LICENSE) by [Newsman App - Smart Email Service Provider](https://www.newsmanapp.com).  
