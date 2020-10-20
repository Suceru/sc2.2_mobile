using Engine;
using System.IO;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public class VersionConverter114To115 : VersionConverter
	{
		public override string SourceVersion => "1.14";

		public override string TargetVersion => "1.15";

		public override void ConvertProjectXml(XElement projectNode)
		{
			XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
		}

		public override void ConvertWorld(string directoryName)
		{
			string path = Storage.CombinePaths(directoryName, "Project.xml");
			XElement xElement;
			using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read))
			{
				xElement = XmlUtils.LoadXmlFromStream(stream, null, throwOnError: true);
			}
			ConvertProjectXml(xElement);
			using (Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create))
			{
				XmlUtils.SaveXmlToStream(xElement, stream2, null, throwOnError: true);
			}
		}
	}
}
