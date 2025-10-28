namespace UpdateManager.TemplateManagementCoreLibrary;
public class TemplateDiscoveryService(ITemplatesContext context, ITemplateDiscoveryHandler handler)
{
    public async Task DiscoverMissingTemplatesAsync()
    {
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
            BasicList<string> directories = await ff1.DirectoryListAsync(folder, SearchOption.TopDirectoryOnly);
            foreach (var directory in directories)
            {
                string packageName = ff1.FileName(directory);
                if (existingTemplatesNames.Contains(packageName))
                {
                    continue; // Skip this package
                }
                if (handler.CanIncludeProject(directory) == false)
                {
                    continue;
                }
                Console.WriteLine($"Adding Tool {packageName}");
                NuGetTemplateModel model = ExtractTemplateInfo(directory, packageName, netVersion, prefixName);
                await context.AddTemplateAsync(model); //needs this too.  before happened later but now must be here.
            }
        }
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