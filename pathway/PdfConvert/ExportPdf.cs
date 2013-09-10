﻿// --------------------------------------------------------------------------------------------
// <copyright file="ExportPdf.cs" from='2009' to='2009' company='SIL International'>
//      Copyright © 2009, SIL International. All Rights Reserved.   
//    
//      Distributable under the terms of either the Common Public License or the
//      GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed: 
// 
// <remarks>
// Stylepick FeatureSheet
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using SIL.Tool;


namespace SIL.PublishingSolution
{
    public class ExportPdf : IExportProcess
    {
        private static string _fullPrincePath;
        private static string _processedXhtml;

        #region Properties
        #region ExportType
        public string ExportType
        {
            get
            {
                return "Pdf (Using Prince)";
            }
        }
        #endregion ExportType

        #region Handle
        public bool Handle(string inputDataType)
        {
            bool returnValue = false;
            if (RegPrinceKey != null)
            {
                if (inputDataType.ToLower() == "dictionary" || inputDataType.ToLower() == "scripture")
                {
                    return true;
                }
            }
            else if (RegPrinceKey == null && Common.UnixVersionCheck())
            {
                if (Directory.Exists("/usr/lib/prince/bin"))
                {
                    return true;
                }
            }

            return returnValue;
        }
        #endregion Handle

        #region RegPrinceKey
        public static RegistryKey RegPrinceKey
        {
            get
            {
                RegistryKey regPrinceKey;
                try
                {
                    regPrinceKey =
                        Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MICROSOFT\WINDOWS\CURRENTVERSION\UNINSTALL\Prince_is1");
                    if (regPrinceKey == null)
                        regPrinceKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MICROSOFT\WINDOWS\CURRENTVERSION\UNINSTALL\{3AC28E9C-8F06-4E2C-ADDA-726E2230A03A}");
                    if (regPrinceKey == null)
                        regPrinceKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\MICROSOFT\WINDOWS\CURRENTVERSION\UNINSTALL\Prince_is1");
                    if (regPrinceKey == null)
                        regPrinceKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\MICROSOFT\WINDOWS\CURRENTVERSION\UNINSTALL\{3AC28E9C-8F06-4E2C-ADDA-726E2230A03A}");

                }
                catch (Exception)
                {
                    regPrinceKey = null;
                }
                return regPrinceKey;
            }
        }
        #endregion RegPrinceKey
        #endregion Properties

        /// <summary>
        /// Entry point for InDesign export
        /// </summary>
        /// <param name="exportType">scripture / dictionary</param>
        /// <param name="publicationInformation">structure with other necessary information about project.</param>
        /// <returns></returns>
        public bool Launch(string exportType, PublicationInformation publicationInformation)
        {
            return Export(publicationInformation);
        }

        public bool Export(PublicationInformation projInfo)
        {
            bool success;
            bool isUnixOS = Common.UnixVersionCheck();
            try
            {
                var regPrinceKey = RegPrinceKey;
                if (regPrinceKey != null || isUnixOS)
                {
                    var curdir = Environment.CurrentDirectory;
                    PreExportProcess preProcessor = new PreExportProcess(projInfo);
                    if (isUnixOS)
                    {
                        projInfo.DefaultXhtmlFileWithPath =
                            Common.RemoveDTDForLinuxProcess(projInfo.DefaultXhtmlFileWithPath);
                    }
                    Environment.CurrentDirectory = Path.GetDirectoryName(projInfo.DefaultXhtmlFileWithPath);
                    preProcessor.GetTempFolderPath();
                    preProcessor.ImagePreprocess();
                    preProcessor.ReplaceSlashToREVERSE_SOLIDUS();
                    if (projInfo.SwapHeadword)
                        preProcessor.SwapHeadWordAndReversalForm();
                    preProcessor.MovePictureAsLastChild(preProcessor.ProcessedXhtml);
                    preProcessor.SetNonBreakInVerseNumber(preProcessor.ProcessedXhtml);
                    preProcessor.ReplaceDoubleSlashToLineBreak(preProcessor.ProcessedXhtml);
                    preProcessor.MoveCallerToPrevText(preProcessor.ProcessedXhtml);
                    string tempFolder = Path.GetDirectoryName(preProcessor.ProcessedXhtml);
                    string tempFolderName = Path.GetFileName(tempFolder);
                    var mc = new MergeCss { OutputLocation = tempFolderName };
                    string mergedCSS = mc.Make(projInfo.DefaultCssFileWithPath, "Temp1.css");
                    preProcessor.ReplaceStringInCss(mergedCSS);
                    preProcessor.InsertPropertyInCSS(mergedCSS);
                    preProcessor.RemoveTextIntent(mergedCSS);
                    

                    Dictionary<string, Dictionary<string, string>> cssClass = new Dictionary<string, Dictionary<string, string>>();
                    CssTree cssTree = new CssTree();
                    cssTree.OutputType = Common.OutputType.ODT;
                    cssClass = cssTree.CreateCssProperty(mergedCSS, true);
                    if(cssClass.ContainsKey("@page") && cssClass["@page"].ContainsKey("-ps-hide-versenumber-one"))
                    {
                        string value = cssClass["@page"]["-ps-hide-versenumber-one"];
                        if(value.ToLower() == "true")
                        {
                            preProcessor.RemoveVerseNumberOne(preProcessor.ProcessedXhtml, mergedCSS);
                        }
                    }

                    string xhtmlFileName = Path.GetFileNameWithoutExtension(projInfo.DefaultXhtmlFileWithPath);
                    string defaultCSS = Path.GetFileName(mergedCSS);
                    Common.SetDefaultCSS(preProcessor.ProcessedXhtml, defaultCSS);
                    _processedXhtml = preProcessor.ProcessedXhtml;
                    if (!isUnixOS)
                    {
                        Object princePath = regPrinceKey.GetValue("InstallLocation");
                        _fullPrincePath = Common.PathCombine((string)princePath, "Engine/Bin/Prince.exe");
                        var myPrince = new Prince(_fullPrincePath);
                        myPrince.AddStyleSheet(defaultCSS);
                        myPrince.Convert(_processedXhtml, xhtmlFileName + ".pdf");
                    }
                    else
                    {
                        if (isUnixOS)
                        {
                            if (!Directory.Exists("/usr/lib/prince/bin"))
                            {
                                return success = false;
                                //MessageBox.Show(@"Sorry a preview of this stylesheet is not available. Please install PrinceXML or LibreOffice to enable the preview.", "Pathway Configuration Tool" , MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                            }
                        }
                        Environment.CurrentDirectory = Path.GetDirectoryName(projInfo.DefaultXhtmlFileWithPath);
                        Directory.SetCurrentDirectory(Path.GetDirectoryName(projInfo.DefaultXhtmlFileWithPath));
                        string p1Error = string.Empty;
                        string inputArguments = "";
                        inputArguments = _processedXhtml + " -o " + xhtmlFileName + ".pdf";
                        using (Process p1 = new Process())
                        {
                            p1.StartInfo.FileName = "prince";
                            if (File.Exists(_processedXhtml))
                            {
                                p1.StartInfo.Arguments = inputArguments;
                            }
                            p1.StartInfo.RedirectStandardOutput = true;
                            p1.StartInfo.RedirectStandardError = p1.StartInfo.RedirectStandardOutput;
                            p1.StartInfo.UseShellExecute = !p1.StartInfo.RedirectStandardOutput;
                            p1.Start();
                            p1.WaitForExit();
                            p1Error = p1.StandardError.ReadToEnd();
                        }
                        //Common.RunCommand("prince ", _processedXhtml + " -o, " + xhtmlFileName + ".pdf", 1);
                    }

                    //Copyright information added in PDF files
                    string pdfFIleName = Common.InsertCopyrightInPdf(Common.PathCombine(Environment.CurrentDirectory, xhtmlFileName + ".pdf"), "Prince XML");

                    ////string pdfFIleName = xhtmlFileName + ".pdf";
                    //if (!Common.Testing)
                    //    Process.Start(pdfFIleName);
                    Environment.CurrentDirectory = curdir;
                    Common.CleanupExportFolder(projInfo.DefaultXhtmlFileWithPath, ".tmp,.de", "layout", string.Empty);
                    success = true;
                }
                else
                {
                    //if (Common.Testing) return;
                    //var msg = new[] { "PrinceXML not installed in this system" };
                    //LocDB.Message("defErrMsg", "PrinceXML not installed in this system", msg, LocDB.MessageTypes.Info, LocDB.MessageDefault.First);
                    success = false;
                }
            }
            catch (Exception)
            {
                success = false;
            }
            return success;
        }
    }
}
