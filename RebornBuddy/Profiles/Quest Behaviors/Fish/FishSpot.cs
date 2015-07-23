namespace ExBuddy.NeoProfiles
{
    using System;
    using System.ComponentModel;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Navigation;

    [XmlElement("FishSpot")]
    public class FishSpot
    {
        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }

        [XmlAttribute("Heading")]
        public float Heading { get; set; }

        public FishSpot()
        {
            XYZ = Vector3.Zero;
            Heading = 0f;
        }

        public FishSpot(string xyz, float heading)
        {
            XYZ = new Vector3(xyz);
            Heading = heading;
        }

        public FishSpot(Vector3 xyz, float heading)
        {
            XYZ = xyz;
            Heading = heading;
        }

        public override string ToString()
        {
            var ret = "[FishSpot] Location: " + XYZ + ", Heading: " + Heading;

            return ret;
        }
    }
}