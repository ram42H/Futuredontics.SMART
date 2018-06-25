using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;
using Futuredontics.Smart.DataObject;

namespace Futuredontics.Smart
{

    /// <summary>
    /// PluginEntryPoint plug-in.
    /// This is a generic entry point for a plug-in class. Use the Plug-in Registration tool found in the CRM SDK to register this class, import the assembly into CRM, and then create step associations.
    /// A given plug-in can have any number of steps associated with it. 
    /// </summary>    
    public class CustomOfferValidation : PluginBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomOfferValidation"/> class.
        /// </summary>
        /// <param name="unsecure">Contains public (unsecured) configuration information.</param>
        /// <param name="secure">Contains non-public (secured) configuration information. 
        /// When using Microsoft Dynamics CRM for Outlook with Offline Access, 
        /// the secure string is not passed to a plug-in that executes while the client is offline.</param>
        public CustomOfferValidation(string unsecure, string secure)
            : base(typeof(CustomOfferValidation))
        {
        }

        /// <summary>
        /// Main entry point for he business logic that the plug-in is to execute.
        /// </summary>
        /// <param name="localContext">The <see cref="LocalPluginContext"/> which contains the
        /// <see cref="IPluginExecutionContext"/>,
        /// <see cref="IOrganizationService"/>
        /// and <see cref="ITracingService"/>
        /// </param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances.
        /// The plug-in's Execute method should be written to be stateless as the constructor
        /// is not called for every invocation of the plug-in. Also, multiple system threads
        /// could execute the plug-in at the same time. All per invocation state information
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        protected override void ExecuteCrmPlugin(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }
            IOrganizationService crmService = localContext.OrganizationService;
            bool customOffer = false;
            Guid opportunityId = ((EntityReference)localContext.PluginExecutionContext.InputParameters["Target"]).Id;
            localContext.Trace("Inside Execute");
            localContext.Trace("Message: " + localContext.PluginExecutionContext.MessageName);
            localContext.Trace("Opportunity Id: " + opportunityId.ToString());
            List<Product> recommendedProducts = GetRecommendedProducts(opportunityId, crmService);
            localContext.Trace("No. of Recommended Products: " + recommendedProducts.Count.ToString());
            PopulateRecommendedProductPrices(opportunityId, recommendedProducts, crmService);
            localContext.Trace("Price Populated for Recommended Products: ");
            List<Product> opportunityProducts = GetOpportunityProducts(opportunityId, crmService);
            localContext.Trace("No. of Opportunity Products: " + opportunityProducts.Count.ToString());
            if (opportunityProducts.Count > 0)
            {
                bool recommendedProductInOpportunityProduct = CheckIfAtleastOneRecommendedProductExits(recommendedProducts, opportunityProducts, localContext);
                customOffer = !recommendedProductInOpportunityProduct;
            }
            localContext.Trace("Custom Offer Validation Result: " + customOffer.ToString());
            localContext.PluginExecutionContext.OutputParameters["CustomOffer"] = customOffer;

            Entity opportunity = new Entity("opportunity", opportunityId);
            opportunity["fdx_customoffer"] = customOffer;
            crmService.Update(opportunity);
            localContext.Trace("Opportunity Updated!");
        }

        private bool CheckIfAtleastOneRecommendedProductExits(List<Product> recommendedProducts, List<Product> opportunityProducts, LocalPluginContext localContext)
        {
            bool recommendedProductInOpportunityProduct = false;
            foreach (Product recommendedProduct in recommendedProducts)
            {
                Product opportunityProduct = opportunityProducts.Find(a => a.Id.Equals(recommendedProduct.Id));
                if (opportunityProduct != null) localContext.Trace("Opportunity Product Price Matched with Recommended Product");

                if (opportunityProduct != null && (opportunityProduct.Price >= recommendedProduct.Price))
                {
                    localContext.Trace("Opportunity Product Price >= Recommended Product Price");
                    recommendedProductInOpportunityProduct = true;
                }
                if (recommendedProductInOpportunityProduct) break;
            }
            return recommendedProductInOpportunityProduct;
        }

        private List<Product> GetOpportunityProducts(Guid opportunityId, IOrganizationService crmService)
        {
            List<Product> opportunityProducts = new List<Product>();
            QueryByAttribute opportunityProductsQueryByOpportunityId = new QueryByAttribute("opportunityproduct");
            opportunityProductsQueryByOpportunityId.AddAttributeValue("opportunityid", opportunityId);
            opportunityProductsQueryByOpportunityId.ColumnSet = new ColumnSet("productid", "fdx_netprice");
            EntityCollection opportunityProductRecords = crmService.RetrieveMultiple(opportunityProductsQueryByOpportunityId);
            foreach (Entity opportunityProductRecord in opportunityProductRecords.Entities)
            {
                Product product = new Product();
                product.Id = ((EntityReference)opportunityProductRecord["productid"]).Id;
                if (opportunityProductRecord.Contains("fdx_netprice"))
                    product.Price = ((Money)opportunityProductRecord["fdx_netprice"]).Value;
                opportunityProducts.Add(product);
            }
            return opportunityProducts;
        }

        private List<Product> GetRecommendedProducts(Guid opportunityId, IOrganizationService crmService)
        {
            List<Product> recommendedProducts = new List<Product>();
            string prospectGroupFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                              <entity name='product'>
                                                <attribute name='name' />
                                                <attribute name='productid' />
                                                <attribute name='productnumber' />
                                                <attribute name='description' />
                                                <attribute name='statecode' />
                                                <attribute name='productstructure' />
                                                <order attribute='productnumber' descending='false' />
                                                <link-entity name='fdx_fdx_prospectgroup_product' from='productid' to='productid' visible='false' intersect='true'>
                                                  <link-entity name='fdx_prospectgroup' from='fdx_prospectgroupid' to='fdx_prospectgroupid' alias='ae'>
                                                    <link-entity name='opportunity' from='fdx_prospectgroup' to='fdx_prospectgroupid' alias='af'>
                                                      <filter type='and'>
                                                        <condition attribute='opportunityid' operator='eq' uitype='opportunity' value='{0}' />
                                                      </filter>
                                                    </link-entity>
                                                  </link-entity>
                                                </link-entity>
                                              </entity>
                                            </fetch>";
            prospectGroupFetchXml = string.Format(prospectGroupFetchXml, opportunityId.ToString());
            EntityCollection productRecords = crmService.RetrieveMultiple(new FetchExpression(prospectGroupFetchXml));
            foreach (Entity productRecord in productRecords.Entities)
            {
                Product product = new Product();
                product.Id = productRecord.Id;
                recommendedProducts.Add(product);
            }
            return recommendedProducts;
        }

        private void PopulateRecommendedProductPrices(Guid opportunityId, List<Product> recommendedProducts, IOrganizationService crmService)
        {
            string priceListItemFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                              <entity name='productpricelevel'>
                                                <attribute name='productid' />
                                                <attribute name='amount' />
                                                <link-entity name='pricelevel' from='pricelevelid' to='pricelevelid'>
                                                  <link-entity name='opportunity' from='pricelevelid' to='pricelevelid' alias='opppri'>
                                                    <filter type='and'>
                                                        <condition attribute='opportunityid' operator='eq' uitype='opportunity' value='{0}' />
                                                     </filter>
                                                  </link-entity>
                                                </link-entity>
                                              </entity>
                                            </fetch>";
            priceListItemFetchXml = string.Format(priceListItemFetchXml, opportunityId.ToString());
            EntityCollection priceListItemRecords = crmService.RetrieveMultiple(new FetchExpression(priceListItemFetchXml));
            foreach (Entity priceListItemRecord in priceListItemRecords.Entities)
            {
                Guid priceListItemProductId = ((EntityReference)priceListItemRecord["productid"]).Id;
                Product product = recommendedProducts.Find(a => a.Id.Equals(priceListItemProductId));
                if (product != null)
                {
                    product.Price = ((Money)priceListItemRecord["amount"]).Value;
                }
            }
        }
    }
}
