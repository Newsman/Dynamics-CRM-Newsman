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
    }
}
