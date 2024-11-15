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
        Form dlog = new Form();
        dlog.Text = "Setup Layers";
        dlog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        dlog.MaximizeBox = false;
        dlog.StartPosition = FormStartPosition.CenterScreen;



    }   // ShowMainDialog

    void HandleMainClosing(Object sender, EventArgs args){


    }   //  HandleMainClosing


}   // EntryPoint

