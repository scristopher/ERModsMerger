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
    /// Logique d'interaction pour ModItemUC.xaml
    /// </summary>
    public partial class ModItemUC : UserControl
    {
        public ModConfig ModConfig { get; set; }

        string _modName = "I am the default pretty long mod name";
        public string ModName { get { return _modName; } set { _modName = value;} }

        public string CurrentPath { get; set; }


        bool _modEnabled = true;
        public bool ModEnabled { get { return _modEnabled; } set { _modEnabled = value; } }

        public event EventHandler? ModDeleted;

        public event EventHandler? ModEnabledChanged;

        public ModItemUC()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void DeleteModFromListBtn_Click(object sender, RoutedEventArgs e)
        {
            if(ModDeleted != null)
            {
                ModDeleted(this, EventArgs.Empty);
            }
        }

        private void CheckBoxEnableMod_Changed(object sender, RoutedEventArgs e)
        {
            if (ModEnabledChanged != null)
            {
                ModEnabledChanged(this, e);
            }
        }

        private void BtnDropDownExpand_Click(object sender, RoutedEventArgs e)
        {
            if (this.Height == 50)
                this.Height = double.NaN; //set to auto
            else
                this.Height = 50;
        }

        private void txtAddNote_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtAddNote.Text == "")
                txtAddNote.Text = "Add a note...";
            else
            {
                if(ModConfig != null)
                {
                    ModConfig.Note = txtAddNote.Text;
                    ModsMergerConfig.LoadedConfig.Save();
                }
            }
        }

        private void txtAddNote_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtAddNote.Text == "Add a note...")
                txtAddNote.Text = "";
        }
    }

}
