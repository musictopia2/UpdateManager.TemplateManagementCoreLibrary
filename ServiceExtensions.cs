namespace UpdateManager.TemplateManagementCoreLibrary;
public static class ServiceExtensions
{
    //this is not going to be a post build.   this time, has to run a separate app for this.
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterDiscoverUpdateTemplateServices()
        {
            services.AddTransient<ITemplatesContext, FileTemplatesContext>()
                .AddSingleton<INugetTemplatePacker, NugetPacker>()
                .AddSingleton<PrivateTemplateDeploymentProcessor>()
                .AddTransient<TemplateDiscoveryService>();
            ; //for now, just one.
            return services;
        }
        public IServiceCollection RegisterPublicTemplateUploadServices()
        {
            services.AddTransient<ITemplatesContext, FileTemplatesContext>()
                 .AddSingleton<IUploadedTemplatesStorage, FileUploadedTemplatesStorage>()
                .AddSingleton<INugetUploader, PublicNugetUploader>()
                .AddSingleton<NuGetPublicTemplateUploadManager>();
            return services;
        }
    }   
}