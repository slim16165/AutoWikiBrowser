﻿/*
(c) 2008 Stephen Kennedy, Sam Reed

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using WikiFunctions.Lists;

namespace WikiFunctions.Lists
{
    /// <summary>
    /// An interface implemented by objects which attach to the WikiFunctions' ListMaker combo box and return lists of articles
    /// </summary>
    public interface IListMakerProvider
    {
        /// <summary>
        /// Process the user input (if any) and return a list of articles
        /// </summary>
        /// <param name="userInput"></param>
        /// <returns></returns>
        List<Article> MakeList(string[] userInput); // TODO: This may need to use param, or accept one string and perform string splitting within the encapsulated code

        /// <summary>
        /// The text to display as the combobox list item
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// The text to display inside the Select Source text box
        /// </summary>
        string SelectSourceTextBoxText { get; }

        /// <summary>
        /// Indicates whether the Select Source text box should be enabled
        /// </summary>
        bool SelectSourceTextBoxEnabled { get;}

        /// <summary>
        /// Called when the ListMaker Provider has been selected in the ComboBox
        /// </summary>
        void Selected();

        /// <summary>
        /// True if the object expects to be started on a seperate thread
        /// </summary>
        bool RunOnSeperateThread { get;}
    }

    // TODO: Move elsewhere when finished
    // TODO: Document me (just a header for each class saying what it does)
    #region ListMakerProviders
    internal class CategoryListMakerProvider : IListMakerProvider
    {
        protected bool subCats = false;

        //public virtual List<Article> Search(string searchCriteria)
        //{
        //    return Search(searchCriteria.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
        //}

        public virtual List<Article> MakeList(string[] searchCriteria)
        {
            searchCriteria = Tools.RegexReplaceOnArray(searchCriteria, "^" + Variables.NamespacesCaseInsensitive[14], "");

            return GetLists.FromCategory(subCats, searchCriteria);
        }

        public virtual string DisplayText
        { get { return "Category"; } }

        public string SelectSourceTextBoxText
        { get { return Variables.Namespaces[14]; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class CategoryRecursiveListMakerProvider : CategoryListMakerProvider
    {
        public override List<Article> MakeList(string[] searchCriteria)
        {
            GetLists.QuietMode = true;
            subCats = true;
            List<Article> ret = base.MakeList(searchCriteria);
            GetLists.QuietMode = false;

            return ret;
        }

        public override string DisplayText
        { get { return "Category (recursive)"; } }
    }

    internal sealed class TextFileListMakerProvider : IListMakerProvider
    {
        private OpenFileDialog openListDialog;

        public TextFileListMakerProvider()
        {
            openListDialog = new OpenFileDialog();
            openListDialog.Filter = "text files|*.txt|All files|*.*";
            openListDialog.Multiselect = true;
        }

        public List<Article> MakeList(string[] searchCriteria)
        {
            List<Article> ret = new List<Article>();
            try
            {
                if (openListDialog.ShowDialog() == DialogResult.OK)
                {
                    ret = GetLists.FromTextFile(openListDialog.FileNames);
                }
                return ret;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
                return ret;
            }
        }

        public string DisplayText
        { get { return "Text File"; } }

        public string SelectSourceTextBoxText
        { get { return "From file:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return false; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return false; } }
    }

    internal class WhatLinksHereListMakerProvider : IListMakerProvider
    {
        public virtual List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromWhatLinksHere(false, searchCriteria); }

        public virtual string DisplayText
        { get { return "What links here"; } }

        public virtual string SelectSourceTextBoxText
        { get { return "What links to:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class WhatLinksHereIncludingRedirectsListMakerProvider : WhatLinksHereListMakerProvider
    {
        public override List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromWhatLinksHere(false, true, searchCriteria); }

        public override string DisplayText
        { get { return base.DisplayText + " (inc. Redirects)"; } }
    }

    internal sealed class WhatTranscludesPageListMakerProvider : WhatLinksHereListMakerProvider
    {
        public override List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromWhatLinksHere(true, searchCriteria); }

        public override string DisplayText
        { get { return "What transcludes page"; } }

        public override string SelectSourceTextBoxText
        { get { return "What embeds:"; } }
    }

    internal sealed class LinksOnPageListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromLinksOnPage(searchCriteria); }

        public string DisplayText
        { get { return "Links on page"; } }

        public string SelectSourceTextBoxText
        { get { return "Links on:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class ImagesOnPageListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromImagesOnPage(searchCriteria); }

        public string DisplayText
        { get { return "Images on page"; } }

        public string SelectSourceTextBoxText
        { get { return "Images on:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class TransclusionsOnPageListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromTransclusionsOnPage(searchCriteria); }

        public string DisplayText
        { get { return "Transclusions on page"; } }

        public string SelectSourceTextBoxText
        { get { return "Transclusions on:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; }
        }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class GoogleSearchListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromGoogleSearch(searchCriteria); }

        public string DisplayText
        { get { return "Google Search"; } }

        public string SelectSourceTextBoxText
        { get { return "Google Search:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal class UserContribsListMakerProvider : IListMakerProvider
    {
        protected bool all = false;

        public virtual List<Article> MakeList(string[] searchCriteria)
        {
            searchCriteria = Tools.RegexReplaceOnArray(searchCriteria, "^" + Variables.NamespacesCaseInsensitive[2], "");

            return GetLists.FromUserContribs(all, searchCriteria);
        }

        public virtual string DisplayText
        { get { return "User contribs"; } }

        public string SelectSourceTextBoxText
        { get { return Variables.Namespaces[2]; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class UserContribsAllListMakerProvider : UserContribsListMakerProvider
    {
        public override List<Article> MakeList(string[] searchCriteria)
        {
            all = false;
            return base.MakeList(searchCriteria);
        }

        public override string DisplayText
        { get { return base.DisplayText + " (all)"; } }
    }

    internal sealed class SpecialPageListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        {
            searchCriteria = Tools.RegexReplaceOnArray(searchCriteria, "^" + Variables.NamespacesCaseInsensitive[-1], "");

            return GetLists.FromSpecialPage(searchCriteria);
        }

        public string DisplayText
        { get { return "Special page"; } }

        public string SelectSourceTextBoxText
        { get { return Variables.Namespaces[-1]; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class ImageFileLinksListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        {
            searchCriteria = Tools.RegexReplaceOnArray(searchCriteria, "^" + Variables.NamespacesCaseInsensitive[6], "");

            return GetLists.FromImageLinks(searchCriteria);
        }

        public string DisplayText
        { get { return "Image file links"; } }

        public string SelectSourceTextBoxText
        { get { return Variables.Namespaces[6]; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class WikiSearchListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromWikiSearch(searchCriteria); }

        public string DisplayText
        { get { return "Wiki search"; } }

        public string SelectSourceTextBoxText
        { get { return "Wiki search:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class RedirectsListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromRedirects(searchCriteria); }

        public string DisplayText
        { get { return "Redirects"; } }

        public string SelectSourceTextBoxText
        { get { return "Redirects to:"; } }

        public bool SelectSourceTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    internal sealed class MyWatchlistListMakerProvider : IListMakerProvider
    {
        public List<Article> MakeList(string[] searchCriteria)
        { return GetLists.FromWatchList(); }

        public string DisplayText
        { get { return "My Watchlist"; } }

        public string SelectSourceTextBoxText
        { get { return ""; } }

        public bool SelectSourceTextBoxEnabled
        { get { return false; } }

        public void Selected() { }

        public bool RunOnSeperateThread
        { get { return true; } }
    }

    //public class DatabaseScannerListMakerProvider : IListMakerProvider
    //{
    //    #region IListMakerProvider Members

    //    public List<Article> Search(string[] searchCriteria)
    //    { return new List<Article>(); }

    //    public string DisplayText
    //    { get { return "Database dump"; } }

    //    public string SelectSourceTextBoxText
    //    { get { return ""; } }

    //    public bool SelectSourceTextBoxEnabled
    //    { get { return false; } }

    //    public void Selected() { }

    //    public bool IsThreaded
    //    { get { return false; } }

    //    #endregion
    //}
    #endregion
}
