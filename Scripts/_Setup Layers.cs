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
    string defaultBasePath = "";
    Button BrowseButton;

    public void FromVegas(Vegas vegas){

        myVegas = vegas;

        DialogResult result = ShowMainDialog();
        myVegas.UpdateUI();

    }   // FromVegas

    DialogResult ShowMainDialog(){
        
        int buttonWidth = 80;
        int buttonTop = 20;

        // SETUP FORM
        Form dlog = new Form();
        dlog.Text = "Setup Layers";
        dlog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        dlog.MaximizeBox = false;
        dlog.Width = 610;
        dlog.Height = 800;
        dlog.StartPosition = FormStartPosition.CenterScreen;
        dlog.FormClosing += this.HandleMainClosing;

        //  BROWSE FOR FILE
        FileNameBox = AddTextControl(dlog, "Image File Path", 6, 460, 10, defaultBasePath);

        BrowseButton = new Button();
        BrowseButton.Left = FileNameBox.Right + 4;
        BrowseButton.Top = FileNameBox.Top - 2;
        BrowseButton.Width = buttonWidth;
        BrowseButton.Height = BrowseButton.Font.Height + 12;
        BrowseButton.Text = "Browse...";
        BrowseButton.Click += new EventHandler(this.HandleBrowseClick);
        dlog.Controls.Add(BrowseButton);



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

        return dlog.ShowDialog(myVegas.MainWindow);
    }   // ShowMainDialog

    void HandleMainClosing(Object sender, EventArgs args){


    }   //  HandleMainClosing


    void HandleBrowseClick(Object sender, EventArgs args)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "All Files (*.*)|*.*";
        saveFileDialog.CheckPathExists = false;
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
    }   // AddRadioControl



}   // EntryPoint

