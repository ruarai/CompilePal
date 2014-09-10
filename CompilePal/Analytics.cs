using GoogleAnalyticsTracker.Simple;

namespace CompilePal
{
    class Analytics
    {
        public static void Startup()
        {
            try
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                    using (var tracker = new SimpleTracker("UA-46896943-4", "www.compilepal.com"))
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
                if (!System.Diagnostics.Debugger.IsAttached)
                    using (var tracker = new SimpleTracker("UA-46896943-4", "www.compilepal.com"))
                    {
                        tracker.TrackPageViewAsync("Compile Pal", version);
                    }
            }
            catch { }
        }

    }
}
