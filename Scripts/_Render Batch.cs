/**
 * Sample script that performs batch renders with GUI for selecting
 * render templates.
 *
 * Revision Date: Jun. 28, 2006.
 **/
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;


using ScriptPortal.Vegas;

public class EntryPoint {

    // set this to true if you want to allow files to be overwritten
    bool OverwriteExistingFiles = false;

    String defaultBasePath = @"E:\_Render\RenderFile";
    const string shortRenderTemplateName = "YT Shorts (608x1080 60fps)";
    const int QUICKTIME_MAX_FILE_NAME_LENGTH = 55;
    int maxShortLength = 60;

    ScriptPortal.Vegas.Vegas myVegas = null;

    enum RenderMode
    {
        Project = 0,
        Selection,
        Regions,
    }

    ArrayList SelectedTemplates = new ArrayList();
    ArrayList SelectedShortsTemplates = new ArrayList();

    public void FromVegas(Vegas vegas)
    {
        myVegas = vegas;
        string foundPath = GetTargetDrive();

        // TODO: This needs to be updated so that we throw an error when there is no saved file
        String projectPath = myVegas.Project.FilePath;
        if (!string.IsNullOrEmpty(foundPath) || foundPath != ""){
            string projFileName = vegas.Project.FilePath;
            string projName = System.IO.Path.GetFileNameWithoutExtension(projFileName);
            defaultBasePath = Path.Combine(foundPath,projName);
        }
        else if (String.IsNullOrEmpty(projectPath))
        {
            String dir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            defaultBasePath = Path.Combine(dir, defaultBasePath);
        }
        else
        {
            String dir = Path.GetDirectoryName(projectPath);
            String fileName = Path.GetFileNameWithoutExtension(projectPath);
            defaultBasePath = Path.Combine(dir, fileName + "_");
        }

        DialogResult result = ShowBatchRenderDialog();
        myVegas.UpdateUI();
        if (DialogResult.OK == result)
        {
            // inform the user of some special failure cases
            String outputFilePath = FileNameBox.Text;
            RenderMode renderMode = RenderMode.Project;
            if (RenderRegionsButton.Checked)
            {
                renderMode = RenderMode.Regions;
            }
            else if (RenderSelectionButton.Checked)
            {
                renderMode = RenderMode.Selection;
            }
            DoBatchRender(SelectedTemplates, outputFilePath, renderMode);
        }
    }

    void DoBatchRender(ArrayList selectedTemplates, String basePath, RenderMode renderMode){
        
        String outputDirectory = Path.GetDirectoryName(basePath);
        String baseFileName = Path.GetFileName(basePath);

        // make sure templates are selected
        if ((null == selectedTemplates) || (0 == selectedTemplates.Count))
            throw new ApplicationException("No render templates selected.");

        // make sure the output directory exists
        if (!Directory.Exists(outputDirectory))
            throw new ApplicationException("The output directory does not exist.");

        List<RenderArgs> renders = new List<RenderArgs>();

        int currentRegionNum = 0;
        int totalRegionCount = myVegas.Project.Regions.Count.ToString().Length;

        // Iterate through Regions
        foreach (ScriptPortal.Vegas.Region region in myVegas.Project.Regions) {
            string filePrefix = FilePrefixNumber(currentRegionNum,totalRegionCount);            

            renders.AddRange(RenderRegions( region, selectedTemplates, basePath, filePrefix ) ); 


            currentRegionNum++;
        }



        
        // validate all files and prompt for overwrites
        foreach (RenderArgs args in renders) {
            ValidateFilePath(args.OutputFile);
            if (!OverwriteExistingFiles)
            {
                if (File.Exists(args.OutputFile)) {
                    String msg = "File(s) exists. Do you want to overwrite them?";
                    DialogResult rs;
                    rs = MessageBox.Show(msg,
                                         "Overwrite files?",
                                         MessageBoxButtons.OKCancel,
                                         MessageBoxIcon.Warning,
                                         MessageBoxDefaultButton.Button2);
                    if (DialogResult.Cancel == rs) {
                        return;
                    } else {
                        OverwriteExistingFiles = true;
                    }
                }
            }
        }
        
        // perform all renders.  The Render method returns a member of the RenderStatus enumeration.  If it is
        // anything other than OK, exit the loop.
        foreach (RenderArgs args in renders) {
            if (RenderStatus.Canceled == DoRender(args)) {
                break;
            }
        }

    }

    RenderStatus DoRender(RenderArgs args)
    {
        RenderStatus status = myVegas.Render(args);
        switch (status)
        {
            case RenderStatus.Complete:
            case RenderStatus.Canceled:
                break;
            case RenderStatus.Failed:
            default:
                StringBuilder msg = new StringBuilder("Render failed:\n");
                msg.Append("\n    file name: ");
                msg.Append(args.OutputFile);
                msg.Append("\n    Template: ");
                msg.Append(args.RenderTemplate.Name);
                throw new ApplicationException(msg.ToString());
        }
        return status;
    }

    String FixFileName(String name)
    {
        const Char replacementChar = '-';
        foreach (char badChar in Path.GetInvalidFileNameChars()) {
            name = name.Replace(badChar, replacementChar);
        }
        return name;
    }

    void ValidateFilePath(String filePath)
    {
        if (filePath.Length > 260)
            throw new ApplicationException("File name too long: " + filePath);
        foreach (char badChar in Path.GetInvalidPathChars()) {
            if (0 <= filePath.IndexOf(badChar)) {
                throw new ApplicationException("Invalid file name: " + filePath);
            }
        }
    }

    class RenderItem
    {
        public readonly Renderer Renderer = null;
        public readonly RenderTemplate Template = null;
        public readonly String Extension = null;
        
        public RenderItem(Renderer r, RenderTemplate t, String e)
        {
            this.Renderer = r;
            this.Template = t;
            // need to strip off the extension's leading "*"
            if (null != e) this.Extension = e.TrimStart('*');
        }
    }

    
    Label TemplatesLabel;
    Label ShortsLabel;
    Button BrowseButton;
    TextBox FileNameBox;
    TreeView TemplateTree;
    TreeView ShortsTree;
    RadioButton RenderProjectButton;
    RadioButton RenderRegionsButton;
    RadioButton RenderSelectionButton;
    CheckBox RenderCreateShortsCheckBox;
    CheckBox IncludeTemplateNameBox;
    TextBox ShortsMaxLength;
    CheckBox SerializeOutputCheckBox;

    DialogResult ShowBatchRenderDialog()
    {
        Form dlog = new Form();
        dlog.Text = "Batch Render";
        dlog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        dlog.MaximizeBox = false;
        dlog.StartPosition = FormStartPosition.CenterScreen;
        dlog.Width = 610;
        dlog.Height = 900;
        
        dlog.FormClosing += this.HandleFormClosing;

        int titleBarHeight = dlog.Height - dlog.ClientSize.Height;
        int buttonWidth = 80;
        int buttonTop = 0;

        FileNameBox = AddTextControl(dlog, "Base File Name", titleBarHeight + 6, 460, 10, defaultBasePath);

        BrowseButton = new Button();
        BrowseButton.Left = FileNameBox.Right + 4;
        BrowseButton.Top = FileNameBox.Top - 2;
        BrowseButton.Width = buttonWidth;
        BrowseButton.Height = BrowseButton.Font.Height + 12;
        BrowseButton.Text = "Browse...";
        BrowseButton.Click += new EventHandler(this.HandleBrowseClick);
        dlog.Controls.Add(BrowseButton);

        TemplatesLabel = AddLabelOnly(dlog,"All Templates",60,BrowseButton.Bottom + 10);
        ShortsLabel = AddLabelOnly(dlog,"Shorts Templates",60,BrowseButton.Bottom + 10);

        //  All Templates Tree
        int treeWidthDefault = (int)((dlog.Width - 45) / 2);
        TemplateTree = new TreeView();
        TemplateTree.Left = 10;
        TemplateTree.Width = treeWidthDefault; //(int)(dlog.Width * .46);
        TemplateTree.Top = ShortsLabel.Bottom + 10;
        TemplateTree.Height = 300;
        TemplateTree.CheckBoxes = true;
        TemplateTree.AfterCheck += new TreeViewEventHandler(this.HandleTreeViewCheck);
        dlog.Controls.Add(TemplateTree);
        TemplatesLabel.Left = TemplateTree.Left;

        // All Shorts Tree
        ShortsTree = new TreeView();
        ShortsTree.Left = TemplateTree.Right + 10;
        ShortsTree.Width = treeWidthDefault;
        ShortsTree.Top = ShortsLabel.Bottom + 10;
        ShortsTree.Height = 300;
        ShortsTree.CheckBoxes = true;
        ShortsTree.AfterCheck += new TreeViewEventHandler(this.HandleTreeViewCheck);
        dlog.Controls.Add(ShortsTree);
        ShortsLabel.Left = ShortsTree.Left;


        buttonTop = TemplateTree.Bottom + 16;
        int buttonsLeft = dlog.Width - (2*(buttonWidth+10));

        RenderCreateShortsCheckBox = AddCheckBox( dlog,
                                                "Render Shorts",
                                                6,
                                                buttonTop,
                                                true
                                                );

        ShortsMaxLength = AddTextControl(dlog,
                                        "Shorts Max Length",
                                        RenderCreateShortsCheckBox.Right + 10,
                                        150,
                                        RenderCreateShortsCheckBox.Top - 3,
                                        "60");

        SerializeOutputCheckBox = AddCheckBox(dlog,
                                                "Serialize Out Files",
                                                ShortsMaxLength.Right + 10,
                                                RenderCreateShortsCheckBox.Top - 3,
                                                true
                                                );

        buttonTop = RenderCreateShortsCheckBox.Bottom + 16;

        RenderProjectButton = AddRadioControl(  dlog,
                                                "Render Project",
                                                6,
                                                buttonTop,
                                                (0 != myVegas.SelectionLength.Nanos));
        RenderSelectionButton = AddRadioControl(dlog,
                                                "Render Selection",
                                                RenderProjectButton.Right,
                                                buttonTop,
                                                (0 != myVegas.SelectionLength.Nanos));
        RenderRegionsButton = AddRadioControl(  dlog,
                                                "Render Regions",
                                                RenderSelectionButton.Right,
                                                buttonTop,
                                                true);
        RenderRegionsButton.Checked = true;

            
        Button okButton = new Button();
        okButton.Text = "OK";
        okButton.Left = dlog.Width - (2*(buttonWidth+20));
        okButton.Top = buttonTop;
        okButton.Width = buttonWidth;
        okButton.Height = okButton.Font.Height + 12;
        okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        dlog.AcceptButton = okButton;
        dlog.Controls.Add(okButton);

        Button cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.Left = dlog.Width - (1*(buttonWidth+20));
        cancelButton.Top = buttonTop;
        cancelButton.Height = cancelButton.Font.Height + 12;
        cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        dlog.CancelButton = cancelButton;
        dlog.Controls.Add(cancelButton);

        dlog.Height = titleBarHeight + okButton.Bottom + 8;
        dlog.ShowInTaskbar = true;

        FillTemplateTree();
        
        FillTemplateTree(true);

        return dlog.ShowDialog(myVegas.MainWindow);
    }

    Label AddLabelOnly(Form dlog, String labelName, int left, int top){
        
        Label label = new Label();
        label.AutoSize = true;
        label.Text = labelName + ":";
        label.Left = left;
        label.Top = top + 4;
        dlog.Controls.Add(label);
        return label;
    }

    TextBox AddTextControl(Form dlog, String labelName, int left, int width, int top, String defaultValue)
    {
        Label label = new Label();
        label.AutoSize = true;
        label.Text = labelName + ":";
        label.Left = left;
        label.Top = top + 4;
        dlog.Controls.Add(label);

        TextBox textbox = new TextBox();
        textbox.Multiline = false;
        textbox.Left = label.Right;
        textbox.Top = top;
        textbox.Width = width - (label.Width);
        textbox.Text = defaultValue;
        dlog.Controls.Add(textbox);

        return textbox;
    }

    CheckBox AddCheckBox(Form dlog,String labelName, int left, int top, bool isChecked = false, bool isEnabled = true){
        Label label = new Label();
        label.AutoSize = true;
        label.Text = labelName;
        label.BackColor = Color.Transparent;
        label.Left = left;
        label.Top = top + 4;
        label.Enabled = isEnabled;
        dlog.Controls.Add(label);
        
        CheckBox checkBox = new CheckBox();
        checkBox.Text = "   "; 
        checkBox.Checked = isChecked;
        checkBox.Enabled = isEnabled;
        checkBox.IsAccessible = isEnabled;
        checkBox.AutoSize = true;
        checkBox.FlatStyle = FlatStyle.System;
        checkBox.Left = label.Right + 20;
        checkBox.Top = label.Top;
        dlog.Controls.Add(checkBox);

        return checkBox;
    }
    RadioButton AddRadioControl(Form dlog, String labelName, int left, int top, bool enabled)
    {
        Label label = new Label();
        label.AutoSize = true;
        label.Text = labelName;
        label.Left = left;
        label.Top = top + 4;
        label.Enabled = enabled;
        dlog.Controls.Add(label);

        RadioButton radiobutton = new RadioButton();
        radiobutton.Left = label.Right;
        radiobutton.Width = 36;
        radiobutton.Top = top;
        radiobutton.Enabled = enabled;
        dlog.Controls.Add(radiobutton);

        return radiobutton;
    }

    static Guid[] TheDefaultTemplateRenderClasses =
        {
            Renderer.CLSID_SfWaveRenderClass,
            Renderer.CLSID_SfW64ReaderClass,
            Renderer.CLSID_CSfAIFRenderFileClass,
            Renderer.CLSID_CSfFLACRenderFileClass,
            Renderer.CLSID_CSfPCARenderFileClass,
        };

    bool AllowDefaultTemplates(Guid rendererID)
    {
        foreach (Guid guid in TheDefaultTemplateRenderClasses)
        {
            if (guid == rendererID)
                return true;
        }
        return false;
    }

    void FillTemplateTree(bool onlyShorts = false)
    {
        int projectAudioChannelCount = 0;
        if (AudioBusMode.Stereo == myVegas.Project.Audio.MasterBusMode) {
            projectAudioChannelCount = 2;
        } else if (AudioBusMode.Surround == myVegas.Project.Audio.MasterBusMode) {
            projectAudioChannelCount = 6;
        }
        bool projectHasVideo = ProjectHasVideo();
        bool projectHasAudio = ProjectHasAudio();
        int  projectVideoStreams = !projectHasVideo ? 0 :
                (Stereo3DOutputMode.Off != myVegas.Project.Video.Stereo3DMode ? 2 : 1);

        foreach (Renderer renderer in myVegas.Renderers) {
            try {
                String rendererName = renderer.FileTypeName;
                TreeNode rendererNode = new TreeNode(rendererName);
                rendererNode.Tag = new RenderItem(renderer, null, null);
                foreach (RenderTemplate template in renderer.Templates) {
                    try {
                        // filter out invalid templates
                        if (!template.IsValid()) {
                            continue;
                        }
                        // filter out video templates when project has
                        // no video.
                        if (!projectHasVideo && (0 < template.VideoStreamCount)) {
                            continue;
                        }
                        // filter out templates that are 3d when the project is just 2d
                        if (projectHasVideo && projectVideoStreams < template.VideoStreamCount) {
                            continue;
                        }

                        // filter the default template (template 0) and we don't allow defaults
                        //   for this renderer
                        if (template.TemplateID == 0 && !AllowDefaultTemplates(renderer.ClassID)) {
                            continue;
                        }

                        // filter out audio-only templates when project has no audio
                        if (!projectHasAudio && (0 == template.VideoStreamCount) && (0 < template.AudioStreamCount)) {
                            continue;
                        }
                        // filter out templates that have more channels than the project
                        if (projectAudioChannelCount < template.AudioChannelCount) {
                            continue;
                        }
                        // filter out templates that don't have
                        // exactly one file extension
                        String[] extensions = template.FileExtensions;
                        if (1 != extensions.Length) {
                            continue;
                        }
                        
                        if (onlyShorts && !template.Name.ToLower().Contains("short")){continue;}

                        String templateName = template.Name;
                        TreeNode templateNode = new TreeNode(templateName);
                        templateNode.Tag = new RenderItem(renderer, template, extensions[0]);
         
                        rendererNode.Nodes.Add(templateNode);
                    } catch (Exception e) {
                        // skip it
                        MessageBox.Show(e.ToString());
                    }
                }
                if (0 == rendererNode.Nodes.Count) {
                    continue;
                } else if (1 == rendererNode.Nodes.Count) {
                    // skip it if the only template is the project
                    // settings template.
                    if (0 == ((RenderItem) rendererNode.Nodes[0].Tag).Template.Index) {
                        continue;
                    }
                } else {
                    if (onlyShorts){
                        ShortsTree.Nodes.Add(rendererNode);
                    }
                    else{
                        TemplateTree.Nodes.Add(rendererNode);
                    }
                }
            } catch {
                // skip it
            }
        }
    }

    bool ProjectHasVideo() {
        foreach (Track track in myVegas.Project.Tracks) {
            if (track.IsVideo()) {
                return true;
            }
        }
        return false;
    }

    bool ProjectHasAudio() {
        foreach (Track track in myVegas.Project.Tracks) {
            if (track.IsAudio()) {
                return true;
            }
        }
        return false;
    }
    
    void UpdateSelectedTemplates()
    {
        SelectedTemplates.Clear();
        SelectedShortsTemplates.Clear();

        // Standard Templates
        foreach (TreeNode node in TemplateTree.Nodes) {
            foreach (TreeNode templateNode in node.Nodes) {
                if (templateNode.Checked) {
                    SelectedTemplates.Add(templateNode.Tag);
                }
            }
        }

        // Shorts
        foreach (TreeNode node in ShortsTree.Nodes) {
            foreach (TreeNode templateNode in node.Nodes) {
                if (templateNode.Checked) {
                    SelectedShortsTemplates.Add(templateNode.Tag);
                }
            }
        }

    }

    void HandleBrowseClick(Object sender, EventArgs args)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "All Files (*.*)|*.*";
        saveFileDialog.CheckPathExists = true;
        saveFileDialog.AddExtension = false;
        if (null != FileNameBox) {
            String filename = FileNameBox.Text;
            String initialDir = Path.GetDirectoryName(filename);
            if (Directory.Exists(initialDir)) {
                saveFileDialog.InitialDirectory = initialDir;
            }
            saveFileDialog.DefaultExt = Path.GetExtension(filename);
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(filename);
        }
        if (System.Windows.Forms.DialogResult.OK == saveFileDialog.ShowDialog()) {
            if (null != FileNameBox) {
                FileNameBox.Text = Path.GetFullPath(saveFileDialog.FileName);
            }
        }
    }

    void HandleTreeViewCheck(object sender, TreeViewEventArgs args)
    {
        if (args.Node.Checked) {
            if (0 != args.Node.Nodes.Count) {
                if ((args.Action == TreeViewAction.ByKeyboard) || (args.Action == TreeViewAction.ByMouse)) {
                    SetChildrenChecked(args.Node, true);
                }
            } else if (!args.Node.Parent.Checked) {
                args.Node.Parent.Checked = true;
            }
        } else {
            if (0 != args.Node.Nodes.Count) {
                if ((args.Action == TreeViewAction.ByKeyboard) || (args.Action == TreeViewAction.ByMouse)) {
                    SetChildrenChecked(args.Node, false);
                }
            } else if (args.Node.Parent.Checked) {
                if (!AnyChildrenChecked(args.Node.Parent)) {
                    args.Node.Parent.Checked = false;
                }
            }
        }
    }

    void HandleFormClosing(Object sender, FormClosingEventArgs args)
    {
        Form dlg = sender as Form;
        if (null == dlg) return;
        if (DialogResult.OK != dlg.DialogResult) return;
        String outputFilePath = FileNameBox.Text;

    /*
        // TODO: Validation of CheckBox
        StringBuilder tmpMsg = new StringBuilder();
        tmpMsg.Append(RenderCreateShortsCheckBox.Checked? "Box is Checked":"Box is NOT Checked");
        MessageBox.Show(dlg,tmpMsg.ToString(),"Test Message",MessageBoxButtons.OK, MessageBoxIcon.Information);
        args.Cancel = true;
        return;
    */

        bool parsedShortLength = Int32.TryParse( ShortsMaxLength.Text.Trim(), out maxShortLength);
        if (!parsedShortLength){
            String title = "Short Length must be an Integer";
            StringBuilder msg = new StringBuilder();
            msg.Append("The shorts length was not an integer.\n");
            msg.Append("Please remove all non-numeric characters.");
            MessageBox.Show(dlg, msg.ToString(), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            args.Cancel = true;
            return;
        }

        try {
            String outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputDirectory)) throw new ApplicationException();
        } catch {
            String title = "Invalid Directory";
            StringBuilder msg = new StringBuilder();
            msg.Append("The output directory does not exist.\n");
            msg.Append("Please specify the directory and base file name using the Browse button.");
            MessageBox.Show(dlg, msg.ToString(), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            args.Cancel = true;
            return;
        }
        try {
            String baseFileName = Path.GetFileName(outputFilePath);
            if (String.IsNullOrEmpty(baseFileName)) throw new ApplicationException();
            if (-1 != baseFileName.IndexOfAny(Path.GetInvalidFileNameChars())) throw new ApplicationException();
        } catch {
            String title = "Invalid Base File Name";
            StringBuilder msg = new StringBuilder();
            msg.Append("The base file name is not a valid file name.\n");
            msg.Append("Make sure it contains one or more valid file name characters.");
            MessageBox.Show(dlg, msg.ToString(), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            args.Cancel = true;
            return;
        }
        UpdateSelectedTemplates();
        if (0 == SelectedTemplates.Count)
        {
            String title = "No Templates Selected";
            StringBuilder msg = new StringBuilder();
            msg.Append("No render templates selected.\n");
            msg.Append("Select one or more render templates from the available formats.");
            MessageBox.Show(dlg, msg.ToString(), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            args.Cancel = true;
            return;
        }
    }
    
    void SetChildrenChecked(TreeNode node, bool checkIt)
    {
        foreach (TreeNode childNode in node.Nodes) {
            if (childNode.Checked != checkIt)
                childNode.Checked = checkIt;
        }
    }

    bool AnyChildrenChecked(TreeNode node)
    {
        foreach (TreeNode childNode in node.Nodes) {
            if (childNode.Checked) return true;
        }
        return false;
    }
    

    string GetTargetDrive() {
        string foundPath = "";  
        DriveInfo[] drives = DriveInfo.GetDrives();

        foreach (DriveInfo drive in drives) {
            string checkDir = Path.Combine(drive.Name, "_Render");  
            Console.WriteLine(checkDir);

            if (Directory.Exists(checkDir)) {
                foundPath = checkDir;
                break;
            }
        }

        return foundPath + "\\";  
    }

    /// <summary>
    /// Search for a Template using the exact name
    /// </summary>
    /// <param name="templateName">The template name as a string</param>
    /// <returns></returns>
    /// <exception cref="Exception">IF template is not found, throws error</exception>
    RenderTemplate GetTemplateByName(string templateName){
        if (string.IsNullOrEmpty(templateName) || templateName.Trim() == "") 
        {
            throw new Exception("Template Name Cannot be Empty");
        }

        foreach (Renderer renderer in myVegas.Renderers)
        {   
            foreach (RenderTemplate renderTemplate in renderer.Templates)
            {
                if (renderTemplate.Name.ToLower().Contains(templateName.ToLower()))
                {
                    return renderTemplate;
                }
            }
        }
        string errorMsg = "Failed to find Render Template: " + templateName;
        throw new Exception(errorMsg);

    }


    /// <summary>
    /// Checks if the region meets the criteria for a Short
    /// </summary>
    /// <param name="region">The Region</param>
    /// <returns>true - if criteria met</returns>

    bool IsShortCheck(ScriptPortal.Vegas.Region region){
        double clipLength = region.Length.ToMilliseconds() / 1000;
        if (clipLength < maxShortLength){ return true;}
            
        return false;

    }

    /// <summary>
    /// Checks if the region meets the criteria for a Short
    /// </summary>
    /// <param name="region">The Region</param>
    /// <param name="lengthInSeconds">The max allowed length in seconds</param>
    /// <returns>true - if criteria is met</returns>
    bool IsShortCheck(ScriptPortal.Vegas.Region region, int lengthInSeconds = 60){
        double clipLength = region.Length.ToMilliseconds() / 1000;
        if (clipLength < lengthInSeconds){ return true;}
            
        return false;

    }
    
    /// <summary>
    /// Creates a prefix to the file
    /// </summary>
    /// <param name="fileNumber">The file number</param>
    /// <param name="stringLength">Determines the length of the string</param>
    /// <returns>The prefix that should be used for a serialized file</returns>
    string FilePrefixNumber(int fileNumber,int stringLength){
        if (stringLength < 2){ return fileNumber.ToString();}
        string prefixName = new string('0',stringLength);
        string outString = prefixName + fileNumber.ToString();
        return outString.Substring(outString.Length - stringLength);
    }

    List<RenderArgs> RenderRegions( ScriptPortal.Vegas.Region region, 
                                    ArrayList selectedTemplates, 
                                    String basePath, string prefixName = "") {

        List<RenderArgs> renders = new List<RenderArgs>();

        String outputDirectory = Path.GetDirectoryName(basePath);
        String baseFileName = Path.GetFileName(basePath);

        // Iterate Through Selected Templates
        foreach (RenderItem renderItem in selectedTemplates){


            String templateNameAppended = "";
            if(selectedTemplates.Count > 1){
                templateNameAppended = " - " + FixFileName(renderItem.Template.Name) ; 

            } 
            string prefixText = (prefixName != "" && prefixName.Length > 0)?prefixName + " - ": "";
            String regionFilename = Path.Combine(outputDirectory,
                                                    prefixText + 
                                                    FixFileName(region.Label) + 
                                                    templateNameAppended + 
                                                    " (" + FixFileName(baseFileName) + ")" +
                                                    renderItem.Extension
            );

            RenderArgs args = new RenderArgs();
            args.OutputFile = regionFilename;


            // RENDER SHORTS
            if ( SelectedShortsTemplates.Count > 0
                    && region.Label.ToLower().Contains("#short") 
                    && IsShortCheck(region)
                )
            {
                foreach (RenderItem renderShortItem in SelectedShortsTemplates){
                    args.RenderTemplate = renderShortItem.Template;
                    break;
                }

                args.Start = region.Position;
                args.Length = region.Length;
                renders.Add(args);
                break;

            }
            // RENDER STANDARD
            else {
                args.RenderTemplate = renderItem.Template;
                args.OutputFile = Regex.Replace(args.OutputFile,"#short","(Adjusted Output)",RegexOptions.IgnoreCase);
                
                args.Start = region.Position;
                args.Length = region.Length;
                renders.Add(args);
            }


        }


        return renders;

    }


    List<RenderArgs> RenderRegions( RenderItem renderItem, 
                                    ArrayList selectedTemplates, 
                                    String basePath) 
    {
        
        List<RenderArgs> renders = new List<RenderArgs>();

        String outputDirectory = Path.GetDirectoryName(basePath);
        String baseFileName = Path.GetFileName(basePath);

        int regionIndex = 0;
        
        int leadingZeros = myVegas.Project.Regions.Count.ToString().Length;
        foreach (ScriptPortal.Vegas.Region region in myVegas.Project.Regions) {
            string propList = "";
            foreach(var prop in region.GetType().GetProperties()) {
                propList+= prop.Name + ", ";
                
            }

            string prefixName = (SerializeOutputCheckBox.Checked)? FilePrefixNumber(regionIndex, leadingZeros) + " - ": "";

            String templateNameAppended = "";
            if(selectedTemplates.Count > 1){
                templateNameAppended = " - " + FixFileName(renderItem.Template.Name) ; 

            } 
            String regionFilename = Path.Combine(outputDirectory,
                                                    prefixName + 
                                                    FixFileName(region.Label) + 
                                                    templateNameAppended + 
                                                    " (" + FixFileName(baseFileName) + ")" +
                                                    renderItem.Extension
            );

            RenderArgs args = new RenderArgs();
            args.OutputFile = regionFilename;

            

            // RENDER SHORTS
            if (RenderCreateShortsCheckBox.Checked 
                    && region.Label.ToLower().Contains("#short") 
                    && IsShortCheck(region)
                )
            {
                

                foreach (RenderItem renderShortItem in SelectedShortsTemplates){
                    args.RenderTemplate = renderShortItem.Template;
                    break;
                }


            }
            else {
                args.RenderTemplate = renderItem.Template;
                args.OutputFile = Regex.Replace(args.OutputFile,"#short","(Adjusted Output)",RegexOptions.IgnoreCase);
            }

            args.Start = region.Position;
            args.Length = region.Length;
            renders.Add(args);
            regionIndex++;
        }

        return renders;
    }

}

