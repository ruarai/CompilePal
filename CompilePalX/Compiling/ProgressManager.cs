using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace CompilePalX
{
    internal delegate void OnTitleChange(string title);
    internal delegate void OnProgressChange(double progress);
    static class ProgressManager
    {
        public static event OnTitleChange TitleChange;
        public static event OnProgressChange ProgressChange;

        private static TaskbarItemInfo taskbarInfo;
        private static bool ready;
        private static string defaultTitle = "Compile Pal";

        static public void Init(TaskbarItemInfo _taskbarInfo)
        {
            taskbarInfo = _taskbarInfo;
            ready = true;

            TitleChange(
	            $"{defaultTitle} {UpdateManager.CurrentVersion}X {GameConfigurationManager.GameConfiguration.Name}");
        }


        static public double Progress
        {
            get
            {
                return taskbarInfo.Dispatcher.Invoke(() => { return ready ? taskbarInfo.ProgressValue : 0; });
            }
            set { SetProgress(value); }
        }

        static public void SetProgress(double progress)
        {
            if (ready)
            {
                taskbarInfo.Dispatcher.Invoke(() =>
                {
                    taskbarInfo.ProgressState = TaskbarItemProgressState.Normal;

                    taskbarInfo.ProgressValue = progress;
                    ProgressChange(progress * 100);

                    if (progress >= 1)
                    {
                        TitleChange($"{Math.Floor(progress * 100d)}% - {defaultTitle} {UpdateManager.CurrentVersion}X");

                        if (ConfigurationManager.Settings.PlaySoundOnCompileCompletion)
                        {
                            System.Media.SystemSounds.Exclamation.Play();
                        }
                    }
                    else if (progress <= 0)
                    {
                        taskbarInfo.ProgressState = TaskbarItemProgressState.None;
                        TitleChange(
	                        $"{defaultTitle} {UpdateManager.CurrentVersion}X {GameConfigurationManager.GameConfiguration.Name}");
                    }
                    else
                    {
                        TitleChange($"{Math.Floor(progress * 100d)}% - {defaultTitle} {UpdateManager.CurrentVersion}X");
                    }
                });

            }
        }

        static public void ErrorProgress()
        {
            taskbarInfo.Dispatcher.Invoke(() =>
                                          {
                                              if (ready)
                                              {
                                                  SetProgress(1);
                                                  taskbarInfo.ProgressState = TaskbarItemProgressState.Error;
                                              }
                                          });

        }

        static public void PingProgress()
        {
            taskbarInfo.Dispatcher.Invoke(() =>
                                          {
                                              if (ready)
                                              {
                                                  if (taskbarInfo.ProgressValue >= 1)
                                                      SetProgress(0);
                                              }
                                          });
        }
    }
}
