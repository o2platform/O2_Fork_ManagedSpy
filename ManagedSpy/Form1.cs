using System;
using System.Windows.Forms;
using O2.DotNetWrappers.ExtensionMethods;

namespace ManagedSpy 
{    
    public partial class Form1 : Form
    {
        
        public Form1()
        {
            InitializeComponent();                    
        }

        public void Form1_Load(object sender, EventArgs e) 
        {
//            mainGui1.RefreshWindows();
        }
        public void Form1_FormClosing(object sender, FormClosingEventArgs e) 
        {
            mainGui1.StopLogging();
        }
    }
}