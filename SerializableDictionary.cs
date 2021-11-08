using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Common.Classes
{

	//[XmlRoot("dictionary")]
	//forget it, I really don't know how to handle this 
	//[XmlSchemaProvider("GetDictionarySchema")]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
	{
		#region IXmlSerializable Members
		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		/*
		<?xml version="1.0" encoding="utf-8"?>
		<xs:schema targetNamespace="http://tempuri.org/XMLSchema.xsd"
			elementFormDefault="qualified"
			xmlns="http://tempuri.org/XMLSchema.xsd"
			xmlns:mstns="http://tempuri.org/XMLSchema.xsd"
			xmlns:xs="http://www.w3.org/2001/XMLSchema"
		>
			<xs:element name="item" >
				<xs:complexType>
					<xs:sequence>
						<xs:element name="key" type="xs:string"/>
						<xs:element name="value" type="xs:string"/>
					</xs:sequence>
				</xs:complexType>
			</xs:element>
		</xs:schema>
		*/

		// in Swagger the default xml for this object looks like:
		/*
		<dict_menber>
			<additionalProp>string</additionalProp>
		</dict_menber>

		it should look like
		<dict_menber>
			<item>
			  <key>
				<string>string</string>
			  </key>
			  <value>
				<string>string</string>
			  </value>
			</item>
		</dict_menber>

		*/
		// I was hoping this would fix (using the XmlSchemaProvider attrib) it but I get a 406
		public static XmlQualifiedName GetDictionarySchema(XmlSchemaSet schemas)
		{
			XmlSchema schema = new XmlSchema();

			// <xs:element name="stringElementWithAnyAttribute">
			XmlSchemaElement element = new XmlSchemaElement();
			schema.Items.Add(element);
			element.Name = "item";
			XmlSchemaComplexType item = new XmlSchemaComplexType();
			element.SchemaType = item;

			XmlSchemaSequence sequence = new XmlSchemaSequence();
			item.Particle = sequence;

			XmlSchemaElement key_elem = new XmlSchemaElement();
			key_elem.Name = "key";
			key_elem.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
			sequence.Items.Add(key_elem);

			XmlSchemaElement value_elem = new XmlSchemaElement();
			value_elem.Name = "value";
			value_elem.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

			sequence.Items.Add(value_elem);

			schemas.Add(schema);
			return new XmlQualifiedName("dictionary");
		}

		public void ReadXml(System.Xml.XmlReader reader)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

			bool wasEmpty = reader.IsEmptyElement;
			reader.Read();

			if (wasEmpty)
				return;


			while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
			{
				reader.ReadStartElement("item");

				reader.ReadStartElement("key");
				TKey key = (TKey)keySerializer.Deserialize(reader);
				reader.ReadEndElement();

				reader.ReadStartElement("value");
				TValue value = (TValue)valueSerializer.Deserialize(reader);
				reader.ReadEndElement();

				this.Add(key, value);

				reader.ReadEndElement();
				reader.MoveToContent();
			}

			reader.ReadEndElement();
		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

			foreach (TKey key in this.Keys)
			{
				writer.WriteStartElement("item");
				writer.WriteStartElement("key");
				keySerializer.Serialize(writer, key);
				writer.WriteEndElement();

				writer.WriteStartElement("value");
				TValue value = this[key];
				valueSerializer.Serialize(writer, value);
				writer.WriteEndElement();

				writer.WriteEndElement();

			}
		}
		#endregion
	}
}
