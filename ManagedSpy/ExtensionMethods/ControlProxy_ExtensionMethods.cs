using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ManagedSpy;
using System.ComponentModel;
using O2.DotNetWrappers.ExtensionMethods;

namespace ManagedSpy
{
    public static class ControlProxy_ExtensionMethods
    {        
        public static ControlProxy subscribeToEvents(this ControlProxy controlProxy, EventFilterDialog dialog)
        {            
            if (controlProxy.notNull() && dialog.notNull() && dialog.EventList.notNull())
            {
                "[ControlProxy] Subscribing to Events".info();
                foreach (EventDescriptor ed in controlProxy.GetEvents())
                    if (dialog.EventList[ed.Name].Display)
                    {                        
                        controlProxy.SubscribeEvent(ed);
                    }
            }
            return controlProxy;
        }

        public static ControlProxy unsubscribeAllEvents(this ControlProxy controlProxy)
        { 
            //"[ControlProxy] Unsubscribing to All Events".info();
            if (controlProxy.notNull())
                foreach (EventDescriptor ed in controlProxy.GetEvents())             
                    controlProxy.UnsubscribeEvent(ed);            
            return controlProxy;
        }
        public static ControlProxy eventFired_Remove(this ControlProxy controlProxy, ControlProxyEventHandler controlProxyEventHandler)
        { 
            if (controlProxy.notNull())
                controlProxy.EventFired -= controlProxyEventHandler;
            return controlProxy;
        }
            
    }
}
