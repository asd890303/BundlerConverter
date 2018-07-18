using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bundler
{
    class Program
    {
        //Project path and version.
        static string projectPath = @"";

        //For add thirdparty preload
        static string version = "";

        static void Main(string[] args)
        {
            #region WWW
            List<JObject> jsonContent = GetSourceBundleContent("CAN", "Scripts", "WWW");
            jsonContent.AddRange(GetSourceBundleContent("CAN", "Css", "WWW"));
            jsonContent.AddRange(GetSourceBundleContent("USA", "Scripts", "WWW"));
            jsonContent.AddRange(GetSourceBundleContent("USA", "Css", "WWW"));
            #endregion

            CreateBundleConfig(jsonContent, "WWW");

            #region SSL
            projectPath = projectPath.Replace(@"_WWW", "_SSL");

            List<JObject> jsonContent_SSL = GetSourceBundleContent("CAN", "Scripts", "SSL");
            jsonContent_SSL.AddRange(GetSourceBundleContent("CAN", "Css", "SSL"));
            jsonContent_SSL.AddRange(GetSourceBundleContent("USA", "Scripts", "SSL"));
            jsonContent_SSL.AddRange(GetSourceBundleContent("USA", "Css", "SSL"));
            #endregion

            CreateBundleConfig(jsonContent_SSL, "SSL");

            Console.ReadLine();
        }

        static public void Write(string msg)
        {
            Console.Out.WriteLine(msg);
        }

        static public List<JObject> GetSourceBundleContent(string country, string type, string site)
        {
            try
            {
               
                string scriptPath = @"Content/" + country + @"/EN/" + type;
                string fullPath = projectPath + scriptPath;

                List<string> sourceBundlesList = Directory.GetFiles(fullPath, "*.bundle").Select(Path.GetFileName).ToList();

                //List<string> sourceSelfBundlesList = Directory.GetFiles(fullPath, "*.min.js").Select(s =>
                //Regex.Replace(Path.GetFileNameWithoutExtension(s), @".+\.v\d+.[ws]\.\d{5,}\.min\.js"))
                //.Except(sourceBundlesList.Select(s => Path.GetFileNameWithoutExtension(s).Replace(".js", ""))).ToList();

                //Total Count
                Write("Start " + country + " " + type + ":" + sourceBundlesList.Count());

                List<JObject> jsonContent = new List<JObject>();
                foreach (string bundleName in sourceBundlesList)
                {
                    //Json memeber
                    string outputFileName = "";
                    List<string> inputFiles = new List<string>();

                    //Read origin *.bundle
                    XmlDocument source = new XmlDocument();
                    source.Load(fullPath + @"\" + bundleName);

                    //Set outputFileName
                    outputFileName = scriptPath + "/" + source.SelectSingleNode(@"//bundle").Attributes["output"].Value;

                    //Find input files
                    foreach (XmlNode f in source.SelectNodes(@"//bundle//file"))
                    {
                        //Remove '/' path.
                        string innerXml = f.InnerXml.ToString().Remove(0, 1);

                        //If contain thirdParty preload.js , remove all js and add new reference.
                        //Set inputFiles
                        if (type == "Scripts" && (
                            innerXml.Contains("loadCSS") || innerXml.Contains("onloadCSS") || innerXml.Contains("cssrelpreload")))
                        {
                            if (!inputFiles.Contains(@"Content/" + country + "/EN/Scripts/ThirdParty/preload.v1.w." + version + ".js"))
                                inputFiles.Add(@"Content/" + country + "/EN/Scripts/ThirdParty/preload.v1.w." + version + ".js");
                        }
                        else
                        {
                            //TODO Version convert
                            inputFiles.Add(innerXml);
                        }
                    }

                    jsonContent.Add(new JObject(
                        new JProperty("outputFileName", outputFileName),
                        new JProperty("inputFiles", inputFiles))
                    );
                }

                //Only WWW neeeded.
                if (type == "Scripts" && site == "WWW")
                {
                    //Add thirdparty Preload.js.
                    jsonContent.Add(new JObject(
                           new JProperty("outputFileName", @"Content/" + country + "/EN/Scripts/ThirdParty/preload.v1.w." + version + ".js"),
                           new JProperty("inputFiles", new List<string>() {
                           "Content/"+country+"/EN/Scripts/ThirdParty/loadCSS.v1.w.14253.js",
                           "Content/"+country+"/EN/Scripts/ThirdParty/onloadCSS.v1.w.14253.js",
                           "Content/"+country+"/EN/Scripts/ThirdParty/cssrelpreload.v1.w.14253.js"
                           }))
                       );
                }
                return jsonContent;

            }
            catch (Exception ex)
            {
                System.Console.Out.WriteLine(ex.ToString());
                Write(ex.ToString());
                Console.ReadLine();
                return null;
            }

        }

        static public void CreateBundleConfig(List<JObject> jsonContent, string site)
        {
            string targrt = projectPath + @"/bundleconfig.json";
            if (!(File.Exists(targrt)))
            {
                StreamWriter file = File.CreateText(targrt);
                file.Close();
            }
            File.WriteAllText(targrt, string.Empty);
            File.AppendAllText(targrt, JsonConvert.SerializeObject(jsonContent.ToArray(), Newtonsoft.Json.Formatting.Indented));

            Write("===============================");
            Write(site + "- Done!");
            Write("===============================");
        }
    }
}
