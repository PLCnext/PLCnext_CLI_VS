#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace PlcncliFeatures.PlcNextProject.ProjectConfigWindow
{
    internal abstract class PersistSolutionOptions<T> : IVsPersistSolutionOpts
    {
        protected PersistSolutionOptions()
            : this(default)
        {
        }

        protected PersistSolutionOptions(T data)
        {
            this.Data = data;
        }

        public T Data { get; private set; }

        public int SaveUserOptions(IVsSolutionPersistence pPersistence)
        {
            return VSConstants.S_OK;
        }

        public int LoadUserOptions(IVsSolutionPersistence pPersistence, uint grfLoadOpts)
        {
            return VSConstants.S_OK;
        }

        public int WriteUserOptions(IStream pOptionsStream, string pszKey)
        {
            try
            {
                using (ComStreamWrapper sw = new ComStreamWrapper(pOptionsStream))
                {
                    WriteData(sw, this.Data);
                }
            }
            catch (Exception) { }

            return VSConstants.S_OK;
        }

        public int ReadUserOptions(IStream pOptionsStream, string pszKey)
        {
            try
            {
                using (ComStreamWrapper sw = new ComStreamWrapper(pOptionsStream))
                {
                    Data = ReadData(sw);
                }
            }
            catch (Exception) { }

            return VSConstants.S_OK;
        }

        protected abstract T ReadData(Stream stream);
        
        protected abstract void WriteData(Stream stream, T data);
    }
}
