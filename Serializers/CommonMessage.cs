using Io.Platform6.Imdg.Cm;
using Google.Protobuf;
using Hazelcast.IO.Serialization;
using Hazelcast.IO;

namespace P6Connector.Serializers {

	public class MessageSerializer: IStreamSerializer<CommonMessage> {
		public int GetTypeId() {
			return 10;
		}

		public void Write(IObjectDataOutput output, CommonMessage message) {
			output.WriteByteArray(message.ToByteArray());
		}

		public CommonMessage Read(IObjectDataInput input) {
			return CommonMessage.Parser.ParseFrom(input.ReadByteArray());
		}

		public void Destroy() {}
	}
}
