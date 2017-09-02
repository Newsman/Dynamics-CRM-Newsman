using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using NewsmanLib.APIHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewsmanLibConsole
{
    class Program
    {
        private const string apikey = "dmeo";
        private const string userid = "demo";
        private const string defaultList = "demo";
        private static string expand = string.Empty;

        static void Main(string[] args)
        {
            using (NewsmanAPI api = new NewsmanAPI(apikey, userid))
            {
                #region commented
                //var lists = api.RetrieveLists();
                //var segments = api.RetrieveSegments(defaultList);

                //var newSegment = api.CreateSegment(defaultList, "segment din consola");

                //List<Subscriber> subscribers = new List<Subscriber>();
                //subscribers.Add(new Subscriber { Email = "abcdee@nubiz.com", Firstname = "abf", Lastname = "cr" });
                //subscribers.Add(new Subscriber { Email = "bcderr@nubiz.com", Firstname = "bcf", Lastname = "dr" });
                //subscribers.Add(new Subscriber { Email = "george.calinescu@outlook.com", Firstname = "george", Lastname = "calinescu" });
                //var resp = api.ImportSubscribers(defaultList, "75031", subscribers);

                //var status = api.ImportStatus(resp.Replace("\"", ""));
                //DateTimeOffset.UtcNow.Ticks.ToString() 
                #endregion

                TimeSpan ts = DateTime.Now.Subtract(DateTime.Now.AddMinutes(-2));

                History(api);
            }
        }

        static void History(NewsmanAPI api)
        {
            int pageSize = 1000;

            //crm connection
            CrmServiceClient conn = new CrmServiceClient("connection string");

            // Cast the proxy client to the IOrganizationService interface.
            IOrganizationService service = conn.OrganizationWebProxyClient != null ? 
                (IOrganizationService)conn.OrganizationWebProxyClient : 
                (IOrganizationService)conn.OrganizationServiceProxy;

            //initialize duplicate detection collection
            EntityCollection crtRecords = RetrieveAllCurrentHistory(service);

            double dblTimestamp = 1504127167.2155;
            double crtTimestamp = 0;
            List<ListHistory> list = api.RetrieveListHistory(defaultList, pageSize, "0");

            List<string> contactslist = new List<string>();
            DateTime startProcessingTime = DateTime.Now;
            double failTimestamps = 0;
            
            while (true)
            {
                if (list.Count > 0)
                {
                    crtTimestamp = Convert.ToDouble(list[list.Count - 1].timestamp);
                }
                else
                {
                    failTimestamps = crtTimestamp;
                    break;
                }

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var record = list[i];

                    if (Convert.ToDouble(record.timestamp) >= dblTimestamp)
                    {
                        if (RecordExists(crtRecords, record))
                            continue;

                        contactslist.Add(record.email);
                        //save checkpoint for the next history retrieval
                        crtTimestamp = Convert.ToDouble(list[i].timestamp);
                    }
                }

                if (dblTimestamp != 0 && crtTimestamp < dblTimestamp)
                    break;

                list = api.RetrieveListHistory(defaultList, pageSize, crtTimestamp.ToString());
            }

            var original = contactslist.Count;
            contactslist.ForEach(Concatenate);
            expand += "//";
            var duplicates = contactslist.Distinct().Count();
        }

        static void Concatenate(string s)
        {
            expand += s;
            return;
        }

        static DateTime ConvertTimestamp(string timestamp)
        {
            double dTimeStamp = Convert.ToDouble(timestamp);
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddSeconds(dTimeStamp).ToLocalTime();
        }

        static bool RecordExists(EntityCollection list, ListHistory item)
        {
            return list.Entities.Any(i => (i.Contains("nmc_subscriberid") && (string)i["nmc_subscriberid"] == item.subscriber_id) &&
                (i.Contains("nmc_action") && (string)i["nmc_action"] == item.action) &&
                (i.Contains("nmc_timestamp") && (string)i["nmc_timestamp"] == item.timestamp));
        }

        static EntityCollection RetrieveAllCurrentHistory(IOrganizationService service)
        {
            EntityCollection allhistoryRecords = new EntityCollection();

            QueryExpression qry = new QueryExpression("nmc_newsmanhistory");
            qry.NoLock = true;
            qry.ColumnSet = new ColumnSet("nmc_subscriberid", "nmc_action", "nmc_timestamp");
            qry.PageInfo = new PagingInfo();
            qry.PageInfo.Count = 5000;
            qry.PageInfo.PageNumber = 1;
            qry.PageInfo.PagingCookie = null;

            while (true)
            {
                EntityCollection results = service.RetrieveMultiple(qry);
                allhistoryRecords.Entities.AddRange(results.Entities);

                if (results.MoreRecords)
                {
                    qry.PageInfo.PageNumber++;
                    qry.PageInfo.PagingCookie = results.PagingCookie;
                }
                else
                {
                    break;
                }
            }

            return allhistoryRecords;
        }
    }
}
