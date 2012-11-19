using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using O2.DotNetWrappers.ExtensionMethods;

namespace ManagedSpy
{
    public static class MainGui_ExtensionMethods
    {
        public static string TestFile1 { get; set; }

        static MainGui_ExtensionMethods()
        {
            TestFile1 = @"C:/_WorkDir/O2/O2 Install/_TempDir_v4.5.1.0/11_17_2012/Util - Simple Text Editor [18704]\Util - Simple Text Editor.exe";
        }
        public static MainGui add_ExtraMenuItems(this MainGui mainGui)
        {         

            mainGui.control<MenuStrip>(true)
    			    .add_MenuItem("Sample App")
				    .add_MenuItem("Open Simple TextEditor", ()=> TestFile1.startProcess());
            return mainGui;           
        }
    }
}
