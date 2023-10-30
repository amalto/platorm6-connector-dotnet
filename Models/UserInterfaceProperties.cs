using System.Collections.Generic;

namespace P6Connector.Models {
	public class UserInterfaceProperties {
		/** Visibility of the entry menu. */
		public bool visible { get; set; }
		/** Icon's name of the entry menu. */
		public string iconName { get; set; }
		/** Position of the entry in the menu. */
		public int weight { get; set; }
		/** Multi-language label for the entry menu (language: 'en-US', 'fr-FR'). */
		public Dictionary< string, string > label { get; set; }
	}
}