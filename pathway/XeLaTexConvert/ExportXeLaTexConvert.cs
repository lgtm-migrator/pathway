// --------------------------------------------------------------------------------------------
// <copyright file="ExportProcess.cs" from='2009' to='2009' company='SIL International'>
//      Copyright � 2009, SIL International. All Rights Reserved.   
//    
//      Distributable under the terms of either the Common Public License or the
//      GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed: 
// 
// <remarks>
// Export process used to Export the ODT and Prince PDF output
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SIL.Tool;
using Test;

namespace SIL.PublishingSolution
{
    public class ExportXeLaTex : IExportProcess
    {
        #region Public Functions
        public string ExportType
        {
            get
            {
                return "XeLaTex";
            }
        }

        public bool Handle(string inputDataType)
        {
            bool returnValue = false;
            string dataType = inputDataType.ToLower();
            if (dataType == "dictionary" || dataType == "scripture")
            {
                returnValue = true;
            }
            if (string.IsNullOrEmpty(XeLaTexInstallation.GetXeLaTexDir()))
            {
                returnValue = false;
            }
            return returnValue;
        }

        /// <summary>
        /// Convert XHTML to ODT
        /// </summary>
        public bool Export(PublicationInformation projInfo)
        {
            PreExportProcess preProcessor = new PreExportProcess(projInfo);
            //preProcessor.GetTempFolderPath();
            //preProcessor.PreserveSpace();
            //preProcessor.ImagePreprocess();
            //preProcessor.ReplaceInvalidTagtoSpan();
            //preProcessor.InsertHiddenChapterNumber();
            //preProcessor.InsertHiddenVerseNumber();
            //preProcessor.GetDefinitionLanguage();
            
            string fileName = Path.GetFileNameWithoutExtension(projInfo.DefaultXhtmlFileWithPath);
            //projInfo.DefaultXhtmlFileWithPath = preProcessor.ProcessedXhtml;
            projInfo.DefaultCssFileWithPath = preProcessor.ProcessedCss;
            projInfo.ProjectPath = Path.GetDirectoryName(preProcessor.ProcessedXhtml);
            projInfo.DefaultXhtmlFileWithPath = preProcessor.PreserveSpace(); 

            Dictionary<string, Dictionary<string, string>> cssClass = new Dictionary<string, Dictionary<string, string>>();
            CssTree cssTree = new CssTree();
            cssTree.OutputType = Common.OutputType.XELATEX;
            cssClass = cssTree.CreateCssProperty(projInfo.DefaultCssFileWithPath, true);

            string xeLatexFullFile = Path.Combine(projInfo.ProjectPath, fileName + ".tex");
            StreamWriter xeLatexFile = new StreamWriter(xeLatexFullFile);
            
            Dictionary<string, List<string>> classInlineStyle = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, string>> xeTexAllClass = new Dictionary<string, Dictionary<string, string>>();
            XeLaTexStyles xeLaTexStyles = new XeLaTexStyles();
            classInlineStyle = xeLaTexStyles.CreateXeTexStyles(projInfo.ProjectPath, xeLatexFile, cssClass);

            XeLaTexContent xeLaTexContent = new XeLaTexContent();
            Dictionary<string, List<string>> classInlineText = xeLaTexStyles._classInlineText;
            Dictionary<string, Dictionary<string, string>> newProperty = xeLaTexContent.CreateContent(projInfo, cssClass, xeLatexFile, classInlineStyle, cssTree.SpecificityClass, cssTree.CssClassOrder, classInlineText);

            CloseDocument(xeLatexFile);

            string include = xeLaTexStyles.PageStyle.ToString();
            ModifyXeLaTexStyles modifyXeLaTexStyles = new ModifyXeLaTexStyles();
            modifyXeLaTexStyles.ModifyStylesXML(projInfo.ProjectPath, xeLatexFile, newProperty, cssClass, xeLatexFullFile, include);

            //CallXeTex(Path.GetFileName(xeLatexFullFile));
            Dictionary<string, string> imgPath = new Dictionary<string, string>();
            if (newProperty.ContainsKey("ImagePath"))
            {
                imgPath = newProperty["ImagePath"];
            }
            UpdateXeLaTexFontCacheIfNecessary();
            CallXeLaTex(xeLatexFullFile, true, imgPath);
            return true;
        }

        protected void UpdateXeLaTexFontCacheIfNecessary()
        {
            Debug.Assert(XeLaTexInstallation.GetXeLaTexDir() != "");
            var systemFontList = FontFamily.Families;
            if (systemFontList.Length != XeLaTexInstallation.GetXeLaTexFontCount())
            {
                using (var p2 = new Process())
                {
                    var xelatexPath = XeLaTexInstallation.GetXeLaTexDir();
                    xelatexPath = Path.Combine(xelatexPath, "bin");
                    xelatexPath = Path.Combine(xelatexPath, "win32");
                    p2.StartInfo.WorkingDirectory = xelatexPath;
                    p2.StartInfo.FileName = "fc-cache";
                    p2.StartInfo.Arguments = "-v -r";
                    p2.Start();
                    p2.WaitForExit();
                }
                XeLaTexInstallation.SetXeLaTexFontCount(systemFontList.Length);
            }
        }
        
        public void CallXeLaTex(string xeLatexFullFile, bool openFile, Dictionary<string, string> ImageFilePath)
        {

            string str = XeLaTexInstallation.GetXeLaTexDir();

            string instPath = Common.PathCombine(str, "bin");
            instPath = Common.PathCombine(instPath, "win32");
            string originalDirectory = Directory.GetCurrentDirectory();
            string dest = Common.PathCombine(instPath, Path.GetFileName(xeLatexFullFile));
            File.Copy(xeLatexFullFile, dest, true);

            Directory.SetCurrentDirectory(instPath);
            const string name = "xelatex.exe";
            //string p1Output = string.Empty;
            string p1Error = string.Empty;
            using (Process p1 = new Process())
            {
                p1.StartInfo.FileName = name;
                if (xeLatexFullFile != null)
                    p1.StartInfo.Arguments = "-interaction=batchmode \"" + Path.GetFileName(xeLatexFullFile) + "\"";
                p1.StartInfo.RedirectStandardOutput = true;
                p1.StartInfo.RedirectStandardError = p1.StartInfo.RedirectStandardOutput;
                p1.StartInfo.UseShellExecute = !p1.StartInfo.RedirectStandardOutput;
                p1.Start();
                p1.WaitForExit();
                //p1Output = p1.StandardOutput.ReadToEnd();
                p1Error = p1.StandardError.ReadToEnd();
            }
            Directory.SetCurrentDirectory(originalDirectory);
            string texNameOnly = Path.GetFileNameWithoutExtension(xeLatexFullFile);
            string userFolder = Path.GetDirectoryName(xeLatexFullFile);
            string logFullName = CopyProcessResult(instPath, texNameOnly, ".log", userFolder);
            string pdfFullName = CopyProcessResult(instPath, texNameOnly, ".pdf", userFolder);

            if (openFile && File.Exists(pdfFullName))
            {
                Common.OpenOutput(pdfFullName);
            }
            //MessageBox.Show(pdfFullName, "XeLaTex output");
            //MessageBox.Show(p1Error, "XeLaTex errors");
            //MessageBox.Show(string.Format("Review {0} for conversion results.", logFullName), "XeLaTex log");
            //const bool recursive = true;

            try
            {
                File.Delete(Common.PathCombine(instPath, texNameOnly + ".log"));
                File.Delete(Common.PathCombine(instPath, texNameOnly + ".pdf"));
                File.Delete(Common.PathCombine(instPath, texNameOnly + ".aux"));
                File.Delete(dest);
            }
            catch{}
            
        }

        protected static string CopyProcessResult(string instPath, string texNameOnly, string ext, string userFolder)
        {
            const bool overwrite = true;
            string logName = texNameOnly + ext;
            string tmpLogFullName = Path.Combine(instPath, logName);
            string logFullName = Path.Combine(userFolder, logName);
            if (File.Exists(tmpLogFullName))
                File.Copy(tmpLogFullName, logFullName, overwrite);
            return logFullName;
        }

        
        private void CloseDocument(StreamWriter xeLatexFile)
        {
            xeLatexFile.WriteLine();
            xeLatexFile.WriteLine(@"\end{document}");
            xeLatexFile.Flush();
            xeLatexFile.Close();
        }


        private void Launch(string ldmlFullName)
        {
            try
            {
                Common.OpenOutput(ldmlFullName);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1155)
                {

                }
            }
        }

        #endregion
    }
}
