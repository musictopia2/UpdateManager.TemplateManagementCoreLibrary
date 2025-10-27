namespace UpdateManager.TemplateManagementCoreLibrary;
public static class ServiceExtensions
{
    //this is not going to be a post build.   this time, has to run a separate app for this.
    public static IServiceCollection RegisterUpdateTemplateServices(this IServiceCollection services)
    {
        services.AddTransient<ITemplatesContext, FileTemplatesContext>()
            .AddSingleton<INugetTemplatePacker, NugetPacker>()
            .AddSingleton<PrivateTemplateDeploymentProcessor>()
            ; //for now, just one.
        return services;
    }
    public static IServiceCollection RegisterTemplateDiscoveryServices(this IServiceCollection services)
    {
        services.AddTransient<ITemplatesContext, FileTemplatesContext>()
            .AddTransient<TemplateDiscoveryService>();
        return services;
    }
    public static IServiceCollection RegisterPublicTemplateUploadServices(this IServiceCollection services)
    {
        services.AddSingleton<IToolsContext, FileToolsContext>()
            .AddSingleton<IUploadedToolsStorage, FileUploadedToolsStorage>()
            .AddSingleton<INugetUploader, PublicNugetUploader>()
            .AddSingleton<NuGetPublicTemplateUploadManager>();
        return services;
    }
}