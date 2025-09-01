using System;
using Newtonsoft.Json;

namespace MainSample.WebAssembly.Types
{
    public class Deployment
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("short_id")]
        public string ShortId { get; set; }

        [JsonProperty("project_id")]
        public string ProjectId { get; set; }

        [JsonProperty("project_name")]
        public string ProjectName { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("created_on")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("latest_stage")]
        public LatestStage LatestStage { get; set; }

        [JsonProperty("deployment_trigger")]
        public DeploymentTrigger DeploymentTrigger { get; set; }

        [JsonProperty("stages")]
        public Stage[] Stages { get; set; }

        [JsonProperty("build_config")]
        public BuildConfig BuildConfig { get; set; }

        [JsonProperty("source")]
        public Source Source { get; set; }

        [JsonProperty("compatibility_date")]
        public string CompatibilityDate { get; set; }

        [JsonProperty("compatibility_flags")]
        public object[] CompatibilityFlags { get; set; }

        [JsonProperty("build_image_major_version")]
        public int BuildImageMajorVersion { get; set; }

        [JsonProperty("usage_model")]
        public string UsageModel { get; set; }

        [JsonProperty("aliases")]
        public string[] Aliases { get; set; }

        [JsonProperty("is_skipped")]
        public bool IsSkipped { get; set; }

        [JsonProperty("production_branch")]
        public string ProductionBranch { get; set; }
        public string AssemblyVersion { get; set; }
        public string MudBlazorVersion { get; set; }
    }

    public class LatestStage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("started_on")]
        public DateTime? StartedOn { get; set; }

        [JsonProperty("ended_on")]
        public DateTime? EndedOn { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class DeploymentTrigger
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("commit_hash")]
        public string CommitHash { get; set; }

        [JsonProperty("commit_message")]
        public string CommitMessage { get; set; }

        [JsonProperty("commit_dirty")]
        public bool CommitDirty { get; set; }
    }

    public class BuildConfig
    {
        [JsonProperty("build_command")]
        public string BuildCommand { get; set; }

        [JsonProperty("destination_dir")]
        public string DestinationDir { get; set; }

        [JsonProperty("build_caching")]
        public object BuildCaching { get; set; }

        [JsonProperty("root_dir")]
        public string RootDir { get; set; }

        [JsonProperty("web_analytics_tag")]
        public object WebAnalyticsTag { get; set; }

        [JsonProperty("web_analytics_token")]
        public object WebAnalyticsToken { get; set; }
    }

    public class Source
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("config")]
        public Config Config { get; set; }
    }

    public class Config
    {
        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("repo_name")]
        public string RepoName { get; set; }

        [JsonProperty("production_branch")]
        public string ProductionBranch { get; set; }

        [JsonProperty("pr_comments_enabled")]
        public bool PrCommentsEnabled { get; set; }
    }

    public class Stage
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("started_on")]
        public DateTime? StartedOn { get; set; }

        [JsonProperty("ended_on")]
        public DateTime? EndedOn { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
