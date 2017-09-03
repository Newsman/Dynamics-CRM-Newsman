using Microsoft.Xrm.Sdk;
using NewsmanLib.APIHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsmanLib
{
    public class ResetConfig : PluginBase
    {
        #region Overrides
        public override void PostUpdate(Entity entity, ConnectionHelper helper)
        {
            if (helper.PluginExecutionContext.Depth > 1)
                return;

            Entity image = helper.PluginExecutionContext.PostEntityImages.Contains("Image") ?
                helper.PluginExecutionContext.PostEntityImages["Image"] : entity;

            string name = (string)image.GetValue("nmc_name", image);

            #region Update config params

            //if param value changes, reset available lists
            if ((name == "ApiKey" || name == "UserId") &&
                entity.Contains("nmc_value") && entity["nmc_value"] != null)
            {
                ResetConfigurationParameters(helper, name, entity["nmc_value"].ToString());
            }

            #endregion
        }
        #endregion

        #region Methods
        private void ResetConfigurationParameters(ConnectionHelper helper, string name, string value)
        {
            try
            {
                #region check config params
                string apikey = null;
                string userid = null;
                Entity nmLists = Common.GetParamEntity(helper.OrganizationService, "Newsman Lists");
                Entity defaultList = Common.GetParamEntity(helper.OrganizationService, "Default List");

                if (name == "ApiKey")
                {
                    apikey = value;
                    userid = Common.GetParamValue(helper.OrganizationService, "UserId");
                }

                if (name == "UserId")
                {
                    userid = value;
                    apikey = Common.GetParamValue(helper.OrganizationService, "ApiKey");
                }

                if (apikey == null || userid == null)
                    return;
                #endregion

                //create newsman api instance
                NewsmanAPI nmapi = new NewsmanAPI(apikey, userid);
                string listInformation = null;

                helper.TracingService.Trace("Retrieving lists");
                try
                {
                    listInformation = nmapi.RetrieveListsJson();
                }
                catch (Exception e)
                {
                    Common.LogToCRM(helper.OrganizationService, $"Exception when retrieving Newsman lists",
                        $"Message: {e.Message} // Stack trace: {e.StackTrace}");
                }
                helper.TracingService.Trace("Retrieved");

                if (nmLists == null)
                {
                    //create lists config param
                    Entity listsParam = new Entity("nmc_newsmanconfig");
                    listsParam.Attributes["nmc_name"] = "Newsman Lists";
                    listsParam.Attributes["nmc_value"] = listInformation;

                    helper.TracingService.Trace("Create params");
                    helper.OrganizationService.Create(listsParam);

                    if (listInformation != null)
                    {
                        Common.LogToCRM(helper.OrganizationService, $"Created Newsman Config parameter for retrieved lists",
                            listsParam.Attributes["nmc_value"].ToString());
                    }
                }
                else
                {
                    //update lists config param
                    nmLists.Attributes["nmc_value"] = listInformation;

                    helper.TracingService.Trace("Update params");
                    helper.OrganizationService.Update(nmLists);

                    if (listInformation != null)
                    {
                        Common.LogToCRM(helper.OrganizationService, $"Updated Newsman Config parameter for retrieved lists",
                            nmLists.Attributes["nmc_value"].ToString());
                    }
                }

                //reset Default List when modifying apikey
                if (listInformation == null && defaultList != null)
                {
                    defaultList.Attributes["nmc_value"] = null;
                    helper.OrganizationService.Update(defaultList);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        #endregion
    }
}
