#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Windows;

namespace PlcNextVSExtension.PlcNextProject
{
    internal class SolutionSaveService
    {
        public static bool SaveAndCloseSolution(Solution solution)
        {            
            ThreadHelper.ThrowIfNotOnUIThread();
            if (solution != null)
            {
                if (!solution.IsOpen)
                    return true;

                MessageBoxResult result = MessageBox.Show("Do you want to save the current solution?",
                                                          "Save current solution",
                                                          MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    solution.Close(true);
                    return true;
                }
                else if (result == MessageBoxResult.No)
                {
                    solution.Close(false);
                    return true;
                }
                else if(result == MessageBoxResult.Cancel)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
