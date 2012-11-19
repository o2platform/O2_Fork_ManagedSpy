using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using O2.DotNetWrappers.ExtensionMethods;
using System.Diagnostics;
using O2.Kernel;
using Microsoft.ManagedSpy;

namespace ManagedSpy
{
    public static class MainGui_ExtensionMethods
    {
        public static string TestFile1 { get; set; }

        static MainGui_ExtensionMethods()
        {
            TestFile1 = @"C:/_WorkDir/O2/O2 Install/_TempDir_v4.5.1.0/11_17_2012/Util - Simple Text Editor [18704]\Util - Simple Text Editor.exe";
        }
        public static MainGui enableVisualStudioObjectCreation(this MainGui mainGui)
        {
            var vsModules = (from frame in new StackTrace().GetFrames()
                             let module = frame.GetMethod().Module
                             where module.Name.contains("VisualStudio")
                             select module.Name).distinct();
            if (vsModules.notEmpty())
            {
                var callbackFromVs = (Action<Type>)"onMainGuiCtor".o2Cache();
                callbackFromVs.invoke(mainGui.type());
            }
            return mainGui;
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