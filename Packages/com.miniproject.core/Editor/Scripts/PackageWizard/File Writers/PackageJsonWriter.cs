﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniProject.Core.Editor.PackageWizard.EditorWindow;
using MiniProject.Core.Editor.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scripts.Core;

namespace MiniProject.Core.Editor.PackageWizard
{
    public class PackageJsonWriter : FileWriterBase
    {
        protected override void TryCreateFile(in string filePath, in string fileContents)
        {
            FileOperations.Create(filePath, fileContents);
        }

        protected override void TryUpdateFile(in string filePath, in string fileContents)
        {
            throw new System.NotImplementedException();
        }

        public void Generate(PackageData packageData, string pathToRuntimeDirectory)
        {
            var emptyArray = new JArray();
            var author = new JObject
            {
                { "name", packageData.AuthorInfo.Name },
                { "email", packageData.AuthorInfo.Email },
                { "url", packageData.AuthorInfo.Url }
            };
            var packageInfo = new JObject
            {
                { "name", packageData.Name },
                { "version", packageData.Version },
                { "displayName", packageData.DisplayName },
                { "description", packageData.Description },
                { "unity", packageData.UnityVersionFormatted },
                { "unityRelease", packageData.UnityRelease },
                { "keywords", emptyArray },
                { "author", author },
                {
                    "dependencies",
                    GetPackageDependenciesAsJObject(packageData.Dependencies, packageData.CustomDependencies)
                }
            };
            var path = Path.Combine(pathToRuntimeDirectory, "package.json");
            TryCreateFile(path, JsonConvert.SerializeObject(packageInfo, Formatting.Indented));
        }

        //Get Package Dependencies
        //================================================================================================================//

        /// <summary>
        /// Combines the list of dependency tags & custom dependencies, in a Package.json "dependencies" friendly format.
        /// { packageName, packageVersion }
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="customDependencies"></param>
        /// <returns></returns>
        private static JObject GetPackageDependenciesAsJObject(
            in IEnumerable<PackageData.DependencyData> dependencies,
            in IEnumerable<PackageData.DependencyData> customDependencies)
        {
            var packageDependencies = new JObject();
            
            if (dependencies == null || dependencies.Any() == false)
                return packageDependencies;

            foreach (var dependencyData in dependencies)
            {
                packageDependencies.Add(dependencyData.Domain, dependencyData.Version);
            }

            foreach (var dependencyData in customDependencies)
            {
                packageDependencies.Add(dependencyData.Domain, dependencyData.Version);
            }

            return packageDependencies;
        }
        
        //================================================================================================================//

    }
}