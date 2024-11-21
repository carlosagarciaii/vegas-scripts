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


    ScriptPortal.Vegas.Vegas myVegas = null;

    TextBox FileNameBox;
    string defaultBasePath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    Button BrowseButton;
    TextBox TrackNameBox;
    
    RadioButton AddFadeNoneOption;
    RadioButton AddFadeCurveOption;
    RadioButton AddFadeSharpOption;
    TextBox FadeDurationTime;

    public void FromVegas(Vegas vegas){

        myVegas = vegas;

        DialogResult result = ShowMainDialog();
        myVegas.UpdateUI();

    }   // FromVegas

    DialogResult ShowMainDialog(){
        
        int buttonWidth = 80;
        int buttonTop = 10;

        // SETUP FORM
        Form dlog = new Form();
        dlog.Text = "Setup Layers";
        dlog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        dlog.MaximizeBox = false;
        dlog.Width = 610;
        dlog.Height = 700;
        dlog.StartPosition = FormStartPosition.CenterScreen;
        dlog.FormClosing += this.HandleMainClosing;

        //  BROWSE FOR FILE
        FileNameBox = AddTextControl(dlog, "Image File Path", 6, 460, buttonTop, defaultBasePath);

        BrowseButton = new Button();
        BrowseButton.Left = FileNameBox.Right + 4;
        BrowseButton.Top = FileNameBox.Top - 2;
        BrowseButton.Width = buttonWidth;
        BrowseButton.Height = BrowseButton.Font.Height + 12;
        BrowseButton.Text = "Open...";
        BrowseButton.Click += new EventHandler(this.HandleOpenFileClick);
        dlog.Controls.Add(BrowseButton);

        buttonTop = FileNameBox.Bottom + 10;

        // Track Name
        TrackNameBox = AddTextControl(dlog,"Track Name", 6, 460, buttonTop, "");


        buttonTop = TrackNameBox.Bottom + 10;

        //  Fade Options
        Label fadeOptionsLabel = AddLabelOnly(dlog,"Fade Options",8,buttonTop);
        AddFadeNoneOption = AddRadioControl(dlog,"None",fadeOptionsLabel.Right + 5,buttonTop,true);
        AddFadeCurveOption = AddRadioControl(dlog,"Curved",AddFadeNoneOption.Right ,buttonTop,false);
        AddFadeSharpOption = AddRadioControl(dlog,"Sharp",AddFadeCurveOption.Right ,buttonTop,false);
        
        buttonTop = AddFadeNoneOption.Bottom + 10;

        // Fade Time

        FadeDurationTime = AddTextControl(dlog,"Fade Duration",9,460, buttonTop, "00:00:00:15");

        buttonTop = FadeDurationTime.Bottom + 10;

        // BUTTONS
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


        buttonTop = cancelButton.Bottom + 10;
        dlog.Height = buttonTop + 50;

        return dlog.ShowDialog(myVegas.MainWindow);
    }   // ShowMainDialog

    void HandleMainClosing(Object sender, EventArgs args){
        Form dlg = sender as Form;
        if (null == dlg) return;
        if (DialogResult.OK != dlg.DialogResult) return;

        string mediaFilePath = FileNameBox.Text;
        string fadeDurationInput = FadeDurationTime.Text.Trim();

        // Validation Checks
        if (!File.Exists(mediaFilePath)){
            DisplayErrorMsg(dlg,"File Not Found","Could not find the file", mediaFilePath);
            return;
        }

        Media media = new Media(mediaFilePath);
        if (media == null)
        {
            DisplayErrorMsg(dlg,"Failed to load Media File","Failed to load Media File:", mediaFilePath);
            return;
        }

        if (myVegas.Project.Regions.Count == 0){
            DisplayErrorMsg(dlg,"No Regions Found","No Regions were set in the project");
            return;
        }

        var timeFormatRegex = @"^(\d\d:){3}\d\d$";
        var match = Regex.Match(fadeDurationInput,timeFormatRegex);
        if (!AddFadeNoneOption.Checked && !match.Success){
            DisplayErrorMsg(dlg,"Fade Duration Error","An imporoper format was used for the fade duration.","Formatting should be: \t##:##:##:##","","Example:\t00:00:01:00");
            return;
        }


        // Start Work
        VideoTrack newTrack = new VideoTrack(0,TrackNameBox.Text);
        myVegas.Project.Tracks.Add(newTrack);
        
        foreach (ScriptPortal.Vegas.Region region in myVegas.Project.Regions) {
            
            Timecode clipStart = region.Position;
            Timecode clipLength = region.Length;

            VideoEvent clipEvent = new VideoEvent(clipStart,clipLength);
            newTrack.Events.Add(clipEvent);

            Take take = new Take(media.GetVideoStreamByIndex(0));
            clipEvent.Takes.Add(take);

            if (!AddFadeNoneOption.Checked){
                Timecode fadeDuration = new Timecode(fadeDurationInput);
                if (!AddFadeNoneOption.Checked)
                clipEvent.FadeIn.Length = fadeDuration;
                clipEvent.FadeOut.Length = fadeDuration;
                if(AddFadeCurveOption.Checked){
                    clipEvent.FadeIn.Curve = CurveType.Smooth;
                }
                else if (AddFadeSharpOption.Checked){
                    clipEvent.FadeOut.Curve = CurveType.Sharp;
                }
                
            }
        }


    }   //  HandleMainClosing


    void HandleOpenFileClick(Object sender, EventArgs args)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Image Files |*.png;*.jpg;*.jpeg;*.bmp;*.tif; *.tiff|Video Files | *.mpg;*.mp2;*.mp4;*.mov;*.avc;*.m2v;*.m2t;*.mpa;*.m2ts;*.mxf;*.mxf;*.wmv;*.avi";
        openFileDialog.CheckPathExists = true;
        openFileDialog.AddExtension = false;
        if (null != FileNameBox) {
            String filename = FileNameBox.Text;
            String initialDir = Path.GetDirectoryName(filename);

            if (Directory.Exists(filename)){ 
                initialDir = filename;
                filename = "";
            }
            if (Directory.Exists(initialDir)) {
                openFileDialog.InitialDirectory = initialDir;
            }
            openFileDialog.DefaultExt = Path.GetExtension(filename);
            openFileDialog.FileName = Path.GetFileNameWithoutExtension(filename);
        }
        if (System.Windows.Forms.DialogResult.OK == openFileDialog.ShowDialog()) {
            if (null != FileNameBox) {
                FileNameBox.Text = Path.GetFullPath(openFileDialog.FileName);
            }
        }
    }



    Label AddLabelOnly(Form dlog, String labelName, int left, int top){
        
        Label label = new Label();
        label.AutoSize = true;
        label.Text = labelName + ":";
        label.Left = left;
        label.Top = top + 4;
        dlog.Controls.Add(label);
        return label;
    }   // AddLabelOnly

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
    }   // AddTextControl

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
    }   // AddCheckBox

    RadioButton AddRadioControl(Form dlog, String labelName, int left, int top, bool isChecked = false, bool enabled = true)
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
        radiobutton.Checked = isChecked;
        dlog.Controls.Add(radiobutton);

        return radiobutton;
    }   // AddRadioControl

    RadioButton AddRadioControl(GroupBox gbox, String labelName, int left, int top, bool enabled)
    {
        Label label = new Label();
        label.AutoSize = true;
        label.Text = labelName;
        label.Left = left;
        label.Top = top + 4;
        label.Enabled = enabled;
        gbox.Controls.Add(label);

        RadioButton radiobutton = new RadioButton();
        radiobutton.Left = label.Right;
        radiobutton.Width = 36;
        radiobutton.Top = top;
        radiobutton.Enabled = enabled;
        gbox.Controls.Add(radiobutton);
        return radiobutton;
    }   // AddRadioControl


    void DisplayErrorMsg(Form dlog, string msgBoxTitle, params string[] errorMsgLines ){
            String title = msgBoxTitle;
            StringBuilder msg = new StringBuilder();
            foreach(string msgLine in errorMsgLines){
                msg.Append(msgLine + "\n");
            }
            MessageBox.Show(dlog, msg.ToString(), msgBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);

    }


}   // EntryPoint

