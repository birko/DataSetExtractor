using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataSetExtractor
{
    /// <summary>
    /// Interaction logic for StatusWindow.xaml
    /// </summary>
    public partial class StatusWindow : Window
    {
        public System.ComponentModel.BackgroundWorker Worker { get; set; }

        public StatusWindow(int max)
        {
            InitializeComponent();
            SetMaxStatus(max);
        }

        public void SetMaxStatus(int max)
        {
            if (progressBarGeenerate.Dispatcher.CheckAccess())
            {
                progressBarGeenerate.Maximum = max;
            }
            else
            {
                progressBarGeenerate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                {
                    progressBarGeenerate.Maximum = max;
                }));
            }
        }

        public void Update(int progress, string text)
        {
            UpdateProgress(progress);
            UpdateText(text);
        }

        private void UpdateProgress(int progress)
        {
            if (progressBarGeenerate.Dispatcher.CheckAccess())
            {
                progressBarGeenerate.Value = progress;
            }
            else
            {
                progressBarGeenerate.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                {
                    progressBarGeenerate.Value = progress;
                }));
            }
        }

        private void UpdateText(string text)
        {
            if (labelGenerateText.Dispatcher.CheckAccess())
            {
                labelGenerateText.Content = text;
            }
            else
            {
                labelGenerateText.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
                {
                    labelGenerateText.Content = text;
                }));
            }
        }

        public void SafeClose()
        {
            if (Worker == null || !Worker.IsBusy)
            {
                if (Dispatcher.CheckAccess())
                {
                    Close();
                }
                else
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Close();
                    }));
                }
            }
        }

        public void Start(Action workFunction = null,  Action closeFunction = null)
        {
            if (workFunction != null)
            {
                Worker = new System.ComponentModel.BackgroundWorker()
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true,
                };
                Worker.DoWork += (sender, e) => {
                    workFunction.Invoke();
                };
                Worker.RunWorkerCompleted += (sender, e) => {
                    SafeClose();
                    if (e.Error != null)
                    {
                        throw e.Error;
                    }
                    else if(closeFunction != null)
                    {
                        closeFunction?.Invoke();
                    }
                };
                Worker.ProgressChanged += (sender, e) => {
                    Update(e.ProgressPercentage, (string)e.UserState);
                };
                Worker.RunWorkerAsync();
            }
            ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //if (Worker != null && !Worker.IsBusy)
            //{
            //    Worker = null;
            //}
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Worker != null && Worker.IsBusy)
            {
                e.Cancel = true;
            }
        }
    }
}
