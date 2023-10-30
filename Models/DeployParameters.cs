namespace P6Connector.Models {
	public class DeployParameters {
		/** Service's identifier. */
		public string Id;
		/** Path of the endpoint's URL to get the service's client script. */
		public string Path;
		/** Base path of the endpoint's URL to get the service's client script.  */
		public string BasePath;
		/** Semver versions of all the service's components. */
		public Versions Versions;
		/** Properties of the service's entry menu. */
		public UserInterfaceProperties Ui;
	}
}