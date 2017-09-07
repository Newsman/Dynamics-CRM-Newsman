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
            //initialize process timer
            DateTime startProcessingTime = DateTime.Now;
            int totalSeconds = 110;
            int nmcPageCount = 1000;

            Entity image = helper.PluginExecutionContext.PostEntityImages.Contains("Image") ?
                helper.PluginExecutionContext.PostEntityImages["Image"] : entity;

            string name = (string)image.GetValue("nmc_name", image);

            #region Retrieve history
            if (entity.Contains("nmc_nextrunon") && name == "ApiKey")
            {
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
                string lastTimestamp = RetrieveLastTimestamp(helper.OrganizationService);
                double dblTimestamp = lastTimestamp != null ? Convert.ToDouble(lastTimestamp) : (double)0;
                Common.LogToCRM(helper.OrganizationService, $"Attempting history retrieve with list_id {defaultList}, count {nmcPageCount} and timestamp {0}",
                    $"Current last timestamp is {dblTimestamp}");

                //initialize duplicate detection collection
                EntityCollection crtRecords = RetrieveAllCurrentHistory(helper.OrganizationService);

                try
                {
                    //first page
                    var history = nmapi.RetrieveListHistory(defaultList, nmcPageCount, "0");
                    double crtTimestamp = 0;

                    while (true)
                    {
                        //break loop when no other records are returned
                        if (history.Count == 0)
                            break;

                        for (int i = 0; i < history.Count; i++)
                        {
                            #region Check timer
                            //break loop before CRM plugin times out
                            if (DateTime.Now.Subtract(startProcessingTime).TotalSeconds > totalSeconds)
                                break;
                            #endregion

                            //get next record
                            var record = history[i];

                            //save checkpoint for the next history retrieval
                            crtTimestamp = Convert.ToDouble(history[i].timestamp);

                            if (RecordExists(crtRecords, record))
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
                            nmHistoryRec.Attributes["nmc_datetime"] = ConvertTimestamp(record.timestamp);
                            nmHistoryRec.Attributes["nmc_customerid"] = GetEntityReference(helper.OrganizationService, record.email);
                            nmHistoryRec.Attributes["nmc_newsletterid"] = newsletter;

                            helper.OrganizationService.Create(nmHistoryRec);
                            #endregion

                            #region Check timer
                            //break loop before CRM plugin times out
                            if (DateTime.Now.Subtract(startProcessingTime).TotalSeconds > totalSeconds)
                                break;
                            #endregion
                        }


                        #region Check timer
                        //break loop before CRM plugin times out
                        if (DateTime.Now.Subtract(startProcessingTime).TotalSeconds > totalSeconds)
                        {
                            Common.LogToCRM(helper.OrganizationService, "History records creating loop break caused by forced timeout",
                                $"Last processed timestamp is: {crtTimestamp}");
                            break;
                        }
                        #endregion

                        //next pages
                        history = nmapi.RetrieveListHistory(defaultList, nmcPageCount, crtTimestamp.ToString());
                    }
                }
                catch (Exception e)
                {
                    Common.LogToCRM(helper.OrganizationService, "Exception creating history records", e.Message + " // " + e.StackTrace);
                }
            }
            #endregion
        }

        #endregion

        #region Methods
        private bool RecordExists(EntityCollection list, ListHistory item)
        {
            return list.Entities.Any(i => (i.Contains("nmc_subscriberid") && (string)i["nmc_subscriberid"] == item.subscriber_id) &&
                (i.Contains("nmc_action") && (string)i["nmc_action"] == item.action) &&
                (i.Contains("nmc_timestamp") && (string)i["nmc_timestamp"] == item.timestamp));
        }

        private EntityCollection RetrieveAllCurrentHistory(IOrganizationService service)
        {
            EntityCollection allhistoryRecords = new EntityCollection();

            QueryExpression qry = new QueryExpression("nmc_newsmanhistory");
            qry.NoLock = true;
            qry.ColumnSet = new ColumnSet("nmc_subscriberid", "nmc_action", "nmc_timestamp");
            qry.Criteria.AddCondition("nmc_datetime", ConditionOperator.Last7Days);
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

        private DateTime ConvertTimestamp(string timestamp)
        {
            double dTimeStamp = Convert.ToDouble(timestamp);
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddSeconds(dTimeStamp);
        }

        private string RetrieveLastTimestamp(IOrganizationService service)
        {
            QueryExpression qry = new QueryExpression("nmc_newsmanhistory");
            qry.ColumnSet = new ColumnSet("nmc_timestamp");
            qry.Orders.Add(new OrderExpression("nmc_timestamp", OrderType.Ascending));
            qry.TopCount = 1;
            Entity last = service.RetrieveMultiple(qry).Entities.FirstOrDefault();
            if (last != null && last.Contains("nmc_timestamp"))
            {
                return last["nmc_timestamp"].ToString();
            }

            return null;
        }
        #endregion
    }
}
