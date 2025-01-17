﻿namespace WikiFunctions.Controls;

public partial class PageNotContainsControl : PageContainsControl
{
    public PageNotContainsControl()
    {
        InitializeComponent();
    }

    public override bool Matches(Article article)
    {
        return !base.Matches(article);
    }

    public override string SkipReason => "Page doesn't contain: " + txtContains.Text;
}