using GoogleAnalyticsTracker.Simple;

namespace CompilePal
{
    class Analytics
    {
        public static void Startup()
        {
            try
            {
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
                using (var tracker = new SimpleTracker("UA-46896943-4", "www.compilepal.com"))
                {
                    tracker.TrackPageViewAsync("Compile Pal", version);
                }
            }
            catch { }
        }
        
    }
}
