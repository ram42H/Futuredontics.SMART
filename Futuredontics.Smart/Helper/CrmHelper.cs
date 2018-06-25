using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Futuredontics.Smart.Helper
{
    class CrmHelper
    {
        public static int GetCustomOfferApprovalValidity(IOrganizationService crmService)
        {
            int approvalValidityInDays = 0;
            QueryByAttribute querySalesParameterRecord = new QueryByAttribute("fdx_salesparameter");
            querySalesParameterRecord.AddAttributeValue("fdx_name", "Parameters");
            querySalesParameterRecord.ColumnSet = new ColumnSet("fdx_customoffervalidityindays");
            EntityCollection salesParameters = crmService.RetrieveMultiple(querySalesParameterRecord);
            if (salesParameters.Entities.Count == 1)
            {
                approvalValidityInDays = (int)salesParameters.Entities[0]["fdx_customoffervalidityindays"];
            }
            return approvalValidityInDays;
        }
    }
}
