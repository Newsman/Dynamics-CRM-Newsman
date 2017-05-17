using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using NewsmanLib.APIHelper;
using System.Web.Script.Serialization;

namespace NewsmanLib
{
    public class NewsmanConfig : PluginBase
    {
        public override void PostCreate(Entity entity, ConnectionHelper helper)
        {
            if (entity.Contains("nmc_name") &&
                (entity["nmc_name"].ToString() == "ApiKey" || entity["nmc_name"].ToString() == "UserId"))
            {
                #region check config params
                string apikey = null;
                string userid = null;
                Entity nmLists = Common.GetParamEntity(helper.OrganizationService, "Newsman Lists");

                if (entity["nmc_name"].ToString() == "ApiKey")
                {
                    apikey = entity["nmc_value"].ToString();
                    userid = Common.GetParamValue(helper.OrganizationService, "UserId");
                }

                if (entity["nmc_name"].ToString() == "UserId")
                {
                    userid = entity["nmc_value"].ToString();
                    apikey = Common.GetParamValue(helper.OrganizationService, "ApiKey");
                }

                if (apikey == null || userid == null)
                    return; 
                #endregion

                //create newsman api instance
                NewsmanAPI nmapi = new NewsmanAPI(apikey, userid);

                if (nmLists == null)
                {
                    //create lists config param
                    Entity listsParam = new Entity("nmc_newsmanconfig");
                    listsParam.Attributes["nmc_name"] = "Newsman Lists";
                    listsParam.Attributes["nmc_value"] = nmapi.RetrieveListsJson();
                    helper.OrganizationService.Create(listsParam);

                    //log
                    Common.LogToCRM(helper.OrganizationService, $"Created Newsman Config parameter for retrieved lists", 
                        listsParam.Attributes["nmc_value"].ToString());
                }
                else
                {
                    //update lists config param
                    nmLists.Attributes["nmc_value"] = nmapi.RetrieveListsJson();
                    helper.OrganizationService.Update(nmLists);

                    //log
                    Common.LogToCRM(helper.OrganizationService, $"Updated Newsman Config parameter for retrieved lists",
                        nmLists.Attributes["nmc_value"].ToString());
                }
            }
        }

        public override void PostUpdate(Entity entity, ConnectionHelper helper)
        {
            Entity image = helper.PluginExecutionContext.PostEntityImages.Contains("Image") ?
                helper.PluginExecutionContext.PostEntityImages["Image"] : entity;

            string name = (string)image.GetValue("nmc_name", image);
            if (name == "Default List")
            {
                //reload segments
                //..
            }
        }
    }
}
