﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.SharpDevelop.Project
{
	public class ReferenceProjectItem : ProjectItem
	{
		protected ReferenceProjectItem(IProject project, ItemType itemType)
			: base(project, itemType)
		{
		}
		
		public ReferenceProjectItem(IProject project)
			: base(project, ItemType.Reference)
		{
		}
		
		public ReferenceProjectItem(IProject project, string include)
			: base(project, ItemType.Reference, include)
		{
		}
		
		internal ReferenceProjectItem(IProject project, Microsoft.Build.BuildEngine.BuildItem buildItem)
			: base(project, buildItem)
		{
		}
		
		[Browsable(false)]
		public string HintPath {
			get {
				return GetEvaluatedMetadata("HintPath");
			}
			set {
				SetEvaluatedMetadata("HintPath", value);
			}
		}
		
		[DefaultValue("global")]
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Aliases}",
		                   Description = "${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Aliases.Description}")]
		public string Aliases {
			get {
				return GetEvaluatedMetadata("Aliases", "global");
			}
			set {
				SetEvaluatedMetadata("Aliases", value);
			}
		}
		
		[DefaultValue(false)]
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.SpecificVersion}",
		                   Description = "${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.SpecificVersion.Description}")]
		public bool SpecificVersion {
			get {
				return this.Include.Contains(",");
			}
			/* set {
				if (this.SpecificVersion == value)
					return;
				if (value) {
					this.Include = this.AssemblyName.FullName;
				} else {
					this.Include = this.AssemblyName.ShortName;
					Ensure that reference still resolves to the same assembly
				}
			} */
		}
		
		internal const string CopyLocalMetadataName = "Private";
		
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.LocalCopy}",
		                   Description = "${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.LocalCopy.Description}")]
		public bool CopyLocal {
			get {
				return GetEvaluatedMetadata(CopyLocalMetadataName, !IsGacReference);
			}
			set {
				SetEvaluatedMetadata(CopyLocalMetadataName, value);
			}
		}
		
		DomAssemblyName assemblyName;
		
		/// <summary>
		/// Gets the assembly name.
		/// </summary>
		[Browsable(false)]
		public DomAssemblyName AssemblyName {
			get { return assemblyName ?? new DomAssemblyName(Include); }
			internal set { assemblyName = value; }
		}
		
		[ReadOnly(true)]
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Name}",
		                   Description="${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Name.Description}")]
		public string Name {
			get {
				return this.AssemblyName.ShortName;
			}
		}
		
		[ReadOnly(true)]
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Version}",
		                   Description="${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Version.Description}")]
		public Version Version {
			get {
				if (this.AssemblyName.Version == null)
					return null;
				else
					return new Version(this.AssemblyName.Version);
			}
		}
		
		[ReadOnly(true)]
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Culture}",
		                   Description="${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.Culture.Description}")]
		public string Culture {
			get {
				return this.AssemblyName.Culture;
			}
		}
		
		[ReadOnly(true)]
		[LocalizedProperty("${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.PublicKeyToken}",
		                   Description="${res:ICSharpCode.SharpDevelop.Internal.Project.ProjectReference.PublicKeyToken.Description}")]
		public string PublicKeyToken {
			get {
				return this.AssemblyName.PublicKeyToken;
			}
		}
		
		string redist;
		
		/// <summary>
		/// The name of the package in which the assembly is redistributed to the user.
		/// "Microsoft-Windows-CLRCoreComp" = .NET 2.0
		/// "Microsoft-Windows-CLRCoreComp-v3.5" = .NET 3.5
		/// </summary>
		[Browsable(false)]
		public string Redist {
			get { return redist; }
			set { redist = value; }
		}
		
		string fullPath;
		
		[ReadOnly(true)]
		public override string FileName {
			get {
				if (fullPath != null) {
					return fullPath;
				}
				
				if (Project != null) {
					string projectDir = Project.Directory;
					string hintPath = HintPath;
					try {
						if (hintPath != null && hintPath.Length > 0) {
							return FileUtility.NormalizePath(Path.Combine(projectDir, hintPath));
						}
						string name = FileUtility.NormalizePath(Path.Combine(projectDir, Include));
						if (File.Exists(name)) {
							return name;
						}
						if (File.Exists(name + ".dll")) {
							return name + ".dll";
						}
						if (File.Exists(name + ".exe")) {
							return name + ".exe";
						}
					} catch {} // ignore errors when path is invalid
				}
				return Include;
			}
			set {
				fullPath = value;
			}
		}
		
		[Browsable(false)]
		public bool IsGacReference {
			get {
				return !Path.IsPathRooted(this.FileName);
			}
		}
		
		protected override void FilterProperties(PropertyDescriptorCollection globalizedProps)
		{
			base.FilterProperties(globalizedProps);
			PropertyDescriptor copyLocalPD = globalizedProps["CopyLocal"];
			globalizedProps.Remove(copyLocalPD);
			globalizedProps.Add(new ReplaceDefaultValueDescriptor(copyLocalPD, !IsGacReference));
		}
		
		sealed class ReplaceDefaultValueDescriptor : PropertyDescriptor
		{
			PropertyDescriptor baseDescriptor;
			bool newDefaultValue;
			
			public override bool ShouldSerializeValue(object component)
			{
				return (bool)GetValue(component) != newDefaultValue;
			}
			
			public override void ResetValue(object component)
			{
				SetValue(component, newDefaultValue);
			}
			
			public ReplaceDefaultValueDescriptor(PropertyDescriptor baseDescriptor, bool newDefaultValue)
				: base(baseDescriptor)
			{
				this.baseDescriptor = baseDescriptor;
				this.newDefaultValue = newDefaultValue;
			}
			
			public override string DisplayName {
				get { return baseDescriptor.DisplayName; }
			}
			
			public override string Description {
				get { return baseDescriptor.Description; }
			}
			
			public override Type ComponentType {
				get { return baseDescriptor.ComponentType; }
			}
			
			public override bool IsReadOnly {
				get { return baseDescriptor.IsReadOnly; }
			}
			
			public override bool CanResetValue(object component)
			{
				return baseDescriptor.CanResetValue(component);
			}
			
			public override object GetValue(object component)
			{
				return baseDescriptor.GetValue(component);
			}
			
			public override void SetValue(object component, object value)
			{
				baseDescriptor.SetValue(component, value);
			}
			
			public override Type PropertyType {
				get { return baseDescriptor.PropertyType; }
			}
		}
	}
}
