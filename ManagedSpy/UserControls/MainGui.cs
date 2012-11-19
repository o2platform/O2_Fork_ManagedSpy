using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.ManagedSpy;
using System.Diagnostics;
using System.Threading;
using O2.Kernel;
using O2.DotNetWrappers.ExtensionMethods;

namespace ManagedSpy
{
        using O2.XRules.Database.Utils;
    using O2.DotNetWrappers.DotNet;

    /// <summary>
            /// This is the main window of ManagedSpy.
        /// Its a fairly simple Form containing a TreeView and TabControl.
        /// The TreeView contains processes and thier windows
        /// The TabControl contains properties and events.
        /// </summary>
        /// 
    public partial class MainGui : UserControl
    {
        public System.Windows.Forms.PropertyGrid propertyGrid;
        public System.Windows.Forms.ToolStripMenuItem ShowNative;
        public System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        public System.Windows.Forms.ToolStripButton tsButtonStartStop;
        public ClickToolStrip toolStrip1;
        public System.Windows.Forms.TabControl tabControl1;
        public System.Windows.Forms.DataGridView eventGrid;
        public System.Windows.Forms.TreeView treeWindow;
        public System.Windows.Forms.MenuStrip menuStrip1;

        //Other objects
        public ControlProxy currentProxy = null;
        public EventFilterDialog dialog = new EventFilterDialog();

        public MainGui()  
        {               
            //this.enableVisualStudioObjectCreation();            
            
            this.buildGui();
                                    
            this.add_ExtraMenuItems();
            this.add_EventsFilters();      
      
            //this.RefreshWindows();
        }

        public MainGui buildGui()
        {             
            this.createControls()
                .createMenu()
                .createToolStripMenu()
                .wireEvents();

            return this;
        }

        public MainGui createControls()
        { 
            this.width(800).height(500);
            var panel                    = this.add_Panel().insert_LogViewer();
            this.menuStrip1              = panel.insert_Above(25).add_Control<MenuStrip>();
            this.toolStrip1              = panel.insert_Above(25).add_Control<ClickToolStrip>();
            this.toolStripStatusLabel1   = panel.insert_Below(25).add_Control<StatusStrip>()
                                                        .add_Control<ToolStripStatusLabel>();    
            
            this.tabControl1             = panel.add_TabControl();
            this.treeWindow              = panel.insert_Left().add_TreeView();
                
            this.propertyGrid            = this.tabControl1.add_Tab("Properties").add_PropertyGrid();
            this.eventGrid               = this.tabControl1.add_Tab("Events").add_DataGridView()
                                                           .add_Columns("Event Name", "Event Args");
            return this;
        }

        public MainGui createMenu()
        {          
            //original ManagedSpy menu   
            var fileMenu = this.menuStrip1.add_MenuItem("File")
                                          .add_MenuItem("Exit",                ()=>  this.exitToolStripMenuItem_Click(null,null));
            var viewMenu = this.menuStrip1.add_MenuItem("View")
                                          .add_MenuItem("Filter Events",       () => this.filterEventsToolStripMenuItem_Click(null, null))
                                          .add_MenuItem("Show Window",         () => this.showWindowToolStripMenuItem_Click(null, null))
                                          .add_MenuItem("Refresh",             () => this.refreshToolStripMenuItem_Click(null, null));

            this.ShowNative = viewMenu    .add_MenuItem("Show Native Windows", () => { });
            this.ShowNative.CheckOnClick = true;

            var helpMenu = this.menuStrip1.add_MenuItem("Help")
                                          .add_MenuItem("About ManagedSpy",    () => this.aboutManagedSpyToolStripMenuItem_Click(null, null));;  
            
            //Extra menu items
            this.menuStrip1.add_MenuItem("Sample Apps")
                           .add_MenuItem("Open Simple TextEditor",             () => MainGui_ExtensionMethods.TestFile1.startProcess());

            this.menuStrip1.add_MenuItem("REPL")
                           .add_MenuItem("REPL MainGui",                       () => this.script_Me())
                           .add_MenuItem("Insert REPL in Gui",                 () => this.insert_Below_Script_Me(this.propertyGrid))
                           .add_MenuItem("Log Viewer",                         () => open.logViewer());

            return this;
        }

        public MainGui createToolStripMenu()
        {                         
                                        this.toolStrip1.add_Button("",  ManagedSpy_FormImages.tsbuttonFilterEvents_Image,  () => this.tsbuttonFilterEvents_Click(null,null) );
                                        this.toolStrip1.add_Button("",  ManagedSpy_FormImages.tsbuttonRefresh_Image,       () => this.tsbuttonRefresh_Click     (null,null) );   
            this.tsButtonStartStop    = this.toolStrip1.add_Button("",  ManagedSpy_FormImages.tsButtonStartStop_Image,     () => this.tsButtonStartStop_Click   (null,null) );   
                                        this.toolStrip1.add_Button("",  ManagedSpy_FormImages.tsButtonClear_Image,         () => this.tsButtonClear_Click     (null,null) );   

            this.tsButtonStartStop.CheckOnClick          = true;
            this.tsButtonStartStop.DisplayStyle          = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsButtonStartStop.ImageTransparentColor = System.Drawing.Color.Magenta;

            this.menuStrip1.splitContainer().splitterWidth(1).fixedPanel1();
            this.toolStrip1.splitContainer().splitterWidth(1).fixedPanel1();
            this.toolStripStatusLabel1.GetCurrentParent().splitContainer().splitterWidth(1).fixedPanel1();                
            return this;
        }

        public MainGui wireEvents()
        { 
            //treeView events
            this.treeWindow.BeforeExpand    += new System.Windows.Forms.TreeViewCancelEventHandler (this.treeWindow_BeforeExpand);
            this.treeWindow.AfterSelect     += new System.Windows.Forms.TreeViewEventHandler       (this.treeWindow_AfterSelect);
            return this;
        }


        public void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        
         
        public void refreshToolStripMenuItem_Click(object sender, EventArgs e) {
            RefreshWindows();
        }

        /// <summary>
        /// This rebuilds the window hierarchy
        /// </summary>
        
        public void RefreshWindows() 
        {
            
            "Getting TopLevelWindows".info();                       
            ControlProxy[] topWindows = Microsoft.ManagedSpy.ControlProxy.TopLevelWindows;
            "Got {0} TopLevelWindows".info(topWindows.size());
            ShowWindows(topWindows);                        
        }
        
        public void ShowWindows(ControlProxy[]  topWindows) 
        {
            this.treeWindow.BeginUpdate();
            this.treeWindow.Nodes.Clear();
                        
            if (topWindows != null && topWindows.Length > 0) 
            {
                foreach (ControlProxy cproxy in topWindows) 
                {
                    "cproxy: {0}".info(cproxy.OwningProcess);
             //       continue;
                    TreeNode procnode;

                    //only showing managed windows
                    if (this.ShowNative.Checked || cproxy.IsManaged) {
                        Process proc = cproxy.OwningProcess;
                        if (proc != null && proc.Id != Process.GetCurrentProcess().Id) {
                            procnode = treeWindow.Nodes[proc.Id.ToString()];
                            if (procnode == null) {
                                procnode = treeWindow.Nodes.Add(proc.Id.ToString(),
                                    proc.ProcessName +
                                    "  " + proc.MainWindowTitle + 
                                    " [" + proc.Id.ToString() + "]");
                                procnode.Tag = proc;
                            }
                            string name = String.IsNullOrEmpty(cproxy.GetComponentName()) ?
                                "<noname>" : cproxy.GetComponentName();
                            TreeNode node = procnode.Nodes.Add(cproxy.Handle.ToString(), 
                                name + 
                                "     [" +
                                cproxy.GetClassName() + 
                                "]");
                            node.Tag = cproxy;
                        }
                    }
                }
            }
            if (treeWindow.Nodes.Count == 0) {
                treeWindow.Nodes.Add("No managed processes running.");
                treeWindow.Nodes.Add("Select View->Refresh.");
            }
            this.treeWindow.EndUpdate();
        }

        /// <summary>
        /// Called when the user selects a control in the treeview
        /// </summary>
        public void treeWindow_AfterSelect(object sender, TreeViewEventArgs e) {
            this.propertyGrid.SelectedObject = this.treeWindow.SelectedNode.Tag;
            this.toolStripStatusLabel1.Text = treeWindow.SelectedNode.Text;
            StopLogging();
            //this.eventGrid.Rows.Clear();
            StartLogging();
        }

        /// <summary>
        /// This is called when the selected ControlProxy raises an event
        /// </summary>
        public void ProxyEventFired(object sender, ProxyEventArgs args) 
        {
            eventGrid.FirstDisplayedScrollingRowIndex = 
                this.eventGrid.Rows.Add(new object[] { args.eventDescriptor.Name, args.eventArgs.ToString() });
        }

        /// <summary>
        /// Used to build the treeview as the user expands nodes.
        /// We always stay one step ahead of the user to get the expand state set correctly.
        /// So, for instance, when we just show processes, we have already calculated all the top level windows.
        /// When the user expands a process -- we calculate the children of all top level windows
        /// And so on...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void treeWindow_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
            foreach (TreeNode child in e.Node.Nodes) {
                child.Nodes.Clear();
                ControlProxy proxy = child.Tag as ControlProxy;
                if (proxy != null) {
                    foreach (ControlProxy proxychild in proxy.Children) {
                        string name = String.IsNullOrEmpty(proxychild.GetComponentName()) ?
                            "<noname>" : proxychild.GetComponentName();
                        TreeNode node = child.Nodes.Add(proxychild.Handle.ToString(), name + "     [" +
                            proxychild.GetClassName() + "]");
                        node.Tag = proxychild;
                    }
                }
            }
        }

        public void flashWindow_Click(object sender, EventArgs e) {
            FlashCurrentWindow();
        }
        public void showWindowToolStripMenuItem_Click(object sender, EventArgs e) {
            FlashCurrentWindow();
        }
        /// <summary>
        /// This uses ControlPaint.DrawReversibleFrame to highlight the given window
        /// </summary>
        public void FlashCurrentWindow() {
            ControlProxy proxy = propertyGrid.SelectedObject as ControlProxy;
            if (proxy != null && proxy.IsManaged && proxy.GetValue("Location") != null) {

                IntPtr handle = proxy.Handle;
                Point topleft = (Point)proxy.GetValue("Location");
                if (proxy.Parent != null) {
                    topleft = (Point)proxy.Parent.PointToScreen(topleft);
                }
                Size size = (Size)proxy.GetValue("Size");
                Rectangle r = new Rectangle(topleft, size);

                for (int i = 1; i <= 7; i++) {
                    ControlPaint.DrawReversibleFrame(r, Color.Red, FrameStyle.Thick);
                    Thread.Sleep(100);
                }
                Thread.Sleep(250);  //extra delay at the end.
                ControlPaint.DrawReversibleFrame(r, Color.Red, FrameStyle.Thick);
            }
        }

        /// <summary>
        /// Starts event logging
        /// </summary>
        public void StartLogging() {
            if (tsButtonStartStop.Checked) 
            {
                currentProxy = propertyGrid.SelectedObject as ControlProxy;
                currentProxy.subscribeToEvents(dialog);                
                currentProxy.EventFired += new ControlProxyEventHandler(ProxyEventFired);                
            }
        }   

        /// <summary>
        /// Stops event Logging
        /// </summary>
        public void StopLogging() 
        {
               currentProxy.unsubscribeAllEvents()
                           .eventFired_Remove(new ControlProxyEventHandler(ProxyEventFired));
            
        }

        public void tsButtonStartStop_Click(object sender, EventArgs e) {
            StopLogging();
            StartLogging();
            if (tsButtonStartStop.Checked) {
                tsButtonStartStop.Image = ManagedSpy.Properties.Resources.Stop;
            }
            else {
                tsButtonStartStop.Image = ManagedSpy.Properties.Resources.Play;
            }
        }

        

        public void tsbuttonRefresh_Click(object sender, EventArgs e) {
            RefreshWindows();
        }

        public void tsButtonClear_Click(object sender, EventArgs e) 
        {
            this.eventGrid.Rows.Clear();
        }

        public void tsbuttonFilterEvents_Click(object sender, EventArgs e) {
            dialog.ShowDialog();
            StopLogging();
            StartLogging();
        }

        public void filterEventsToolStripMenuItem_Click(object sender, EventArgs e) {
            dialog.ShowDialog();
            StopLogging();
            StartLogging();
        }

        public void aboutManagedSpyToolStripMenuItem_Click(object sender, EventArgs e) {
            HelpAbout about = new HelpAbout();
            about.ShowDialog();
        }

        private void ShowNative_Click(object sender, EventArgs e)
        {

        }
    }

    
}
