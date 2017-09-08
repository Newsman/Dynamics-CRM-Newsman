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
    internal static class Common
    {
        public static string GetParamValue(IOrganizationService service, string name)
        {
            QueryExpression qry = new QueryExpression("nmc_newsmanconfig");
            qry.ColumnSet = new ColumnSet("nmc_value");
            qry.Criteria.AddCondition("nmc_name", ConditionOperator.Equal, name);
            
            Entity p = service.RetrieveMultiple(qry).Entities.FirstOrDefault();
            if (p != null && p.Contains("nmc_value"))
            {
                return p["nmc_value"].ToString();
            }
            return null;
        }

        public static Entity GetParamEntity(IOrganizationService service, string name)
        {
            QueryExpression qry = new QueryExpression("nmc_newsmanconfig");
            qry.ColumnSet = new ColumnSet("nmc_value");
            qry.Criteria.AddCondition("nmc_name", ConditionOperator.Equal, name);

            return service.RetrieveMultiple(qry).Entities.FirstOrDefault();
        }

        public static void ListInputParameters(IPluginExecutionContext context)
        {
            StringBuilder sb = new StringBuilder("Listing input parameters: ");
            foreach (var key in context.InputParameters)
            {
                sb.AppendLine($"{key.Key} has value {key.Value.ToString()} and type {key.Value.GetType()}");
            }

            throw new InvalidPluginExecutionException(sb.ToString());
        }

        /// <summary>
        /// Target types: Account - 1, Contact - 2, Lead - 4
        /// </summary>
        /// <param name="service">CRM org service</param>
        /// <param name="listId">CRM marketing list guid</param>
        /// <returns></returns>
        public static MarketingListInfo GetListTargetType(IOrganizationService service, Guid listId)
        {
            Entity mklist = service.Retrieve("list", listId, new ColumnSet("listname", "createdfromcode", "nmc_newsmansegmentid"));
            MarketingListInfo listInfo = new MarketingListInfo();

            if (mklist.Contains("listname"))
            {
                listInfo.ListName = (string)mklist["listname"];
            }

            if (mklist.Contains("createdfromcode"))
            {
                int entityType = ((OptionSetValue)mklist["createdfromcode"]).Value;
                listInfo.ListTargetType = entityType == 1 ? "account" : (entityType == 2 ? "contact" : "lead");
            }

            if (mklist.Contains("nmc_newsmansegmentid"))
            {
                listInfo.NewsmanSegmentId = mklist["nmc_newsmansegmentid"].ToString();
            }

            return listInfo;
        }

        public static void LogToCRM(IOrganizationService service, string name, string details)
        {
            Entity log = new Entity("nmc_newsmanlog");
            log.Attributes["nmc_name"] = name;
            log.Attributes["nmc_details"] = details;

            service.Create(log);
        }

        public static List<Subscriber> CreateSubscribers(EntityCollection members)
        {
            List<Subscriber> sub = new List<Subscriber>();
            foreach (Entity member in members.Entities)
            {
                if (member.Contains("emailaddress1"))
                {
                    sub.Add(EntityToSubscriber(member, "emailaddress1"));
                }
                if (member.Contains("emailaddress2"))
                {
                    sub.Add(EntityToSubscriber(member, "emailaddress2"));
                }
                if (member.Contains("emailaddress3"))
                {
                    sub.Add(EntityToSubscriber(member, "emailaddress3"));
                }
            }

            return sub;
        }

        private static Subscriber EntityToSubscriber(Entity member, string emailField)
        {
            return new Subscriber() {
                Email = member[emailField].ToString(),
                Firstname = member.Contains("firstname") ? member["firstname"].ToString() : "",
                Lastname = member.Contains("lastname") ? member["lastname"].ToString() : member.Contains("name") ? member["name"].ToString() : ""
            };
        }

        public static Subscriber[] CreateSubscribers(IOrganizationService service, Guid[] memberIds, string memberType)
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
            foreach (Guid memberId in memberIds)
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

        public static Subscriber CreateSubscriber(IOrganizationService service, Guid memberId, string memberType)
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
    }
}
