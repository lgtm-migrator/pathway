﻿// --------------------------------------------------------------------------------------------
#region // Copyright Â© 2009, SIL International. All Rights Reserved.
// <copyright file="PdfTest.cs" from='2009' to='2009' company='SIL International'>
//		Copyright Â© 2009, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
#endregion
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed: 
// 
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using SIL.PublishingSolution;
using NUnit.Framework;
using System.Windows.Forms;

namespace Test.CssDialog
{
    /// <summary>
    ///This is a test class for SettingsTest and is intended
    ///to contain all SettingsTest Unit Tests
    ///</summary>
    [TestFixture]
    public class PreviewTest
    {
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///A test for ShowPreview Constructor
        ///</summary>
        //[Test]
        public void PreviewConstructorTest()
        {
            Preview target = new Preview();
            //Assert.Inconclusive("TODO: Implement code to verify target");
        }

        /// <summary>
        ///A test for PdfPreview
        ///</summary>
        [Test]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void PdfPreviewTest()
        {
            Preview target = new Preview();
            Form myForm = new Form();
            target.ParentForm = myForm;
            CommonTestMethod.DisableDebugAsserts();
            target.PdfPreview();
            Assert.Fail("The given key was not present in the dictionary.");
        }

        /// <summary>
        ///A test for Show
        ///</summary>
        [Test]
        public void ShowTest()
        {
            Preview target = new Preview(); // TODO: Initialize to an appropriate value
            target.Show();
        }
    }
}