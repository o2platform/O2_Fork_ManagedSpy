using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using O2.DotNetWrappers.ExtensionMethods;

namespace ManagedSpy
{
    public static class Form1_ExtensionMethods
    {
        public static string TestFile1 { get; set; }

        static Form1_ExtensionMethods()
        {
            TestFile1 = @"C:/_WorkDir/O2/O2 Install/_TempDir_v4.5.1.0/11_17_2012/Util - Simple Text Editor [18704]\Util - Simple Text Editor.exe";
        }
        public static Form1 add_ExtraMenuItems(this Form1 form1)
        {         

            form1.control<MenuStrip>(true)
    			 .add_MenuItem("Sample App")
				 .add_MenuItem("Open Simple TextEditor", ()=> TestFile1.startProcess());
            return form1;           
        }
    }
}
