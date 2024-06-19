using Mono.Xml;
using System.Security;

public class XMLParser : SecurityParser
{
    public void Parse(string xml)
    {
        LoadXml(xml);
    }
}
