namespace P6Connector
{
	public static class Constants {
		private const string IdSeparator = ".";
		private const string Platform6 = "platform6";
		public const string Platform6AppKey = Platform6 + IdSeparator;
		public const string RequestPrefix = Platform6AppKey + "request" + IdSeparator;
        public const string ResponsePrefix = Platform6AppKey + "response" + IdSeparator;
        public const string ReceiverIdPrefix = "cmb" + IdSeparator;
		public const string SenderIdPrefix = "tmp" + IdSeparator;
		public const string ActionDeploy = "deploy";
		public const string ActionUnDeploy = "undeploy";
		public const string ServiceManagerId = Platform6AppKey + "manager";
	}
}