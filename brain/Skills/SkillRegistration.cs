using System.Reflection;

namespace Brain.Skills;

public static class SkillRegistration
{
    /// <summary>Registers every concrete <see cref="ISkill"/> in this assembly.</summary>
    public static IServiceCollection AddSkills(this IServiceCollection services)
    {
        var skillTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => typeof(ISkill).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false });

        foreach (var skillType in skillTypes)
        {
            services.AddScoped(typeof(ISkill), skillType);
        }

        return services;
    }
}
