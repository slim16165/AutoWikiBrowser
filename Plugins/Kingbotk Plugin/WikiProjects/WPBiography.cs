﻿/*
Copyright (C) 2008 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/
Copyright (C) 2008 Sam Reed (Reedy) http://www.reedyboy.net/

This program is free software; you can redistribute it and/or modify it under the terms of Version 2 of the GNU General Public License as published by the Free Software Foundation.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License Version 2 along with this program; if not, write to the Free Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using WikiFunctions;

namespace AutoWikiBrowser.Plugins.Kingbotk.WikiProjects;

internal enum Living
{
    Unknown,
    Living,
    Dead
}

internal sealed class WPBiography : PluginBase
{
    internal WPBiography()
        : base("WPBiography|Wpbiography|WPBIO|WP Biography|WPbiography|Wikiproject Biography|WP Bio|Bio")
    {
        // Specify alternate names only

        _ourSettingsControl = new BioWithWorkgroups(PluginName, Prefix, true, _params);
    }

    private const string PluginName = "WikiProject Biography";

    private const string Prefix = "Bio";
    private const string WorkgroupsGroups = "Work Groups";

    private const string OthersGroup = "Others";

    private readonly TemplateParameters[] _params =
    {
        new TemplateParameters
        {
            StorageKey = "PolWG",
            Group = WorkgroupsGroups,
            ParamName = "Politician"
        },
        new TemplateParameters
        {
            StorageKey = "ArtsEntsWG",
            Group = WorkgroupsGroups,
            ParamName = "A&E"
        },
        new TemplateParameters
        {
            StorageKey = "FilmWG",
            Group = WorkgroupsGroups,
            ParamName = "Film Bio"
        },
        new TemplateParameters
        {
            StorageKey = "MilitaryWG",
            Group = WorkgroupsGroups,
            ParamName = "Military"
        },
        new TemplateParameters
        {
            StorageKey = "PeerageWG",
            Group = WorkgroupsGroups,
            ParamName = "Peerage"
        },
        new TemplateParameters
        {
            StorageKey = "RoyaltyWG",
            Group = WorkgroupsGroups,
            ParamName = "Royalty"
        },
        new TemplateParameters
        {
            StorageKey = "MusiciansWG",
            Group = WorkgroupsGroups,
            ParamName = "Musician"
        },
        new TemplateParameters
        {
            StorageKey = "ScienceWG",
            Group = WorkgroupsGroups,
            ParamName = "S&A"
        },
        new TemplateParameters
        {
            StorageKey = "SportWG",
            Group = WorkgroupsGroups,
            ParamName = "Sports"
        },
        new TemplateParameters
        {
            StorageKey = "LivingPerson",
            Group = OthersGroup,
            ParamName = "Living"
        },
        new TemplateParameters
        {
            StorageKey = "NotLivingPerson",
            Group = OthersGroup,
            ParamName = "Not Living"
        },
        new TemplateParameters
        {
            StorageKey = "ActivePol",
            Group = OthersGroup,
            ParamName = "Active Politician"
        }
    };

    // Settings:
    private readonly TabPage _ourTab = new TabPage("Biography");

    private readonly GenericWithWorkgroups _ourSettingsControl;

    protected internal override string PluginShortName => "Biography";

    protected override string PreferredTemplateName => PluginName;

    protected override void ImportanceParameter(Importance importance)
    {
    }

    protected internal override IGenericSettings GenericSettings => _ourSettingsControl;

    internal override bool HasReqPhotoParam => true;

    internal override void ReqPhoto()
    {
        AddNewParamWithAYesValue("needs-photo");
    }

    // Initialisation:
    protected internal override void Initialise()
    {
        OurMenuItem = new ToolStripMenuItem("Biography Plugin");
        InitialiseBase();
        // must set menu item object first
        _ourTab.UseVisualStyleBackColor = true;
        _ourTab.Controls.Add(_ourSettingsControl);
    }

    // Article processing:
    protected override bool SkipIfContains()
    {
        return false;
    }

    protected override void ProcessArticleFinish()
    {
        Living living = Living.Unknown;

        StubClass();

        foreach (ListViewItem lvi in _ourSettingsControl.ListView1.Items)
        {
            if (lvi.Checked)
            {
                TemplateParameters tp = (TemplateParameters) lvi.Tag;

                if (tp.Group == WorkgroupsGroups)
                {
                    string param = tp.ParamName.ToLower().Replace(" ", "-");
                    AddAndLogNewParamWithAYesValue(param + "-work-group");
                    // Probably needs some reformatting
                    AddEmptyParam(param + "-priority");
                }
                else
                {
                    switch (tp.ParamName)
                    {
                        case "Not Living":
                            living = Living.Dead;
                            break;
                        case "Living":
                            living = Living.Living;
                            break;
                        default:
                            AddAndLogNewParamWithAYesValue(tp.ParamName.ToLower().Replace(" ", "-"));
                            break;
                    }
                }
            }
        }

        switch (living)
        {
            case Living.Living:
                if (!Template.HasYesParamLowerOrTitleCase(true, "living"))
                {
                    AddAndLogNewParamWithAYesValue("living");
                }
                break;
            case Living.Dead:
                if (!Template.HasYesParamLowerOrTitleCase(false, "living"))
                {
                    Template.NewOrReplaceTemplateParm("living", "no", TheArticle, true, false, false, "", true);
                }
                break;
            case Living.Unknown:
                Template.NewOrReplaceTemplateParm("living", "", TheArticle, false, false, true, "", true);
                break;
        }

        if (TheArticle.Namespace == Namespace.Talk && TheArticle.ProcessIt && !PluginManager.BotMode)
        {
            // Since we're dealing with talk pages, we want a listas= even if it's the same as the
            // article title without namespace (otherwise it sorts to namespace)
            Template.NewOrReplaceTemplateParm("listas", Tools.MakeHumanCatKey(TheArticle.FullArticleTitle, TheArticle.AlteredArticleText), TheArticle, true, false, true, "");
        }
    }

    /// <summary>
    /// Send the template to the plugin for preinspection
    /// </summary>
    /// <returns>False if OK, TRUE IF BAD TAG</returns>
    protected override bool TemplateFound()
    {
        if (Template.Parameters.ContainsKey("importance"))
        {
            Template.Parameters.Remove("importance");
            TheArticle.ArticleHasAMinorChange();
        }

        if (Template.Parameters.ContainsKey("priority"))
        {
            string priorityValue = Template.Parameters["priority"].Value;

            foreach (KeyValuePair<string, Templating.TemplateParametersObject> kvp in Template.Parameters)
            {
                if (kvp.Key.Contains("-priority") && !string.IsNullOrEmpty(kvp.Value.Value))
                {
                    kvp.Value.Value = priorityValue;
                }
            }

            Template.Parameters.Remove("priority");
            TheArticle.ArticleHasAMinorChange();
        }

        return false;
    }

    protected override string WriteTemplateHeader()
    {
        string res = "{{" + PluginName + Environment.NewLine;

        if (Template.Parameters.ContainsKey("living"))
        {
            Template.Parameters["living"].Value = Template.Parameters["living"].Value.ToLower();
            res += "|living=" + Template.Parameters["living"].Value + ParameterBreak;

            Template.Parameters.Remove("living");
            // we've written this parameter; if we leave it in the collection PluginBase.TemplateWritingAndPlacement() will write it again
        }
        if (TheArticle.Namespace == Namespace.Talk)
        {
            res += WriteOutParameterToHeader("class");
        }

        return res;
    }

    // User interface:
    protected override void ShowHideOurObjects(bool visible)
    {
        PluginManager.ShowHidePluginTab(_ourTab, visible);
    }

    // XML settings:
    protected internal override void ReadXML(XmlTextReader reader)
    {
        Enabled = PluginManager.XMLReadBoolean(reader, Prefix + "Enabled", Enabled);

        _ourSettingsControl.ReadXML(reader);
    }

    protected internal override void Reset()
    {
        _ourSettingsControl.Reset();
    }

    protected internal override void WriteXML(XmlTextWriter writer)
    {
        writer.WriteAttributeString(Prefix + "Enabled", Enabled.ToString());
        _ourSettingsControl.WriteXML(writer);
    }
}