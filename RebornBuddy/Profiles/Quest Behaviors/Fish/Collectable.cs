namespace ExBuddy.NeoProfiles
{
    using System;
    using System.Xml.Serialization;

    using Clio.Utilities;

    [XmlRoot(IsNullable = true, Namespace = "")]
    [Clio.XmlEngine.XmlElement("Collectable")]
    [XmlType(AnonymousType = true)]
    [Serializable]
    public class Collectable
    {
        [Clio.XmlEngine.XmlAttribute("Name")]
        public string Name { get; set; }

        [Clio.XmlEngine.XmlAttribute("Value")]
        public int UValue { get; set; }
        public uint Value { get { return Convert.ToUInt32(UValue); } }
    }
}
