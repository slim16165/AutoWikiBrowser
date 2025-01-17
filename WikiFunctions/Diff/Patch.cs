﻿/*
 * Patches, a supporting class for Diffs
 */

using System.Collections;

namespace WikiFunctions;

public class Patch : IEnumerable
{
    private readonly Hunk[] hunks;

    internal Patch(Hunk[] hunks)
    {
        this.hunks = hunks;
    }

    public class Hunk
    {
        private readonly object[] rightData;
        private readonly int leftstart, leftcount, rightstart, rightcount;
        private readonly bool same;

        internal Hunk(object[] rightData, int st, int c, int rs, int rc, bool s)
        {
            this.rightData = rightData;
            leftstart = st;
            leftcount = c;
            rightstart = rs;
            rightcount = rc;
            same = s;
        }

        public bool Same => same;

        public int Start => leftstart;

        public int Count => leftcount;

        public int End => leftstart + leftcount - 1;

        public IList Right => same ? null : new Range(rightData, rightstart, rightcount);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return hunks.GetEnumerator();
    }

    public IList Apply(IList original)
    {
        ArrayList right = new ArrayList();
        foreach (Hunk hunk in this)
        {
            if (hunk.Same)
                right.AddRange(new Range(original, hunk.Start, hunk.Count));
            else
                right.AddRange(hunk.Right);
        }
        return right;
    }

}