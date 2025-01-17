﻿using WikiFunctions.Plugin;

namespace WikiFunctions.Plugins.ListMaker.NoLimitsPlugin;

/// <summary>
/// 
/// </summary>
internal class Base : IAWBBasePlugin
{
    internal static IAutoWikiBrowser AWB;

    public void Initialise(IAutoWikiBrowser sender)
    {
        AWB = sender;
    }

    public string Name => "No Limits Plugin";

    public string WikiName => "[[WP:AWB|" + Name + "]]";

    public void LoadSettings(object[] prefs)
    {
    }

    public object[] SaveSettings()
    {
        return new object[0];
    }

    public static bool CanUsePlugin()
    {
        if (AWB.TheSession.User.IsBot || AWB.TheSession.User.IsSysop || AWB.TheSession.User.HasApiHighLimit)
            return true;

        Tools.MessageBox("Action only allowed for Admins and Bot accounts, or those with the \"apihighlimits\" right");
        return false;
    }
}