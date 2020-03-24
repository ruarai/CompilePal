# Collection of Custom compile step programs
This is a curated list of helpful programs that can be run as custom compile steps. Feel free to open an issue to add your program to the list.

- Da Spud Lord's collection - [Download](https://tf2maps.net/posts/453521/)
  - CompileChecklist - Displays a messagebox with a pre-compile checklist. 
  This list can be customized fairly easily to display whatever reminders one may find useful, such as a reminder to enable clipping visgroups.
  
  - SteamCheck: Checks if Steam is running, and if not, launches Steam. 
  Primarily useful for cubemap generating, as TF2 will not launch into the map if Steam is not running.
  Shutdown: An enhanced version of CompilePal's native shutdown feature, this gives the user a chance to cancel the shutdown with a pop-up (if, perhaps, you change your mind on shutting down your PC). 
  The default delay of 10 seconds can be adjusted with the parameter -delay <seconds>, 
  and you also have the option of putting your computer into hibernate instead of a full shutdown with -hibernate.
  
  - GameKiller: Closes your game if it is running, to ensure CompilePal doesn't try to launch a second instance of the game when doing cubemaps.
