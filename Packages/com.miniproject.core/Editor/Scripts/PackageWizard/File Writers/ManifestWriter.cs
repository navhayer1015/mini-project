//#define DEBUG
#undef DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniProject.Core.Editor.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MiniProject.Core.Editor.PackageWizard
{
    public class ManifestWriter : FileWriterBase
    {
        private const string PACKAGES_KEY = "Packages";
        private const string DEPENDENCIES_KEY = "dependencies";
        private const string IGNORE_PROJECT_SETTINGS = "projectsettings";

        private const string CLIMB_TO_PROJECTS = @"..\..\..\";
        
        private const string MANIFEST_FILENAME = "manifest.json";
        private const string PACKAGESLOCK_FILENAME = "packages-lock.json";

        //ManifestWriter Functions
        //================================================================================================================//

        /// <summary>
        /// This function will include the package, with a generated path in the respective platform & unity versions,
        /// to manifest.json & packages-lock.json. If the entry already exists, then the it will only overwrite the
        /// directory information.
        /// </summary>
        /// <param name="packageName">com.miniproject.EXAMPLE</param>
        /// <param name="supportedUnityVersions">No need to include the subversions of Unity, just the major will suffice. Examples: "2021", "2022"</param>
        /// <param name="supportedPlatforms">All platforms will need to start with "miniproject-". Examples: "miniproject-ios","miniproject-webgl"</param>
        public void UpdateManifestFiles(in string packageName, string[] supportedUnityVersions, string[] supportedPlatforms)
        {
            bool DirectoryContainsItem(in DirectoryInfo directoryInfo, in string[] searchFor)
            {
                var name = directoryInfo.FullName.ToLower();
                for (var i = 0; i < searchFor.Length; i++)
                {
                    if (name.Contains(searchFor[i]))
                        return true;
                }

                return false;
            }
            //----------------------------------------------------------//

            for (var i = 0; i < supportedPlatforms.Length; i++)
            {
                if (supportedPlatforms[i].Contains("miniproject-") == false)
                    throw new Exception("All supported platforms must be preceded by miniproject- an example miniproject-android");
            }

            var packagePath = $"file:../../../../../Packages/{packageName}";

            //Go to Projects folder
            // /Projects/
            var path = Path.Combine(Application.dataPath, CLIMB_TO_PROJECTS);
            var directory = new DirectoryInfo(path);


            var projectDirectories = directory
                .EnumerateDirectories(PACKAGES_KEY, SearchOption.AllDirectories)
                .Where(x => x.FullName.ToLower().Contains(IGNORE_PROJECT_SETTINGS) == false)
                .Where(x => DirectoryContainsItem(x, supportedUnityVersions))
                .Where(x => DirectoryContainsItem(x, supportedPlatforms))
                .ToArray();

#if DEBUG
            Debug.Log("Found Packages:\n" + string.Join('\n', projectDirectories.Select(x => x.FullName)));
#endif


            for (var i = 0; i < projectDirectories.Length; i++)
            {
                var files = projectDirectories[i].GetFiles("*.json");
#if DEBUG
                Debug.Log(
                    $"{projectDirectories[i].FullName} [{files.Length}] => {string.Join(", ", files.Select(x => x.Name))}");    
#endif

                for (var ii = 0; ii < files.Length; ii++)
                {
                    switch (files[ii].Name)
                    {
                        case MANIFEST_FILENAME:
                            TryAddKeyToManifest(files[ii], packageName, packagePath);
                            continue;
                        case PACKAGESLOCK_FILENAME:
                            TryAddKeyToPackagesLock(files[ii], packageName, packagePath);
                            continue;
                        default:
                            continue;
                    }
                }
            }
        }

        //FileWriterBase Functions
        //================================================================================================================//

        protected override void TryCreateFile(in string filePath, in string fileContents) =>
            throw new NotImplementedException("This file should already exist, you should not try creating it");

        protected override void TryUpdateFile(in string filePath, in string fileContents)
        {
            if (File.Exists(filePath) == false)
                throw new ArgumentException($"No File found matching path: {filePath}");
            
            File.WriteAllText(filePath, fileContents);
        }
        
        //Update Manifest File Functions
        //================================================================================================================//
        private void TryAddKeyToManifest(in FileInfo fileInfo, in string key, in string value)
        {
            var fileText = File.ReadAllText(fileInfo.FullName);
            var data = JObject.Parse(fileText);

            if (data.ContainsKey(DEPENDENCIES_KEY) == false)
                throw new Exception();

            var dependencies = (JObject)data[DEPENDENCIES_KEY];

            if (dependencies.HasValues == false)
                throw new Exception();

            if (dependencies.ContainsKey(key))
            {
                dependencies[key] = value;

                fileText = JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            //If we need to add a new key, its a little more involved.
            else
            {
                //If we want to add a new value, but have it sit at the [0] position, we need to create a new list
                // with the order that we are expecting.
                var temp = dependencies.Values<JProperty>().ToList();
                temp.Insert(0, new JProperty(key, value));

                var newDependencies = new JObject();
                foreach (var dependency in temp)
                {
                    newDependencies.Add(dependency);
                }

                var outObject = new JObject { { DEPENDENCIES_KEY, newDependencies } };

                fileText = JsonConvert.SerializeObject(outObject, Formatting.Indented);
            }
            
#if DEBUG
                Debug.Log(fileText);
#endif
            TryUpdateFile(fileInfo.FullName, fileText);
        }

        private void TryAddKeyToPackagesLock(in FileInfo fileInfo, in string key, in string value)
        {
            var fileText = File.ReadAllText(fileInfo.FullName);
            var data = JObject.Parse(fileText);

            if (data.ContainsKey(DEPENDENCIES_KEY) == false)
                throw new KeyNotFoundException($"[{DEPENDENCIES_KEY}] not found in {fileInfo.Name}");

            var dependencies = (JObject)data[DEPENDENCIES_KEY];

            var newValue = JObject.FromObject(
                new
                {
                    version = value,
                    depth = 0,
                    source = "local",
                    dependencies = new JObject()
                });

            if (dependencies.ContainsKey(key))
            {
                dependencies[key] = newValue;

                fileText = JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            //If we need to add a new key, its a little more involved.
            else
            {
                //If we want to add a new value, but have it sit at the [0] position, we need to create a new list
                // with the order that we are expecting.
                var temp = dependencies.Values<JProperty>().ToList();
                temp.Insert(0, new JProperty(key, newValue));

                var newDependencies = new JObject();
                foreach (var dependency in temp)
                {
                    newDependencies.Add(dependency);
                }

                var outObject = new JObject { { DEPENDENCIES_KEY, newDependencies } };

                fileText = JsonConvert.SerializeObject(outObject, Formatting.Indented);

            }
            
#if DEBUG
                Debug.Log(fileText);
#endif
            TryUpdateFile(fileInfo.FullName, fileText);
        }
        
        //================================================================================================================//

    }
}