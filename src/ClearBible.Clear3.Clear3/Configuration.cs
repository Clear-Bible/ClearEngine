﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;

namespace Clear3
{
    class Configuration
    {
        public static Dictionary<string, string> GetSettings(string configFile)
        {
            var settings = new Dictionary<string, string>();

            XmlDocument configDoc = new XmlDocument();

            configDoc.Load(configFile);

            XmlNodeList nodeList = configDoc.SelectNodes("config/*");

            for (int i = 0; i < nodeList.Count; i++)
            {
                string attribName = nodeList[i].Name;
                string attribValue = nodeList[i].Attributes.GetNamedItem("Value").Value;
                settings.Add(attribName, attribValue);
            }

            return settings;
        }
    }
}