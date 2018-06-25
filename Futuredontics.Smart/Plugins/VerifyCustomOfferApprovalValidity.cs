using Futuredontics.Smart.Helper;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Futuredontics.Smart.Plugins
{
    public class VerifyCustomOfferApprovalValidity : CodeActivity
    {
        [Input("Custom Offer Approved Date")]
        public InArgument<DateTime> CustomOfferApprovedDate { get; set; }

        //[Input("Opportunity Record")]
        //public InArgument<EntityReference> OpportunityRecord { get; set; }

        [Output("Custom Offer Approval Valid")]
        public OutArgument<bool> CustomOfferApprovalValid { get; set; }

        protected override void Execute(CodeActivityContext executionContext)
        {
            //Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            //Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService crmService = serviceFactory.CreateOrganizationService(context.UserId);

            int customOfferApprovalValidityInDays = CrmHelper.GetCustomOfferApprovalValidity(crmService);

            DateTime customOfferApprovedDate = CustomOfferApprovedDate.Get<DateTime>(executionContext);
            

            bool isCustomOfferApprovalValid = DateTime.Today.Subtract(customOfferApprovedDate.Date).TotalDays <= customOfferApprovalValidityInDays;
            CustomOfferApprovalValid.Set(executionContext, isCustomOfferApprovalValid);

            //if(!isCustomOfferApprovalValid)
            //{
            ///   Guid opportunityRecordId = OpportunityRecord.Get<EntityReference>(executionContext).Id;
            //    Entity opportunity = new Entity("opportunity", opportunityRecordId);
            //    opportunity["fdx_approvalstatus"] = null;
            //    opportunity["fdx_triggercustomofferapprovalemailnotificati"] = false;
            //    crmService.Update(opportunity);
            //}

            tracingService.Trace("Custom Offer Approval Validity In Days " + customOfferApprovalValidityInDays);
            tracingService.Trace("Custom Offer Approved Date " + customOfferApprovedDate.Date.ToString());
            tracingService.Trace("Today " + DateTime.Today.ToString());
            tracingService.Trace("Is Custom Offer Approval Valid?" + isCustomOfferApprovalValid.ToString());
        }
    }
}
