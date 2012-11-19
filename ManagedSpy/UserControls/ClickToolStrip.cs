using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ManagedSpy
{
    /// <summary>
    /// This is to ensure when you click on the toolstrip, our application doesn't have to be
    /// active for the click to register.
    /// </summary>
    public class ClickToolStrip : ToolStrip
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_ACTIVATE = 0x0001;

        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_MOUSEACTIVATE) {
                m.Result = (IntPtr)MA_ACTIVATE;
            }
            else {
                base.WndProc(ref m);
            }
        }
    }    
}
