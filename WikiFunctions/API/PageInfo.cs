﻿/*
Copyright (C) 2009 Max Semenik

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

using System.Xml;
using System.IO;
using System;
namespace WikiFunctions.API;

/// <summary>
/// This class represents information about the page currently being edited
/// </summary>
public sealed class PageInfo
{
    internal PageInfo()
    {
    }

    // TODO: adopt for retrieval of information for protection, deletion, etc.
    internal PageInfo(string xml)
    {
        XmlReader xr = XmlReader.Create(new StringReader(xml));

        string normalisedFrom = null, redirectFrom = null;

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);

        var redirects = doc.GetElementsByTagName("r");

        if (redirects.Count > 0) // We have redirects
        {
            var first = redirects[0].Attributes;
            var last = redirects[redirects.Count - 1].Attributes;
            if (first != null && last != null && (first["from"].Value == last["to"].Value ||
                                                  last["from"].Value == last["to"].Value))
            {
                // Redirect loop
                TitleChangedStatus = PageTitleStatus.RedirectLoop;
                OriginalTitle = Title = first["from"].Value;
                Exists = true;
                Text = "";
                return; // We're not going to have any page text as there is a redirect loop
            }

            redirectFrom = first != null ? first["from"].Value : "";
            // Valid redirects
            TitleChangedStatus = redirects.Count == 1
                ? PageTitleStatus.Redirected
                : PageTitleStatus.MultipleRedirects;
        }
        else
        {
            TitleChangedStatus = PageTitleStatus.NoChange;
        }

        string curtimestamp = "";
        if(xr.ReadToFollowing("api"))
            curtimestamp = xr.GetAttribute("curtimestamp");

        if (!xr.ReadToFollowing("page"))
        {
            if (redirects.Count > 0)
            {
                // If there are redirects, but no page element, chances are it's a redirect to IW or something
                // similar
                return;
            }
            throw new Exception("Cannot find <page> element");
        }

        // Normalised before redirect, so would be root. Could still be multiple redirects, or looped
        var normalised = doc.GetElementsByTagName("n");

        if (normalised.Count > 0 && normalised[0].Attributes != null)
        {
            normalisedFrom = normalised[0].Attributes["from"].Value;

            if (TitleChangedStatus == PageTitleStatus.NoChange)
                TitleChangedStatus = PageTitleStatus.Normalised;
            else
                TitleChangedStatus |= PageTitleStatus.Normalised;
        }

        // Normalisation occurs before redirection, so if that exists, that is the title passed to the API
        if (!string.IsNullOrEmpty(normalisedFrom))
        {
            OriginalTitle = normalisedFrom;
        }
        else if (!string.IsNullOrEmpty(redirectFrom))
        {
            OriginalTitle = redirectFrom;
        }

        Exists = (xr.GetAttribute("missing") == null); //if null, page exists
        IsWatched = (xr.GetAttribute("watched") != null);

        var tokens = doc.GetElementsByTagName("tokens");
        if (tokens.Count == 0)
        {
            // Token support for < 1.24
            EditToken = xr.GetAttribute("edittoken");
            ProtectToken = xr.GetAttribute("protecttoken");
            DeleteToken = xr.GetAttribute("deletetoken");
            MoveToken = xr.GetAttribute("movetoken");
            WatchToken = xr.GetAttribute("watchtoken");
        }
        else if (tokens[0].Attributes != null)
        {
            EditToken = tokens[0].Attributes["csrftoken"].Value;
            ProtectToken = tokens[0].Attributes["csrftoken"].Value;
            DeleteToken = tokens[0].Attributes["csrftoken"].Value;
            MoveToken = tokens[0].Attributes["csrftoken"].Value;
            WatchToken = tokens[0].Attributes["watchtoken"].Value;
            RollbackToken = tokens[0].Attributes["rollbacktoken"].Value;
        }

        // if UseInToken = false then won't be given starttimestamp, so use curtimestamp instead
        TokenTimestamp = xr.GetAttribute("starttimestamp");
        if(string.IsNullOrEmpty(TokenTimestamp))
            TokenTimestamp = curtimestamp;

        long revId;
        RevisionID = long.TryParse(xr.GetAttribute("lastrevid"), out revId) ? revId : -1;

        Title = xr.GetAttribute("title");
        DisplayTitle = xr.GetAttribute("displaytitle");
        var ns = xr.GetAttribute("ns");
        NamespaceID = ns != null ? int.Parse(ns) : 0;

        if (xr.ReadToDescendant("protection") && !xr.IsEmptyElement)
        {
            foreach (XmlNode xn in doc.GetElementsByTagName("pr"))
            {
                switch (xn.Attributes["type"].Value)
                {
                    case "edit":
                        EditProtection = xn.Attributes["level"].Value;
                        break;
                    case "move":
                        MoveProtection = xn.Attributes["level"].Value;
                        break;
                    case "create":
                        CreateProtection = xn.Attributes["level"].Value;
                        break;
                }
            }
        }
       
        xr.ReadToFollowing("revisions");

        xr.ReadToDescendant("rev");
        Timestamp = xr.GetAttribute("timestamp");

        // API returns \n line endings, we have standardized on \r\n (including under Mono)
        Text = xr.ReadString().Replace("\n", "\r\n");
    }

    /// <summary>
    /// Display title of the Page in HTML format, used e.g. if page has some italics (using {{italic title}} etc.)
    /// </summary>
    public string DisplayTitle
    { get; private set; }

    /// <summary>
    /// Title of the Page
    /// </summary>
    public string Title
    { get; private set; }

    /// <summary>
    /// Original title (before redirects/normalisation) of the Page
    /// </summary>
    public string OriginalTitle
    { get; private set; }

    /// <summary>
    /// Why OriginalTitle differs from Title
    /// </summary>
    public PageTitleStatus TitleChangedStatus 
    { get; private set; }

    /// <summary>
    /// Text of the Page
    /// </summary>
    public string Text
    { get; private set; }

    /// <summary>
    /// Whether the page exists or not
    /// </summary>
    public bool Exists
    { get; private set; }

    /// <summary>
    /// Revision ID, -1 if N/A
    /// </summary>
    public long RevisionID
    { get; private set; }

    /// <summary>
    /// Namespace number
    /// </summary>
    public int NamespaceID
    { get; private set; }

    /// <summary>
    /// Timestamp of the latest revision of the page
    /// </summary>
    public string Timestamp
    { get; private set; }

    /// <summary>
    /// Edit token (https://www.mediawiki.org/wiki/Manual:Edit_token)
    /// </summary>
    public string EditToken
    { get; internal set; }

    /// <summary>
    /// Delete Token
    /// </summary>
    public string DeleteToken
    { get; internal set; }

    /// <summary>
    /// Protect Token
    /// </summary>
    public string ProtectToken
    { get; internal set; }

    /// <summary>
    /// Move Token
    /// </summary>
    public string MoveToken
    { get; internal set; }

    /// <summary>
    /// Watch Token
    /// </summary>
    public string WatchToken
    { get; internal set; }

    /// <summary>
    /// Rollback Token
    /// </summary>
    public string RollbackToken
    { get; internal set; }

    /// <summary>
    /// Time when the token was obtained. Used for deletion detection.
    /// </summary>
    public string TokenTimestamp
    { get; private set; }

    /// <summary>
    /// String of any edit protection applied to the page
    /// </summary>
    public string EditProtection
    { get; private set; }

    /// <summary>
    /// String of any move protection applied to the page
    /// </summary>
    public string MoveProtection
    { get; private set; }
        
    /// <summary>
    /// String of any create protection applied to the page
    /// </summary>
    public string CreateProtection
    { get; private set; }

    /// <summary>
    /// Whether the current user is watching this page
    /// </summary>
    public bool IsWatched
    { get; set; }

    /// <summary>
    /// Was the specified PageInfo redirected to get to the final target
    /// </summary>
    /// <param name="page">PageInfo object</param>
    /// <returns>Whether the article was redirected</returns>
    public static bool WasRedirected(PageInfo page)
    {
        PageTitleStatus pts = page.TitleChangedStatus;

        if (pts == PageTitleStatus.NoChange)
            return false;

        return ((pts & PageTitleStatus.Redirected) == PageTitleStatus.Redirected ||
                (pts & PageTitleStatus.RedirectLoop) == PageTitleStatus.RedirectLoop ||
                (pts & PageTitleStatus.MultipleRedirects) == PageTitleStatus.MultipleRedirects);
    }
}

/// <summary>
/// 
/// </summary>
[Flags]
public enum PageTitleStatus
{
    NoChange = 0,
    RedirectLoop = 1,
    MultipleRedirects = 2,
    Redirected = 4,
    Normalised = 8,
}