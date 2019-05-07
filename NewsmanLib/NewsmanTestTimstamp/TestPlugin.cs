using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsmanTestTimstamp
{
    public class TestPlugin : PluginBase
    {
        public override void PreUpdate(Entity entity, ConnectionHelper helper)
        {
            QueryExpression qry = new QueryExpression("nmc_newsmanhistory");
            qry.ColumnSet = new ColumnSet("nmc_timestamp");
            qry.Orders.Add(new OrderExpression("nmc_timestamp", OrderType.Ascending));
            qry.NoLock = true;
            qry.TopCount = 1;
            Entity last = helper.OrganizationService.RetrieveMultiple(qry).Entities.FirstOrDefault();
            if (last != null && last.Contains("nmc_timestamp"))
            {
                throw new InvalidPluginExecutionException("TIMESTAMP CITIT: " + last["nmc_timestamp"].ToString());
            }

            return;
        }
    }
}
