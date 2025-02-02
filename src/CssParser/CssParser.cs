// --------------------------------------------------------------------------------------------
// <copyright file="CssParser.cs" from='2009' to='2014' company='SIL International'>
//      Copyright ( c ) 2014, SIL International. All Rights Reserved.   
//    
//      Distributable under the terms of either the Common Public License or the
//      GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed: 
// 
// <remarks>
// Handling duplicate classes
// </remarks>
// --------------------------------------------------------------------------------------------

#region Using

using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using Antlr.Runtime.Tree;
using System.Windows.Forms;
using SIL.Tool;

#endregion

namespace SIL.PublishingSolution
{
    public class CssParser
    {
        #region Properties
        public string ErrorText { get; private set; }
        public Dictionary<string, ArrayList> ErrorList = new Dictionary<string, ArrayList>(); // For Error Report
        #endregion Properties

        #region Private Variables
        private bool _isReCycle;
        readonly TreeNode _nodeTemp = new TreeNode();
        readonly TreeNode _nodeFinal = new TreeNode("ROOT");
        readonly ArrayList _checkRuleNode = new ArrayList();
        readonly ArrayList _checkMediaNode = new ArrayList();
        readonly Dictionary<string, ArrayList> _pagePropertyInfo = new Dictionary<string, ArrayList>();

        readonly ArrayList _checkPageName = new ArrayList();

        #endregion

        #region Constant Variables
        const string _pageSeperator = "~";
        #endregion

        #region Public Variable
        public struct Rule
        {
            public string ClassName;
            public string PseudoName;
            public bool IsPseudo;
            public int NodeCount;
            public bool IsClassContent;
            public bool HasProperty;
        }
        public Common.OutputType OutputType;

        #endregion

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// Parsing the CSS file when each file having @import.
        /// </summary>
        /// <param name="inputCSSPath">Accepts file path of the CSS file</param>
        /// <returns>returns TreeNode</returns>
        /// -------------------------------------------------------------------------------------------
        public TreeNode BuildTree(string inputCSSPath)
        {
            var emptyTree = new TreeNode();
            if (inputCSSPath.Length <= 0 || !File.Exists(inputCSSPath)) return emptyTree;

            try
            {
                string BaseCssFileWithPath = inputCSSPath;
                ArrayList arrayCSSFile = Common.GetCSSFileNames(inputCSSPath, BaseCssFileWithPath);
                arrayCSSFile.Add(BaseCssFileWithPath);
                Common.RemovePreviousMirroredPage(arrayCSSFile);
                try
                {
                    GetErrorReport(inputCSSPath);
                }
                catch{}
                string file = Common.MakeSingleCSS(inputCSSPath, "_MergedCSS.css");
                var fileSize = new FileInfo(file);
                if (fileSize.Length > 0)
                {
                    string tempCSS = file;
                    ParseCSS(tempCSS);
                    if (File.Exists(tempCSS))
                    {
                        File.Delete(tempCSS);
                    }
                    return _nodeFinal;
                }
                return emptyTree;
            }
            catch
            {
                return emptyTree;
            }
        }


        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// This function parsing the CSS file and creates a TreeNode nodeTemp.
        /// </summary>
        /// <param name="path">Its gets the file path of the CSS File</param>
        /// -------------------------------------------------------------------------------------------
        private void ParseCSS(string path)
        {
            var ctp = new CssTreeParser();
            try
            {
                ctp.Parse(path);
                ErrorText = ctp.ErrorText();
            }
            catch (Exception)
            {
                ErrorText = ctp.ErrorText();
                throw;
            }
            CommonTree r = ctp.Root;
            _nodeTemp.Nodes.Clear();

            if (r.Text != "nil" && r.Text != null)
            {
                _nodeTemp.Text = "nil";
                AddSubTree(_nodeTemp, r, ctp);
            }
            else
            {
                string rootNode = r.Text ?? "nil";
                _nodeTemp.Text = rootNode;
                foreach (CommonTree child in ctp.Children(r))
                {
                    AddSubTree(_nodeTemp, child, ctp);
                }
            }

            // To validate the nodes in nodeTemp has copied to nodeFine
            if (_isReCycle == false)
            {
                _nodeFinal.Nodes.Clear();
                MakeTreeNode(_nodeTemp, _nodeFinal, false);
                // To traverse the node second time.
                if (_isReCycle)
                {
                    MakeTreeNode(_nodeFinal, _nodeFinal, true);
                }
            }
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// This method adds each node,subnode,child in Treenode named as nodeTemp.
        /// </summary>
        /// <param name="n">Its gets the node element from the TreeTemp</param>
        /// <param name="t">Its gets the parent of the current tree</param>
        /// <param name="ctp">input CSSTreeParser</param>
        /// -------------------------------------------------------------------------------------------
        private static void AddSubTree(TreeNode n, CommonTree t, CssTreeParser ctp)
        {
            TreeNode nodeTemp = n.Nodes.Add(t.Text);
            foreach (CommonTree child in ctp.Children(t))
                AddSubTree(nodeTemp, child, ctp);
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// To avoiding duplication of nodes based on Inheritance.
        /// </summary>    
        /// <param name="fromNode">Source TreeNode</param>
        /// <param name="toNode">Destination TreeNode</param>
        /// <param name="status">By default it is false for first time and true for second time</param>
        /// -------------------------------------------------------------------------------------------
        private void MakeTreeNode(TreeNode fromNode, TreeNode toNode, bool status)
        {
            // Filter the Duplicate Classes from NodeTemp Treenode
            foreach (TreeNode node in fromNode.Nodes)
            {
                if (node.Text == "RULE")
                {
                    bool commaRule = false;
                    foreach (TreeNode chkNode in node.Nodes)
                    {
                        if (chkNode.Text == ",")
                        {
                            ParseCommaRule(node, toNode);
                            commaRule = true;
                            break;
                        }
                    }
                    if (!commaRule)
                    {
                        ParseRule(node, toNode);
                    }
                }
                else if (node.Text == "PAGE")
                {
                    if (status == false)
                    {
                        ParsePage(node, toNode);
                    }
                }
                else if (node.Text == "MEDIA")
                {
                    if (status == false)
                    {
                        if (node.FirstNode.Text.ToUpper() == "PRINT")
                        {
                            ParseMedia(node, toNode);
                        }
                        else
                        {
                            toNode.Nodes.Add((TreeNode)node.Clone());
                        }
                    }
                }
            }

            if (status == false)
            {
                _isReCycle = true;
            }
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// This method inherits the page properties which having same classname in CSS file and added to the TreeNode.
        /// </summary>
        /// <param name="node">nodes from nodeTemp(treenode)</param>
        /// <param name="nodeFine">nodes added to nodeFine(treenode)</param>
        /// -------------------------------------------------------------------------------------------
        private void ParseMedia(TreeNode node, TreeNode nodeFine)
        {
            string mediaType = node.FirstNode.Text;
            int cnt = _checkMediaNode.Count;
            bool isAttribPropertyWritten = false;
            if (cnt == 0)
            {
                foreach (TreeNode mainNode in node.Nodes)
                {
                    string attribName = string.Empty;
                    string attribValue = string.Empty;
                    if (mainNode.Text == "RULE")
                    {
                        foreach (TreeNode regionNode in mainNode.Nodes)
                        {
                            if (regionNode.Text == "ANY")
                            {
                                if (regionNode.FirstNode.Text == "ATTRIB")
                                {
                                    attribName = regionNode.FirstNode.FirstNode.Text;
                                    if (regionNode.FirstNode.Nodes.Count > 1)
                                    {
                                        attribValue = regionNode.FirstNode.LastNode.Text.Replace("\"", "");
                                        attribValue = attribValue.Replace("\'", "");
                                    }
                                }
                                _checkMediaNode.Add(mediaType + attribName + attribValue);
                            }
                            else if (regionNode.Text == "PROPERTY")
                            {
                                _checkMediaNode.Add(mediaType + attribName + attribValue + regionNode.FirstNode.Text);
                            }
                        }
                    }
                }
                nodeFine.Nodes.Add((TreeNode)node.Clone());
            }
            else
            {
                mediaType = node.FirstNode.Text;
                foreach (TreeNode mainNode in node.Nodes)
                {
                    string attribName = string.Empty;
                    string attribValue = string.Empty;
                    if (mainNode.Text == "RULE")
                    {
                        foreach (TreeNode regionNode in mainNode.Nodes)
                        {
                            if (regionNode.Text == "ANY")
                            {
                                if (regionNode.FirstNode.Text == "ATTRIB")
                                {
                                    attribName = regionNode.FirstNode.FirstNode.Text;
                                    if (regionNode.FirstNode.Nodes.Count > 1)
                                    {
                                        attribValue = regionNode.FirstNode.LastNode.Text.Replace("\"", "");
                                    }
                                }
                                if (!_checkMediaNode.Contains(mediaType + attribName + attribValue))
                                {
                                    _checkMediaNode.Add(mediaType + attribName + attribValue);
                                    nodeFine.LastNode.Nodes.Add(mainNode);
                                    isAttribPropertyWritten = true;
                                }
                                else
                                {
                                    foreach (TreeNode RTNode in mainNode.Nodes)
                                    {
                                        if (RTNode.Text == "PROPERTY")
                                        {
                                            InsertMediaProperty(mediaType + attribName + attribValue, mediaType + attribName + attribValue + RTNode.FirstNode.Text, RTNode);
                                        }
                                    }
                                }
                            }
                            else if (regionNode.Text == "PROPERTY" && isAttribPropertyWritten == false)
                            {
                                if (!_checkMediaNode.Contains(mediaType + attribName + attribValue + regionNode.FirstNode.Text))
                                {
                                    _checkMediaNode.Add(mediaType + attribName + attribValue + regionNode.FirstNode.Text);
                                    nodeFine.LastNode.Nodes.Add((TreeNode)regionNode.Clone());
                                }
                            }
                        }
                    }
                }
            }
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// This method inherits the properties which having same classname in CSS file and added to the TreeNode.
        /// </summary>
        /// <param name="node">nodes from nodeTemp(treenode)</param>
        /// <param name="nodeFine">nodes added to nodeFine(treenode)</param>
        /// -------------------------------------------------------------------------------------------
        /// 
        private void ParsePage(TreeNode node, TreeNode nodeFine)
        {
            string pageName = GetPageName(node);
            if (!_checkPageName.Contains(pageName))
            {
                GetPageContains(node);
                nodeFine.Nodes.Add((TreeNode)node.Clone());
                _checkPageName.Add(pageName);
            }
            else
            {
                string duppageName = GetPageName(node);
                foreach (TreeNode sNode in _nodeFinal.Nodes)
                {
                    if(sNode.Text == "PAGE")
                    {
                        pageName = GetPageName(sNode);
                        ProcessParserPageNodewise(node, pageName, duppageName, sNode);
                    }
                }
            }
        }

        private void ProcessParserPageNodewise(TreeNode node, string pageName, string duppageName, TreeNode sNode)
        {
            if (pageName == duppageName)
            {
                foreach (TreeNode dupNode in node.Nodes)
                {
                    ArrayList temp;
                    if (dupNode.Text == "REGION" && _pagePropertyInfo.ContainsKey(pageName + "." + dupNode.FirstNode.Text))
                    {
                        PagePropertyFirstNode(pageName, dupNode, sNode);
                    }
                    else if (dupNode.Text == "PROPERTY" && _pagePropertyInfo.ContainsKey(pageName))
                    {
                        temp = _pagePropertyInfo[pageName];

                        if (!temp.Contains(dupNode.FirstNode.Text))
                        {
                            sNode.Nodes.Add(dupNode);
                            ArrayList tempList = GetPropertyNames(dupNode.Parent);
                            _pagePropertyInfo[pageName] = tempList;
                        }
                        else
                        {
                            foreach (TreeNode pNode in sNode.Nodes)
                            {
                                if (pNode.Text == "PROPERTY" && pNode.FirstNode.Text == dupNode.FirstNode.Text)
                                {
                                    sNode.Nodes.Remove(pNode);
                                    sNode.Nodes.Add(dupNode);
                                }
                            }
                        }
                    }
                    else if (dupNode.Text == "REGION")
                    {
                        ArrayList t = GetPropertyNames(dupNode);
                        _pagePropertyInfo[pageName + "." + dupNode.FirstNode.Text] = t;
                        sNode.Nodes.Add(dupNode);
                    }
                    else if (dupNode.Text == "PROPERTY")
                    {
                        ArrayList t = GetPropertyNames(dupNode);
                        _pagePropertyInfo[pageName] = t;
                        sNode.Nodes.Add(dupNode);
                    }
                }
            }
        }

        private void PagePropertyFirstNode(string pageName, TreeNode dupNode, TreeNode sNode)
        {
            ArrayList regtemp;
            regtemp = _pagePropertyInfo[pageName + "." + dupNode.FirstNode.Text];
            foreach (TreeNode prpNode in dupNode.Nodes)
            {
                if (prpNode.Text == "PROPERTY" && !regtemp.Contains(prpNode.FirstNode.Text))
                {
                    foreach (TreeNode pNode in sNode.Nodes)
                    {
                        if (pNode.Text == "REGION" && pNode.FirstNode.Text == dupNode.FirstNode.Text)
                        {
                            foreach (TreeNode mNode in pNode.Nodes)
                            {
                                if (mNode != null && mNode.Text == "PROPERTY" && prpNode.Text == "PROPERTY" &&
                                    mNode.FirstNode.Text == prpNode.FirstNode.Text)
                                {
                                    pNode.Nodes.Remove(mNode);
                                }
                            }
                            pNode.Nodes.Add(prpNode);
                        }
                    }
                    ArrayList tempList = GetPropertyNames(dupNode.Parent);
                    _pagePropertyInfo[pageName + "." + dupNode.FirstNode.Text] = tempList;
                }
                else
                {
                    foreach (TreeNode pNode in sNode.Nodes)
                    {
                        if (pNode.Text == "REGION" && pNode.FirstNode.Text == dupNode.FirstNode.Text)
                        {
                            foreach (TreeNode propNode in pNode.Nodes)
                            {
                                if (prpNode.Text == "PROPERTY" && propNode.Text == "PROPERTY" &&
                                    propNode.FirstNode.Text == prpNode.FirstNode.Text)
                                {
                                    pNode.Nodes.Remove(propNode);
                                    pNode.Nodes.Add(prpNode);
                                }
                            }
                            break;
                        }
                    }
                }
            }
        }

        private ArrayList GetPropertyNames(TreeNode node)
        {
            ArrayList temp = new ArrayList();
            foreach (TreeNode propNode in node.Nodes)
            {
                if (propNode.Text == "PROPERTY")
                {
                    temp.Add(propNode.FirstNode.Text);
                }
            }
            return temp;
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// To collect the page details from the Page node and store in Arraylist
        /// </summary>
        /// <param name="node">current Page node</param>
        /// -------------------------------------------------------------------------------------------
        private void GetPageContains(TreeNode node)
        {
            string pageName = "PAGE";
            ArrayList regionExists;
            foreach (TreeNode item in node.Nodes)
            {
                switch (item.Text)
                {
                    case "PSEUDO":
                        pageName = pageName + _pageSeperator + item.FirstNode.Text;
                        break;
                    case "REGION":
                        regionExists = new ArrayList();
                        foreach (TreeNode PropNode in item.Nodes)
                        {
                            if(PropNode.Text == "PROPERTY")
                            {
                                if (_pagePropertyInfo.ContainsKey(pageName + "." + item.FirstNode.Text))
                                {
                                    regionExists.AddRange(_pagePropertyInfo[pageName + "." + item.FirstNode.Text]);
                                }
                                regionExists.Add(PropNode.FirstNode.Text);
                            }
                        }
                        _pagePropertyInfo[pageName + "." + item.FirstNode.Text] = regionExists;                       
                        break;
                    case "PROPERTY":
                        regionExists = new ArrayList();
                        if (_pagePropertyInfo.ContainsKey(pageName))
                        {
                            regionExists.AddRange(_pagePropertyInfo[pageName]);
                        }
                        regionExists.Add(item.FirstNode.Text);
                        _pagePropertyInfo[pageName] = regionExists;
                        break;
                    default:
                        break;
                }
            }
        }
        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// To get the page name with Pseudo value like page, page~first, page~left, page~right
        /// </summary>
        /// <param name="node">current Page node</param>
        /// <returns>returns pagename</returns>
        /// ------------------------------------------------------------------------------------------- 
        private static string GetPageName(TreeNode node)
        {
            string pageName = "PAGE";
            if (node.FirstNode != null && node.FirstNode.Text == "PSEUDO")
            {
                pageName = pageName + _pageSeperator + node.FirstNode.FirstNode.Text;
            }
            return pageName;
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// This method inherits the properties which having same classname in CSS file and added to the TreeNode.
        /// </summary>
        /// <param name="node">nodes from nodeTemp(treenode)</param>
        /// <param name="nodeFine">nodes added to nodeFine(treenode)</param>
        /// -------------------------------------------------------------------------------------------
        private void ParseRule(TreeNode node, TreeNode nodeFine)
        {
            foreach (TreeNode childNode in node.Nodes)
            {
                var newRuleNode = new Rule();
                GetRuleContains(node, ref newRuleNode);
                if (ValidateFirstNode(childNode))
                {
                    if (_isReCycle == false)
                    {
                        if (newRuleNode.HasProperty)
                        {
                            if (_checkRuleNode.Contains(newRuleNode.ClassName))
                            {
                                InsertNewRuleProperty(node, newRuleNode.ClassName, 'u', true);
                            }
                            else
                            {
                                _checkRuleNode.Add(newRuleNode.ClassName);
                                nodeFine.Nodes.Add((TreeNode)node.Clone());
                                break;
                            }
                        }
                    }
                    else
                    {
                        string[] parentClass = newRuleNode.ClassName.Trim().Split('.');
                        if (parentClass.Length > 2 && _checkRuleNode.Contains("." + parentClass[1]))
                        {
                            InsertNewRuleProperty(node, "." + parentClass[1], 'd', false);
                        }
                        else if (parentClass.Length > 2 && _checkRuleNode.Contains("." + parentClass[parentClass.Length - 1]))
                        {
                            if (parentClass[parentClass.Length - 1].IndexOf("=") > 0 && parentClass[parentClass.Length - 1].IndexOf(":") > 0)
                            {
                                parentClass[parentClass.Length - 1] = parentClass[parentClass.Length - 1].Substring(0, parentClass[parentClass.Length - 1].IndexOf("="));
                                InsertNewRuleProperty(node, "." + parentClass[parentClass.Length - 1], 'd', false);
                            }
                            else if (parentClass[parentClass.Length - 1].IndexOf(":") > 0)
                            {
                                parentClass[parentClass.Length - 1] = parentClass[parentClass.Length - 1].Substring(0, parentClass[parentClass.Length - 1].IndexOf(":"));
                                InsertNewRuleProperty(node, "." + parentClass[parentClass.Length - 1], 'd', false);
                            }
                        }
                        else if (parentClass.Length >= 2)
                        {
                            if (parentClass[1].IndexOf(":") > 0)
                            {
                                parentClass[1] = parentClass[1].Substring(0, parentClass[1].IndexOf(":"));
                                InsertNewRuleProperty(node, "." + parentClass[1], 'd', false);
                            }
                            else if (parentClass[parentClass.Length - 1].IndexOf("=") > 0 && parentClass[parentClass.Length - 1].IndexOf(":") > 0)
                            {
                                parentClass[parentClass.Length - 1] = parentClass[parentClass.Length - 1].Substring(0, parentClass[parentClass.Length - 1].IndexOf("="));
                                InsertNewRuleProperty(node, "." + parentClass[parentClass.Length - 1], 'd', false);
                            }
                            else if (parentClass[parentClass.Length - 1].IndexOf(":") > 0)
                            {
                                parentClass[parentClass.Length - 1] = parentClass[parentClass.Length - 1].Substring(0, parentClass[parentClass.Length - 1].IndexOf(":"));
                                InsertNewRuleProperty(node, "." + parentClass[parentClass.Length - 1], 'd', false);
                                if (parentClass[1].IndexOf('>') > 0)
                                {
                                    parentClass[1] = parentClass[1].Replace(">", "");
                                    InsertNewRuleProperty(node, "." + parentClass[1], 'd', false);
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        private static ArrayList GetPropertyList(TreeNode node)
        {
            var propL = new ArrayList();
            foreach (TreeNode PropNode in node.Nodes)
            {
                if (PropNode.Text == "PROPERTY")
                {
                    propL.Add(PropNode.FirstNode.Text);
                }
            }
            return propL;
        }

        private void InsertNewRuleProperty(TreeNode repNode, string className, char dir, bool isSameClass)
        {
            var repRuleNode = new Rule();
            GetRuleContains(repNode, ref repRuleNode);
            ArrayList repProperty;
            foreach (TreeNode RuleNode in _nodeFinal.Nodes)
            {
                var newRuleNode = new Rule();
                GetRuleContains(RuleNode, ref newRuleNode);
                if (newRuleNode.ClassName == className)
                {
                    if (dir == 'u')
                    {
                        repProperty = GetPropertyList(RuleNode);
                        InsertInfoNode(repNode, repProperty);                        
                        foreach (TreeNode childNode in repNode.Nodes)
                        {
                            if (childNode.Text == "PROPERTY" && isSameClass)
                            {
                                if (!repProperty.Contains(childNode.FirstNode.Text))
                                {
                                    RuleNode.Nodes.Add((TreeNode)childNode.Clone());
                                }
                                else
                                {
                                    ReplaceRuleNode(childNode);
                                }
                            }
                        }
                    }
                    else
                    {
                        repProperty = GetPropertyList(repNode);
                        InsertInfoNode(repNode, repProperty);  
                        foreach (TreeNode childNode in RuleNode.Nodes)
                        {
                            if (childNode.Text == "PROPERTY")
                            {
                                if (isSameClass || !repProperty.Contains(childNode.FirstNode.Text))
                                {
                                    repNode.Nodes.Add((TreeNode)childNode.Clone());
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// To insert the information node for the Pseudo class which has only content file but it's classname has styles
        /// for example .definition{color:red; font-size:12pt;} .definition:before{content:'';}
        /// </summary>
        /// <param name="repNode"></param>
        /// <param name="repProperty"></param>
        private static void InsertInfoNode(TreeNode repNode, ArrayList repProperty)
        {
            if (repProperty.Count == 1 && repProperty.Contains("content"))
            {
                var node = new TreeNode();
                node.Nodes.Add("PROPERTY");
                node.Nodes[0].Nodes.Add("pathway");
                node.Nodes[0].Nodes.Add("emptyPsuedo");
                repNode.Nodes.Add(node.FirstNode);
            }
        }
        private string GetRuleClassname(TreeNode node)
        {
            string className = string.Empty;
            foreach (TreeNode childNode in node.Nodes)
            {
                if (childNode.Text == "CLASS")
                {
                    if (OutputType != Common.OutputType.EPUB)
                    {
                        childNode.FirstNode.Text = childNode.FirstNode.Text.Replace("_", "").Replace("-", "");
                    }
                    className += "." + childNode.FirstNode.Text;
                    if (childNode.Nodes.Count > 1)
                    {
                        for (int i = 0; i < childNode.Nodes.Count; i++)
                        {
                            if (childNode.Nodes[i].Text == "ATTRIB")
                            {
                                className += "=" +
                                             childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                 "\"", "");
                            }
                            else if (childNode.Nodes[i].Text == "HASVALUE")
                            {
                                className += "~" +
                                             childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                 "\"", "");
                            }
                        }
                    }
                }
                else if (childNode.Text == "TAG")
                {
                    className += " " + childNode.FirstNode.Text;
                    if (childNode.Nodes.Count > 1)
                    {
                        for (int i = 0; i < childNode.Nodes.Count; i++)
                        {
                            if (childNode.Nodes[i].Text == "ATTRIB")
                            {
                                className += "=" +
                                             childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                 "\"", "");
                            }
                            else if (childNode.Nodes[i].Text == "HASVALUE")
                            {
                                className += "~" +
                                             childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                 "\"", "");
                            }
                        }
                    }
                }
                else if (childNode.Text == "ANY")
                {
                    if (childNode.FirstNode != null)
                    {
                        className += "*" + childNode.FirstNode.Text;
                    }
                    else
                    {
                        className += "*";
                    }
                    
                }
                else if (childNode.Text == "PARENTOF")
                {
                    className += ">";
                }
                else if (childNode.Text == "PRECEDES")
                {
                    className += "+";
                }
                else if (childNode.Text == "PSEUDO")
                {
                    className += ":" + childNode.FirstNode.Text;
                }
            }
            return className;
        }

        private void ReplaceRuleNode(TreeNode newNode)
        {
            foreach (TreeNode snode in _nodeFinal.Nodes)
            {
                if (snode.Text == "RULE" && GetRuleClassname(snode) == GetRuleClassname(newNode.Parent))
                {
                    foreach (TreeNode PNode in snode.Nodes)
                    {
                        if(PNode.Text == "PROPERTY" && PNode.FirstNode.Text == newNode.FirstNode.Text)
                        {
                            PNode.Remove();
                            snode.Nodes.Add(newNode);
                        }
                    }
                    
                }
            }
        }

        private static bool ValidateFirstNode(TreeNode childNode)
        {
            return childNode.Text == "CLASS" || childNode.Text == "TAG" || childNode.Text == "ANY";
        }


        /// -------------------------------------------------------------------------------------------
        /// <summary>
        ///  TO handle Multi-line synatx
        ///  .p1:before,
        ///  .p2:before,
        ///  .p3:before {content: "text"};
        /// </summary>
        /// <param name="node">Current TreeNode</param>
        /// <param name="nodeFine">Main TreeNode</param>
        /// -------------------------------------------------------------------------------------------
        private void ParseCommaRule(TreeNode node, TreeNode nodeFine)
        {
            var propertyNode = new TreeNode();
            var ruleNode = new TreeNode();
            foreach (TreeNode propNode in node.Nodes)
            {
                if (propNode.Text == "PROPERTY")
                {
                    propertyNode.Nodes.Add((TreeNode)propNode.Clone());
                }
            }
            foreach (TreeNode subNode in node.Nodes)
            {
                if (subNode.Text == ",")
                {
                    foreach (TreeNode item in propertyNode.Nodes)
                    {
                        ruleNode.Nodes.Add((TreeNode)item.Clone());
                    }
                    ruleNode.Text = "RULE";
                    var newRule = new Rule();
                    GetRuleContains(ruleNode, ref newRule);
                    ParseRule((TreeNode)ruleNode.Clone(), nodeFine);
                    ruleNode.Nodes.Clear();
                }
                else
                {
                    ruleNode.Nodes.Add((TreeNode)subNode.Clone());
                }
            }
            ParseRule((TreeNode)ruleNode.Clone(), nodeFine);
        }

        /// -------------------------------------------------------------------------------------------
        /// <summary>
        /// To insert the new page property which are not existing in the current node.
        /// </summary>
        /// <param name="parentName">Class name of the current treeenode</param>
        /// <param name="propertyName">Property name of the current treenode</param>
        /// <param name="propNode">Treenode of the current node</param>
        /// -------------------------------------------------------------------------------------------
        private void InsertMediaProperty(string parentName, string propertyName, TreeNode propNode)
        {
            foreach (TreeNode nodeFinalNode in _nodeFinal.Nodes)
            {
                if (nodeFinalNode.Text == "MEDIA")
                {
                    string mediaType = nodeFinalNode.FirstNode.Text;
                    foreach (TreeNode mainNode in nodeFinalNode.Nodes)
                    {
                        string attribName = string.Empty;
                        string attribValue = string.Empty;
                        if (mainNode.Text == "RULE")
                        {
                            foreach (TreeNode regionNode in mainNode.Nodes)
                            {
                                if (regionNode.Text == "ANY" && regionNode.FirstNode.Text == "ATTRIB")
                                {
                                    attribName = regionNode.FirstNode.FirstNode.Text;
                                    if (regionNode.FirstNode.Nodes.Count > 1)
                                    {
                                        attribValue = regionNode.FirstNode.LastNode.Text.Replace("\"", "");
                                        attribValue = attribValue.Replace("\'", "");
                                    }
                                }
                                else if (regionNode.Text == "PROPERTY")
                                {
                                    if (parentName == mediaType + attribName + attribValue && !_checkMediaNode.Contains(propertyName))
                                    {
                                        _checkMediaNode.Add(propertyName);
                                        mainNode.Nodes.Add(propNode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// To list the errors found in CSS inout file and error's are added into ErrorList.
        /// </summary>
        /// <param name="path">Input CSS Filepath</param>
        public void GetErrorReport(string path)
        {
            if(!File.Exists(path)) return;

            var ctp = new CssTreeParser();
            try
            {
                ctp.Parse(path);
                ctp.ValidateError();
                if (ctp.Errors.Count > 0)
                {
                    ErrorText += ctp.ErrorText() + "\r\n";
                    string fileName = Path.GetFileName(path);
                    if (ErrorList.ContainsKey(fileName))
                    {
                        ErrorList[fileName].Add(ctp.Errors);
                    }
                    else
                    {
                        var err = new ArrayList();
                        err.AddRange(ctp.Errors);
                        ErrorList[fileName] = err;
                    }
                }
            }
            catch (Exception)
            {
                ErrorText += ctp.Errors;
                throw;
            }
            finally
            {
              
            }

        }

        private void GetRuleContains(TreeNode node, ref Rule getRuleInfo)
        {
            getRuleInfo.NodeCount = node.Nodes.Count;
            foreach (TreeNode childNode in node.Nodes)
            {
                if (childNode.Text == "CLASS")
                {
                    if (OutputType != Common.OutputType.EPUB)
                    {
                        childNode.FirstNode.Text = childNode.FirstNode.Text.Replace("_", "").Replace("-", "");
                    }
                    getRuleInfo.ClassName += "." + childNode.FirstNode.Text;
                    if (childNode.Nodes.Count > 1)
                    {
                        for (int i = 0; i < childNode.Nodes.Count; i++)
                        {
                            if (childNode.Nodes[i].Text == "ATTRIB")
                            {
                                getRuleInfo.ClassName += "=" +
                                                         childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                             "\"", "");
                            }
                            else if (childNode.Nodes[i].Text == "HASVALUE")
                            {
                                getRuleInfo.ClassName += "~" +
                                                         childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                             "\"", "");
                            }
                        }
                    }
                }
                else if (childNode.Text == "TAG")
                {
                    getRuleInfo.ClassName += " " + childNode.FirstNode.Text;
                    if (childNode.Nodes.Count > 1)
                    {
                        for (int i = 0; i < childNode.Nodes.Count; i++)
                        {
                            if (childNode.Nodes[i].Text == "ATTRIB")
                            {
                                getRuleInfo.ClassName += "=" +
                                                         childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                             "\"", "");
                            }
                            else if (childNode.Nodes[i].Text == "HASVALUE")
                            {
                                getRuleInfo.ClassName += "~" +
                                                         childNode.Nodes[i].LastNode.Text.Replace("'", "").Replace(
                                                             "\"", "");
                            }
                        }
                    }
                }
                else if (childNode.Text == "ANY")
                {
                    if (childNode.FirstNode != null)
                    {
                        getRuleInfo.ClassName += "*" + childNode.FirstNode.Text;
                    }
                    else
                    {
                        getRuleInfo.ClassName += "*";
                    }
                    
                }
                else if (childNode.Text == "PARENTOF")
                {
                    getRuleInfo.ClassName += ">";
                }
                else if (childNode.Text == "PRECEDES")
                {
                    getRuleInfo.ClassName += "+";
                }
                else if (childNode.Text == "PSEUDO")
                {
                    getRuleInfo.PseudoName = childNode.FirstNode.Text;
                    getRuleInfo.ClassName += ":" + getRuleInfo.PseudoName;

                    getRuleInfo.IsPseudo = true;
                }
                else if (childNode.Text == "PROPERTY")
                {
                    getRuleInfo.HasProperty = true;
                    if (childNode.FirstNode.Text == "content")
                    {
                        if (!getRuleInfo.IsPseudo)
                        {
                            getRuleInfo.IsClassContent = true;
                        }
                        for (int i = 0; i < childNode.Nodes.Count; i++)
                        {
                            if ((!_isReCycle) && (childNode.Nodes[i].Text.IndexOf("'") >= 0 || childNode.Nodes[i].Text.IndexOf("\"") >= 0))
                            {
                                
                                childNode.Nodes[i].Text =  Common.UnicodeConversion(childNode.Nodes[i].Text);
                            }
                        }
                    }

                }
            }
        }
    }
}
