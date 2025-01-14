﻿// --------------------------------------------------------------------------------------------
// <copyright file="InStyles.cs" from='2009' to='2014' company='SIL International'>
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
// Creates the InDesign Styles file 
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using SIL.Tool;

namespace SIL.PublishingSolution
{
    public class InStyles : InStyleBase
    {
        #region Private Variables

        Dictionary<string, Dictionary<string, string>> _cssProperty = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, Dictionary<string, string>> _IDAllClass = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> _IDProperty = new Dictionary<string, string>();
        private Dictionary<string, string> _IDClass = new Dictionary<string, string>();
        InMapProperty mapProperty = new InMapProperty();
        private bool _isWidowOrphanDisabled;
        
        #endregion

        public Dictionary<string, Dictionary<string, string>> CreateIDStyles(string projectPath, Dictionary<string, Dictionary<string, string>> cssProperty)
        {
            try
            {
                _cssProperty = cssProperty;
                CheckPropertyForWidowsOrphans();
                CreateFile(projectPath);
                string headerFontName = GetHeaderFontName();
                CreateRootCharacterStyleGroup(headerFontName);
                CreateCharacterStyle();
                CreateRootParagraphStyleGroup(headerFontName);
                CreateParagraphStyle();
                CreateTOCStyle();
                CreateRootCellStyleGroup();
                CreateRootTableStyleGroup();
                CreateRootObjectStyleGroup();
                CreateTrapPreset();
                EndIDStyles();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
            return _IDAllClass;
        }

        private void CheckPropertyForWidowsOrphans()
        {
            if (_cssProperty["@page"].ContainsKey("-ps-disable-widow-orphan"))
            {
                _isWidowOrphanDisabled = bool.Parse(_cssProperty["@page"]["-ps-disable-widow-orphan"]);
            }
        }

        private string GetHeaderFontName()
        {
            string headerFontName = "Times New Roman";
            if (_cssProperty.ContainsKey("div_.zxx") && _cssProperty["div_.zxx"].ContainsKey("font-family"))
            {
                headerFontName = _cssProperty["div_.zxx"]["font-family"];
            }
            else if (_cssProperty.ContainsKey("headword") && _cssProperty["headword"].ContainsKey("font-family"))
            {
                headerFontName = _cssProperty["headword"]["font-family"];
            }
            return headerFontName;
        }

        /// <summary>
        /// Increase font-size 250% for Subscript and Superscript
        /// </summary>
        /// <param name="isIncrease">to increase font-size even the property is not super/sub script</param>
        private void SuperscriptSubscriptIncreaseFontSize(bool isIncrease)
        {
            bool isSuperSub = _IDProperty.ContainsKey("Position") && (_IDProperty["Position"] == "Subscript" || _IDProperty["Position"] == "Superscript");

            if (isSuperSub || isIncrease) // increase font-size for superscipt & subscript
            {
                string newValue = "100%";
                if (_IDProperty.ContainsKey("PointSize"))
                {
                    string fontValue = _IDProperty["PointSize"];
                    int counter;
                    string retValue = Common.GetNumericChar(fontValue, out counter);
                    if (retValue.Length > 0)
                    {
                        float value = float.Parse(retValue) * 1.0F;
                        string unit = fontValue.Substring(counter);
                        newValue = value + unit;
                    }
                    else
                    {
                        if (fontValue == "larger" || fontValue == "smaller")
                        {
                            newValue = fontValue;
                        }
                    }
                }
                _IDProperty["PointSize"] = newValue;
            }
        }

        private void CreateParagraphStyle()
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> cssClass in _cssProperty)
            {
                _writer.WriteStartElement("ParagraphStyle");
                _writer.WriteAttributeString("Self", "ParagraphStyle/" + cssClass.Key);
                _writer.WriteAttributeString("Name", cssClass.Key);
                _writer.WriteAttributeString("Imported", "false");
                _writer.WriteAttributeString("NextStyle", "ParagraphStyle/" + cssClass.Key);
                _writer.WriteAttributeString("KeyboardShortcut", "0 0");
                DisableWidowsandOrphans(cssClass, _isWidowOrphanDisabled);
                _IDProperty = mapProperty.IDProperty(cssClass.Value);
                InsertKeepWithNextForSectionHead(cssClass);
                SuperscriptSubscriptIncreaseFontSize(false);
                DeleteRelativeInFootnote(cssClass);
                PositionProperty();

                _IDClass = new Dictionary<string, string>(); // note: ToDo seperate the process
                _IDAllClass[cssClass.Key] = _IDClass;

                foreach (KeyValuePair<string, string> property in _IDProperty)
                {
                    if (property.Key == "AppliedFont")
                    {
                        _IDClass[property.Key] = property.Value;
                        continue;
                    }
                    if (property.Key == "StrokeColor")
                    {
                        _IDClass[property.Key] = property.Value;
                        InsertBackgroundColor(property.Value);
                    }
                    else
                    {
                        _IDClass[property.Key] = property.Value;
                        _writer.WriteAttributeString(property.Key, property.Value);
                    }
                }
                _writer.WriteStartElement("Properties");
                _writer.WriteStartElement("BasedOn");
                _writer.WriteAttributeString("type", "string");
                _writer.WriteString("$ID/[No paragraph style]");
                _writer.WriteEndElement();
                _writer.WriteStartElement("PreviewColor");
                _writer.WriteAttributeString("type", "enumeration");
                _writer.WriteString("Nothing");
                _writer.WriteEndElement();
                CreateParagraphProperty("AppliedFont", "string");
                string propertyType = Common.GetLeadingType(_IDProperty);
                CreateParagraphProperty("Leading", propertyType);
                _writer.WriteEndElement();

                _writer.WriteEndElement();
            }
            _writer.WriteEndElement(); //End RootParagraphStyleGroup
            CalcColumnWidth();
        }

        private static void DisableWidowsandOrphans(KeyValuePair<string, Dictionary<string, string>> cssClass, bool disableWidowOrphan)
        {
            if (disableWidowOrphan)
            {
                if (cssClass.Value.ContainsKey("widows"))
                {
                    cssClass.Value.Remove("widows");
                }

                if (cssClass.Value.ContainsKey("orphans"))
                {
                    cssClass.Value.Remove("orphans");
                }
            }
        }

        private void CalcColumnWidth()
        {
            try
            {
                if (_cssProperty.ContainsKey("columns") && _cssProperty["columns"].ContainsKey("column-count"))
                {
                    string columnCount = _cssProperty["columns"]["column-count"];
                    string pageWidth = _cssProperty["@page"]["page-width"];
                    string pageLeftMargins = _cssProperty["@page"]["margin-left"];
                    string pageRightMargins = _cssProperty["@page"]["margin-right"];
                    string columnGap = "20";
                    if (columnCount == "1")
                    {
                        columnGap = "3";
                        Common.ColumnWidth = (Convert.ToDouble(pageWidth) -
                                             (Convert.ToDouble(pageLeftMargins) + Convert.ToDouble(columnGap) +
                                               Convert.ToDouble(pageRightMargins)));
                    }
                    else if (columnCount == "2")
                    {
                        Common.ColumnWidth = ((Convert.ToDouble(pageWidth)/2) -
                                              (Convert.ToDouble(pageLeftMargins) + Convert.ToDouble(columnGap) +
                                               Convert.ToDouble(pageRightMargins)));
                    }
                    else
                    {
                        columnGap = "3";
                        if (_cssProperty["columns"].ContainsKey("column-gap"))
                            columnGap = _cssProperty["@page"]["column-gap"];

                        Common.ColumnWidth = (Convert.ToDouble(pageWidth) -
                                              (Convert.ToDouble(pageLeftMargins) + Convert.ToDouble(columnGap) +
                                               Convert.ToDouble(pageRightMargins)));
                    }
                }
            }
            catch
            {
            }
        }

        private void InsertKeepWithNextForSectionHead(KeyValuePair<string, Dictionary<string, string>> cssClass)
        {
            if (cssClass.Key.ToLower() == "sectionhead" || cssClass.Key.ToLower() == "parallelpassagereference")
            {
                _IDProperty["KeepWithNext"] = "2";
            }
        }

        private void DeleteRelativeInFootnote(KeyValuePair<string, Dictionary<string, string>> cssClass)
        {
            if (cssClass.Key.IndexOf("..footnote-cal") > 0 || cssClass.Key.IndexOf("..footnote-marker") > 0)
            {
                ArrayList removeProperty = new ArrayList();
                foreach (KeyValuePair<string, string> property in _IDProperty)
                {
                    if (property.Value.IndexOf("%") > 0)
                    {
                        removeProperty.Add(property.Key);
                    }
                }
                foreach (string property in removeProperty)
                {
                    _IDProperty.Remove(property);
                }
            }
        }

        private void CreateCharacterStyle()
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> cssClass in _cssProperty)
            {
                if (cssClass.Value.ContainsKey("display") && (cssClass.Value["display"] == "footnote"
                                                               || cssClass.Value["display"] == "prince-footnote"))
                {

                    _writer.WriteStartElement("CharacterStyle");
                    _writer.WriteAttributeString("Self", "CharacterStyle/" + cssClass.Key);
                    _writer.WriteAttributeString("Name", cssClass.Key);
                    _writer.WriteAttributeString("Imported", "false");
                    _writer.WriteAttributeString("NextStyle", "CharacterStyle/" + cssClass.Key);
                    _writer.WriteAttributeString("KeyboardShortcut", "0 0");
                    _IDProperty = mapProperty.IDProperty(cssClass.Value);
                    SuperscriptSubscriptIncreaseFontSize(true);
                    DeleteRelativeInFootnote(cssClass);
                    
                    _IDClass = new Dictionary<string, string>(); // note: ToDo seperate the process
                    _IDAllClass[cssClass.Key] = _IDClass;

                    foreach (KeyValuePair<string, string> property in _IDProperty)
                    {
                        if (property.Key == "AppliedFont")
                        {
                            _IDClass[property.Key] = property.Value;
                            continue;
                        }
                        if (property.Key == "StrokeColor")
                        {
                            _IDClass[property.Key] = property.Value;
                            InsertBackgroundColor(property.Value);
                        }
                        else
                        {
                            _IDClass[property.Key] = property.Value;
                            _writer.WriteAttributeString(property.Key, property.Value);
                        }
                    }
                    _writer.WriteStartElement("Properties");
                    _writer.WriteStartElement("BasedOn");
                    _writer.WriteAttributeString("type", "string");
                    _writer.WriteString("$ID/[No paragraph style]");
                    _writer.WriteEndElement();
                    _writer.WriteStartElement("PreviewColor");
                    _writer.WriteAttributeString("type", "enumeration");
                    _writer.WriteString("Nothing");
                    _writer.WriteEndElement();
                    _writer.WriteStartElement("Leading");
                    _writer.WriteAttributeString("type", "enumeration");
                    _writer.WriteString("Auto");
                    _writer.WriteEndElement();
                    _writer.WriteEndElement();
                    _writer.WriteEndElement();
                }
            }
            _writer.WriteEndElement(); //End of RootCharacterStyleGroup
        }



        private void PositionProperty()
        {
            //Note: Paragraph Margins are not Completed.
            //Note: Currently "left" and "right" are added to padding because of Indesign 
            //Note: which does not have property for Paragraph Margin.
            //Note: Also It does not support the Negative Values in Margin. Everything restricted within the frames.

                if (_IDProperty.ContainsKey("position") && (_IDProperty.ContainsKey("left") || _IDProperty.ContainsKey("right")) )
                {
                    _IDProperty.Remove("position");

                    if (_IDProperty.ContainsKey("left"))
                    {
                        if (_IDProperty.ContainsKey("LeftIndent"))
                        {
                            _IDProperty["LeftIndent"] = (int.Parse(_IDProperty["LeftIndent"]) 
                                                        + int.Parse(_IDProperty["left"])).ToString();
                        }
                        else
                        {
                            _IDProperty["LeftIndent"] = _IDProperty["left"];
                        }
                    }
                    else if (_IDProperty.ContainsKey("right"))
                    {
                        if (_IDProperty.ContainsKey("RightIndent"))
                        {
                            _IDProperty["RightIndent"] = (int.Parse(_IDProperty["RightIndent"]) 
                                                         + int.Parse(_IDProperty["right"])).ToString();
                        }
                        else
                        {
                            _IDProperty["RightIndent"] = _IDProperty["right"];
                        }
                    }
                }

                if (_IDProperty.ContainsKey("position"))
                {
                    _IDProperty.Remove("position");
                }
                if (_IDProperty.ContainsKey("left"))
                {
                    _IDProperty.Remove("left");
                }
                if (_IDProperty.ContainsKey("right"))
                {
                    _IDProperty.Remove("right");
                }
        }

        private void CreateParagraphProperty(string propertyName, string propertyType)
        {
            if (_IDProperty.ContainsKey(propertyName))
            {
                _writer.WriteStartElement(propertyName);
                _writer.WriteAttributeString("type", propertyType);
                _writer.WriteString(_IDProperty[propertyName]);
                _writer.WriteEndElement();
            }
        }

        private void InsertBackgroundColor(string propertyValue)
        {
            _IDClass["StrokeWeight"] = "1";
            _IDClass["StrokeColor"] = propertyValue;
            _IDClass["EndJoin"] = "BevelEndJoin";

            _writer.WriteAttributeString("StrokeWeight", "1");
            _writer.WriteAttributeString("StrokeColor", propertyValue);
            _writer.WriteAttributeString("EndJoin", "BevelEndJoin");
        }
    }
}