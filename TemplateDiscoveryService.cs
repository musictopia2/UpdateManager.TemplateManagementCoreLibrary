namespace UpdateManager.TemplateManagementCoreLibrary;
public class TemplateDiscoveryService(ITemplatesContext context, ITemplateDiscoveryHandler handler)
{
    
    //no need to do any changes to the c# files
    public async Task DiscoverMissingTemplatesAsync()
    {
        //BasicList<NuGetTemplateModel> output = [];
        BasicList<NuGetTemplateModel> exixistingTemplates = await context.GetTemplatesAsync();
        var existingTemplatesNames = new HashSet<string>(exixistingTemplates.Select(p => p.PackageName));
        BasicList<string> folders = await handler.GetTemplateDirectoriesAsync();
        string netVersion = bb1.Configuration!.GetNetVersion();
        string prefixName = bb1.Configuration!.GetPackagePrefixFromConfig();
        foreach (var folder in folders)
        {
            if (ff1.DirectoryExists(folder) == false)
            {
                continue; //does not exist. continue
            }
            BasicList<string> toCheck = await ff1.DirectoryListAsync(folder, SearchOption.AllDirectories);
            toCheck.RemoveAllAndObtain(d =>
            {
                if (d.Contains("Archived", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return handler.CanIncludeProject(d) == false;
            });
            foreach (var dir in toCheck)
            {
                var projectFiles = await ff1.GetSeveralSpecificFilesAsync(dir, "csproj");
                foreach (var projectFile in projectFiles)
                {
                    if (Path.GetFileName(projectFile).Contains(".backup", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Skip this file
                    }
                    string packageName = ff1.FileName(projectFile);

                    // **Skip the extraction if the package already exists**
                    if (existingTemplatesNames.Contains(packageName))
                    {
                        continue; // Skip this package
                    }

                    if (handler.CanIncludeProject(projectFile) == false)
                    {
                        continue;
                    }
                    NuGetTemplateModel model = ExtractTemplateInfo(projectFile, packageName, netVersion, prefixName);
                    //output.Add(model);
                }
            }
        }
        //return output;
    }
    private NuGetTemplateModel ExtractTemplateInfo(string directoryPath, string packageName, string netVersion, string prefixName)
    {
        NuGetTemplateModel model = new();
        //you can customize any other stuff but some things are forced.
        model.PackageName = packageName;
        handler.CustomizePackageModel(model);
        model.PackageName = packageName;
        model.Directory = directoryPath;
        model.FeedType = handler.GetFeedType(directoryPath);
        model.NugetPackagePath = Path.Combine(directoryPath, "nupkg");
        if (model.FeedType == EnumFeedType.Local)
        {
            model.Version = "1.0.0"; //when you do a build, will already increment by 1.
        }
        else
        {
            model.Version = $"{netVersion}.0.0"; //when you do a first build, then will increment by 1.
        }
        if (model.FeedType == EnumFeedType.Public)
        {
            if (handler.NeedsPrefix(model))
            {
                model.PrefixForPackageName = prefixName; //must be forced to this.
            }
        }
        return model;
    }
}