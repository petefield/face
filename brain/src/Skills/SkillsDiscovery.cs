
using Retrievo;
using Retrievo.Models;

using Brain.Skills;
using Brain.Skills.Mcp;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

public interface ISkillsRegister
{
    Task BuildIndex(CancellationToken cancellationToken);
    
    IReadOnlyList<ISkill> Search(string query, int limit = 5);

    IReadOnlyList<ISkill> LocalSkills {get;} 

    bool TryGetSkill(string name, out ISkill? skill);

}

public class SkillsRegister(
    IServiceScopeFactory scopeFactory,
    McpSkillProvider mcpSkills) : ISkillsRegister
{
    private HybridSearchIndex? _index = null;

    private IEnumerable<ISkill> _mcpSkills = [];
    private IEnumerable<ISkill> _localSkills = [];

    public IReadOnlyList<ISkill>  LocalSkills => (IReadOnlyList<ISkill>)_localSkills;

    public IReadOnlyList<ISkill>  RemoteSkills => (IReadOnlyList<ISkill>)_mcpSkills;



    public async Task BuildIndex(CancellationToken cancellationToken)
    {

    using var scope = scopeFactory.CreateScope();

        _localSkills = scope.ServiceProvider.GetServices<ISkill>();
        _mcpSkills = await mcpSkills.GetSkillsAsync(cancellationToken);

        var builder = new HybridSearchIndexBuilder();

        foreach (var skill in _mcpSkills)
        {
            builder.AddDocument(new Document
            {
                Id = skill.Name,
                Body = string.Join(" ", new[]
                {
                    skill.Name.Replace('_', ' '),
                    skill.Description,
                })
            });
        }

        _index = builder.Build();

    }

    public IReadOnlyList<ISkill> Search(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        if (_index is null)
            throw new InvalidOperationException("index is not initialised");

        var searchResults = _index.Search(new HybridQuery
        {
            Text = query,
            TopK = limit
        });

        List<ISkill> skills =[];

        foreach(var searchResult in searchResults.Results)
        {
            skills.Add(_mcpSkills.Single(s => s.Name == searchResult.Id));

        }

        return skills;
    }

    public bool TryGetSkill(string name, out ISkill? skill)
    {

        skill = _mcpSkills.Concat(_localSkills).SingleOrDefault(s => s.Name == name);
        return skill is not null;

    }
}