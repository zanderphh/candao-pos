﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CnSharp.Delivery.VisualStudio.PackingTool {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class PackSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static PackSettings defaultInstance = ((PackSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new PackSettings())));
        
        public static PackSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool QueryFilesVersionFromReleaseServer {
            get {
                return ((bool)(this["QueryFilesVersionFromReleaseServer"]));
            }
            set {
                this["QueryFilesVersionFromReleaseServer"] = value;
            }
        }
    }
}
