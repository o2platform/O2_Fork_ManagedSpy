using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using O2.DotNetWrappers.ExtensionMethods;
using System.Diagnostics;
using O2.Kernel;
using Microsoft.ManagedSpy;
using O2.DotNetWrappers.Windows;
using System.Drawing;

namespace ManagedSpy
{
    public static class MainGui_ExtensionMethods
    {
        public static string TestFile1 { get; set; }

        static MainGui_ExtensionMethods()
        {  
            TestFile1 = @"Util - Simple Text Editor.exe".local();
        }
        public static MainGui enableVisualStudioObjectCreation(this MainGui mainGui)
        { 
            var vsModules = (from frame in new StackTrace().GetFrames()
                             let module = frame.GetMethod().Module 
                             where module.Name.contains("O2_FluentSharp_CoreLib")
                             select module.Name).distinct();
            //show.info(vsModules);     
            if (vsModules.empty()) 
            { 
                var callbackFromVs = (Action<Type>)"onMainGuiCtor".o2Cache();
                callbackFromVs.invoke(mainGui.type());
            }
            return mainGui;
        }
        
        public static MainGui add_TargetProcesses(this MainGui mainGui)
        {                         
            var currentProcessId = Process.GetCurrentProcess().Id;
            var processes = Process.GetProcesses()
                                   .Where((process)=>process.Id != currentProcessId)
                                   .Where((process)=> process.MainWindowHandle != IntPtr.Zero);
                                   //.Where((process)=> process.processHasModule("mscorlib"));

            mainGui.treeWindow.clear();
            var dotNetProcesses = mainGui.treeWindow.add_Node(".Net Processes");
            var otherProcesses  = mainGui.treeWindow.add_Node("Other Processes");
            var accessDenied    = mainGui.treeWindow.add_Node("Access Denied");
                
            foreach (var process in processes)
            { 
            //    if processHasModule(process,"mscorlib")
                var text = "{0}             id: {1}".format(process.ProcessName, process.Id);                                                    

                var targetNode = process.doWeHaveAccess()
                                    ?   process.processHasModule("mscorlib") 
                                            ? dotNetProcesses
                                            : otherProcesses
                                    :   accessDenied;

               targetNode.add_Node(text, process)
                         .showVisualClueWhenProcessIsHooked(process);                
            }
            
            dotNetProcesses.expand()
                           .nodes().first().selected();
            
            return mainGui;
        }

        public static TreeNode showVisualClueWhenProcessIsHooked(this TreeNode treeNode, Process process)
        { 
            if (process.doWeHaveAccess() && process.processHasModule("ManagedSpyLib.dll"))
            {
                treeNode.set_Text(treeNode.get_Text() + "   [Hooked]");                
                treeNode.color(Color.DarkGreen);
            }
            else
                treeNode.color(Color.DarkRed);
          return treeNode;  
        }


        public static ControlProxy currentProxy(this MainGui mainGui)
        {
            return mainGui.propertyGrid.SelectedObject as ControlProxy;
        }

        public static ControlProxy resetEventsSubscriptions(this MainGui mainGui)
        {
            return mainGui.currentProxy().unsubscribeAllEvents()
                                         .subscribeToEvents(mainGui.dialog);
        }

        public static MainGui add_EventsFilters(this MainGui mainGui)
        {
            try
            {
                var eventFilterDialog = mainGui.dialog;
                var dataGridView = eventFilterDialog.control<DataGridView>().columnWidth(0, -1).columnWidth(1, 50);
                var rightPanel = mainGui.tabControl1.insert_Right("Event Filters");
                rightPanel.add_Control(dataGridView);

                Action<bool> setDisplayForAllEvents =
                    (value) =>
                    {
                        dataGridView.invokeOnThread(
                                ()=>{
                                        dataGridView.Rows.forEach(
                                                (DataGridViewRow row) =>{       
                                                                            row.Cells[1].Value = value;
                                                                            mainGui.dialog.EventList[row.Cells[0].Value.str()].Display = value;
                                                                        });
                                        mainGui.resetEventsSubscriptions();
                                    });
                    };

                dataGridView.add_ContextMenu()
                            .add_MenuItem("DeSelect All", true,  () => setDisplayForAllEvents(false))
                            .add_MenuItem("Select All",          () => setDisplayForAllEvents(true));

                dataGridView.onClick((row, cell) =>
                {
                    if (cell == 1)
                    {
                        var name = dataGridView.Rows[row].Cells[0].Value.str();
                        var value =!mainGui.dialog.EventList[name].Display;                        
                        mainGui.dialog.EventList[name].Display = value;
                        mainGui.resetEventsSubscriptions();
                    }
                });
            }
            catch (Exception ex)
            {
                ex.log("in add_EventsFilters");
            }
            return mainGui;
        }
    }
}