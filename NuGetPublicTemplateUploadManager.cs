namespace UpdateManager.TemplateManagementCoreLibrary;
public class NuGetPublicTemplateUploadManager(ITemplatesContext templatesContext,
    IUploadedTemplatesStorage uploadContext,
    INugetUploader uploader
    )
{
    public async Task UploadTemplatesAsync(CancellationToken cancellationToken = default)
    {
        string feedUrl = bb1.Configuration!.GetStagingPackagePath();
        BasicList<UploadTemplateModel> list = await GetUploadedTemplatesAsync(feedUrl, cancellationToken);
        list = list.ToBasicList(); //try to make a copy here too.
        await UploadTemplatesAsync(list, cancellationToken);
        await CheckTemplatesAsync(list, feedUrl);
    }
    public async Task<bool> HasItemsToProcessAsync()
    {
        var list = await uploadContext.GetAllUploadedTemplatesAsync();
        return list.Count > 0;
    }
    private async Task UploadTemplatesAsync(BasicList<UploadTemplateModel> tools, CancellationToken cancellationToken)
    {
        await tools.ForConditionalItemsAsync(x => x.Uploaded == false, async item =>
        {
            bool rets;
            rets = await uploader.UploadNugetPackageAsync(item.NugetFilePath, cancellationToken);
            if (rets)
            {
                item.Uploaded = true;
                await uploadContext.UpdateUploadedTemplateAsync(item); //update this one since it was not uploaded
                Console.WriteLine("Your package was pushed");
            }
        });
    }
    private async Task CheckTemplatesAsync(BasicList<UploadTemplateModel> tools, string feedUrl)
    {
        await tools.ForConditionalItemsAsync(x => x.Uploaded, async item =>
        {
            Console.WriteLine($"Checking {item.PackageId} to see if its on public nuget");
            bool rets;
            rets = await NuGetPackageChecker.IsPublicPackageAvailableAsync(item.PackageId, item.Version);
            if (rets)
            {
                Console.WriteLine($"Package {item.PackageId} is finally on nuget.  Can now delete");
                await uploadContext.DeleteUploadedTemplateAsync(item.PackageId);
                await LocalNuGetFeedManager.DeletePackageFolderAsync(feedUrl, item.PackageId);
            }
        });
    }
    private async Task<BasicList<UploadTemplateModel>> GetUploadedTemplatesAsync(string feedUrl, CancellationToken cancellationToken)
    {
        var stagingTools = await LocalNuGetFeedManager.GetAllPackagesAsync(feedUrl, cancellationToken);
        var allPackages = await templatesContext.GetTemplatesAsync();
        var uploadedPackages = await uploadContext.GetAllUploadedTemplatesAsync();
        BasicList<UploadTemplateModel> output = [];
        //the moment of truth has to be the staging packages.
        foreach (var name in stagingTools)
        {
            //this means needs to add package.
            var ourPackage = allPackages.SingleOrDefault(x => x.GetPackageID().Equals(name, StringComparison.CurrentCultureIgnoreCase));
            var uploadedPackage = uploadedPackages.SingleOrDefault(x => x.PackageId.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            //i am guessing if you are now temporarily ignoring it, still okay to process because it was the past.
            //same thing for development.
            if (uploadedPackage is null && ourPackage is not null)
            {
                string packageId = ourPackage.GetPackageID();
                uploadedPackage = new()
                {
                    PackageId = packageId,
                    Version = ourPackage.Version,
                    NugetFilePath = LocalNuGetFeedManager.GetNugetFile(feedUrl, packageId, ourPackage.Version)
                };
                output.Add(uploadedPackage);
            }
            else if (uploadedPackage is not null && ourPackage is not null)
            {
                if (uploadedPackage.Version != ourPackage.Version)
                {
                    //this means needs to use the new version regardless of status
                    uploadedPackage.Version = ourPackage.Version;
                    uploadedPackage.NugetFilePath = LocalNuGetFeedManager.GetNugetFile(feedUrl, uploadedPackage.PackageId, ourPackage.Version);
                    uploadedPackage.Uploaded = false; //we have new version now.
                }
                output.Add(uploadedPackage);
            }
        }
        await uploadContext.SaveUpdatedUploadedListAsync(output); //i think.
        return output;
    }
}