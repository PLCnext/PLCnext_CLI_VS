#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using Microsoft.VisualStudio.PlatformUI;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PlcNextVSExtension.PlcNextProject.ProjectCMakeFlagsEditor
{
    public class CMakeFlagsEditorViewModel : INotifyPropertyChanged
    {
        private readonly string cmakeFlagsFileName = "CMakeFlags.txt";
        private string cmakeFlagsFilePath;
        private readonly string exampleText = "Example:\r\n"
                                            + "-G %22Unix Makefiles%22\r\n"
                                            + "-DCMAKE_MAKE_PROGRAM=%22mymakepath%22";
        private string flags;
        private bool exampleIsShown = false;

        public string Flags
        {
            get => flags; 
            set
            {
                flags = value;
                OnPropertyChanged();
            }
        }
        public bool ExampleIsShown
        {
            get => exampleIsShown; 
            private set
            {
                exampleIsShown = value;
                OnPropertyChanged();
            }
        }
        public CMakeFlagsEditorViewModel(string projectDirectory)
        {
            InitializeText();

            void InitializeText()
            {
                Flags = LoadTextFromFile();

                SetExampleIfEmpty();

                string LoadTextFromFile()
                {
                    cmakeFlagsFilePath = Path.Combine(projectDirectory, cmakeFlagsFileName);
                    if (!File.Exists(cmakeFlagsFilePath))
                    {
                        return string.Empty;
                    };

                    try
                    {
                        return File.ReadAllText(cmakeFlagsFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + ex.StackTrace ?? string.Empty, $"Exception during reading of {cmakeFlagsFileName}");
                        return string.Empty;
                    }
                }
            }
        }

        public void SetExampleIfEmpty()
        {
            if (string.IsNullOrWhiteSpace(Flags))
            {
                Flags = exampleText;
                ExampleIsShown = true;
            }
        }

        public void ClearIfExampleWasShown()
        {
            if (ExampleIsShown)
            {
                Flags = "";
                ExampleIsShown = false;
            }
        }

        #region Commands

        public ICommand CloseButtonClickCommand => new DelegateCommand<Window>(OnCloseButtonClicked);
        public ICommand CancelButtonClickCommand => new DelegateCommand<Window>(OnCancelButtonClicked);

        private void OnCloseButtonClicked(Window window)
        {
            if (!exampleIsShown)
            {
                WriteFile();
            }
            else
            {
                if (File.Exists(cmakeFlagsFilePath)) 
                {
                    DeleteFile();
                }
            }

            window.DialogResult = true;
            window.Close();

            void WriteFile()
            {
                try
                {
                    File.WriteAllText(cmakeFlagsFilePath, Flags);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + ex.StackTrace ?? string.Empty, $"Exception during writing of {cmakeFlagsFileName}");
                }
            }
            void DeleteFile()
            {
                File.Delete(cmakeFlagsFilePath);
            }
        }

        private void OnCancelButtonClicked(Window window)
        {
            window.DialogResult = false;
            window.Close();
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
