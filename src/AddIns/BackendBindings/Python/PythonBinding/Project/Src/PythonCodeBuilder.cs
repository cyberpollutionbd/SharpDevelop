// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Matthew Ward" email="mrward@users.sourceforge.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Text;
using ICSharpCode.Scripting;

namespace ICSharpCode.PythonBinding
{
	public class PythonCodeBuilder : ScriptingCodeBuilder
	{
		bool insertedCreateComponentsContainer;
		
		public PythonCodeBuilder()
		{
		}
		
		public PythonCodeBuilder(int initialIndent)
			: base(initialIndent)
		{
		}
		
		/// <summary>
		/// Inserts the following line of code before all the other lines of code:
		/// 
		/// "self._components = System.ComponentModel.Container()"
		/// 
		/// This line will only be inserted once. Multiple calls to this method will only result in one
		/// line of code being inserted.
		/// </summary>
		public void InsertCreateComponentsContainer()
		{
			if (!insertedCreateComponentsContainer) {
				string text = String.Format("self._components = {0}()", typeof(Container).FullName);
				InsertIndentedLine(text);
				insertedCreateComponentsContainer = true;
			}
		}
	}
}
