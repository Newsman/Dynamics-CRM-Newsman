using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NewsmanLib.APIHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsmanLib
{
    public class MarketingList : PluginBase
    {
        public override void PreCreate(Entity entity, ConnectionHelper helper)
        {
            try
            {
                if (entity.Contains("listname") && entity["listname"] != null)
                {
                    #region check config params
                    string apikey = Common.GetParamValue(helper.OrganizationService, "ApiKey");
                    string userid = Common.GetParamValue(helper.OrganizationService, "UserId");
                    string nmList = Common.GetParamValue(helper.OrganizationService, "Default List");

                    if (apikey == null || userid == null || nmList == null)
                        return;
                    #endregion

                    NewsmanAPI api = new NewsmanAPI(apikey, userid);
                    string segmentId = api.CreateSegment(nmList, (string)entity["listname"]);
                    entity["nmc_newsmansegmentid"] = segmentId;

                    Common.LogToCRM(helper.OrganizationService, $"Created segment for {(string)entity["listname"]}", $"Segment id: {segmentId}");
                } 
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
