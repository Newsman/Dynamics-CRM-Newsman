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
    public class ListMember : PluginBase
    {
        #region Plugin Methods

        public override void PreAddListMembers(Guid mkList, Guid[] members, ConnectionHelper ch)
        {
            PostAddListMembers(mkList, members, ch);
        }
        public override void PostAddMember(Guid mkList, Guid member, ConnectionHelper ch)
        {
            //get member type
            MarketingListInfo listInfo = Common.GetListTargetType(ch.OrganizationService, mkList);
            SendMemberToNewsman(listInfo, member, ch.OrganizationService);
        }

        public override void PostAddListMembers(Guid mkList, Guid[] members, ConnectionHelper ch)
        {
            //get member type
            MarketingListInfo listInfo = Common.GetListTargetType(ch.OrganizationService, mkList);

            foreach(Guid member in members)
            {
                SendMemberToNewsman(listInfo, member, ch.OrganizationService);
            }
        }
        #endregion

        #region Private Methods
        private void SendMemberToNewsman(MarketingListInfo listInfo, Guid member, IOrganizationService service)
        {
            //get member email and information
            Subscriber subscriber = Common.CreateSubscriber(service, member, listInfo.ListTargetType);

            if (subscriber != null)
            {
                #region check config params
                string apikey = Common.GetParamValue(service, "ApiKey");
                string userid = Common.GetParamValue(service, "UserId");
                string nmList = Common.GetParamValue(service, "Default List");

                if (apikey == null || userid == null || nmList == null)
                    return;
                #endregion
                using (NewsmanAPI api = new NewsmanAPI(apikey, userid))
                {
                    var resp = api.ImportSubscribers(nmList, listInfo.NewsmanSegmentId, subscriber);
                    Common.LogToCRM(service, $"Importing [{subscriber.Email}] for [{listInfo.ListName}] list", $"Import id: {resp.Replace("\"","")}");
                }
            }
        }

        #endregion
    }
}
