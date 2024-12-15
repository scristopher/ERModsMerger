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

namespace ERModsManager.UCs
{
    /// <summary>
    /// Logique d'interaction pour LogItemUC.xaml
    /// </summary>
    public partial class LogItemUC : UserControl
    {
        public LOG? Log {  get; set; }

        private string _title = "";
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                TitleTxtBlock.Text = _title;
            }
        }

        private LOGTYPE _type = LOGTYPE.INFO;
        public LOGTYPE Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                SetLogoType(_type);
            }
        }

        private double _progress = 0;
        public double Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                _progress = value;
                SetProgress(_progress);
            }
        }

        public LogItemUC()
        {
            InitializeComponent();
        }

        public LogItemUC(LOG log)
        {
            InitializeComponent();
            Log = log;
            Title = log.Message;
            Type = log.Type;

            Log.PropertyChanged += Log_PropertyChanged;
            Log.SubLogAdded += Log_SubLogAdded;
        }

        private void Log_SubLogAdded(LOG log)
        {
            Application.Current.Dispatcher.Invoke(
             new Action(() => {
                 SubLogsStackPanel.Children.Add(new LogItemUC(log));
             }));
        }

        private void Log_PropertyChanged(LOG log)
        {
            Application.Current.Dispatcher.Invoke(
            new Action(() => {
                Title = log.Message;
                Type = log.Type;
                Progress = log.Progress;
            }));
        }

        private void SetLogoType(LOGTYPE type)
        {
            ImgError.Visibility = Visibility.Hidden;
            ImgInfo.Visibility = Visibility.Hidden;
            ImgWarning.Visibility = Visibility.Hidden;
            ImgSuccess.Visibility = Visibility.Hidden;

            switch (type)
            {
                case LOGTYPE.INFO:      ImgInfo.Visibility = Visibility.Visible; break;
                case LOGTYPE.WARNING:   ImgWarning.Visibility = Visibility.Visible; break;
                case LOGTYPE.ERROR:     ImgError.Visibility = Visibility.Visible; break;
                case LOGTYPE.SUCCESS:     ImgSuccess.Visibility = Visibility.Visible; break;
                default: break;
            }
        }

        private void SetProgress(double percent)
        {
            ColumnDef0.Width = new GridLength(percent, GridUnitType.Star);
            ColumnDef1.Width = new GridLength(100 - percent, GridUnitType.Star);
        }
    }
}
