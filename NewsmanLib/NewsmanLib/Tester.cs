using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewsmanLib
{
    class Tester : IPlugin
    {
        void IPlugin.Execute(IServiceProvider serviceProvider)
        {
            ConnectionHelper ch = new ConnectionHelper(serviceProvider);
            IPluginExecutionContext context = ch.PluginExecutionContext;

            if (context.ParentContext.MessageName == "AddListMembers")
            {
                Common.ListInputParameters(context);
            }
        }
    }
}
