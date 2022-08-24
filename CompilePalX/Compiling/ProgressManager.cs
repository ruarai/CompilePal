using System;
using System.Media;
using System.Windows.Shell;

namespace CompilePalX
{
    delegate void OnTitleChange(string title);
    delegate void OnProgressChange(double progress);
    static class ProgressManager
    {

        private static TaskbarItemInfo taskbarInfo;
        private static bool ready;
        private static readonly string defaultTitle = "Compile Pal";


        public static double Progress
        {
            get => taskbarInfo.Dispatcher.Invoke(() => { return ready ? taskbarInfo.ProgressValue : 0; });
            set => SetProgress(value);
        }
        public static event OnTitleChange TitleChange;
        public static event OnProgressChange ProgressChange;

        public static void Init(TaskbarItemInfo _taskbarInfo)
        {
            taskbarInfo = _taskbarInfo;
            ready = true;

            TitleChange(
                $"{defaultTitle} {UpdateManager.CurrentVersion}X {GameConfigurationManager.GameConfiguration.Name}");
        }

        public static void SetProgress(double progress)
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

                        SystemSounds.Exclamation.Play();
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

        public static void ErrorProgress()
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

        public static void PingProgress()
        {
            taskbarInfo.Dispatcher.Invoke(() =>
            {
                if (ready)
                {
                    if (taskbarInfo.ProgressValue >= 1)
                    {
                        SetProgress(0);
                    }
                }
            });
        }
    }
}
