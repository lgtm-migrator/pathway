﻿// --------------------------------------------------------------------------------------------
// <copyright file="WordPressConvert.cs" from='2010' to='2010' company='SIL International'>
//      Copyright © 2010, SIL International. All Rights Reserved.   
//    
//      Distributable under the terms of either the Common Public License or the
//      GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed: 
// 
// <remarks>
// Create Wordpress blog 
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SIL.Tool;

namespace SIL.PublishingSolution
{
    public class ExportWordPress : IExportProcess
    {
        #region property string ExportType
        /// <summary>Text to appear in drop down list.</summary>
        public string ExportType
        {
            get
            {
                return "WordPress Alpha";
            }
        }
        #endregion property string ExportType

        #region bool Handle(string inputDataType)
        /// <summary>
        /// The calling program identifies the kind of data
        /// </summary>
        /// <param name="inputDataType">dictionary or scripture</param>
        /// <returns>true if this backend can handle the data</returns>
        public bool Handle(string inputDataType)
        {
            bool returnValue = false;
            string dataType = inputDataType.ToLower();
            if (dataType == "dictionary")
            {
                returnValue = true;
            }
            return returnValue;
        }
        #endregion bool Handle(string inputDataType)

        #region bool Export(PublicationInformation projInfo)
        /// <summary>
        /// Entry point for WordPress converter
        /// </summary>
        /// <param name="projInfo">values passed including xhtml and css names</param>
        /// <returns>true if succeeds</returns>
        public bool Export(PublicationInformation projInfo)
        {
            try
            {
                var xhtml = projInfo.DefaultXhtmlFileWithPath;
                PreExportProcess preProcessor = new PreExportProcess(projInfo);
                preProcessor.InsertFolderNameForAudioFilesinXhtml();

                InsertBeforeAfterInXHTML(projInfo);

                const string prog = "WordPress.bat";
                var processFolder = Common.PathCombine(Common.GetAllUserPath(), "WordPress");
                if (!Directory.Exists(processFolder))
                    processFolder = Common.FromRegistry("WordPress");
                var progFullPath = Common.PathCombine(processFolder, prog);
                var args = string.Format(@"""{0}""", xhtml);
                SubProcess.Run(processFolder, progFullPath, args, true);
                if (projInfo.IsOpenOutput)
                {
                    string dataResult = Common.PathCombine(Path.GetDirectoryName(xhtml), "data.sql");
                    string msg = string.Format("Please import the file {0} to your WordPress MySql database. Would you like more details?", dataResult);
                    DialogResult dialogResult = MessageBox.Show(msg, "WordPress Export", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (dialogResult == DialogResult.Yes)
                        SubProcess.Run(processFolder, @"""WordPress site setup.txt""");
                }

                WebonaryFileTransfer webonaryFtp = new WebonaryFileTransfer();
                webonaryFtp.projInfo = projInfo;
                webonaryFtp.ShowDialog();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        #endregion bool Export(PublicationInformation projInfo)

        #region Private Functions
        #region Handle After Before
        /// <summary>
        /// Inserting After & Before content to XHTML file
        /// </summary>
        private void InsertBeforeAfterInXHTML(PublicationInformation projInfo)
        {
            if (projInfo == null) return;

            string cssFilePath = projInfo.DefaultXhtmlFileWithPath.Replace(".xhtml", ".css");

            if (projInfo.DefaultCssFileWithPath.Length == 0)
            {
                if (File.Exists(cssFilePath))
                    projInfo.DefaultCssFileWithPath = cssFilePath;
            }

            if (projInfo.DefaultXhtmlFileWithPath == null || projInfo.DefaultCssFileWithPath == null) return;
            if (projInfo.DefaultXhtmlFileWithPath.Trim().Length == 0 || projInfo.DefaultCssFileWithPath.Trim().Length == 0) return;

            Dictionary<string, Dictionary<string, string>> cssClass = new Dictionary<string, Dictionary<string, string>>();
            CssTree cssTree = new CssTree();
            cssClass = cssTree.CreateCssProperty(projInfo.DefaultCssFileWithPath, true);

            AfterBeforeProcessEpub afterBeforeProcess = new AfterBeforeProcessEpub();
            afterBeforeProcess.RemoveAfterBefore(projInfo, cssClass, cssTree.SpecificityClass, cssTree.CssClassOrder);

            Common.StreamReplaceInFile(projInfo.DefaultXhtmlFileWithPath, "&nbsp;", "&#x2007;");

            RemovePagedStylesFromCss(cssFilePath);
            //if (projInfo.IsReversalExist)
            //{
            //    cssClass = cssTree.CreateCssProperty(projInfo.DefaultRevCssFileWithPath, true);
            //    string originalDefaultXhtmlFileName = projInfo.DefaultXhtmlFileWithPath;
            //    projInfo.DefaultXhtmlFileWithPath = Path.Combine(Path.GetDirectoryName(projInfo.DefaultXhtmlFileWithPath), "FlexRev.xhtml");
            //    AfterBeforeProcessEpub afterBeforeProcessReversal = new AfterBeforeProcessEpub();
            //    afterBeforeProcessReversal.RemoveAfterBefore(projInfo, cssClass, cssTree.SpecificityClass, cssTree.CssClassOrder);
            //    Common.StreamReplaceInFile(projInfo.DefaultXhtmlFileWithPath, "&nbsp;", "&#x2007;");
            //    projInfo.DefaultXhtmlFileWithPath = originalDefaultXhtmlFileName;
            //}
        }
        #endregion

        /// <summary>
        /// Removes stylings that don't work with e-book readers from the specified .css file.
        /// </summary>
        /// <param name="cssFile"></param>
        private void RemovePagedStylesFromCss(string cssFile)
        {
            if (!File.Exists(cssFile)) { return; }
            // open the file
            var reader = new StreamReader(cssFile);
            var writer = new StreamWriter(cssFile + ".tmp");
            bool done = false;
            string oneLine = null;
            while (!done)
            {
                oneLine = reader.ReadLine();
                if (oneLine == null)
                {
                    done = true;
                    continue;
                }
                if (oneLine.Contains("/** imported"))
                {
                    writer.WriteLine(oneLine);
                    done = true;
                    continue;
                }
                // epub readers vary in their support for :before and :after (which FLEx uses to insert content: punctuation) -
                // we've already transformed the text to include these items in ContentCssToXhtml(); remove them from the css now.
                if (oneLine.Contains("content:"))
                {
                    if (!oneLine.Contains("counter(sense, disc)"))
                        continue;
                }
                // epub doesn't work with footnote, prince-footnote, columns, string-sets or counters
                if (oneLine.Contains("display: footnote") || oneLine.Contains("display: prince-footnote") ||
                    oneLine.Contains("position: footnote") || oneLine.Contains("column-count") || oneLine.Contains("column-gap") ||
                    oneLine.Contains("string-set") || oneLine.Contains("column-fill") || oneLine.Contains("counter-reset"))
                {
                    continue;
                }
                // These are blocks that we need to remove completely:
                // - The @page and ::<something> pseudo-elements are also not supported, at least in the way they are
                //   generated by the xhtml export (i.e., for paged media).
                // - The .picture class style from the merged Scripture CSS is too small (we resize it as needed for .epub)
                if (oneLine.Contains("@page") || oneLine.Contains("::") || oneLine.Contains(".picture "))
                {
                    // match the bracket count until we get back to 0 -- this will mark the end of the css block
                    int bracketCount = 1;
                    while (bracketCount != 0 && !reader.EndOfStream)
                    {
                        var nextChar = (char)reader.Read();
                        switch (nextChar)
                        {
                            case '{':
                                // found a sub-element - make sure we have a matching pair
                                bracketCount++;
                                break;
                            case '}':
                                // closed out an element (or sub-element) - decrement the bracket count
                                bracketCount--;
                                break;
                            default:
                                break;
                        }
                    }
                    // the entire CSS should now be dropped -- continue on to the next line of data
                    continue;
                }
                // if we got here, the line is good - write it out
                writer.WriteLine(oneLine);
            }
            // there's nothing more we're interested in - read the rest of the file out
            if (oneLine != null)
            {
                writer.Write(reader.ReadToEnd());
            }
            writer.Close();
            reader.Close();
            // now copy over our changes
            if (File.Exists(cssFile))
            {
                // should always be the case
                File.Delete(cssFile);
            }
            File.Move(cssFile + ".tmp", cssFile);
        }

        #endregion Private Functions


    }
}
