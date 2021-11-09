﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Xml;

namespace Clear3
{
    class Configuration
    {
        public static Dictionary<string, string> GetSettings(string configFile)
        {
            var settings = new Dictionary<string, string>();

            XmlDocument configDoc = new XmlDocument();

            if (File.Exists(configFile))
            {
                configDoc.Load(configFile);

                XmlNodeList nodeList = configDoc.SelectNodes("config/*");

                for (int i = 0; i < nodeList.Count; i++)
                {
                    string attribName = nodeList[i].Name;
                    string attribValue = nodeList[i].Attributes.GetNamedItem("Value").Value;
                    settings.Add(attribName, attribValue);
                }
            }

            return settings;
        }

        public static (Dictionary<string, string>, Dictionary<string, string>, string, Dictionary<string, string>, Dictionary<string, string>) GetCommonSettings(string clearConfigFile, string project)
        {
            // Initialize configuration settings
            var clearSettings = GetSettings(clearConfigFile);
            var runSettings = GetSettings(clearSettings["Run_Configuration_Filename"]);
            if (project == "") project = runSettings["Project"];
            string projectFolder = Path.Combine(clearSettings["Processing_Foldername"], project);
            var projectSettings = GetSettings(clearSettings, "Project_Configuration_Filename", projectFolder);
            var translationSettings = GetSettings(projectSettings, "Translation_Configuration_Filename", projectFolder);
            var preparationSettings = GetSettings(projectSettings, "Preparation_Configuration_Filename", projectFolder);

            return (clearSettings, runSettings, projectFolder, translationSettings, preparationSettings);
        }

        public static Dictionary<string, string> GetSettings(Dictionary<string, string> configSettings, string configFileAttribute, string configFolder)
        {
            string configFilename = configSettings[configFileAttribute];
            string configFile = Path.Combine(configFolder, configFilename);

            return GetSettings(configFile, configFilename);
        }

        public static Dictionary<string, string> GetSettings(string configFile, string defaultConfigFile)
        {
            var settings = GetSettings(configFile);
            var defaultSettings = GetSettings(defaultConfigFile);

            return MergeSettings(settings, defaultSettings);
        }

        public static Dictionary<string, string> MergeSettings(Dictionary<string, string> settings, Dictionary<string, string> defaultSettings)
        {
            foreach (var entry in defaultSettings)
            {
                if (!settings.ContainsKey(entry.Key))
                {
                    settings.Add(entry.Key, entry.Value);
                }
            }

            return settings;
        }
    }
}