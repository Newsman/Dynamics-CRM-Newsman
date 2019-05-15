using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using NewsmanLib.APIHelper;
using System.Web.Script.Serialization;
using Microsoft.Xrm.Sdk.Query;

namespace NewsmanLib
{
    public class NewsmanConfig : PluginBase
    {
        #region Properties
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        #endregion

        #region Overrides
        public override void PostCreate(Entity entity, ConnectionHelper helper)
        {
            try
            {
                if (entity.Contains("nmc_name") &&
                    (entity["nmc_name"].ToString() == "ApiKey" || entity["nmc_name"].ToString() == "UserId"))
                {
                    #region check config params
                    string apikey = null;
                    string userid = null;
                    Entity nmLists = Common.GetParamEntity(helper.OrganizationService, "Newsman Lists");
                    Entity defaultList = Common.GetParamEntity(helper.OrganizationService, "Default List");

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
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public override void PostUpdate(Entity entity, ConnectionHelper helper)
        {
            DateTime startProcessingTime = DateTime.Now;
            Entity image = helper.PluginExecutionContext.PostEntityImages.Contains("Image") ?
                    helper.PluginExecutionContext.PostEntityImages["Image"] : entity;
            string name = (string)image.GetValue("nmc_name", image);

            #region Retrieve history
            if (entity.Contains("nmc_nextrunon") && name == "ApiKey")
            {
                //initialization
                int totalSeconds = 100;
                int nmcPageCount = 100;
                Guid LastTimestampID = InitializeTimestamp(helper.OrganizationService);

                #region check config params
                string apikey = Common.GetParamValue(helper.OrganizationService, "ApiKey");
                string userid = Common.GetParamValue(helper.OrganizationService, "UserId");
                string defaultList = Common.GetParamValue(helper.OrganizationService, "Default List");

                if (apikey == null || userid == null || defaultList == null)
                {
                    Common.LogToCRM(helper.OrganizationService, "History retrieve attempt failed. Missing configuration parameters", "");
                    return;
                }
                #endregion

                //create newsman api instance
                NewsmanAPI nmapi = new NewsmanAPI(apikey, userid);

                //retrieve last history timestamp
                string lastTimestamp = RetrieveLastTimestamp(helper.OrganizationService, LastTimestampID);
                Common.LogToCRM(helper.OrganizationService, $"Attempting history retrieve with list_id {defaultList}, count {nmcPageCount} and timestamp {lastTimestamp}",
                    $"Current last timestamp is {lastTimestamp}");

                //initialize duplicate detection collection
                EntityCollection crtCRMRecords = RetrieveAllCurrentHistory(helper.OrganizationService);
                IEnumerable<DuplicateSubscriber> crtRecords = crtCRMRecords.Entities.Where(i => i.Contains("nmc_subscriberid") && i.Contains("nmc_action") && i.Contains("nmc_timestamp")).
                    Select(s => new DuplicateSubscriber()
                    {
                        Subscriber = (string)s["nmc_subscriberid"],
                        Action = (string)s["nmc_action"],
                        Timestamp = (string)s["nmc_timestamp"]
                    });

                ICollection<DuplicateSubscriber> crtColl = crtRecords.ToList();

                try
                {
                    //first page
                    var history = nmapi.RetrieveListHistory(defaultList, nmcPageCount, lastTimestamp).ToArray();
                    string crtTimestamp = string.Empty;

                    while (true)
                    {
                        //break loop when no other records are returned
                        if (history.Length == 0)
                        {
                            //reset  timestamp
                            ResetTimestamp(helper.OrganizationService, LastTimestampID);

                            break;
                        }

                        //save checkpoint for the next history retrieval
                        crtTimestamp = history.Min(h => h.timestamp);

                        #region Check timer
                        //break loop before CRM plugin times out
                        if (DateTime.Now.Subtract(startProcessingTime).TotalSeconds > totalSeconds)
                        {
                            Common.LogToCRM(helper.OrganizationService, "History records creating loop break caused by forced timeout", null);
                            break;
                        }
                        #endregion

                        for (int i = 0; i < history.Length; i++)
                        {
                            //get next record
                            var record = history[i];
                            if (RecordExists(crtColl, record))
                                continue;

                            #region Newsletter information
                            EntityReference newsletter = GetEntityReference(helper.OrganizationService, record.newsletter_id, "nmc_newsmannewsletter");
                            if (newsletter == null)
                            {
                                //create newsletter record
                                Entity nl = new Entity("nmc_newsmannewsletter");
                                nl.Attributes.Add("nmc_subject", record.newsletter_subject);
                                nl.Attributes.Add("nmc_newsletterid", record.newsletter_id);
                                nl.Attributes.Add("nmc_newsletterlink", NewsmanDefaults.NewsletterLink(defaultList, record.newsletter_id));
                                Guid nlId = helper.OrganizationService.Create(nl);

                                newsletter = new EntityReference("nmc_newsmannewsletter", nlId);
                            }
                            #endregion

                            #region Create history
                            Entity nmHistoryRec = new Entity("nmc_newsmanhistory");
                            nmHistoryRec.Attributes["nmc_date"] = record.date;
                            nmHistoryRec.Attributes["nmc_action"] = record.action;
                            nmHistoryRec.Attributes["nmc_subscriberid"] = record.subscriber_id;
                            nmHistoryRec.Attributes["nmc_timestamp"] = record.timestamp;
                            nmHistoryRec.Attributes["nmc_linkurl"] = record.url;
                            nmHistoryRec.Attributes["nmc_datetime"] = DatetimeFromTimestamp(record.timestamp);
                            nmHistoryRec.Attributes["nmc_emailused"] = record.email;
                            nmHistoryRec.Attributes["nmc_customerid"] = GetEntityReference(helper.OrganizationService, record.email);
                            nmHistoryRec.Attributes["nmc_newsletterid"] = newsletter;

                            helper.OrganizationService.Create(nmHistoryRec);
                            #endregion

                            //extend the collection
                            crtColl.Add(new DuplicateSubscriber() { Action = record.action, Subscriber = record.subscriber_id, Timestamp = record.timestamp });
                        }

                        UpdateLastTimestamp(helper.OrganizationService, LastTimestampID, crtTimestamp);

                        #region Check timer
                        //break loop before CRM plugin times out
                        if (DateTime.Now.Subtract(startProcessingTime).TotalSeconds > totalSeconds)
                        {
                            Common.LogToCRM(helper.OrganizationService, "History records creating loop break caused by forced timeout", null);
                            break;
                        }
                        #endregion

                        //next page
                        history = nmapi.RetrieveListHistory(defaultList, nmcPageCount, crtTimestamp).ToArray();
                    }
                }
                catch (Exception e)
                {
                    Common.LogToCRM(helper.OrganizationService, "Exception creating history records", e.ToString());
                }
            }
            #endregion
        }



        #endregion

        #region Methods
        private Guid InitializeTimestamp(IOrganizationService service)
        {
            Guid nmcLT = Guid.Empty;

            QueryExpression qryLT = new QueryExpression("nmc_newsmanconfig");
            qryLT.NoLock = true;
            qryLT.ColumnSet = new ColumnSet();
            qryLT.Criteria.AddCondition("nmc_name", ConditionOperator.Equal, "Last Timestamp");
            qryLT.TopCount = 1;

            EntityCollection ltResults = service.RetrieveMultiple(qryLT);

            if (ltResults.Entities.Count > 0)
            {
                nmcLT = ltResults[0].Id;
            }
            else
            {
                Entity lastTimestampValue = new Entity("nmc_newsmanconfig");
                lastTimestampValue.Attributes["nmc_name"] = "Last Timestamp";
                lastTimestampValue.Attributes["nmc_value"] = "0";

                nmcLT = service.Create(lastTimestampValue);
            }

            return nmcLT;
        }

        private void ResetTimestamp(IOrganizationService service, Guid TSGuid)
        {
            try
            {
                Entity lastTimestampValue = new Entity("nmc_newsmanconfig");
                lastTimestampValue.Id = TSGuid;
                lastTimestampValue.Attributes["nmc_value"] = "0";

                service.Update(lastTimestampValue);
            }
            catch (Exception e)
            {
                Common.LogToCRM(service, "Error reseting timestamp value", e.ToString());
            }
        }

        private bool RecordExists(EntityCollection list, ListHistory item)
        {
            return list.Entities.Any(i => (i.Contains("nmc_subscriberid") && (string)i["nmc_subscriberid"] == item.subscriber_id) &&
                (i.Contains("nmc_action") && (string)i["nmc_action"] == item.action) &&
                (i.Contains("nmc_timestamp") && (string)i["nmc_timestamp"] == item.timestamp));
        }

        private bool RecordExists(IEnumerable<DuplicateSubscriber> list, ListHistory item)
        {
            return list.Any(s => s.Subscriber == item.subscriber_id && s.Action == item.action && s.Timestamp == item.timestamp);
        }

        private EntityCollection RetrieveAllCurrentHistory(IOrganizationService service)
        {
            EntityCollection allhistoryRecords = new EntityCollection();

            QueryExpression qry = new QueryExpression("nmc_newsmanhistory");
            qry.NoLock = true;
            qry.ColumnSet = new ColumnSet("nmc_subscriberid", "nmc_action", "nmc_timestamp");
            qry.Criteria.AddCondition("nmc_datetime", ConditionOperator.LastXDays, 5);
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

        private EntityReference GetEntityReference(IOrganizationService service, string searchField, string entityType)
        {
            EntityReference lookup = null;
            QueryByAttribute qry = new QueryByAttribute(entityType);
            qry.ColumnSet = new ColumnSet();
            qry.TopCount = 1;

            switch (entityType)
            {
                case "contact":
                    qry.AddAttributeValue("emailaddress1", searchField);
                    break;
                default:
                    qry.AddAttributeValue("nmc_newsletterid", searchField);
                    break;
            }

            Entity lk = service.RetrieveMultiple(qry).Entities.FirstOrDefault();
            if (lk != null)
                lookup = lk.ToEntityReference();

            return lookup;
        }

        private EntityReference GetEntityReference(IOrganizationService service, string searchField)
        {
            EntityReference lookup = null;
            QueryExpression qry = new QueryExpression("account");
            qry.ColumnSet = new ColumnSet();
            qry.TopCount = 1;
            qry.NoLock = true;
            qry.Criteria = new FilterExpression(LogicalOperator.Or);
            qry.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, searchField);
            qry.Criteria.AddCondition("emailaddress2", ConditionOperator.Equal, searchField);
            qry.Criteria.AddCondition("emailaddress3", ConditionOperator.Equal, searchField);

            Entity lk = service.RetrieveMultiple(qry).Entities.FirstOrDefault();
            if (lk != null)
                return lk.ToEntityReference();
            else
            {
                qry = new QueryExpression("contact");
                qry.ColumnSet = new ColumnSet();
                qry.TopCount = 1;
                qry.NoLock = true;
                qry.Criteria = new FilterExpression(LogicalOperator.Or);
                qry.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, searchField);
                qry.Criteria.AddCondition("emailaddress2", ConditionOperator.Equal, searchField);
                qry.Criteria.AddCondition("emailaddress3", ConditionOperator.Equal, searchField);

                lk = service.RetrieveMultiple(qry).Entities.FirstOrDefault();
                if (lk != null)
                    return lk.ToEntityReference();
            }

            return lookup;
        }

        private DateTime DatetimeFromTimestamp(string timestamp)
        {
            double dTimeStamp = Convert.ToDouble(timestamp);
            return Epoch.AddSeconds(dTimeStamp);
        }

        private double TimestampFromDatetime(DateTime dt)
        {
            TimeSpan ts = dt - Epoch;
            return ts.TotalSeconds;
        }

        private string RetrieveLastTimestamp(IOrganizationService service, Guid LTID)
        {
            Entity LTConfig = service.Retrieve("nmc_newsmanconfig", LTID, new ColumnSet("nmc_value"));
            string lastTimestamp = string.Empty;

            if (LTConfig != null && LTConfig.Contains("nmc_value"))
            {
                lastTimestamp = LTConfig["nmc_value"].ToString();

                if (DatetimeFromTimestamp(lastTimestamp) < DateTime.Today.AddDays(-1))
                {
                    return "0";
                }
                else
                    return lastTimestamp;
            }

            return "0";
        }

        private void UpdateLastTimestamp(IOrganizationService service, Guid LTID, string ltValue)
        {
            try
            {
                Entity upLast = new Entity("nmc_newsmanconfig");
                upLast.Id = LTID;
                upLast.Attributes["nmc_value"] = ltValue;
                service.Update(upLast);
            }
            catch (Exception e)
            {
                Common.LogToCRM(service, "Failed updating last timestamp", e.ToString());
            }
        }
        #endregion
    }
}
