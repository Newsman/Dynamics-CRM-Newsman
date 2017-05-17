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
        private const string apikey = "nn";
        private const string userid = "kk";

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
            Subscriber subscriber = CreateSubscriber(service, member, listInfo.ListTargetType);

            if (subscriber != null)
            {
                using (NewsmanAPI api = new NewsmanAPI(apikey, userid))
                {
                    var resp = api.ImportSubscribers("2896", listInfo.NewsmanSegmentId, subscriber);
                    Common.LogToCRM(service, $"Importing [{subscriber.Email}] for [{listInfo.ListName}] list", $"Import id: {resp.Replace("\"","")}");
                }
            }
        }

        private Subscriber[] CreateSubscribers(IOrganizationService service, Guid[] memberIds, string memberType)
        {
            List<Subscriber> sub = new List<Subscriber>();
            ColumnSet cols = null;

            QueryExpression qry = new QueryExpression(memberType);
            switch (memberType)
            {
                case "account":
                    cols = new ColumnSet("name", "emailaddress1");
                    break;
                case "contact":
                case "lead":
                    cols = new ColumnSet("firstname", "lastname", "emailaddress1");
                    break;
                default:
                    cols = new ColumnSet(false);
                    break;
            }
            qry.ColumnSet = cols;
            qry.Criteria = new FilterExpression(LogicalOperator.Or);

            //filter selected members
            foreach(Guid memberId in memberIds)
            {
                qry.Criteria.AddCondition($"{memberType}id", ConditionOperator.Equal, memberId);
            }

            EntityCollection members = service.RetrieveMultiple(qry);

            foreach (Entity member in members.Entities)
            {
                if (member.Contains("emailaddress1"))
                {
                    sub.Add(new Subscriber()
                    {
                        Email = member["emailaddress1"].ToString(),
                        Firstname = member.Contains("firstname") ? member["firstname"].ToString() : "",
                        Lastname = member.Contains("lastname") ? member["lastname"].ToString() : member.Contains("name") ? member["name"].ToString() : ""
                    });
                }
            }

            return sub.ToArray();
        }

        private Subscriber CreateSubscriber(IOrganizationService service, Guid memberId, string memberType)
        {
            Subscriber sub = null;
            ColumnSet cols = null;

            switch (memberType)
            {
                case "account":
                    cols = new ColumnSet("name", "emailaddress1");
                    break;
                case "contact":
                case "lead":
                    cols = new ColumnSet("firstname", "lastname", "emailaddress1");
                    break;
                default:
                    cols = new ColumnSet(false);
                    break;
            }
            Entity member = service.Retrieve(memberType, memberId, cols);

            if (member.Contains("emailaddress1"))
            {
                sub = new Subscriber()
                {
                    Email = member["emailaddress1"].ToString(),
                    Firstname = member.Contains("firstname") ? member["firstname"].ToString() : "",
                    Lastname = member.Contains("lastname") ? member["lastname"].ToString() : member.Contains("name") ? member["name"].ToString() : ""
                };
            }

            return sub;
        }
        #endregion
    }
}
