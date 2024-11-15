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

    public void FromVegas(Vegas myVegas){

        DialogResult result = ShowMainDialog();


    }   // FromVegas

    DialogResult ShowMainDialog(){
        
        int buttonWidth = 80;
        int buttonTop = 20;

        Form dlog = new Form();
        dlog.Text = "Setup Layers";
        dlog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        dlog.MaximizeBox = false;
        dlog.StartPosition = FormStartPosition.CenterScreen;
        dlog.FormClosing += this.HandleMainClosing;


        Button okButton = new Button();
        okButton.Text = "OK";
        okButton.Left = dlog.Width - (2*(buttonWidth+20));
        okButton.Top = buttonTop;
        okButton.Width = buttonWidth;
        okButton.Height = okButton.Font.Height + 12;
        okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        dlog.AcceptButton = okButton;
        dlog.Controls.Add(okButton);

        return dlog.ShowDialog(myVegas.MainWindow);
    }   // ShowMainDialog

    void HandleMainClosing(Object sender, EventArgs args){


    }   //  HandleMainClosing


}   // EntryPoint

