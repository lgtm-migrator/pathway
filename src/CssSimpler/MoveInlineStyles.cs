﻿// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2016, SIL International. All Rights Reserved.
// <copyright from='2016' to='2016' company='SIL International'>
//		Copyright (c) 2016, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MoveInlineStyles.cs
// Responsibility: Greg Trihus
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.Tool;

namespace CssSimpler
{
    public class MoveInlineStyles : XmlCopy
    {

        public MoveInlineStyles(string input, string output, string cssName)
            : base(input, output, false)
        {
            DeclareBefore(XmlNodeType.Element, ResetClassName);
			DeclareBefore(XmlNodeType.Element, Program.EntryReporter);
			DeclareBefore(XmlNodeType.Attribute, LookForStyle);
	        SpaceClass = null;
            Parse();
            var sr = new StreamReader(cssName);
            var outName = cssName + "~Out";
            var sw = new StreamWriter(outName);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                sw.WriteLine(line);
            }
            foreach (var key in SavedStyles.Keys)
            {
                sw.WriteLine("." + key + " { " + SavedStyles[key] + " }");
            }
            sw.Close();
            sr.Close();
            File.Copy(outName, cssName, true);
            File.Delete(outName);
        }

        private string _currentClass = String.Empty;
        private void ResetClassName(XmlReader r)
        {
            _currentClass = String.Empty;
        }

        protected  Dictionary<string, string> SavedStyles = new Dictionary<string, string>();
        protected string LastClass = string.Empty;
        private void LookForStyle(XmlReader r)
        {
            if (r.Name == "class")
            {
                LastClass = r.Value;
                _currentClass = LastClass;
            }
            else if (r.Name == "style")
            {
                var newClass = LastClass;
	            if (_currentClass == string.Empty)
	            {
		            //Style in xhtml file inline style
					newClass = "stxfin" + newClass;
	            }

	            var count = 0;
                while  (SavedStyles.ContainsKey(newClass))
                {
                    if (SavedStyles[newClass] != r.Value)
                    {
                        count += 1;
	                    // ReSharper disable once UseStringInterpolation
						newClass = string.Format("stxfin{0}{1}", LastClass, count);
                    }
                    else
                    {
                        break;
                    }
                }
                if (!SavedStyles.ContainsKey(newClass))
                {
                    SavedStyles[newClass] = r.Value;
                }
                if (_currentClass == string.Empty)
                {
                    WriteClassAttr(newClass);
                }
                SkipAttr = true;
            }
        }
    }
}
