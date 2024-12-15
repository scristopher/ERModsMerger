using ERModsMerger.Core;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ERModsManager.UCs
{
    /// <summary>
    /// Logique d'interaction pour LogsUC.xaml
    /// </summary>
    public partial class LogsUC : UserControl
    {

        public LogsUC()
        {
            InitializeComponent();
            LOG.NewLog += LOG_NewLog;
        }

        private void LOG_NewLog(NewLogEventArgs args)
        {
            
            Application.Current.Dispatcher.Invoke(
              new Action(() => {
                  this.TxtLogs.Text += "\n" + args.Log.Message;
                  this.LogScrollViewer.ScrollToBottom();

                  this.LogsStackPanel.Children.Add(new LogItemUC(args.Log));
              }));
            
        }
    }
}
