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
using O2.Platform.BCL.O2_Views_ASCX;
using O2.DotNetWrappers.DotNet;
using O2.XRules.Database.APIs;

namespace ManagedSpy
{
    public partial class MainGui : UserControl
    {
        public PropertyGrid             propertyGrid            { get; set; }
        public TreeView                 ControlProxyList        { get; set; }
        //public TreeView                 ModulesList             { get; set; }
        public ToolStripMenuItem        ShowNative              { get; set; }
        public ToolStripStatusLabel     toolStripStatusLabel1   { get; set; }
        public ToolStripButton          tsButtonStartStop       { get; set; }
        public ToolStripButton          tsHookProcess           { get; set; }        
        public ClickToolStrip           toolStrip1              { get; set; }        
        public TabControl               tabControl1             { get; set; }
        public DataGridView             eventGrid               { get; set; }
        public TreeView                 treeWindow              { get; set; }
        public MenuStrip                menuStrip1              { get; set; }

        //Other objects
        public ControlProxy             Current_ControlProxy    { get; set; }
        public Process                  Current_Process         { get; set; }
        public EventFilterDialog        dialog                  { get; set; }

        public MainGui()  
        {         
            dialog = new EventFilterDialog();
            this.enableVisualStudioObjectCreation();                                                                                                                                                      
        }

        public MainGui buildGui() 
        {       
        	try
        	{
                this.insert_LogViewer();

	            this.createControls() 
	                .createMainMenu()
	                .createToolStripMenu()
                    .createProcessListContextMenu()
	                .wireEvents();

                this.add_EventsFilters();

                this.add_TargetProcesses();
			}
			catch(Exception ex)
			{
				ex.log();
			}
            return this;
        }

        public MainGui createControls()
        { 
            this.width(800).height(500);            
            var panel                    = this.add_Panel();                                               
            this.menuStrip1              = panel.insert_Above(25).add_Control<MenuStrip>();
            this.toolStrip1              = panel.insert_Above(25).add_Control<ClickToolStrip>();
            this.toolStripStatusLabel1   = panel.insert_Below(25).add_Control<StatusStrip>()
                                                        .add_Control<ToolStripStatusLabel>();    
            
            this.tabControl1             = panel.add_TabControl();
            this.treeWindow              = panel.insert_Left("Current Processes").add_TreeView().sort();
            this.ControlProxyList        = this.treeWindow.insert_Below("Windows/Controls in Selected Process").add_TreeView();
            this.propertyGrid            = this.tabControl1.add_Tab("Properties").add_PropertyGrid();
//            this.ModulesList             = this.propertyGrid.insert_Below("Selected Process Modules").add_TreeView().sort();
//            this.eventGrid               = this.tabControl1.add_Tab("Events").add_DataGridView()
             this.eventGrid               = this.propertyGrid.insert_Below("Events").add_DataGridView()
                                                             .add_Columns("Event Name", "Event Args");

            this.treeWindow.insert_Below(25).add_StatusStrip(); 
            return this;
        }

        public MainGui createMainMenu()
        {          
            //original ManagedSpy menu   
            var fileMenu = this.menuStrip1.add_MenuItem("File")       
                                          .add_MenuItem("Start as Admin ",     ()=> Application.ExecutablePath.str().startProcess_AsAdmin() )                   
                                          .add_MenuItem("Exit",                ()=>  Application.Exit());
            var viewMenu = this.menuStrip1.add_MenuItem("View")
                                          //.add_MenuItem("Filter Events",       () => this.filterEventsToolStripMenuItem_Click(null, null))
                                          .add_MenuItem("Show Window",         () => this.showWindowToolStripMenuItem_Click(null, null))
                                          .add_MenuItem("Refresh",             () => this.RefreshWindows());

            this.ShowNative = viewMenu    .add_MenuItem("Show Native Windows", () => { });
            this.ShowNative.CheckOnClick = true;

            var helpMenu = this.menuStrip1.add_MenuItem("Help")
                                          .add_MenuItem("About ManagedSpy",    () => this.aboutManagedSpyToolStripMenuItem_Click(null, null));;  
            
            //Extra menu items
            this.menuStrip1.add_MenuItem("Sample Apps")
                           .add_MenuItem("Open Simple TextEditor",             () => MainGui_ExtensionMethods.TestFile1.startProcess());

            this.menuStrip1.add_MenuItem("REPL")
                           .add_MenuItem("REPL CurrentProcess",                () => Current_Process.script_Me())
                           .add_MenuItem("REPL MainGui",                       () => this.script_Me())
                           .add_MenuItem("Insert MainGui REPL in Gui",         () => this.insert_Below_Script_Me(this))
                           .add_MenuItem("Log Viewer",                         () => open.logViewer());

            return this;
        }

        public MainGui createToolStripMenu()
        {                          

            this.tsHookProcess =        this.toolStrip1.add_Button("Hook Selected Process",         FormImages.face_glasses, this.HookSelectedProcess);

                                        //this.toolStrip1.add_Button("",  ManagedSpy_FormImages.tsbuttonFilterEvents_Image,  () => this.tsbuttonFilterEvents_Click(null,null) );
                                        this.toolStrip1.add_Button("Refresh Process list",          ManagedSpy_FormImages.tsbuttonRefresh_Image,       () => this.RefreshWindows() );   
            //this.tsButtonStartStop    = this.toolStrip1.add_Button("Start/Stop Event Monitoring",   ManagedSpy_FormImages.tsButtonStartStop_Image,     () => this.tsButtonStartStop_Click   (null,null) );   
                                        this.toolStrip1.add_Button("Clear Captured Events",         ManagedSpy_FormImages.tsButtonClear_Image,         () => this.tsButtonClear_Click     (null,null) );   

            //this.tsButtonStartStop.CheckOnClick          = true;
            //this.tsButtonStartStop.DisplayStyle          = ToolStripItemDisplayStyle.Image;
            //this.tsButtonStartStop.ImageTransparentColor = System.Drawing.Color.Magenta;

            this.menuStrip1.splitContainer().splitterWidth(1).fixedPanel1();
            this.toolStrip1.splitContainer().splitterWidth(1).fixedPanel1();
            this.toolStripStatusLabel1.GetCurrentParent().splitContainer().splitterWidth(1).fixedPanel1();                

            return this;
        }

        public MainGui createProcessListContextMenu()
        {
            this.treeWindow.add_ContextMenu()
                           .add_MenuItem("Main Window - Always on Top"      , true, ()=> Current_Process.MainWindowHandle.window_AlwaysOnTop())
                           .add_MenuItem("Start new Process "               , true, ()=> Current_Process.MainModule.FileName.startProcess() )
                           .add_MenuItem("Start new Process (as Admin)"     , true, ()=> Current_Process.MainModule.FileName.startProcess_AsAdmin() )                           
                           .add_MenuItem("Stop Process"                     , true, ()=> Current_Process.Kill())
                           ;
            return this;
        }


        
        public MainGui wireEvents()
        {                         
            this.treeWindow.afterSelect<Process>(
                    (process)=>{
                                    this.Current_Process = process;
                                    this.ControlProxyList.add_ControlProxies_for_Process(process);
                                    this.propertyGrid.show(process);
                                    //this.ModulesList.add_ProcessModules(process);                                    
                               });

            this.ControlProxyList.beforeExpand<ControlProxy>(
                    (treeNode, controlProxy)=> treeNode.addChildControlProxies(controlProxy));

            this.ControlProxyList.afterSelect<ControlProxy>(
                    (controlProxy)=>{
                                        Current_ControlProxy = controlProxy;
                                        this.propertyGrid.show(controlProxy);
                                        SetEventsListener(controlProxy);
                                    });

            return this;
        }

        public void HookSelectedProcess()
        { 
            var selectedNode = treeWindow.selected();
            if (selectedNode.notNull())
            {
                var process = selectedNode.tag<Process>();
                var controlHandle = process.get_ControlProxy_for_MainWindowHandle(); //will hook
                process.Refresh();
                selectedNode.showVisualClueWhenProcessIsHooked(process);
                ControlProxyList.add_ControlProxies_for_Process(process);
            }
        }

        /*public void StartLoggingForCurrentProcess()
        { 
            StopLogging();
            //this.eventGrid.Rows.Clear();
            StartLogging();
        }*/

        public void RefreshWindows() 
        {
            O2Thread.mtaThread(()=> this.add_TargetProcesses());            
        }
                

        /*
        public void treeWindow_AfterSelect(object sender, TreeViewEventArgs e) 
        {
            var targetObject = this.treeWindow.SelectedNode.Tag;
            
            if (targetObject is Process)
            {                
                var process = (Process)targetObject;
                tsHookProcess.Enabled = true;
                tsButtonStartStop.Enabled = false;
                this.ModulesList.clear().add_Nodes(process.Modules.toList<ProcessModule>().Select((m)=>m.ModuleName));
            }
            if (targetObject is ControlProxy)
            {                 
                tsButtonStartStop.Enabled = false;
                tsButtonStartStop.Enabled = true;
            }

            
            this.propertyGrid.SelectedObject = targetObject;

            this.toolStripStatusLabel1.Text = treeWindow.SelectedNode.Text;

            StopLogging();
            //this.eventGrid.Rows.Clear();
            StartLogging();
        }*/

        /// <summary>
        /// This is called when the selected ControlProxy raises an event
        /// </summary>
        public void ProxyEventFired(object sender, ProxyEventArgs args) 
        {
            try
            {
                eventGrid.FirstDisplayedScrollingRowIndex =
                    this.eventGrid.Rows.Add(new object[] { args.eventDescriptor.Name, args.eventArgs.ToString() });
            }
            catch (Exception ex)
            { 
                ex.log();
            }
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
        public void treeWindow_BeforeExpand(object sender, TreeViewCancelEventArgs e) 
        {            
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

        public MainGui SetEventsListener(ControlProxy controlProxy)
        { 
            controlProxy.unsubscribeAllEvents()
                        .eventFired_Remove(new ControlProxyEventHandler(ProxyEventFired));

            controlProxy.subscribeToEvents(dialog)
                        .eventFired_Add(new ControlProxyEventHandler(ProxyEventFired));
            return this;
        }

        /*
        /// <summary>
        /// Starts event logging
        /// </summary>
        public void StartLogging() {
            if (tsButtonStartStop.Checked) 
            {
                Current_ControlProxy = propertyGrid.SelectedObject as ControlProxy;
                Current_ControlProxy.subscribeToEvents(dialog);                
                Current_ControlProxy.EventFired += new ControlProxyEventHandler(ProxyEventFired);                
            }
        }   

        /// <summary>
        /// Stops event Logging
        /// </summary>
        public void StopLogging() 
        {
               currentProxy.unsubscribeAllEvents()
                           .eventFired_Remove(new ControlProxyEventHandler(ProxyEventFired));
            
        }*/

/*        public void tsButtonStartStop_Click(object sender, EventArgs e) 
        {
            StopLogging();
            StartLogging();
            if (tsButtonStartStop.Checked) 
            {
                tsButtonStartStop.Image = ManagedSpy.Properties.Resources.Stop;
            }
            else 
            {
                tsButtonStartStop.Image = ManagedSpy.Properties.Resources.Play;
            }
        }
*/
        

/*        public void tsbuttonRefresh_Click(object sender, EventArgs e) {
            RefreshWindows();
        }*/

        public void tsButtonClear_Click(object sender, EventArgs e) 
        {
            this.eventGrid.Rows.Clear();
        }

/*        public void tsbuttonFilterEvents_Click(object sender, EventArgs e) {
            dialog.ShowDialog();
            StopLogging();
            StartLogging();
        }

        public void filterEventsToolStripMenuItem_Click(object sender, EventArgs e) {
            dialog.ShowDialog();
            StopLogging();
            StartLogging();
        }
        */
        public void aboutManagedSpyToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            HelpAbout about = new HelpAbout();
            about.ShowDialog();
        }

        private void ShowNative_Click(object sender, EventArgs e)
        {

        }
    }

    
}
