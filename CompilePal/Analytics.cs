using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GoogleAnalyticsTracker.Simple;
using GoogleAnalyticsTracker.Core;

namespace CompilePal
{
    class Analytics
    {
        public static void Startup()
        {
            try
            {
                using (SimpleTracker tracker = new SimpleTracker("UA-46896943-4", "www.compilepal.com"))
                {
                    tracker.TrackPageViewAsync("Compile Pal", "");
                }
            }
            catch { }
        }

        public static void Version(string version)
        {
            try
            {
                using (SimpleTracker tracker = new SimpleTracker("UA-46896943-4", "www.compilepal.com"))
                {
                    tracker.TrackPageViewAsync("Compile Pal", version);
                }
            }
            catch { }
        }
        
    }
}
