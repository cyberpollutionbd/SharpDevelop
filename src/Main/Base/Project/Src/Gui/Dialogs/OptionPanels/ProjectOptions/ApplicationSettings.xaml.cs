﻿/*
 * Created by SharpDevelop.
 * User: Peter Forstmeier
 * Date: 01.04.2012
 * Time: 17:16
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Project;

namespace ICSharpCode.SharpDevelop.Gui.OptionPanels
{
	/// <summary>
	/// Interaction logic for ApplicationSettingsXaml.xaml
	/// </summary>
	public partial class ApplicationSettings : ProjectOptionPanel
	{
		private const string iconsfilter = "${res:SharpDevelop.FileFilter.Icons}|*.ico|${res:SharpDevelop.FileFilter.AllFiles}|*.*";
		private const string manifestFilter = "${res:Dialog.ProjectOptions.ApplicationSettings.Manifest.ManifestFiles}|*.manifest|${res:SharpDevelop.FileFilter.AllFiles}|*.*";
		private const string win32filter = "Win32 Resource files|*.res|${res:SharpDevelop.FileFilter.AllFiles}|*.*";
		
		public ApplicationSettings()
		{
			InitializeComponent();
			this.DataContext = this;
		}
		
		
		private void Initialize()
		{
			startupObjectComboBox.Items.Clear();
			foreach (IClass c in GetPossibleStartupObjects(base.Project)) {
				startupObjectComboBox.Items.Add(c.FullyQualifiedName);
			}

			this.outputTypeComboBox.SelectedValue = OutputType.Value.ToString();
			
			FillManifestCombo();
			
			// embedding manifests requires the project to target MSBuild 3.5 or higher
			project_MinimumSolutionVersionChanged(null, null);
			// re-evaluate if the project has the minimum version whenever this options page gets visible
			// because the "convert project" button on the compiling tab page might have updated the MSBuild version.
			base.Project.MinimumSolutionVersionChanged += project_MinimumSolutionVersionChanged;
			
			projectFolderTextBox.Text = base.BaseDirectory;
			projectFileTextBox.Text = Path.GetFileName(base.Project.FileName);
			
			//OptionBinding
			RefreshStartupObjectEnabled(this, EventArgs.Empty);
			RefreshOutputNameTextBox(this, null);
			
			ApplicationIconTextBox_TextChanged(this,null);
			this.startupObjectComboBox.SelectionChanged += (s,e) => {IsDirty = true;};
			this.outputTypeComboBox.SelectionChanged +=  OutputTypeComboBox_SelectionChanged;
		}

		
		private List<String> itemsSource;
		
		public List<string> ManifestItems {
			get { return itemsSource; }
			set { itemsSource = value; 
				base.RaisePropertyChanged(() => ManifestItems);
			}
		}
		
		
		void FillManifestCombo()
		{
			itemsSource = new List<string>();
			itemsSource.Add(StringParser.Parse("${res:Dialog.ProjectOptions.ApplicationSettings.Manifest.EmbedDefault}"));
			itemsSource.Add(StringParser.Parse("${res:Dialog.ProjectOptions.ApplicationSettings.Manifest.DoNotEmbedManifest}"));
			foreach (string fileName in Directory.GetFiles(base.BaseDirectory, "*.manifest")) {
				itemsSource.Add(Path.GetFileName(fileName));
			}
			itemsSource.Add(StringParser.Parse("<${res:Global.CreateButtonText}...>"));
			itemsSource.Add(StringParser.Parse("<${res:Global.BrowseText}...>"));
//			applicationManifestComboBox.Items.Clear();
//			applicationManifestComboBox.Items.Add(StringParser.Parse("${res:Dialog.ProjectOptions.ApplicationSettings.Manifest.EmbedDefault}"));
//			applicationManifestComboBox.Items.Add(StringParser.Parse("${res:Dialog.ProjectOptions.ApplicationSettings.Manifest.DoNotEmbedManifest}"));
//			foreach (string fileName in Directory.GetFiles(base.BaseDirectory, "*.manifest")) {
//				applicationManifestComboBox.Items.Add(Path.GetFileName(fileName));
//			}
//			applicationManifestComboBox.Items.Add(StringParser.Parse("<${res:Global.CreateButtonText}...>"));
//			applicationManifestComboBox.Items.Add(StringParser.Parse("<${res:Global.BrowseText}...>"));
//			applicationManifestComboBox.ItemsSource = itemsSource;
			ManifestItems = itemsSource;
		}
		
		
		public ProjectProperty<string> AssemblyName {
			get { return GetProperty("AssemblyName", "", TextBoxEditMode.EditRawProperty); }
		}
		
		
		public ProjectProperty<string> RootNamespace {
			get { return GetProperty("RootNamespace", "", TextBoxEditMode.EditRawProperty); }
		}
		
		
		public ProjectProperty<OutputType> OutputType {
			get {
				return GetProperty("OutputType", ICSharpCode.SharpDevelop.Project.OutputType.Exe); }
		}
		
		
		public ProjectProperty<string> StartupObject {
			get {return GetProperty("StartupObject", "", TextBoxEditMode.EditRawProperty); }	
		}
		
		
		public ProjectProperty<string> ApplicationIcon {
			get { return GetProperty("ApplicationIcon", "", TextBoxEditMode.EditRawProperty); }
		}
		
		
		public ProjectProperty<string> ApplicationManifest {
			get { 
				return GetProperty("ApplicationManifest", "", TextBoxEditMode.EditRawProperty); }
		}
		
		
		public ProjectProperty<bool> NoWin32Manifest {
			get { 
				return GetProperty("NoWin32Manifest",false); }
		}
		
		
		public ProjectProperty<string> Win32Resource {
			get { return GetProperty("Win32Resource", "", TextBoxEditMode.EditRawProperty); }
		}
		
		
		#region overrides
		
		
		protected override void Load(MSBuildBasedProject project, string configuration, string platform)
		{
			base.Load(project, configuration, platform);
			Initialize();
			if (string.IsNullOrEmpty(this.ApplicationManifest.Value)) {
				if (this.NoWin32Manifest.Value) {
					applicationManifestComboBox.SelectedIndex = 1;
				} else {
					applicationManifestComboBox.SelectedIndex = 0;
				}
			}
			else {
				applicationManifestComboBox.Text = this.ApplicationManifest.Value;
			}
			IsDirty = false;
		}
		
		
		protected override bool Save(MSBuildBasedProject project, string configuration, string platform)
		{
			if (applicationManifestComboBox.SelectedIndex == 0) {
				// Embed default manifest
				this.NoWin32Manifest.Value = false;
				this.ApplicationManifest.Value = "";
				this.NoWin32Manifest.Location = ApplicationManifest.Location;
			} else if (applicationManifestComboBox.SelectedIndex == 1) {
				// No manifest
				this.NoWin32Manifest.Value = true;
				this.ApplicationManifest.Value = "";
				this.NoWin32Manifest.Location = ApplicationManifest.Location;
			} else {
				ApplicationManifest.Value = applicationManifestComboBox.Text;
				this.NoWin32Manifest.Value = false;
				this.NoWin32Manifest.Location = ApplicationManifest.Location;
			}
			return base.Save(project, configuration, platform);
		}
		
		#endregion
		
		
		public static IList<IClass> GetPossibleStartupObjects(IProject project)
		{
			List<IClass> results = new List<IClass>();
			IProjectContent pc = ParserService.GetProjectContent(project);
			if (pc != null) {
				foreach(IClass c in pc.Classes) {
					foreach (IMethod m in c.Methods) {
						if (m.IsStatic && m.Name == "Main") {
							results.Add(c);
						}
					}
				}
			}
			return results;
		}
		
		
		void project_MinimumSolutionVersionChanged(object sender, EventArgs e)
		{
			// embedding manifests requires the project to target MSBuild 3.5 or higher
			applicationManifestComboBox.IsEnabled = base.Project.MinimumSolutionVersion >= Solution.SolutionVersionVS2008;
		}
			
		
		#region refresh Outputpath + StartupOptions
		
		void RefreshOutputNameTextBox (object sender, TextChangedEventArgs e)
		{
			if (this.outputTypeComboBox.SelectedValue != null) {
				var enmType = (OutputType) Enum.Parse(typeof(OutputType),this.outputTypeComboBox.SelectedValue.ToString());
				this.outputNameTextBox.Text = this.assemblyNameTextBox.Text + CompilableProject.GetExtension(enmType);
			}
		}
		
		
		void RefreshStartupObjectEnabled(object sender, EventArgs e)
		{
			if (this.outputTypeComboBox.SelectedValue != null) {
				var enmType = (OutputType) Enum.Parse(typeof(OutputType),this.outputTypeComboBox.SelectedValue.ToString());
				bool isLibrary = ICSharpCode.SharpDevelop.Project.OutputType.Library == enmType;
				startupObjectComboBox.IsEnabled = !isLibrary;
			}
		}
		
		
		void OutputTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			RefreshOutputNameTextBox(this,null);
			RefreshStartupObjectEnabled(this,null);
			IsDirty = true;
		}
		
		
		#endregion
	
		#region ApplicationIcon
		
		void ApplicationIconButton_Click(object sender, RoutedEventArgs e)
		{
			string fileName = OptionsHelper.OpenFile(iconsfilter,base.BaseDirectory,
			                                         ApplicationIcon.Value,TextBoxEditMode.EditRawProperty);
			if (!String.IsNullOrEmpty(fileName))
			{
				this.applicationIconTextBox.Text = fileName;
			}
		}
		

		void ApplicationIconTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (base.Project != null) {
				if(FileUtility.IsValidPath(this.applicationIconTextBox.Text))
				{
					string appIconPath = Path.Combine(base.BaseDirectory, this.applicationIconTextBox.Text);
					if (File.Exists(appIconPath)) {
						try {
							MemoryStream memoryStream = new MemoryStream(); 
							using (var stream = new FileStream(appIconPath, FileMode.Open, FileAccess.Read))
							{
								memoryStream.SetLength(stream.Length);
								stream.Read(memoryStream.GetBuffer(), 0, (int)stream.Length);
								
								memoryStream.Flush();
								stream.Close();
								
								Image image = new Image();
								BitmapImage src = new BitmapImage();
								src.BeginInit();
								src.StreamSource = memoryStream;
								src.EndInit();
								image.Source = src;
								image.Stretch = Stretch.Uniform;
								Image = image.Source;
							}

						} catch (OutOfMemoryException) {
							Image = null;
							MessageService.ShowErrorFormatted("${res:Dialog.ProjectOptions.ApplicationSettings.InvalidIconFile}",
							                                  FileUtility.NormalizePath(appIconPath));
						}
					} else {
						Image = null;
					}
				}
			}
		}
		
		private ImageSource image;
		
		public ImageSource Image {
			get { return image; }
			set { image = value;
				base.RaisePropertyChanged(() => Image);			}
		}
		
		/*
		void ApplicationIconTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (base.Project != null) {
				if(FileUtility.IsValidPath(this.applicationIconTextBox.Text))
				{
					string appIconPath = Path.Combine(base.BaseDirectory, this.applicationIconTextBox.Text);
					if (File.Exists(appIconPath)) {
//		http://stackoverflow.com/questions/569561/dynamic-loading-of-images-in-wpf				
						try {
							using (var stream = new FileStream(appIconPath, FileMode.Open, FileAccess.Read))
							{
								Image image = new Image();
								BitmapImage src = new BitmapImage();
								src.BeginInit();
								src.StreamSource = stream;
								src.EndInit();
								
								image.Source = src;
								image.Stretch = Stretch.Uniform;
								this.applicationIconImage.Source = image.Source;
								this.applicationIconImage.Stretch = Stretch.Fill;
							}

						} catch (OutOfMemoryException) {
							this.applicationIconImage.Source = null;
							MessageService.ShowErrorFormatted("${res:Dialog.ProjectOptions.ApplicationSettings.InvalidIconFile}",
							                                  FileUtility.NormalizePath(appIconPath));
						}
					} else {
						this.applicationIconImage.Source = null;
					}
				}
			}
		}
		*/
		#endregion
		
		#region manifest
		
		void ApplicationManifestComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Console.WriteLine("ComboBox_SelectionChanged  {0}",applicationManifestComboBox.Text);
			if (applicationManifestComboBox.SelectedIndex == applicationManifestComboBox.Items.Count - 2) {
				CreateManifest();
			} else if (applicationManifestComboBox.SelectedIndex == applicationManifestComboBox.Items.Count - 1) {
				BrowseForManifest();
			}
		}
		
		
		void BrowseForManifest()
		{
			var fileName = OptionsHelper.OpenFile(manifestFilter,base.BaseDirectory,
			                                      ApplicationManifest.Value,TextBoxEditMode.EditRawProperty);
			if (!String.IsNullOrEmpty(fileName)) {
				applicationManifestComboBox.Text = fileName;
			}
			Console.WriteLine("aaa {0}",applicationManifestComboBox.Text);
			Console.WriteLine("bbb {0}",ApplicationManifest.Value);
		}
		
		void CreateManifest()
		{
			string manifestFile = Path.Combine(base.BaseDirectory, "app.manifest");
			if (!File.Exists(manifestFile)) {
				string defaultManifest;
				using (Stream stream = typeof(ApplicationSettings).Assembly.GetManifestResourceStream("Resources.DefaultManifest.manifest")) {
					if (stream == null)
						throw new ResourceNotFoundException("DefaultManifest.manifest");
					using (StreamReader r = new StreamReader(stream)) {
						defaultManifest = r.ReadToEnd();
					}
				}
				defaultManifest = defaultManifest.Replace("\t", EditorControlService.GlobalOptions.IndentationString);
				File.WriteAllText(manifestFile, defaultManifest, System.Text.Encoding.UTF8);
				FileService.FireFileCreated(manifestFile, false);
			}
			
			if (!base.Project.IsFileInProject(manifestFile)) {
				FileProjectItem newItem = new FileProjectItem(base.Project, ItemType.None);
				newItem.Include = "app.manifest";
				ProjectService.AddProjectItem(base.Project, newItem);
				ProjectBrowserPad.RefreshViewAsync();
			}
		
			FileService.OpenFile(manifestFile);
			
//			this.applicationManifestComboBox.Items.Insert(0,"app.manifest");
			ManifestItems.Insert(0,"app.manifest");
			this.applicationManifestComboBox.SelectedIndex = 0;
		}
		
		#endregion
		
		#region Win32ResourceFile
		
		void Win32ResourceComboButton_Click(object sender, RoutedEventArgs e)
		{
			string fileName = OptionsHelper.OpenFile(win32filter,base.BaseDirectory,
			                                         Win32Resource.Value,TextBoxEditMode.EditRawProperty);
			if (!String.IsNullOrEmpty(fileName))
			{
				this.win32ResourceFileTextBox.Text = fileName;
			}
		}
		
		void ApplicationManifestComboBox_LostFocus(object sender, RoutedEventArgs e)
		{
			Console.WriteLine("ComboBox_LostFocus {0}",applicationManifestComboBox.Text);
		}
		
		
		#endregion	
	}
}