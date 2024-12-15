using DotNext.Collections.Generic;
using ERModsMerger.Core;
using ERModsMerger.Core.Utility;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ERModsManager.UCs
{
    /// <summary>
    /// Logique d'interaction pour ModsListUC.xaml
    /// </summary>
    public partial class ModsListUC : UserControl
    {
        public List<ModItemUC> ModsList{ get; set; }

        public ModsListUC()
        {
            InitializeComponent();
            ModsList = new List<ModItemUC>();
            LoadProfiles();
            CreateModList();
        }

        /// <summary>
        /// (Re)Create the mod list based on the config file, if some mods doesn't exist in folders, update mod list config and save
        /// </summary>
        private void CreateModList()
        {
            if (ModsMergerConfig.LoadedConfig != null && ModsListStackPanel != null)
            {
                ModsListStackPanel.Children.Clear();
                ModsList = new List<ModItemUC>();

                var toRemove = new List<ModConfig>();

                foreach(var modConfig in  ModsMergerConfig.LoadedConfig.CurrentProfile.Mods)
                {
                    if (Directory.Exists(modConfig.DirPath))
                    {
                        AddModToList(modConfig);
                    }
                    else
                    {
                        toRemove.Add(modConfig);
                    }
                }

                //delete mods if needed
                foreach (var mod in toRemove)
                    ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Remove(mod);

                //check if this config is already merged
                if(!ModsMergerConfig.LoadedConfig.CurrentProfile.Modified)
                    MergedIndicatorBorder.Visibility = Visibility.Visible;
                else
                    MergedIndicatorBorder.Visibility = Visibility.Hidden;


                //add entries to context menu
                ImportMergedModsFromProfile_Menu.Items.Clear();
                foreach (var profile in ModsMergerConfig.LoadedConfig.Profiles)
                {
                    if(profile.ProfileName != ModsMergerConfig.LoadedConfig.CurrentProfile.ProfileName)
                    {
                        MenuItem menuItem = new MenuItem();
                        menuItem.Header = "Import merged mods from " + profile.ProfileName;
                        menuItem.Tag = profile;
                        menuItem.Click += MenuItemImportFromMergedProfile_Click;
                        ImportMergedModsFromProfile_Menu.Items.Add(menuItem);
                    }
                   
                }
               


                ModsMergerConfig.LoadedConfig.Save();
            }
        }

        private void CheckModsListFilesConflicts()
        {
            
            var dispatcher = new MergeableFilesDispatcher();

            foreach(var modItem in ModsList.FindAll(x=>x.ModEnabled))
                foreach(var filePath in modItem.FileTree.Paths.FindAll(x=>!x.IsDirectory && x.Enabled))
                        dispatcher.AddFile(filePath.Path, filePath.ModRelativePath);
                    

            dispatcher.SearchForConflicts();

            foreach (var modItem in ModsList)
                modItem.FileTree.FindConflictingFileTrees(dispatcher.Conflicts);

        }

        private ModItemUC AddModToList(ModConfig modConfig, bool isNew = false)
        {
            var modItem = new ModItemUC();
            modItem.ModName = modConfig.Name;
            modItem.CurrentPath = modConfig.DirPath;
            modItem.ModEnabled = modConfig.Enabled;
            modItem.ModConfig = modConfig;
            if (modConfig.Note != "")
                modItem.txtAddNote.Text = modConfig.Note;

            modItem.ModDeleted += ModItem_ModDeleted;
            modItem.MouseDown += ModItem_MouseDown;
            modItem.ModEnabledChanged += ModItem_ModEnabledChanged;

            modItem.FileTree.Paths = modConfig.ModFiles;
            modItem.FileTree.Load(modConfig.DirPath, false, modItem.ModConfig);

            ModsListStackPanel.Children.Add(modItem);
            ModsList.Add(modItem);

            if(isNew)
            {
                ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Add(modConfig);
                modItem.ModConfig = ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Last();
            }

            CheckModsListFilesConflicts();

            ModsMergerConfig.LoadedConfig.Save();

            return modItem;
        }


        private void UserControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            }
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    var ext = System.IO.Path.GetExtension(file);
                    var name = System.IO.Path.GetFileName(file);
                    var nameNoExt = System.IO.Path.GetFileNameWithoutExtension(file);

                    var modConfig = FIlesTreeAligner.TryAlignAndCopyToModsToMerge(file, nameNoExt);

                    if (modConfig != null)
                    {
                        ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
                        MergedIndicatorBorder.Visibility = Visibility.Hidden;


                        var modItem = AddModToList(modConfig, true);

                        
                    }
                    else // nope
                    {
                        string mess = "Unable to load this mod, format or folder structure might be too complicated to parse.\n\n" +
                                      "Try to respect this structure:\n\n" +
                                      "📂  MyMod\n" +
                                      "   📄  regulation.bin\n" +
                                      "   📂  event\n" +
                                      "      📄  m10_00_00_00.emevd.dcx\n" +
                                      "      📄  m12_01_00_00.emevd.dcx\n" +
                                      "      📄  ...\n" +
                                      "   📂  parts\n" +
                                      "      📄  am_f_0000.partsbnd.dcx\n" +
                                      "      📄  wp_a_0424_l.partsbnd.dcx\n" +
                                      "      📄  ...\n\n\n" +
                                      "*Don't drop folder / .zip that contain more than 1 mod / version.\n" +
                                      "*Only drop folder or .zip, if you want to drop only one file (eg: regulation.bin), it must be in a named folder (it's important so the app know what's the name of your mod).";

                        MessageBox.Show(mess, "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ModItem_ModEnabledChanged(object? sender, EventArgs e)
        {
            if(this.IsInitialized)
            {
                ModItemUC item = ((ModItemUC)sender);

                var configMod = ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Find(x => x.Name == item.ModName);
                if (configMod != null)
                {
                    if (item.ModEnabled)
                        configMod.Enabled = true;
                    else
                        configMod.Enabled = false;
                }

                ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
                MergedIndicatorBorder.Visibility = Visibility.Hidden;

                CheckModsListFilesConflicts();

                ModsMergerConfig.SaveConfig(Global.ConfigFilePath);
            }
           
        }

        private void ModItem_ModDeleted(object? sender, EventArgs e)
        {
            var Result = MessageBox.Show($"Delete: {((ModItemUC)sender).ModName}?", "", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (Result == MessageBoxResult.Yes)
            {
                ModItemUC item = ((ModItemUC) sender);
                ModsListStackPanel.Children.Remove(item);
                ModsList.Remove(item);

                if(!item.ModConfig.ImportedFromAnotherProfile && Directory.Exists(item.CurrentPath))
                    Directory.Delete(item.CurrentPath, true);

                ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
                MergedIndicatorBorder.Visibility = Visibility.Hidden;

                ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.RemoveAll(x=>x.Name==item.ModName);

                CheckModsListFilesConflicts();

                ModsMergerConfig.SaveConfig(Global.ConfigFilePath);
            }
        }

        ModItemUC? MoveableModItemUC;
        private void ModItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var modItem = sender as ModItemUC;
            this.MouseMove += ModsListUC_MouseMove;
            this.MouseLeave += ModsListUC_MouseLeave;
            this.MouseUp += ModsListUC_MouseUp;

            this.Cursor = Cursors.SizeNS;

            MoveableModItemUC = sender as ModItemUC;
            MoveableModItemUC.Background = new SolidColorBrush(Color.FromArgb(50,255,255,255));
        }

        private void ModsListUC_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.MouseMove -= ModsListUC_MouseMove;
            this.MouseLeave -= ModsListUC_MouseLeave;
            this.MouseUp -= ModsListUC_MouseUp;
            this.Cursor = Cursors.Arrow;
            MoveableModItemUC.Background = new SolidColorBrush(Color.FromArgb(190, 0, 0, 0));
        }

        private void ModsListUC_MouseMove(object sender, MouseEventArgs e)
        {
            var currentModItemRelativeLocation = GetRelativePos(MoveableModItemUC, ModsListStackPanel);
            var mouseRelativeLocation =  e.GetPosition(ModsListStackPanel);

            int currentIndex = ModsListStackPanel.Children.IndexOf(MoveableModItemUC);
            int indexElemAbove = currentIndex -1;
            int indexElemBelow = currentIndex + 1;

            if(indexElemAbove > -1 && mouseRelativeLocation.Y < GetRelativePos(ModsListStackPanel.Children[indexElemAbove], ModsListStackPanel).Y)
            {
                //move up
                int indexInConfig = ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.FindIndex(x => x.Name == MoveableModItemUC.ModName);
                var modItemConfig = ModsMergerConfig.LoadedConfig.CurrentProfile.Mods[indexInConfig];
                ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Remove(modItemConfig);
                ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Insert(indexElemAbove,modItemConfig);
                ModsMergerConfig.SaveConfig(Global.ConfigFilePath);

                ModsListStackPanel.Children.Remove(MoveableModItemUC);
                ModsListStackPanel.Children.Insert(indexElemAbove, MoveableModItemUC);

                ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
                MergedIndicatorBorder.Visibility = Visibility.Hidden;
            }
            else if(indexElemBelow < ModsListStackPanel.Children.Count && mouseRelativeLocation.Y > GetRelativePos(ModsListStackPanel.Children[indexElemBelow], ModsListStackPanel).Y)
            {
                //move down
                int indexInConfig = ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.FindIndex(x => x.Name == MoveableModItemUC.ModName);
                var modItemConfig = ModsMergerConfig.LoadedConfig.CurrentProfile.Mods[indexInConfig];
                ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Remove(modItemConfig);
                ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Insert(indexElemBelow, modItemConfig);
                ModsMergerConfig.SaveConfig(Global.ConfigFilePath);

                ModsListStackPanel.Children.Remove(MoveableModItemUC);
                ModsListStackPanel.Children.Insert(indexElemBelow, MoveableModItemUC);

                ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
                MergedIndicatorBorder.Visibility = Visibility.Hidden;
            }
                    
         }

        private Point GetRelativePos(UIElement elem, UIElement container)
        {
            Point relative =  elem.TranslatePoint(new Point(0, 0), container);
            relative.Y += elem.RenderSize.Height / 2;

            return relative;
        }

        private void ModsListUC_MouseLeave(object sender, MouseEventArgs e)
        {
            this.MouseMove -= ModsListUC_MouseMove;
            this.MouseLeave -= ModsListUC_MouseLeave;
            this.MouseUp -= ModsListUC_MouseUp;
            this.Cursor = Cursors.Arrow;
            MoveableModItemUC.Background = new SolidColorBrush(Color.FromArgb(190, 0, 0, 0));
        }

        private void EnableDisableAll_Click(object sender, RoutedEventArgs e)
        {
            if(EnableDisableAll.IsChecked == true)
                ModsList.ForEach(x=> x.CheckBoxEnableMod.IsChecked = true);
            else
                ModsList.ForEach(x => x.CheckBoxEnableMod.IsChecked = false);

            ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
            MergedIndicatorBorder.Visibility = Visibility.Hidden;
        }

        private void AddProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            ModsMergerConfig.CreateAndLoadProfile();
            ModsMergerConfig.LoadedConfig.CurrentProfile.Modified = true;
            LoadProfiles();
        }

        private void DeleteProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            
            var Result = MessageBox.Show($"Delete: {((ComboBoxItem)ComboProfiles.SelectedItem).Content}?", "", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (Result == MessageBoxResult.Yes)
            {
                ModsMergerConfig.LoadedConfig.CurrentProfile.Delete();
                ComboProfiles.Items.Remove(ComboProfiles.SelectedItem);
                ComboProfiles.SelectedIndex = 0;
            }
        }

        private void ComboProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.IsInitialized)
            {
                
                if(ComboProfiles.SelectedIndex == 0) //main profile
                {
                    DeleteProfileBtn.IsEnabled = false;

                    ModsMergerConfig.LoadedConfig.CurrentProfile = ModsMergerConfig.LoadedConfig.Profiles[ComboProfiles.SelectedIndex];

                    CreateModList();
                }
                else if(ComboProfiles.SelectedIndex != -1)
                {
                    DeleteProfileBtn.IsEnabled = true;
                    ModsMergerConfig.LoadedConfig.CurrentProfile = ModsMergerConfig.LoadedConfig.Profiles[ComboProfiles.SelectedIndex];

                    CreateModList();
                }
                
            }

        }

        private void LoadProfiles()
        {
            ComboProfiles.Items.Clear();
            ImportMergedModsFromProfile_Menu.Items.Clear();
            var profilesToDelete = new List<ProfileConfig>();
            foreach (var profile in ModsMergerConfig.LoadedConfig.Profiles)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = profile.ProfileName;
                ComboProfiles.Items.Add(comboBoxItem);

                //select the last used profile
                if (ModsMergerConfig.LoadedConfig.CurrentProfile.ProfileName == profile.ProfileName)
                    ComboProfiles.SelectedIndex = ComboProfiles.Items.Count - 1;
            }

            profilesToDelete.ForEach(profile => ModsMergerConfig.LoadedConfig.Profiles.Remove(profile));
        }

        private void MenuItemImportFromMergedProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = ((MenuItem)sender).Tag as ProfileConfig;

            var modConfig = new ModConfig("Merged mods from "+profile.ProfileName, profile.MergedModsFolderPath, true);
            modConfig.ImportedFromAnotherProfile = true;

            if (modConfig != null)
            {
                modConfig.Note = $"Merged mods from {profile.ProfileName}:\n\n";

                profile.Mods.ForEach(x => modConfig.Note += $"- {x.Name}\n");

                var modItem = AddModToList(modConfig, true);
            }
        }
    }
}
