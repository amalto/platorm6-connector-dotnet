using System.Collections.Generic;
using Io.Platform6.Imdg.Cm;

namespace P6Connector {

	public class BusConnection {

		public static CommonMessage CreateCommonMessage(
			string senderId, string receiverId, IEnumerable<CommonMessage.Types.Header> headers, IEnumerable<CommonMessage.Types.Attachment> attachments
		) {
			return new CommonMessage {
				Id = Guid.NewGuid().ToString(),
				Destination = Constants.ReceiverIdPrefix + receiverId,
				ReplyTo = senderId,
				Headers = {headers},
				Attachments = {attachments}
			};
		}

		public static CommonMessage.Types.Header CreateHeader(string key, object value) {

			return new CommonMessage.Types.Header{ Key = key, Value = value.ToString() };
		}

		public static string ReadHeaderValue(CommonMessage cm, string key)
		{
			foreach (var header in cm.Headers)
			{
				if (header.Key.Equals(key))
				{
					return header.Value;
				}
			}
			return "";
		}
	}
}