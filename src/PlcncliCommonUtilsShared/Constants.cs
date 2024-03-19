#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion


namespace PlcncliCommonUtils
{
    public static class Constants
    {
        public static void DoNothing()
        {

        }
        public const string PLCnCLIProjectType = "PLCnCLIProjectType";
        public const string PLCnCLIProjectVersion = "PLCnCLIProjectVersion";
        public const int PLCnCLIExtensionVersion = 2;

        // PLCnextCommonPropertiesRule
        public const string PLCnextIncludesKey = "PLCnCLIIncludes";
        public const string PLCnextMacrosKey = "PLCnCLIMacros";
        public const string PLCnextRuleName = "PLCnextCommonProperties";

        //VS
        public const string VCppIncludesRuleName = "ConfigurationDirectories";
        public const string VCppIncludesKey = "IncludePath";
        public const string VCPreprocessorsKey = "PreprocessorDefinitions";
        public const string CLRuleName = "CL";

        //PLCnCLI
        public const string Command_check_project = "check-project";
        public const string Command_generate_code = "generate code";
        public const string Command_get_compiler_specifications = "get compiler-specifications";
        public const string Command_get_project_information = "get project-information";
        public const string Command_get_targets = "get targets";
        public const string Command_new_acfcomponent = "new acfcomponent";
        public const string Command_new_acfproject = "new acfproject";
        public const string Command_new_component = "new component";
        public const string Command_new_consumablelibrary = "new consumablelibrary";
        public const string Command_new_plmproject = "new project";
        public const string Command_new_program = "new program";
        public const string Command_set_target = "set target";
        public const string ItemType_component = "component";
        public const string ItemType_program = "program";
        public const string Option_generate_code_project = "-p";
        public const string Option_get_compiler_specifications_project = "-p";
        public const string Option_get_project_information_no_include_detection = "-n";
        public const string Option_get_project_information_project = "-p";
        public const string Option_get_project_information_buildtype = "-b";
        public const string Option_new_component_name = "-n";
        public const string Option_new_component_namespace = "-s";
        public const string Option_new_component_project = "-p";
        public const string Option_new_program_component = "-c";
        public const string Option_new_program_name = "-n";
        public const string Option_new_program_namespace = "-s";
        public const string Option_new_program_project = "-p";
        public const string Option_new_project_componentName = "-c";
        public const string Option_new_project_output = "-o";
        public const string Option_new_project_programName = "-p";
        public const string Option_new_project_projectNamespace = "-n";
        public const string Option_set_target_add = "-a";
        public const string Option_set_target_name = "-n";
        public const string Option_set_target_project = "-p";
        public const string Option_set_target_remove = "-r";
        public const string Option_set_target_version = "-v";
        public const string Option_check_project_project = "-p";
        public const string ProjectType_ACF = "acfproject";
        public const string ProjectType_ConsumableLibrary = "consumablelibrary";
        public const string ProjectType_PLM = "project";
        public const string ProjectType_SN = "snproject";
    }
}
