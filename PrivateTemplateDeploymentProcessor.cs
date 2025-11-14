namespace UpdateManager.TemplateManagementCoreLibrary;
public class PrivateTemplateDeploymentProcessor(ITemplatesContext context, INugetTemplatePacker packer)
{
    public async Task ProcessTemplatesToPrivateFeedAsync()
    {
        var list = await context.GetTemplatesAsync();
        foreach (var template in list)
        {
            await ProcessTemplateAsync(template);
        }
    }
    //decided to make it private so year end processes can do this.
    public async Task ProcessTemplateAsync(NuGetTemplateModel template, bool log = true)
    {
        if (ff1.DirectoryExists(template.Directory!) == false)
        {
            throw new CustomBasicException($"Directory {template.Directory} does not exist");
        }
        BasicList<string> others =
            [
            ".vs",
            "bin",
            "obj",
            ];
        foreach (var other in others)
        {
            string deletes = Path.Combine(template.Directory, other);
            if (ff1.DirectoryExists(deletes))
            {
                await ff1.DeleteFolderAsync(deletes);
            }
        }
        string finals = Path.Combine(template.Directory, $"{template.PackageName}.nuspec");
        if (ff1.FileExists(finals))
        {
            await ff1.DeleteFileAsync(finals);
        }
        finals = Path.Combine(template.Directory, $"{template.PackageName}.sln");
        string possibleSingle = Path.Combine(template.Directory, $"{template.PackageName}.csproj");
        if (ff1.FileExists(possibleSingle))
        {
            //this means there is a csproj file in this folde.
            if (ff1.FileExists(finals))
            {
                await ff1.DeleteFileAsync(finals); //can delete the solution folder because only single project.
            }
        }
        else
        {
            DeleteSubVsFolders(template.Directory, others);
        }
        if (NeedsUpdate(template.Directory!, template.LastUpdated) == false)
        {
            return; //no need to update.
        }
        if (log)
        {
            Console.WriteLine($"Processing Template Updates For {template.PackageName} on {DateTime.Now}");
        }
        await UpdatePackageVersionAsync(template);
        if (log)
        {
            Console.WriteLine("Packging nuget package");
        }
        await CreateAndUploadNuGetPackageAsync(template);
        if (log)
        {
            Console.WriteLine("Record latest update");
        }
        await UpdateCompletedAsync(template);
    }
    private static void DeleteSubVsFolders(string directory, BasicList<string> folders)
    {
        if (folders.Count == 0)
        {
            throw new CustomBasicException("Cannot have empty folder list");
        }

        if (Directory.Exists(directory) == false)
        {
            throw new CustomBasicException($"Directory {directory} does not exist");
        }
        try
        {
            // Get immediate subdirectories only (not recursive)
            var subDirs = Directory.GetDirectories(directory);

            foreach (var subDir in subDirs)
            {
                foreach (var folderName in folders)
                {
                    // Build the full path for the folder to delete
                    var targetFolder = Path.Combine(subDir, folderName);

                    if (Directory.Exists(targetFolder))
                    {
                        try
                        {
                            Directory.Delete(targetFolder, true); // true = recursive delete
                            //Console.WriteLine($"Deleted folder: {targetFolder}");
                        }
                        catch
                        {
                            throw new CustomBasicException($"Failed to delete {targetFolder}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new CustomBasicException($"Error processing directory {directory}:  {ex.Message}");
        }
    }
    private bool NeedsUpdate(string directory, DateTime? lastUpdated)
    {
        if (lastUpdated is null)
        {
            return true;
        }

        DateTime actuals = LastUpdated(directory!);
        if (actuals >= lastUpdated)
        {
            return true;
        }
        return false;
    }
    private static DateTime LastUpdated(string directory)
    {
        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToBasicList();

        if (files.Count == 0)
        {
            throw new CustomBasicException("There must be at least one file for a template");
        }

        // Find the latest last write time (local time)
        DateTime lastWrite = DateTime.MinValue;
        foreach (var file in files)
        {
            DateTime writeTime = File.GetLastWriteTime(file); // local time, not UTC
            if (writeTime > lastWrite)
            {
                lastWrite = writeTime;
            }
        }

        return lastWrite;
    }

    private async Task CreateAndUploadNuGetPackageAsync(NuGetTemplateModel template)
    {
        bool created = await packer.CreateNugetTemplatePackageAsync(template);
        if (created == false)
        {
            throw new CustomBasicException("Failed to create nuget package.");
        }
        if (!Directory.Exists(template.NugetPackagePath))
        {
            throw new CustomBasicException($"NuGet package path does not exist: {template.NugetPackagePath}");
        }
        var files = ff1.FileList(template.NugetPackagePath);
        files.RemoveAllOnly(x => !x.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase));
        if (files.Count != 1)
        {
            throw new CustomBasicException($"Error: Expected 1 .nupkg file, but found {files.Count}.");
        }
        string nugetFile = ff1.FullFile(files.Single());
        bool uploaded = await LocalNuGetFeedUploader.UploadPrivateNugetPackageAsync(GetFeedToUse(template), template.NugetPackagePath, nugetFile);
        if (!uploaded)
        {
            throw new CustomBasicException("Failed to publish nuget package to private feed");
        }
        await NuGetTemplateManager.InstallTemplateAsync(template.GetPackageID(), template.Version);
    }
    private async Task UpdatePackageVersionAsync(NuGetTemplateModel template)
    {
        string version = template.Version.IncrementMinorVersion();
        await NuGetTemplateManager.UninstallTemplateAsync(template.GetPackageID());
        //await NuGetToolManager.UninstallToolAsync(template.GetPackageID());
        await context.UpdateTemplateVersionAsync(template.PackageName, version);
    }
    private static string GetFeedToUse(NuGetTemplateModel template)
    {
        string stagingPath = bb1.Configuration!.GetStagingPackagePath();
        string developmentPath = bb1.Configuration!.GetDevelopmentPackagePath();
        string localPath = bb1.Configuration!.GetPrivatePackagePath();
        if (template.Development)
        {
            return developmentPath;
        }
        if (template.FeedType == EnumFeedType.Local)
        {
            return localPath;
        }
        return stagingPath;
    }

    private async Task UpdateCompletedAsync(NuGetTemplateModel templateModel)
    {
        await context.UpdateTemplateStampAsync(templateModel.PackageName);
    }

}