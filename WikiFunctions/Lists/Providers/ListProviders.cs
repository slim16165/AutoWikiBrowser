﻿/*
Copyright (C) 2007 Martin Richards
(C) 2008 Stephen Kennedy, Sam Reed

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
using System.Web;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml;
using WikiFunctions.Controls.Lists;

namespace WikiFunctions.Lists.Providers;
//TODO: normalise usage of FirstToUpperAndRemoveHashOnArray() and alikes

/// <summary>
/// Gets a list of pages in Named Categories for the ListMaker (Non-Recursive)
/// </summary>
public class CategoryListProvider : CategoryProviderBase
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string page in PrepareCategories(searchCriteria))
        {
            list.AddRange(GetListing(page, list.Count));
        }

        return list;
    }

    public override string DisplayText => "Category";
}

/// <summary>
/// Gets a list of pages in Named Categories for the ListMaker (Recursive - Will visit ALL subcategories)
/// </summary>
public class CategoryRecursiveListProvider : CategoryProviderBase
{
    #region Internals
    public const int MaxDepth = 30;

    int depth = MaxDepth;
    /// <summary>
    /// Maximum recursion depth during category scan
    /// </summary>
    public int Depth
    {
        get => depth;
        set => depth = Math.Min(value, MaxDepth);
    }
    #endregion

    public CategoryRecursiveListProvider()
        : this(MaxDepth)
    { }

    public CategoryRecursiveListProvider(int depth)
    {
        Depth = depth;
        Limit = 200000;
    }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        lock (Visited)
        {
            Visited.Clear();
            foreach (string page in PrepareCategories(searchCriteria))
            {
                list.AddRange(RecurseCategory(page, list.Count, Depth));
            }
            Visited.Clear();
        }

        return list;
    }

    public override string DisplayText => "Category (recursive)";
}

/// <summary>
/// Gets a list of pages in Named Categories for the ListMaker (Recursive - Will visit 1 level of subcategories)
/// </summary>
public class CategoryRecursiveOneLevelListProvider : CategoryRecursiveListProvider
{
    public CategoryRecursiveOneLevelListProvider()
        : base(1)
    { }

    public override string DisplayText => "Category (recurse 1 level)";
}

/// <summary>
/// Gets a list of pages in Named Categories for the ListMaker (Recursive - Will visit the specified number of levels of subcategories)
/// </summary>
public class CategoryRecursiveUserDefinedLevelListProvider : CategoryRecursiveListProvider
{
    public CategoryRecursiveUserDefinedLevelListProvider()
        : base(0)
    { }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        int userDepth = Tools.GetNumberFromUser(false, 1000);
        if (userDepth < 0)
            return new List<Article>();

        Depth = userDepth;

        return base.MakeList(searchCriteria);
    }

    public override string DisplayText => "Category (recurse user defined level)";
}

/// <summary>
/// Gets a list of Categories on the specified pages
/// </summary>
public class CategoriesOnPageListProvider : ApiListProviderBase
{
    protected string clshow;

    #region Tags: <categories>/<cl>
    static readonly List<string> pe = new List<string>(new[] { "cl" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "categories" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "prop=categories&cllimit=max&titles="
                         + HttpUtility.UrlEncode(page) + "&clshow=" + clshow;

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Categories on page";

    public override string UserInputTextBoxText => "Pages:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion
}

/// <summary>
/// Gets a List of Categories on a page, excluding hidden categories, includes categories provided to page by a template
/// </summary>
public class CategoriesOnPageNoHiddenListProvider : CategoriesOnPageListProvider
{
    public CategoriesOnPageNoHiddenListProvider()
    {
        clshow = "!hidden";
    }

    public override string DisplayText => base.DisplayText + " (no hidden cats)";
}

/// <summary>
/// Gets a List of only hidden Categories on a page
/// </summary>
public class CategoriesOnPageOnlyHiddenListProvider : CategoriesOnPageListProvider
{
    public CategoriesOnPageOnlyHiddenListProvider()
    {
        clshow = "hidden";
    }

    public override string DisplayText => base.DisplayText + " (only hidden cats)";
}

/// <summary>
/// Gets a list of pages which link to the Named Pages
/// </summary>
public class WhatLinksHereListProvider : ApiListProviderBase, ISpecialPageProvider
{
    public WhatLinksHereListProvider()
    { }

    public WhatLinksHereListProvider(int limit)
    {
        Limit = limit;
    }

    #region Tags: <backlinks>/<bl>
    static readonly List<string> pe = new List<string>(new[] { "bl" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "backlinks" });
    protected override ICollection<string> Actions => ac;

    #endregion

    protected bool IncludeWhatLinksToRedirects;
    protected string Blfilterredir;
    public string ForceQueryLimit { get; set; }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        return MakeList(Namespace.ToString(), searchCriteria);
    }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    protected List<Article> MakeList(string Namespace, params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "list=backlinks&bltitle="
                         + HttpUtility.UrlEncode(page) + "&blnamespace=" + Namespace;

            if (!string.IsNullOrEmpty(ForceQueryLimit))
            {
                url += "&bllimit=" + ForceQueryLimit;
            }
            else
            {
                url += "&bllimit=max";
            }
            if (IncludeWhatLinksToRedirects)
            {
                url += "&blredirect";
            }

            if (!string.IsNullOrEmpty(Blfilterredir))
            {
                url += "&blfilterredir=" + Blfilterredir;
            }

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "What links here";

    public override string UserInputTextBoxText => "What links to:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion

    #region ISpecialPageProvider Members

    public bool PagesNeeded => false;

    public bool NamespacesEnabled => true;

    #endregion
}

/// <summary>
/// Gets a list of pages (all ns's) from which link to the Named Pages
/// </summary>
public class WhatLinksHereAllNSListProvider : WhatLinksHereListProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList("", searchCriteria);
    }

    #region ListMaker properties
    public override string DisplayText => "What links here (all NS)";

    #endregion
}

/// <summary>
/// Gets a list of pages (all ns's) from which link to the Named Pages
/// (If linking page is a redirect, get pages which link to that also)
/// </summary>
public class WhatLinksHereAndToRedirectsAllNSListProvider : WhatLinksHereAllNSListProvider
{
    public WhatLinksHereAndToRedirectsAllNSListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereAndToRedirectsAllNSListProvider()
    {
        IncludeWhatLinksToRedirects = true;
    }

    public override string DisplayText => base.DisplayText + " (and to redirects)";
}

/// <summary>
/// Gets a list of pages which link to the Named Pages
/// (If linking page is a redirect, get pages which link to that also)
/// </summary>
public class WhatLinksHereAndToRedirectsListProvider : WhatLinksHereListProvider
{
    public WhatLinksHereAndToRedirectsListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereAndToRedirectsListProvider()
    {
        IncludeWhatLinksToRedirects = true;
    }

    public override string DisplayText => base.DisplayText + " (and to redirects)";
}

/// <summary>
/// Gets a list of non redirect pages which link to the Named Pages
/// (If linking page is a redirect, get pages which link to that also)
/// </summary>
public class WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider : WhatLinksHereListProvider
{
    public WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereAndPageRedirectsExcludingTheRedirectsListProvider()
    {
        Blfilterredir = "nonredirects";
        IncludeWhatLinksToRedirects = true;
    }

    public override string DisplayText => base.DisplayText + " directly";

    protected override bool EvaluateXmlElement(XmlTextReader xml)
    {
        return !xml.MoveToAttribute("redirect");
    }
}

/// <summary>
/// Gets a list of pages (excluding any redirects) which link to the Named Pages
/// </summary>
public class WhatLinksHereExcludingPageRedirectsListProvider : WhatLinksHereListProvider
{
    public WhatLinksHereExcludingPageRedirectsListProvider(int limit)
        : this()
    {
        Limit = limit;
    }

    public WhatLinksHereExcludingPageRedirectsListProvider()
    {
        Blfilterredir = "nonredirects";
    }

    public override string DisplayText => base.DisplayText + " (no redirects)";
}

/// <summary>
/// Gets a list of pages which redirect to the Named Pages
/// </summary>
public class RedirectsListProvider : WhatLinksHereListProvider
{
    public RedirectsListProvider()
    {
        Blfilterredir = "redirects";
    }

    public override string DisplayText => "What redirects here";

    public override string UserInputTextBoxText => "Redirects to:";
}

/// <summary>
/// Gets a list of pages which redirect to the Named Pages (in all NS's)
/// </summary>
public class RedirectsAllNSListProvider : WhatLinksHereAllNSListProvider
{
    public RedirectsAllNSListProvider()
    {
        Blfilterredir = "redirects";
    }

    public override string DisplayText => "What redirects here (all NS)";

    public override string UserInputTextBoxText => "Redirects to:";
}

/// <summary>
/// Gets a list of pages which transclude the Named Pages
/// </summary>
public class WhatTranscludesPageListProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <embeddedin>/<ei>
    static readonly List<string> pe = new List<string>(new[] { "ei" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "embeddedin" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        return MakeList(Namespace.ToString(), searchCriteria);
    }

    protected List<Article> MakeList(string Namespace, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "list=embeddedin&eititle="
                         + HttpUtility.UrlEncode(page) + "&eilimit=max&einamespace=" + Namespace;

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "What transcludes page";

    public override string UserInputTextBoxText => "What embeds:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected()
    { }
    #endregion

    public virtual bool PagesNeeded => true;

    public bool NamespacesEnabled => true;
}

/// <summary>
/// Gets a list of pages (all ns's) which transclude the Named Pages
/// </summary>
public class WhatTranscludesPageAllNSListProvider : WhatTranscludesPageListProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList("", searchCriteria);
    }

    public override string DisplayText => "What transcludes page (all NS)";
}

/// <summary>
/// Gets a list of all (red && blue) links on the Named Pages
/// </summary>
public class LinksOnPageListProvider : ApiListProviderBase
{
    #region Tags: <links>/<pl>
    static readonly List<string> pe = new List<string>(new[] { "pl" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "links" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "prop=links&titles="
                         + HttpUtility.UrlEncode(page) + "&pllimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Links on page";

    public override string UserInputTextBoxText => "Links on:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion
}

/// <summary>
/// Gets a list of all red links on the Named Pages
/// </summary>
public class LinksOnPageOnlyRedListProvider : ApiListProviderBase
{
    public LinksOnPageOnlyRedListProvider()
    {
        Limit = 5000; // Cant imagine a page having more than 5000 links...
    }

    #region Tags: <pages>/<page>
    static readonly List<string> pe = new List<string>(new[] { "page" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "pages" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "generator=links&titles="
                         + HttpUtility.UrlEncode(page) + "&gpllimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    protected override bool EvaluateXmlElement(XmlTextReader xml)
    {
        return xml.MoveToAttribute("missing");
    }

    public override string DisplayText => "Links on page (only redlinks)";

    public override string UserInputTextBoxText => "Links on:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
}

/// <summary>
/// Gets a list of all blue links on the Named Pages
/// </summary>
public class LinksOnPageOnlyBlueListProvider : LinksOnPageOnlyRedListProvider
{
    protected override bool EvaluateXmlElement(XmlTextReader xml)
    {
        return !base.EvaluateXmlElement(xml);
    }

    public override string DisplayText => "Links on page (only bluelinks)";
}

/// <summary>
/// Gets a list of all Images on the Named Pages
/// </summary>
public class FilesOnPageListProvider : ApiListProviderBase
{
    #region Tags: <images>/<im>
    static readonly List<string> pe = new List<string>(new[] { "im" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "images" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "prop=images&titles="
                         + HttpUtility.UrlEncode(page) + "&imlimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Files on page";

    public override string UserInputTextBoxText => "Files on:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion
}

/// <summary>
/// Gets a list of all the transclusions on the Named Pages
/// </summary>
public class TransclusionsOnPageListProvider : ApiListProviderBase
{
    #region Tags: <templates>/<tl>
    static readonly List<string> pe = new List<string>(new[] { "tl" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "templates" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "prop=templates&titles="
                         + HttpUtility.UrlEncode(page) + "&tllimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Transclusions on page";

    public override string UserInputTextBoxText => "Transclusions on:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion
}

/// <summary>
/// Gets the user contributions of the Named Users
/// </summary>
public class UserContribsListProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <usercontribs>/<item>
    static readonly List<string> pe = new List<string>(new[] { "item" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "usercontribs" });
    protected override ICollection<string> Actions => ac;

    #endregion

    protected string uclimit = "max";

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList("", searchCriteria);
    }

    public List<Article> MakeList(string @namespace, string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "list=usercontribs&ucuser=" +
                         Tools.WikiEncode(
                             Regex.Replace(page, Variables.NamespacesCaseInsensitive[Namespace.Category], ""))
                         + "&uclimit=" + uclimit
                         + "&ucnamespace=" + @namespace;

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "User contribs";

    public override string UserInputTextBoxText => Variables.Namespaces[Namespace.User];

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }

    public override bool RunOnSeparateThread => true;

    #endregion

    #region ISpecialPageProvider Members

    public List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        return MakeList(@namespace.ToString(), searchCriteria);
    }

    public bool PagesNeeded => true;

    public bool NamespacesEnabled => true;

    #endregion
}

/// <summary>
/// Gets the specified number of user contributions for the Named Users
/// </summary>
public class UserContribUserDefinedNumberListProvider : UserContribsListProvider
{
    public UserContribUserDefinedNumberListProvider()
    {
        UpperLimit = 25000;
    }

    protected int UpperLimit;
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        Limit = Tools.GetNumberFromUser(true, UpperLimit);
        uclimit = Limit.ToString();

        return base.MakeList(searchCriteria);
    }

    #region ListMaker properties
    public override string DisplayText => "User contribs (user defined number)";

    #endregion
}

/// <summary>
/// Gets a list of pages which link to the Named Images
/// </summary>
public class ImageFileLinksListProvider : ApiListProviderBase
{
    #region Tags: <imageusage>/<iu>
    static readonly List<string> pe = new List<string>(new[] { "iu" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "imageusage" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);

        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string image = Regex.Replace(page, "^" + Variables.Namespaces[Namespace.File],
                "", RegexOptions.IgnoreCase);
            image = HttpUtility.UrlEncode(image);

            string url = "list=imageusage&iutitle=Image:"
                         + image + "&iulimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Image file links";

    public override string UserInputTextBoxText => Variables.Namespaces[Namespace.File];

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion
}

/// <summary>
/// Gets a list of pages which are returned from a wiki search of the Named Pages
/// </summary>
/// <remarks>Slow query!!</remarks>
public class WikiSearchListProvider : ApiListProviderBase, ISpecialPageProvider
{
    protected string SearchType = "text", SearchPrefix = "";

    #region Tags: <search>/<p>
    static readonly List<string> pe = new List<string>(new[] { "p" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "search" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public WikiSearchListProvider()
    {
        Limit = 1000; // slow query
    }


    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(0, searchCriteria);
    }

    public List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url;
            if (SearchPrefix.Equals("all:"))
            {
                url = string.Format("list=search&srwhat={0}&srnamespace=*&srsearch={1}&srlimit=max",
                    SearchType,
                    HttpUtility.UrlEncode(page)
                );
            }
            else
            {
                url = string.Format("list=search&srwhat={0}&srnamespace={1}&srsearch={2}{3}&srlimit=max",
                    SearchType,
                    @namespace.ToString(),
                    SearchPrefix,
                    HttpUtility.UrlEncode(page)
                );
            }
            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Wiki search (text)";

    public override string UserInputTextBoxText => "Wiki search:";

    public override bool UserInputTextBoxEnabled => true;

    public virtual bool PagesNeeded => true;

    public bool NamespacesEnabled => true;

    public override void Selected() { }
    #endregion
}


/// <summary>
/// Gets a list of pages which are returned from a title wiki search of the Named Pages, across all namespaces
/// </summary>
public class WikiSearchAllNSListProvider : WikiSearchListProvider
{
    public WikiSearchAllNSListProvider()
    {
        SearchPrefix = "all:";
    }

    public override string DisplayText => base.DisplayText + " (all NS)";
}

/// <summary>
/// Gets a list of pages which are returned from a title wiki search of the Named Pages
/// </summary>
public class WikiTitleSearchListProvider : WikiSearchListProvider
{
    public WikiTitleSearchListProvider()
    {
        // SearchType = "title";
        SearchPrefix = "intitle:";
    }

    public override string DisplayText => "Wiki search (title)";
}

/// <summary>
/// Gets a list of pages which are returned from a title wiki search of the Named Pages, across all namespaces
/// </summary>
public class WikiTitleSearchAllNSListProvider : WikiTitleSearchListProvider
{
    public WikiTitleSearchAllNSListProvider()
    {
        SearchPrefix = "all:" + SearchPrefix;
    }

    public override string DisplayText => base.DisplayText + " (all NS)";
}

/// <summary>
/// Gets all the pages from the current user's watchlist
/// </summary>
public class MyWatchlistListProvider : ApiListProviderBase
{
    #region Tags: <watchlistraw>/<wr>
    static readonly List<string> pe = new List<string>(new[] { "wr" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "watchlistraw" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return ApiMakeList("list=watchlistraw&wrlimit=max", 0);
    }

    #region ListMaker properties
    public override string DisplayText => "My watchlist";

    public override string UserInputTextBoxText => "";

    public override bool UserInputTextBoxEnabled => false;

    public override void Selected() { }
    #endregion
}

/// <summary>
/// Runs the Database Scanner
/// </summary>
public class DatabaseScannerListProvider : IListProvider
{
    private readonly ListMaker LMaker;

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="lm">ListMaker for DBScanner to add articles to</param>
    public DatabaseScannerListProvider(ListMaker lm)
    {
        LMaker = lm;
    }

    public List<Article> MakeList(params string[] searchCriteria)
    {
        new DBScanner.DatabaseScanner(LMaker).Show();
        return null;
    }

    public virtual bool StripUrl => false;

    #region ListMaker properties
    public string DisplayText => "Database dump";

    public string UserInputTextBoxText => "";

    public bool UserInputTextBoxEnabled => false;

    public void Selected() { }

    public bool RunOnSeparateThread => false;

    #endregion
}

/// <summary>
/// Gets 100 random articles
/// </summary>
public class RandomPagesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    protected string Extra;
    public RandomPagesSpecialPageProvider()
    {
        Limit = 100;
    }

    #region Tags: <random>/<page>
    static readonly List<string> pe = new List<string>(new[] { "page" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "random" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        string url = "list=random&rnnamespace=" + Namespace +
                     "&rnlimit=max" + Extra;

        list.AddRange(ApiMakeList(url, list.Count));
        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Random pages";

    public override string UserInputTextBoxText => "";

    public override bool UserInputTextBoxEnabled => false;

    public override void Selected() { }
    #endregion

    public bool PagesNeeded => false;

    public bool NamespacesEnabled => true;
}

/// <summary>
/// Gets 100 random redirects
/// </summary>
public class RandomRedirectsSpecialPageProvider : RandomPagesSpecialPageProvider
{
    public RandomRedirectsSpecialPageProvider()
    {
        Extra = "&rnredirect";
    }

    public override string DisplayText => "Random redirects";
}

/// <summary>
/// Returns a list of "all pages" in a namespace
/// </summary>
public class AllPagesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <allpages>/<p>
    static readonly List<string> pe = new List<string>(new[] { "p" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "allpages" });
    protected override ICollection<string> Actions => ac;

    #endregion

    protected string From = "apfrom", Extra;

    #region ISpecialPageProvider Members

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "list=allpages&" + From + "=" +
                         HttpUtility.UrlEncode(page) + "&apnamespace=" + Namespace + "&aplimit=max" + Extra;

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    public override string UserInputTextBoxText => DisplayText;

    public virtual bool PagesNeeded => false;

    #endregion

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }

    public override string DisplayText => "All Pages";

    public virtual bool NamespacesEnabled => true;
}

/// <summary>
/// Returns a list of "all categories"
/// </summary>
public class AllCategoriesSpecialPageProvider : AllPagesSpecialPageProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Category, searchCriteria);
    }

    public override string DisplayText => "All Categories";

    public override string UserInputTextBoxText => "Start Cat.:";

    public override bool NamespacesEnabled => false;
}

/// <summary>
/// Returns a list of "all files"
/// </summary>
public class AllFilesSpecialPageProvider : AllPagesSpecialPageProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.File, searchCriteria);
    }

    public override string DisplayText => "All Files";

    public override string UserInputTextBoxText => "Start File:";

    public override bool NamespacesEnabled => false;
}

/// <summary>
/// Returns a list of "all redirects"
/// </summary>
public class AllRedirectsSpecialPageProvider : AllPagesSpecialPageProvider
{
    public AllRedirectsSpecialPageProvider()
    {
        Extra = "&apfilterredir=redirects";
    }

    public override string DisplayText => "All Redirects";

    public override string UserInputTextBoxText => "Start Redirect:";
}

/// <summary>
/// Returns a list of "all pages", without the redirects
/// </summary>
public class AllPagesNoRedirectsSpecialPageProvider : AllPagesSpecialPageProvider
{
    public AllPagesNoRedirectsSpecialPageProvider()
    {
        Extra = "&apfilterredir=nonredirects";
    }

    public override string DisplayText => base.DisplayText + " (no redirects)";

    public override string UserInputTextBoxText => "Start page:";
}

/// <summary>
/// Returns a list of protected pages
/// </summary>
public class ProtectedPagesSpecialPageProvider : AllPagesSpecialPageProvider
{
    private readonly ProtectionLevel Protlevel = new ProtectionLevel();

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public override List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        Protlevel.ShowDialog();
        Extra = "&apprtype=" + Protlevel.Type + "&apprlevel=" + Protlevel.Level;
        return base.MakeList(Namespace, searchCriteria);
    }

    public override string DisplayText => "Protected Pages";

    public override string UserInputTextBoxText => "Pages:";
}

/// <summary>
/// Returns a list of pages without language links
/// </summary>
public class PagesWithoutLanguageLinksSpecialPageProvider : AllPagesSpecialPageProvider
{
    public PagesWithoutLanguageLinksSpecialPageProvider()
    {
        Extra = "&apfilterlanglinks=withoutlanglinks";
    }

    public override string DisplayText => "Pages without Language Links";

    public override string UserInputTextBoxText => "Pages:";
}
    
/// <summary>
/// Returns a list of pages without language links, with no redirects
/// </summary>
public class PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider : PagesWithoutLanguageLinksSpecialPageProvider
{
    public PagesWithoutLanguageLinksNoRedirectsSpecialPageProvider()
    {
        Extra += "&apfilterredir=nonredirects";
    }

    public override string DisplayText => base.DisplayText + " (no redirects)";
}

/// <summary>
/// Returns a list of subpages for the specified page
/// </summary>
public class PrefixIndexSpecialPageProvider : AllPagesSpecialPageProvider
{
    public PrefixIndexSpecialPageProvider()
    {
        From = "apprefix";
    }

    public override string DisplayText => "All Pages with prefix (Prefixindex)";

    public override bool PagesNeeded => true;
}

/// <summary>
/// Returns a list of recent changes, by default in the 0 namespace
/// </summary>
public class RecentChangesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <recentchanges>/<rc>
    static readonly List<string> pe = new List<string>(new[] { "rc" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "recentchanges" });
    protected override ICollection<string> Actions => ac;

    #endregion

    #region ISpecialPageProvider Members
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string page in searchCriteria)
        {
            string url = "list=recentchanges&rctitles=" + HttpUtility.UrlEncode(page) + "&rcnamespace=" + Namespace + "&rclimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    public override string DisplayText => "Recent Changes";

    public bool PagesNeeded => false;

    #endregion

    public override string UserInputTextBoxText => DisplayText;

    public override bool UserInputTextBoxEnabled => false;

    public override void Selected() { }

    public bool NamespacesEnabled => true;
}

/// <summary>
/// Returns a list of all users (their user pages) on the wiki
/// </summary>
public class AllUsersSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    public AllUsersSpecialPageProvider()
    {
        WantedAttribute = "name";
    }

    #region Tags: <allusers>/<u>
    static readonly List<string> pe = new List<string>(new[] { "u" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "allusers" });
    protected override ICollection<string> Actions => ac;

    #endregion

    #region ISpecialPageProvider Members
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int NamespaceIn, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        list.AddRange(Tools.ConvertNamespace(ApiMakeList("list=allusers&aulimit=max", list.Count), Namespace.User));

        return list;
    }

    public override string DisplayText => "All Users";

    public bool PagesNeeded => false;

    #endregion

    public override string UserInputTextBoxText => "";

    public override bool UserInputTextBoxEnabled => false;

    public override void Selected() { }

    public bool NamespacesEnabled => false;
}

/// <summary>
/// Returns a list of new pages, by default in the 0 namespace
/// </summary>
/// <remarks>
/// Slow(ish) query! Api has:
/// ApiBase::PARAM_MAX => ApiBase::LIMIT_BIG1
/// ApiBase::PARAM_MAX2 => ApiBase::LIMIT_BIG2
/// </remarks>
public class NewPagesListProvider : ApiListProviderBase, ISpecialPageProvider
{
    public NewPagesListProvider()
    {
        Limit = 500;
    }

    #region Tags: <recentchanges>/<rc>
    static readonly List<string> pe = new List<string>(new[] { "rc" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "recentchanges" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        string url = "list=recentchanges"
                     + "&rclimit=max&rctype=new&rcshow=!redirect&rcnamespace=" + Namespace;

        list.AddRange(ApiMakeList(url, list.Count));

        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "New pages";

    public override string UserInputTextBoxText => "";

    public override bool UserInputTextBoxEnabled => false;

    public override void Selected() { }
    #endregion

    public bool PagesNeeded => false;

    public bool NamespacesEnabled => true;
}

/// <summary>
/// Returns a list of pages that contain the specified URL
/// </summary>
public class LinkSearchSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <exturlusage>/<eu>
    static readonly List<string> pe = new List<string>(new[] { "eu" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "exturlusage" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return MakeList(Namespace.Article, searchCriteria);
    }

    public List<Article> MakeList(int Namespace, params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string searchUrl in searchCriteria)
        {
            int index = searchUrl.IndexOf("://", StringComparison.Ordinal);

            string protocol, urlEnd;

            if (index > -1)
            {
                protocol = searchUrl.Substring(0, index);
                urlEnd = searchUrl.Substring(index + 3);
            }
            else
            {
                protocol = "";
                urlEnd = searchUrl;
            }


            string url = "list=exturlusage&euquery=" +
                         HttpUtility.UrlEncode(urlEnd) + "&eunamespace=" + Namespace +
                         "&euprotocol=" + protocol + "&eulimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }

        return list;
    }

    #region ListMaker properties
    public override string DisplayText => "Link search";

    public override string UserInputTextBoxText => "URL:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion

    public bool PagesNeeded => true;

    public bool NamespacesEnabled => true;
}

/// <summary>
/// Returns a list of disambiguation pages
/// </summary>
public class DisambiguationPagesSpecialPageProvider : WhatTranscludesPageListProvider
{
    public override List<Article> MakeList(params string[] searchCriteria)
    {
        return base.MakeList(Namespace.Article, new[] { "Template:Disambiguation" });
    }

    public override List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        return base.MakeList(@namespace, new[] { "Template:Disambiguation" });
    }

    public override string DisplayText => "Disambiguation Pages";

    public override string UserInputTextBoxText => "";

    public override bool UserInputTextBoxEnabled => false;

    public override bool PagesNeeded => false;
}

/// <summary>
/// Returns a list of new files
/// </summary>
/// <remarks>Slow query!</remarks>
public class GalleryNewFilesSpecialPageProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <logevents>/<item>
    static readonly List<string> pe = new List<string>(new[] { "item" });
    protected override ICollection<string> PageElements => pe;

    static readonly List<string> ac = new List<string>(new[] { "logevents" });
    protected override ICollection<string> Actions => ac;

    #endregion

    public GalleryNewFilesSpecialPageProvider()
    {
        Limit = 1000; // slow query
    }

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        list.AddRange(ApiMakeList("list=logevents&letype=upload&lelimit=max", list.Count));

        return list;
    }

    public List<Article> MakeList(int @namespace, string[] searchCriteria)
    {
        return MakeList("");
    }

    #region ListMaker properties
    public override string DisplayText => "New files";

    public override string UserInputTextBoxText => "";

    public override bool UserInputTextBoxEnabled => false;

    public override void Selected() { }
    #endregion

    public bool PagesNeeded => false;

    public bool NamespacesEnabled => false;
}

public class PagesWithPropListProvider : ApiListProviderBase, ISpecialPageProvider
{
    #region Tags: <pageswithprop>/<page>
    protected override ICollection<string> PageElements
    {
        get { return new[] { "page" }; }
    }

    protected override ICollection<string> Actions
    {
        get { return new[] {"pageswithprop"}; }
    }
    #endregion

    public override List<Article> MakeList(params string[] searchCriteria)
    {
        List<Article> list = new List<Article>();

        foreach (string prop in searchCriteria)
        {
            string url = "list=pageswithprop&pwppropname="
                         + HttpUtility.UrlEncode(prop) + "&pwplimit=max";

            list.AddRange(ApiMakeList(url, list.Count));
        }
        return list;
    }

    public List<Article> MakeList(int @namespace, params string[] searchCriteria)
    {
        return MakeList(searchCriteria);
    }

    #region ListMaker properties
    public override string DisplayText => "Pages with a page property";

    public override string UserInputTextBoxText => "Property name:";

    public override bool UserInputTextBoxEnabled => true;

    public override void Selected() { }
    #endregion

    public bool PagesNeeded => true;
    public bool NamespacesEnabled => false;
}