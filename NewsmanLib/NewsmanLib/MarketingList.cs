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
                    {
                        Common.LogToCRM(helper.OrganizationService, $"Creating new segment for {(string)entity["listname"]}: missing configuration parameters", 
                            "-");
                        return;
                    }
                    #endregion

                    NewsmanAPI api = new NewsmanAPI(apikey, userid);
                    string segmentId = api.CreateSegment(nmList, (string)entity["listname"]);
                    entity["nmc_newsmansegmentid"] = segmentId;

                    Common.LogToCRM(helper.OrganizationService, $"Created segment for {(string)entity["listname"]}", $"Segment id: {segmentId}");
                } 
            }
            catch (Exception ex)
            {
                Common.LogToCRM(helper.OrganizationService, $"Error creating segment for {(string)entity["listname"]}",$"Error message:{ex.Message}, stack trace: {ex.StackTrace}");
            }
        }

        public override void PostUpdate(Entity entity, ConnectionHelper helper)
        {
            Entity image = helper.PluginExecutionContext.PostEntityImages["Image"];
            if (entity.Contains("nmc_syncmembers") &&
                entity["nmc_syncmembers"] != null &&
                (bool)entity["nmc_syncmembers"])
            {
                Common.LogToCRM(helper.OrganizationService, $"Marketing list sync triggered: {entity.Id.ToString()}", "Update triggered on marketing list record");

                try
                {
                    #region check config params
                    string apikey = Common.GetParamValue(helper.OrganizationService, "ApiKey");
                    string userid = Common.GetParamValue(helper.OrganizationService, "UserId");
                    string nmList = Common.GetParamValue(helper.OrganizationService, "Default List");

                    if (apikey == null || userid == null || nmList == null)
                    {
                        Common.LogToCRM(helper.OrganizationService, $"Marketing list sync ignored: {entity.Id.ToString()}", "Missing Newsman API configuration!");
                        return;
                    }
                    #endregion

                    using (NewsmanAPI api = new NewsmanAPI(apikey, userid))
                    {

                        #region Generate query criteria
                        int memberTypeCode = image.GetOptionSetValue("createdfromcode");
                        string memberType = memberTypeCode == 1 ? "account" : (memberTypeCode == 2 ? "contact" : "lead");
                        QueryExpression qry = new QueryExpression(memberType);
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
                        qry.ColumnSet = cols;
                        LinkEntity listMember = qry.AddLink("listmember", memberType + "id", "entityid", JoinOperator.Inner);
                        LinkEntity list = listMember.AddLink("list", "listid", "listid");
                        list.LinkCriteria.AddCondition("listid", ConditionOperator.Equal, entity.Id);
                        #endregion

                        #region Send to Newsman
                        StringBuilder importIDs = new StringBuilder();
                     
                        int totalRecordCount = 0;
                        string segment = (string)image.GetValue("nmc_newsmansegmentid");
                        qry.PageInfo = new PagingInfo();
                        qry.PageInfo.Count = 5000;
                        qry.PageInfo.PageNumber = 1;
                        qry.PageInfo.PagingCookie = null;
                        qry.PageInfo.ReturnTotalRecordCount = true;
                        EntityCollection members = helper.OrganizationService.RetrieveMultiple(qry);
                        Common.LogToCRM(helper.OrganizationService, $"Started synchronization for {(string)image["listname"]}", $"First page has {members.TotalRecordCount.ToString("N0")} record(s)");
                        totalRecordCount += members.TotalRecordCount;

                        //first batch
                        string importId = api.ImportSubscribers(nmList, segment, Common.CreateSubscribers(members));
                        importIDs.Append($"{importId};");

                        //rest of the batches
                        while (members.MoreRecords)
                        {
                            members = helper.OrganizationService.RetrieveMultiple(qry);
                            importId = api.ImportSubscribers(nmList, segment, Common.CreateSubscribers(members));
                            importIDs.Append($"{importId};");
                            qry.PageInfo.PageNumber++;
                            qry.PageInfo.PagingCookie = members.PagingCookie;
                            totalRecordCount += members.TotalRecordCount;
                        };
                        Common.LogToCRM(helper.OrganizationService, $"Finished synchronization for {(string)image["listname"]}", $"Total number of records is: {totalRecordCount.ToString("N0")}. List of generated Newsman import ids: {importIDs.ToString()}");
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Common.LogToCRM(helper.OrganizationService, $"Error syncing list members for {(string)image["listname"]}", $"Error message:{ex.Message}, stack trace: {ex.StackTrace}");
                }
                finally
                {
                    Entity resetList = new Entity(entity.LogicalName, entity.Id);
                    resetList.Attributes["nmc_syncmembers"] = false;
                    helper.OrganizationService.Update(resetList);
                }
            }
        }
    }
}
