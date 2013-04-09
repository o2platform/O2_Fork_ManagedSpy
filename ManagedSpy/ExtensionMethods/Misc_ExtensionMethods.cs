using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using O2.DotNetWrappers.ExtensionMethods;
using System.Windows.Forms;

namespace ManagedSpy
{
    // these extension methods should be moved into FluentSharp APIs
    public static class Misc_ExtensionMethods_Processes
    {
        public static bool doWeHaveAccess(this Process process)
        {
            try
            {
                var m = process.Modules;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool processHasModule(this Process process, string moduleName)
        {
            if (process.doWeHaveAccess().isFalse())
            {
                "A call was made to processHasModule for the process {0} which the current user doesn't have access to".error(process.ProcessName);
                return false;
            }
            
            foreach (ProcessModule module in process.Modules)
                if (module.ModuleName.fileName().contains(moduleName))
                    return true;        
            return false;
        }       

        public static Process startProcess_AsAdmin(this string pathToExe)
        {
             
            var process = new Process();
            process.StartInfo.FileName  = pathToExe;          
            process.StartInfo.Verb = "runas";
            process.Start();
            return process;
        }
    }

    public static class Misc_ExtensionMethods_Processes_WinForms
    {
        public static TreeView add_ProcessModules(this TreeView treeView, Process process)  
        {
            treeView.clear();
            if (process.doWeHaveAccess())
                treeView.add_Nodes(process.Modules.toList<ProcessModule>().Select((m)=>m.ModuleName))
                        .white();
            else
                treeView.pink().add_Node("No Access to Process modules");
            return treeView;
        }
    }
}
