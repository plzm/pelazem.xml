using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Xsl;
using pelazem.util;

namespace pelazem.xml
{
	public class XmlUtil
	{
		public XDocument GetXDocument(string xml)
		{
			XDocument result = XDocument.Parse(xml, LoadOptions.None);

			return result;
		}

		public void SetNamespace(XDocument xdoc, string namespaceUri)
		{
			var ns = XNamespace.Get(namespaceUri);

			XElement root = xdoc.Root;

			SetNamespace(xdoc.Root, ns);
		}

		/// <summary>
		/// We have to iterate through all elements and set default namespace
		/// Otherwise there will be a blank xmlns= attribute on all child elements
		/// </summary>
		/// <param name="xe"></param>
		/// <param name="ns"></param>
		private void SetNamespace(XElement xe, XNamespace ns)
		{
			if (string.IsNullOrWhiteSpace(xe.Name.NamespaceName))
				xe.Name = ns + xe.Name.LocalName;

			foreach (var cxe in xe.Elements())
				SetNamespace(cxe, ns);
		}

		public XmlSchemaSet GetXmlSchemaSet(string xsdMarkup, string targetNameSpace = "")
		{
			XmlSchemaSet schemaSet = new XmlSchemaSet();

			schemaSet.Add(targetNameSpace, XmlReader.Create(new StringReader(xsdMarkup)));

			return schemaSet;
		}

		public OpResult Validate(XDocument xdoc, string xsdMarkup, string targetNameSpace = "")
		{
			return Validate(xdoc, GetXmlSchemaSet(xsdMarkup, targetNameSpace));
		}

		public OpResult Validate(XDocument xdoc, XmlSchemaSet schemaSet)
		{
			OpResult result = new OpResult();

			List<string> errors = new List<string>();

			xdoc.Validate(schemaSet, (o, e) =>
			{
				errors.Add(e.Message);
			});

			result.Succeeded = (errors.Count == 0);
			result.Output = errors;

			return result;
		}

		public XDocument Transform(XDocument xdoc, string xsltMarkup)
		{
			bool enableDebug = true;

			var xslt = new XslCompiledTransform(enableDebug);

			var sb = new StringBuilder();

			using (var xwriter = XmlWriter.Create(sb))
			{
				xslt.Load(XmlReader.Create(new StringReader(xsltMarkup)));

				xslt.Transform(xdoc.CreateReader(ReaderOptions.None), xwriter);

				xwriter.Close();
				xwriter.Flush();
			}

			return XDocument.Parse(sb.ToString());
		}

		public string GetXml(XElement xe, XmlWriterSettings xmlWriterSettings = null)
		{
			string result = string.Empty;

			if (xmlWriterSettings == null)
				xmlWriterSettings = new XmlWriterSettings() { Async = true, Encoding = Encoding.UTF8, NamespaceHandling = NamespaceHandling.OmitDuplicates, Indent = true, OmitXmlDeclaration = false };

			using (var ms = new MemoryStream())
			{
				using (XmlWriter writer = XmlWriter.Create(ms, xmlWriterSettings))
				{
					//CancellationToken ct = new CancellationToken();  // Implement when .NET Standard 2.1 available

					// await xe.WriteToAsync(writer, ct);  // Implement when .NET Standard 2.1 available
					xe.WriteTo(writer);  // .NET Standard 2.0
				}

				result = xmlWriterSettings.Encoding.GetString(ms.ToArray());
			}

			return result;
		}
	}
}
