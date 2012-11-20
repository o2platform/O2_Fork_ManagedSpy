using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ManagedSpy;
using System.ComponentModel;
using O2.DotNetWrappers.ExtensionMethods;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace ManagedSpy
{
    public static class ControlProxy_ExtensionMethods
    {
        public static ControlProxy get_ControlProxy_for_MainWindowHandle(this Process process)
        {
            if (process.notNull())
                try
                {
                    return ControlProxy.FromHandle(process.MainWindowHandle);
                }
                catch (Exception ex)
                {
                    ex.log();
                }
            return null;
        }
        public static ControlProxy add_ControlProxy(this TreeNode treeNode, Process process)
        {
            if (treeNode.notNull() && process.notNull())
            { 
                var controlProxy = process.get_ControlProxy_for_MainWindowHandle();
                if (controlProxy.notNull())
                    treeNode.add_Node(controlProxy);
                //treeNode.Tag = 
            }
            return null;
        }

        public static TreeView add_ControlProxies_for_Process(this TreeView treeView, Process process)
        {
            treeView.clear();
            if (process.isProcessHooked())
            {
                treeView.green();
                var controlProxy =process.get_ControlProxy_for_MainWindowHandle();
                if (controlProxy.notNull())
                    treeView.add_Node(controlProxy)
                            .addChildControlProxies(controlProxy);
            }
            else 
            {
                treeView.add_Node("INFO: Process not Hooked");
                treeView.pink();
            }
            return treeView;
        }

        public static TreeNode addChildControlProxies(this TreeNode treeNode, ControlProxy controlProxy)
        {                       
            if (treeNode.notNull() && controlProxy != null) 
            {                
                treeNode.add_Node("...loading data...");
                var children = controlProxy.Children;
                treeNode.clear();
                foreach (ControlProxy proxychild in children) 
                {
                    var name = String.IsNullOrEmpty(proxychild.GetComponentName()) 
                                            ?   "<noname>" 
                                            : proxychild.GetComponentName();
                    var nodeText = name + "     [" +proxychild.GetClassName() + "]";//proxychild.Handle.ToString()
                    var hasChildren = proxychild.Children.size() > 0;
                    treeNode.add_Node(nodeText, proxychild,  hasChildren);                    
                }                
            }    
            
            return treeNode;
        }

        public static bool isProcessHooked(this Process process)
        { 
            return process.processHasModule("ManagedSpyLib.dll");
        }



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
        public static ControlProxy eventFired_Add(this ControlProxy controlProxy, ControlProxyEventHandler controlProxyEventHandler)
        { 
            if (controlProxy.notNull())
                controlProxy.EventFired += controlProxyEventHandler;
            return controlProxy;
        }
            
    }
}
