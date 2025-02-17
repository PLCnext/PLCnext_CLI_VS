#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PlcncliFeatures.ChangeSDKsProperty;
using PlcncliFeatures.PlcNextProject.Commands;
using PlcncliFeatures.PlcNextProject.OnDocSaveService;
using PlcncliFeatures.PlcNextProject;
using Task = System.Threading.Tasks.Task;

namespace PlcncliFeatures
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(SDKsOptionPage), PlcncliServices.NamingConstants.OptionsCategoryName, "SDKs", 0, 0, false)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PlcncliFeaturesPackage.PackageGuidString)]
    public sealed class PlcncliFeaturesPackage : AsyncPackage
    {
        /// <summary>
        /// PlcncliFeaturesPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b03e7970-c741-422e-ac4b-7b9d4effd140";

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcncliFeaturesPackage"/> class.
        /// </summary>
        public PlcncliFeaturesPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //do not remove next line, otherwise XamlParseException will occur, since Utils might not be loaded
            PlcncliCommonUtils.Constants.DoNothing();
            await new SolutionLoadService().InitializeAsync(this);
            await new UpdateSolutionEventsExtension().InitializeAsync(this);
            await SetTargetsCommand.InitializeAsync(this);
            await CMakeFlagsCommand.InitializeAsync(this);
            await ProjectConfigWindowCommand.InitializeAsync(this);
            await UpdateIncludesCommand.InitializeAsync(this);
            await ImportProjectCommand.InitializeAsync(this);
            await new OnDocSaveService().InitializeAsync(this);
            await GeneratePortComment.PortCommentCommand.InitializeAsync(this);
        }

        #endregion
    }
}
