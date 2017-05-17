using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace NewsmanLib
{
    #region PluginBase

    public class PluginBase : IPlugin
    {
        #region Properties

        #endregion

        #region IPlugin

        public void Execute(IServiceProvider serviceProvider)
        {
            ConnectionHelper ch = new ConnectionHelper(serviceProvider);
            ch.Messages = new List<string>();

            try
            {
                #region EntityMessage

                Entity entity;

                if (ch.PluginExecutionContext.InputParameters.Contains("Target") &&
                    ch.PluginExecutionContext.InputParameters["Target"] is Entity)
                {
                    entity = ch.PluginExecutionContext.InputParameters["Target"] as Entity;
                    if (ch.PluginExecutionContext.Stage == 10 && ch.PluginExecutionContext.MessageName == "Create")
                        PrevalidateCreate(entity, ch);
                    if (ch.PluginExecutionContext.Stage == 40 && ch.PluginExecutionContext.MessageName == "Create")
                        PostCreate(entity, ch);
                    if (ch.PluginExecutionContext.Stage == 40 && ch.PluginExecutionContext.MessageName == "Update")
                        PostUpdate(entity, ch);
                    if (ch.PluginExecutionContext.Stage == 20 && ch.PluginExecutionContext.MessageName == "Create")
                        PreCreate(entity, ch);
                    if (ch.PluginExecutionContext.Stage == 20 && ch.PluginExecutionContext.MessageName == "Update")
                        PreUpdate(entity, ch);
                    if (ch.PluginExecutionContext.Stage == 10 && ch.PluginExecutionContext.MessageName == "Update")
                        PrevalidateUpdate(entity, ch);
                }

                if (ch.PluginExecutionContext.InputParameters.Contains("Target") &&
                    ch.PluginExecutionContext.InputParameters["Target"] is EntityReference)
                {
                    EntityReference target = ch.PluginExecutionContext.InputParameters["Target"] as EntityReference;
                    ;
                    if (ch.PluginExecutionContext.Stage > 30 && ch.PluginExecutionContext.MessageName == "Delete")
                        PostDelete(target, ch);
                    if (ch.PluginExecutionContext.Stage < 30 && ch.PluginExecutionContext.MessageName == "Delete")
                        PreDelete(target, ch);
                }

                if (ch.PluginExecutionContext.Stage < 30 && ch.PluginExecutionContext.MessageName == "RetrieveMultiple")
                    PreRetrieveMultiple();
                if (ch.PluginExecutionContext.Stage > 30 && ch.PluginExecutionContext.MessageName == "RetrieveMultiple")
                    PostRetrieveMultiple();

                #endregion

                #region EntityReferenceMessage

                EntityReference moniker;
                if (ch.PluginExecutionContext.InputParameters.Contains("EntityMoniker") &&
                    ch.PluginExecutionContext.InputParameters["EntityMoniker"] is EntityReference)
                {
                    moniker = ch.PluginExecutionContext.InputParameters["EntityMoniker"] as EntityReference;

                    if (ch.PluginExecutionContext.Stage > 30 && (ch.PluginExecutionContext.MessageName == "SetState" || ch.PluginExecutionContext.MessageName == "SetStateDynamicEntity"))
                        PostSetSate(moniker, ch);
                    if (ch.PluginExecutionContext.Stage > 30 && ch.PluginExecutionContext.MessageName == "Assign")
                        PostAssign(moniker, ch);
                    if (ch.PluginExecutionContext.Stage < 30 && (ch.PluginExecutionContext.MessageName == "SetState" || ch.PluginExecutionContext.MessageName == "SetStateDynamicEntity"))
                        PreSetState(moniker, ch);
                    if (ch.PluginExecutionContext.Stage < 30 && ch.PluginExecutionContext.MessageName == "Assign")
                        PreAssign(moniker, ch);
                }

                if (ch.PluginExecutionContext.InputParameters.Contains("RelatedEntities") &&
                    ch.PluginExecutionContext.InputParameters["RelatedEntities"] is EntityReferenceCollection &&
                    ch.PluginExecutionContext.InputParameters.Contains("Target") &&
                    ch.PluginExecutionContext.InputParameters["Target"] is EntityReference)
                {
                    EntityReferenceCollection relatedEntities = (EntityReferenceCollection)ch.PluginExecutionContext.InputParameters["RelatedEntities"];
                    moniker = (EntityReference)ch.PluginExecutionContext.InputParameters["Target"];

                    if (ch.PluginExecutionContext.Stage > 30 && (ch.PluginExecutionContext.MessageName == "Associate"))
                        PostAssociate(relatedEntities, moniker, ch);
                    if (ch.PluginExecutionContext.Stage > 30 && (ch.PluginExecutionContext.MessageName == "Disassociate"))
                        PostDisassociate(relatedEntities, moniker, ch);
                }

                #endregion

                #region OpportunityMessages

                if (ch.PluginExecutionContext.InputParameters.Contains("OpportunityClose") &&
                    ch.PluginExecutionContext.InputParameters["OpportunityClose"] is Entity)
                {
                    Entity target = ch.PluginExecutionContext.InputParameters["OpportunityClose"] as Entity;
                    if (ch.PluginExecutionContext.Stage > 30 && ch.PluginExecutionContext.MessageName == "Lose")
                        PostLoseOpportunity(target, ch);
                    if (ch.PluginExecutionContext.Stage < 30 && ch.PluginExecutionContext.MessageName == "Lose")
                        PreLoseOpportunity(target, ch);
                    if (ch.PluginExecutionContext.Stage > 30 && ch.PluginExecutionContext.MessageName == "Win")
                        PostWinOpportunity(target, ch);
                    if (ch.PluginExecutionContext.Stage < 30 && ch.PluginExecutionContext.MessageName == "Win")
                        PreWinOpportunity(target, ch);
                }

                #endregion

                #region MarketingListMessages
                if (ch.PluginExecutionContext.InputParameters.Contains("ListId") &&
                    ch.PluginExecutionContext.InputParameters["ListId"] is Guid &&
                    ch.PluginExecutionContext.InputParameters.Contains("EntityId") &&
                    ch.PluginExecutionContext.InputParameters["EntityId"] is Guid)
                {
                    var mkList = (Guid)ch.PluginExecutionContext.InputParameters["ListId"];
                    var member = (Guid)ch.PluginExecutionContext.InputParameters["EntityId"];

                    if (ch.PluginExecutionContext.Stage > 30 && (ch.PluginExecutionContext.MessageName == "AddMember"))
                        PostAddMember(mkList, member, ch);
                    if (ch.PluginExecutionContext.Stage > 30 && (ch.PluginExecutionContext.MessageName == "RemoveMember"))
                        PostRemoveMember(mkList, member, ch);
                }

                if (ch.PluginExecutionContext.MessageName == "AddListMembers" &&
                    ch.PluginExecutionContext.InputParameters.Contains("ListId") &&
                    ch.PluginExecutionContext.InputParameters.Contains("MemberIds"))
                {
                    var mkList = (Guid)ch.PluginExecutionContext.InputParameters["ListId"];
                    var members = (Guid[])ch.PluginExecutionContext.InputParameters["MemberIds"];

                    if (ch.PluginExecutionContext.Stage > 30)
                        PostAddListMembers(mkList, members, ch);
                }
                else if (ch.PluginExecutionContext.MessageName == "Update" && ch.PluginExecutionContext.ParentContext != null &&
                    ch.PluginExecutionContext.ParentContext.MessageName == "AddListMembers" &&
                    ch.PluginExecutionContext.ParentContext.InputParameters.Contains("ListId") &&
                    ch.PluginExecutionContext.ParentContext.InputParameters.Contains("MemberIds"))
                {
                    var mkList = (Guid)ch.PluginExecutionContext.ParentContext.InputParameters["ListId"];
                    var members = (Guid[])ch.PluginExecutionContext.ParentContext.InputParameters["MemberIds"];

                    if (ch.PluginExecutionContext.ParentContext.Stage > 30)
                        PostAddListMembers(mkList, members, ch);
                    if (ch.PluginExecutionContext.ParentContext.Stage < 30)
                        PreAddListMembers(mkList, members, ch);
                }
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception handling
                string errorMessage = ex.Message;
                string traceText = string.Empty;
                ch.TracingService.Trace("Context Depth: " + ch.PluginExecutionContext.Depth);
                if (!ch.ThrowError)
                {
                    ch.Messages.Add("CatchBase");
                    ch.Messages.Add("BaseException: " + ex.Message);
                }

                IEnumerator enumer = ch.Messages.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string value = enumer.Current.ToString();
                    traceText += Environment.NewLine + "-" + value;
                    ch.TracingService.Trace(value);
                }

                if (!ch.ThrowError)
                {
                    ch.TracingService.Trace(ex.Message);
                    ch.TracingService.Trace(ex.StackTrace);
                    throw new Exception("An exception has occured. Please see the trace log for further information.",
                        ex);
                }
                else
                {
                    ch.TracingService.Trace(ex.Message);
                    ch.TracingService.Trace(ex.StackTrace);
                    ch.TracingService.Trace(traceText);

                    throw new InvalidPluginExecutionException(ex.Message);
                } 
                #endregion
            }
        }
        #endregion

        #region Methods

        public virtual void PrevalidateUpdate(Entity entity, ConnectionHelper ch)
        { }

        public virtual void PrevalidateCreate(Entity entity, ConnectionHelper helper)
        { }

        public virtual void PostCreate(Entity entity, ConnectionHelper helper)
        { }

        public virtual void PostUpdate(Entity entity, ConnectionHelper helper)
        { }

        public virtual void PreCreate(Entity entity, ConnectionHelper helper)
        { }

        public virtual void PreUpdate(Entity entity, ConnectionHelper helper)
        { }

        public virtual void PostSetSate(EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PostAssign(EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PostDelete(EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PreSetState(EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PreAssign(EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PreDelete(EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PostAssociate(EntityReferenceCollection coll, EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PostDisassociate(EntityReferenceCollection coll, EntityReference moniker, ConnectionHelper helper)
        { }

        public virtual void PostRetrieveMultiple()
        { }

        public virtual void PreRetrieveMultiple()
        { }

        public virtual void PreWinOpportunity(Entity opportunity, ConnectionHelper helper)
        { }

        public virtual void PostWinOpportunity(Entity opportunity, ConnectionHelper helper)
        { }

        public virtual void PreLoseOpportunity(Entity opportunity, ConnectionHelper helper)
        { }

        public virtual void PostLoseOpportunity(Entity opportunity, ConnectionHelper helper)
        { }

        public virtual void PostRemoveMember(Guid mkList, Guid member, ConnectionHelper ch)
        { }

        public virtual void PostAddMember(Guid mkList, Guid member, ConnectionHelper ch)
        { }

        public virtual void PostAddListMembers(Guid mkList, Guid[] members, ConnectionHelper ch)
        { }

        public virtual void PreAddListMembers(Guid mkList, Guid[] members, ConnectionHelper ch)
        { }

        #endregion
    }

    #endregion

    #region ConnectionHelper

    public class ConnectionHelper
    {
        private int calls = 0;

        private IOrganizationService _OrganizationService;

        public IOrganizationService OrganizationService
        {
            get
            {
                if (_OrganizationService == null || calls > 10000)
                {
                    _OrganizationService = OrganizationServiceFactory.CreateOrganizationService(PluginExecutionContext.UserId);
                    calls = 0;
                }
                calls++;
                return _OrganizationService;
            }
        }
        public IOrganizationServiceFactory OrganizationServiceFactory { get; private set; }
        public ITracingService TracingService { get; private set; }
        public IPluginExecutionContext PluginExecutionContext { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }

        #region Messages

        private List<string> _Messages;

        public List<string> Messages
        {
            get { return _Messages; }
            set { _Messages = value; }
        }

        #endregion //Messages

        #region ThrowError

        private bool _ThrowError;

        public bool ThrowError
        {
            get { return _ThrowError; }
            set { _ThrowError = value; }
        }

        #endregion //ThrowError

        public void Log(string message)
        {
            Messages.Add(message);
        }

        public ConnectionHelper(IServiceProvider serviceProvider)
        {
            PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            OrganizationServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            ServiceProvider = serviceProvider;
        }

        #region HelperMethods

        public object FirstNotNull(string attributeName, Entity entity, Entity image)
        {
            return entity != null && entity.Contains(attributeName) ? entity[attributeName] : image != null && image.Contains(attributeName) ? image[attributeName] : null;
        }

        #endregion
    }

    #endregion
}

namespace Microsoft.Xrm.Sdk
{
    public static class EntityExtensions
    {
        public static object GetValue(this Entity entity, string fieldName)
        {
            return entity.Contains(fieldName) && entity[fieldName] != null ? entity[fieldName] : null;
        }

        public static object GetValue(this Entity entity, string fieldName, Entity image)
        {
            return entity.Contains(fieldName) && entity[fieldName] != null
                ? entity[fieldName]
                : (image != null && image.Contains(fieldName) && image[fieldName] != null
                    ? image[fieldName]
                    : null);
        }

        public static Guid GetLookupId(this Entity entity, string fieldName)
        {
            return GetValue(entity, fieldName) != null && GetValue(entity, fieldName) is EntityReference
                ? ((EntityReference)GetValue(entity, fieldName)).Id
                : Guid.Empty;
        }

        public static int GetOptionSetValue(this Entity entity, string fieldName)
        {
            return GetValue(entity, fieldName) != null && GetValue(entity, fieldName) is OptionSetValue
                ? ((OptionSetValue)GetValue(entity, fieldName)).Value
                : -1;
        }

        public static Guid GetLookupId(this Entity entity, string fieldName, Entity image)
        {
            return GetValue(entity, fieldName, image) != null && GetValue(entity, fieldName, image) is EntityReference
                ? ((EntityReference)GetValue(entity, fieldName, image)).Id
                : Guid.Empty;
        }

        public static int GetOptionSetValue(this Entity entity, string fieldName, Entity image)
        {
            return GetValue(entity, fieldName, image) != null && GetValue(entity, fieldName, image) is OptionSetValue
                ? ((OptionSetValue)GetValue(entity, fieldName, image)).Value
                : -1;
        }

        public static string GetOptionSetTextGivenValue(this Entity entity, IOrganizationService service, string entityName, string attributeName, int selectedValue)
        {
            try
            {
                RetrieveAttributeRequest retrieveAttributeRequest = new RetrieveAttributeRequest { EntityLogicalName = entityName, LogicalName = attributeName, RetrieveAsIfPublished = true };
                RetrieveAttributeResponse retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
                OptionMetadata[] optionList;

                if (attributeName == "statecode")
                {
                    StateAttributeMetadata retrievedPicklistAttributeMetadata = (StateAttributeMetadata)retrieveAttributeResponse.AttributeMetadata;
                    optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
                }
                else if (attributeName == "statuscode")
                {
                    StatusAttributeMetadata retrievedPicklistAttributeMetadata = (StatusAttributeMetadata)retrieveAttributeResponse.AttributeMetadata;
                    optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
                }
                else
                {
                    PicklistAttributeMetadata retrievedPicklistAttributeMetadata = (PicklistAttributeMetadata)retrieveAttributeResponse.AttributeMetadata;
                    optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
                }

                string selectedOptionValue = "";
                if (selectedValue != null)
                    foreach (OptionMetadata oMD in optionList)
                    {
                        if (oMD.Value == selectedValue)
                        {
                            selectedOptionValue = oMD.Label.UserLocalizedLabel.Label;
                            break;
                        }
                    }
                return selectedOptionValue;
            }
            catch (System.ServiceModel.FaultException ex1)
            {
                string strEr = ex1.InnerException.Data.ToString();
                return "";
            }

        }

    }

    public static class EntityReferenceExtensions
    {
        public static SetStateResponse SetState(this EntityReference refEntity, int state, int status,
            IOrganizationService service)
        {
            SetStateRequest setState = new SetStateRequest();
            setState.EntityMoniker = refEntity;
            setState.State = new OptionSetValue(state);
            setState.Status = new OptionSetValue(status);
            return (SetStateResponse)service.Execute(setState);
        }
    }
}